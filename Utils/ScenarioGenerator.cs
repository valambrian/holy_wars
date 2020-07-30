using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ScenarioGenerator
{
    // the land fraction was selected empirically
    private const double LAND_FRACTION = 0.6;

    // cell at [x, y] belongs to province cellMap[x, y]
    // zero or less stands for water and will be discared
    private int[,] _cellMap;

    // list of (x, y) coordinates of province centers
    // in terms of cellMap
    private List<IntIntPair> _centers;

    // list of neighboring cells for every province
    private List<List<IntIntPair>> _neighbors;

    // distances between province centers
    // (measured in cells)
    private int[,] _distancesInCells;

    // higher-level map for the provinces
    // it's an attempt to make province centers distribution
    // more or less uniform
    private int[,] _provinceMap;

    // list of (x, y) coordinates on the province map
    // of the neighboring provinces
    private List<IntIntPair> _quadrants;

    // distances between provinces
    // (measured in provinces)
    private int[,] _distancesInProvinces;

    List<String> _humanProvinceNames;

    public Game CreateScenario(GameData data)
    {
        data.turn = 1;

        int height = 42;
        int width = 48;
        // all cells within this radius should ideally belong to the same province
        int radius = 3;

        int cellsPerProvince = radius * (radius + 1) * 3 + 1;

        // the number of provinces is calculated with +/- 5% variance
        int numberOfProvinces = (int)(height * width * LAND_FRACTION / cellsPerProvince * (UnityEngine.Random.Range(0, 10) * 0.01 + 0.95));

        _cellMap = new int[width, height];
        _centers = new List<IntIntPair>();

        int provinceMapXSize = (int)(width * 0.5 / radius);
        int provinceMapYSize = (int)(height * 0.5 / radius);
        _provinceMap = new int[provinceMapXSize, provinceMapYSize];
        _quadrants = new List<IntIntPair>();

        // list of (current number of cells, total number of cells) per province
        List<IntIntPair> cellQtys = new List<IntIntPair>();
        // the number of cells the largest province will have
        int maxCellQty = 0;

        // can't really have more than this number of provinces - the map would be too cluttered
        numberOfProvinces = Math.Min(numberOfProvinces, provinceMapXSize * provinceMapYSize);
        FileLogger.Trace("MAP", "The world will have " + numberOfProvinces + " provinces ");

        _neighbors = new List<List<IntIntPair>>();
        for (int i = 0; i < numberOfProvinces; i++)
        {
            // each province get its own list of neighboring cells coordinates
            _neighbors.Add(new List<IntIntPair>());
        }

        int provinceId = 1;
        int failedTries = 0;
        // placing province centers
        while (_centers.Count < numberOfProvinces && failedTries < 10)
        {
            IntIntPair provinceCenter = SelectProvinceCenter(provinceId, radius);
            if (provinceCenter != null)
            {
                _centers.Add(provinceCenter);
                MarkProvinceCell(provinceCenter.first, provinceCenter.second, provinceId, _cellMap, _neighbors[provinceId - 1], false);
                //FileLogger.Trace("MAP", "The center of province #" + provinceId + " is [" + provinceCenter.first + ", " + provinceCenter.second + "]");

                // reset failures count
                failedTries = 0;
                // move to the next province
                provinceId++;
            }
            else
            {
                failedTries++;
            }
        }

        FileLogger.Trace("MAP", "The number of provinces changes from " + numberOfProvinces + " to " + _centers.Count);
        numberOfProvinces = _centers.Count;

        // calculating the number of cells to create per province
        for (int i = 0; i < numberOfProvinces; i++)
        {
            IntIntPair provinceCenter = _centers[i];

            //int markedCells = MakeProvince(provinceCenter.first, provinceCenter.second, i + 1, radius, cellMap, neighbors[i]);
            //FileLogger.Trace("MAP", "The bulk of province #" + (i + 1).ToString() + " conists of " + markedCells + " cells");

            int cellQty = cellsPerProvince + UnityEngine.Random.Range(-4, 6);
            if (provinceCenter.first > 3 * radius && provinceCenter.first < width - 3 * radius &&
                provinceCenter.second > 3 * radius && provinceCenter.second < height - 3 * radius)
            {
                cellQty += UnityEngine.Random.Range(4, 10);
            }

            // can't be fewer than we already marked
            //cellQty = Math.Max(cellQty, markedCells);

            cellQtys.Add(new IntIntPair(1, cellQty));
            maxCellQty = Math.Max(maxCellQty, cellQty);

            //FileLogger.Trace("MAP", "Province #" + (i + 1).ToString() + " will have " + cellQty + " cells with the center at [" + provinceCenter.first + ", " + provinceCenter.second + "]");
        }

        //FileLogger.Trace("MAP", "The largest province should have " + maxCellQty + " cells");

        // try to reach the desired number of cells per province
        for (int i = 1; i < maxCellQty; i++)
        {
            //FileLogger.Trace("MAP", "Adding cells: pass #" + i);
            for (int p = 0; p < numberOfProvinces; p++)
            {
                if (cellQtys[p].first < cellQtys[p].second && _neighbors[p].Count > 0)
                {
                    if (AddRandomNeighbor(p + 1, _cellMap, _neighbors[p], true) != null)
                    {
                        cellQtys[p].first++;
                    }
                }
            }
        }

        _distancesInProvinces = new int[numberOfProvinces, numberOfProvinces];

        // init distances in provinces
        for (int i = 0; i < numberOfProvinces; i++)
        {
            for (int j = 0; j < numberOfProvinces; j++)
            {
                _distancesInProvinces[i, j] = int.MaxValue;
            }
            _distancesInProvinces[i, i] = 0;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_cellMap[x, y] > 0)
                {
                    SearchForNeighbors(x, y, false);
                }
            }
        }

        // we only need distances from province #1
        UpdateDistancesForProvince(0);

        /*
        for (int i = 0; i < numberOfProvinces - 1; i++)
        {
            for (int j = i + 1; j < numberOfProvinces; j++)
            {
                FileLogger.Trace("MAP", "Distance between provinces #" + (i + 1).ToString() + " and #" + (j + 1).ToString() + " is " + distancesInProvinces[i, j]);
            }
        }
        */

        for (int i = 1; i < numberOfProvinces; i++)
        {
            if (_distancesInProvinces[0, i] == int.MaxValue)
            {
                ConnectIsland(i);
            }
        }

        // for connected water tiles, set province id to -1
        // so that lakes and inner seas can be identified
        // and eliminated
        for (int x = 1; x < width - 1; x++)
        {
            DeepenTheOcean(x, 1);
            DeepenTheOcean(x, height - 1);
        }

        for (int y = 1; y < height - 1; y++)
        {
            DeepenTheOcean(1, y);
            DeepenTheOcean(width - 1, y);
        }

        // dry lakes and inner seas - water tiles that are not
        // connected to the ocean
        for (int x = 1; x < _cellMap.GetLength(0) - 1; x++)
        {
            for (int y = 1; y < _cellMap.GetLength(1) - 1; y++)
            {
                if (_cellMap[x, y] == 0)
                {
                    DryLake(x, y);
                }
            }
        }

        _distancesInCells = new int[numberOfProvinces, numberOfProvinces];
        // calculate distances in cells
        for (int i = 0; i < numberOfProvinces; i++)
        {
            _distancesInCells[i, i] = 0;
            for (int j = i + 1; j < numberOfProvinces; j++)
            {
                int distance = CalculateDistance(_quadrants[i], _quadrants[j]);
                _distancesInCells[i, j] = distance;
                _distancesInCells[j, i] = distance;
            }
        }
        
        // converting the cells list into an array
        List<MapCellData> cells = new List<MapCellData>();
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (_cellMap[x, y] > 0)
                {
                    cells.Add(new MapCellData(x, y, _cellMap[x, y]));
                }
            }
        }
        data.cells = cells.ToArray();

        // setting up province data
        ProvinceData[] templates = data.provinces;
        data.provinces = new ProvinceData[numberOfProvinces];

        // set up Utopia in the center of the map
        data.provinces[0] = templates[0].CloneAs(1);

        // calculate max distance to potential faction capitals
        List<int> orcishCapitalCandidates = new List<int>();
        List<int> elvenCapitalCandidates = new List<int>();
        List<int> dwarvenCapitalCandidates = new List<int>();

        int maxOrcishDistance = 0;
        int maxElvenDistance = 0;
        int maxDwarvenDistance = 0;

        IntIntPair mapCenter = _centers[0];
        for (int i = 1; i < numberOfProvinces; i++)
        {
            if (_distancesInProvinces[0, i] > 1)
            {
                IntIntPair provinceCenter = _centers[i];
                if (provinceCenter.first > mapCenter.first)
                {
                    // orcs: lower-right corner
                    if (provinceCenter.second < mapCenter.second)
                    {
                        orcishCapitalCandidates.Add(i);
                        maxOrcishDistance = Math.Max(maxOrcishDistance, _distancesInProvinces[0, i]);
                    }
                    else
                    {
                        // dwarves: upper-right corner
                        dwarvenCapitalCandidates.Add(i);
                        maxDwarvenDistance = Math.Max(maxDwarvenDistance, _distancesInProvinces[0, i]);
                    }
                }
                else
                {
                    // elves: a 90 degrees sector in the left half of the map
                    if (Math.Abs(provinceCenter.first - mapCenter.first) > Math.Abs(provinceCenter.second - mapCenter.second))
                    {
                        elvenCapitalCandidates.Add(i);
                        maxElvenDistance = Math.Max(maxElvenDistance, _distancesInProvinces[0, i]);
                    }
                }
            }
        }

        // we should be able to find a province for each faction at this distance from the center
        int targetDistance = Math.Min(maxOrcishDistance, Math.Min(maxElvenDistance, maxDwarvenDistance));

        // select candidates for the orcish capital
        List<int> provinceCandidates = new List<int>();
        for (int i = 0; i < orcishCapitalCandidates.Count; i++)
        {
            if (_distancesInProvinces[0, orcishCapitalCandidates[i]] == targetDistance)
            {
                provinceCandidates.Add(orcishCapitalCandidates[i]);
            }
        }

        // if for some reason there is no suitable province at the target distance, search farther
        if (provinceCandidates.Count == 0)
        {
            for (int i = 0; i < orcishCapitalCandidates.Count; i++)
            {
                if (_distancesInProvinces[0, orcishCapitalCandidates[i]] > targetDistance)
                {
                    provinceCandidates.Add(orcishCapitalCandidates[i]);
                }
            }
        }

        // start with the first candidate, search for the one
        // with the largest x and smallest y
        int orcishCapitalIndex = provinceCandidates[0];
        int distance1 = Math.Abs(_centers[orcishCapitalIndex].first - _centers[orcishCapitalIndex].second);
        for (int i = 1; i < provinceCandidates.Count; i++)
        {
            int distance2 = Math.Abs(_centers[provinceCandidates[i]].first - _centers[provinceCandidates[i]].second);
            if (distance2 > distance1)
            {
                orcishCapitalIndex = provinceCandidates[i];
            }
        }
        data.provinces[orcishCapitalIndex] = templates[1].CloneAs(orcishCapitalIndex + 1);

        // select candidates for the elven capital
        provinceCandidates = new List<int>();
        for (int i = 0; i < elvenCapitalCandidates.Count; i++)
        {
            if (_distancesInProvinces[0, elvenCapitalCandidates[i]] == targetDistance)
            {
                provinceCandidates.Add(elvenCapitalCandidates[i]);
            }
        }

        // if for some reason there is no suitable province at the target distance, search farther
        if (provinceCandidates.Count == 0)
        {
            for (int i = 0; i < elvenCapitalCandidates.Count; i++)
            {
                if (_distancesInProvinces[0, elvenCapitalCandidates[i]] > targetDistance)
                {
                    provinceCandidates.Add(elvenCapitalCandidates[i]);
                }
            }
        }

        // start with the first candidate, search for the one with the smallest x
        int elvenCapitalIndex = provinceCandidates[0];
        for (int i = 1; i < provinceCandidates.Count; i++)
        {
            if (_centers[provinceCandidates[i]].first < _centers[elvenCapitalIndex].first)
            {
                elvenCapitalIndex = provinceCandidates[i];
            }
        }
        data.provinces[elvenCapitalIndex] = templates[2].CloneAs(elvenCapitalIndex + 1);

        // select candidates for the dwarven capital
        provinceCandidates = new List<int>();
        for (int i = 0; i < dwarvenCapitalCandidates.Count; i++)
        {
            if (_distancesInProvinces[0, dwarvenCapitalCandidates[i]] == targetDistance)
            {
                provinceCandidates.Add(dwarvenCapitalCandidates[i]);
            }
        }

        // if for some reason there is no suitable province at the target distance, search farther
        if (provinceCandidates.Count == 0)
        {
            for (int i = 0; i < dwarvenCapitalCandidates.Count; i++)
            {
                if (_distancesInProvinces[0, dwarvenCapitalCandidates[i]] > targetDistance)
                {
                    provinceCandidates.Add(dwarvenCapitalCandidates[i]);
                }
            }
        }

        // start with the first candidate, search for the one with the largest x
        int dwarvenCapitalIndex = provinceCandidates[0];
        for (int i = 1; i < provinceCandidates.Count; i++)
        {
            if (_centers[provinceCandidates[i]].first + _centers[provinceCandidates[i]].second >
                _centers[dwarvenCapitalIndex].first + _centers[dwarvenCapitalIndex].second)
            {
                dwarvenCapitalIndex = provinceCandidates[i];
            }
        }
        data.provinces[dwarvenCapitalIndex] = templates[3].CloneAs(dwarvenCapitalIndex + 1);


        // select provinces that can be a secondary orcish province
        provinceCandidates = new List<int>();
        for (int i = 0; i < orcishCapitalCandidates.Count; i++)
        {
            if (_distancesInProvinces[0, orcishCapitalCandidates[i]] == 2 &&
                _distancesInProvinces[orcishCapitalIndex, orcishCapitalCandidates[i]] ==2)
            {
                provinceCandidates.Add(orcishCapitalCandidates[i]);
            }
        }

        // if there is no suitable province at distance of 2 from Utopia, search at distance of 1
        if (provinceCandidates.Count == 0)
        {
            for (int i = 0; i < orcishCapitalCandidates.Count; i++)
            {
                if (_distancesInProvinces[0, orcishCapitalCandidates[i]] >= 1 &&
                _distancesInProvinces[orcishCapitalIndex, orcishCapitalCandidates[i]] >= 2)
                {
                    provinceCandidates.Add(orcishCapitalCandidates[i]);
                }
            }
        }

        int randomIndex;
        int provinceIndex;

        // pick one at random
        if (provinceCandidates.Count > 0)
        {
            randomIndex = UnityEngine.Random.Range(0, provinceCandidates.Count - 1);
            provinceIndex = provinceCandidates[randomIndex];
            data.provinces[provinceIndex] = templates[4].CloneAs(provinceIndex + 1);
        }


        // select provinces that can be a secondary elven province
        provinceCandidates = new List<int>();
        for (int i = 0; i < elvenCapitalCandidates.Count; i++)
        {
            if (_distancesInProvinces[0, elvenCapitalCandidates[i]] == 2 &&
                _distancesInProvinces[elvenCapitalIndex, elvenCapitalCandidates[i]] == 2)
            {
                provinceCandidates.Add(elvenCapitalCandidates[i]);
            }
        }

        if (provinceCandidates.Count == 0)
        {
            for (int i = 0; i < elvenCapitalCandidates.Count; i++)
            {
                if (_distancesInProvinces[0, elvenCapitalCandidates[i]] >= 1 &&
                    _distancesInProvinces[elvenCapitalIndex, elvenCapitalCandidates[i]] >= 2)
                {
                    provinceCandidates.Add(elvenCapitalCandidates[i]);
                }
            }
        }

        // pick one at random
        if (provinceCandidates.Count > 0)
        {
            randomIndex = UnityEngine.Random.Range(0, provinceCandidates.Count - 1);
            provinceIndex = provinceCandidates[randomIndex];
            data.provinces[provinceIndex] = templates[5].CloneAs(provinceIndex + 1);
        }

        // select provinces that can be a secondary dwarven province
        provinceCandidates = new List<int>();
        for (int i = 0; i < dwarvenCapitalCandidates.Count; i++)
        {
            if (_distancesInProvinces[0, dwarvenCapitalCandidates[i]] == 2 &&
                _distancesInProvinces[dwarvenCapitalIndex, dwarvenCapitalCandidates[i]] == 2)
            {
                provinceCandidates.Add(dwarvenCapitalCandidates[i]);
            }
        }

        if (provinceCandidates.Count == 0)
        {
            for (int i = 0; i < dwarvenCapitalCandidates.Count; i++)
            {
                if (_distancesInProvinces[0, dwarvenCapitalCandidates[i]] >= 1 &&
                    _distancesInProvinces[dwarvenCapitalIndex, dwarvenCapitalCandidates[i]] >= 2)
                {
                    provinceCandidates.Add(dwarvenCapitalCandidates[i]);
                }
            }
        }

        // pick one at random
        if (provinceCandidates.Count > 0)
        {
            randomIndex = UnityEngine.Random.Range(0, provinceCandidates.Count - 1);
            provinceIndex = provinceCandidates[randomIndex];
            data.provinces[provinceIndex] = templates[6].CloneAs(provinceIndex + 1);
        }

        _humanProvinceNames = new List<string>();
        TextAsset resourceFile = Resources.Load("HumanNames") as TextAsset;
        StringReader reader = new StringReader(resourceFile.text);
        if (reader == null)
        {
            FileLogger.Error("MAP", "Can't load human province names");
        }
        else
        {
            string currentLine;
            while ((currentLine = reader.ReadLine()) != null)
            {
                _humanProvinceNames.Add(currentLine);
            }
        }

        // select provinces neighboring to Utopia and not used for another purpose yet
        provinceCandidates = new List<int>();
        for (int i = 1; i < numberOfProvinces; i++)
        {
            if (_distancesInProvinces[0, i] == 1 && data.provinces[i] == null)
            {
                provinceCandidates.Add(i);
            }
        }

        // put fighter-producing provinces in the select spots
        int counter = 1;
        while (provinceCandidates.Count > 0)
        {
            provinceIndex = provinceCandidates[0];
            data.provinces[provinceIndex] = templates[7].CloneAs(provinceIndex + 1);
            data.provinces[provinceIndex].name = CreateProvinceName(provinceIndex + 1);
            counter++;
            provinceCandidates.RemoveAll(province => _distancesInProvinces[province, provinceIndex] < 2);
        }

        for (int i = 1; i < numberOfProvinces; i++)
        {
            if (data.provinces[i] == null)
            {
                randomIndex = UnityEngine.Random.Range(8, templates.Length - 1);
                data.provinces[i] = templates[randomIndex].CloneAs(i + 1);
                data.provinces[i].name = CreateProvinceName(i + 1);
            }
        }

        return new Game(data);
    }

    // OBSOLETE
    private int MakeProvince(int x, int y, int provinceId, int radius, int[,] tileMap, List<IntIntPair> neighbors)
    {
        int provinceCells = 0;

        if (radius < 0 || tileMap[x, y] != provinceId)
        {
            FileLogger.Error("MAP", "Can't create province with id #" + provinceId + " with radius " + radius + " at [" + x + ", " + y + "]");
            return provinceCells;
        }

        MarkProvinceCell(x, y, provinceId, tileMap, neighbors, false);
        provinceCells++;

        for (int r = 0; r < radius; r++)
        {
            // making a blob of the radius r
            List<IntIntPair> neighborClones = new List<IntIntPair>(neighbors);
            for (int i = 0; i < neighborClones.Count; i++)
            {
                if (MarkProvinceCell(neighborClones[i].first, neighborClones[i].second, provinceId, tileMap, neighbors, false))
                {
                    provinceCells++;
                }
            }

            // modifying it slightly to look more irregular but still organic
            for (int i = 0; i < r; i++)
            {
                if (neighbors.Count > 0 && AddRandomNeighbor(provinceId, tileMap, neighbors, true) != null)
                {
                    provinceCells++;
                }
            }
        }

        return provinceCells;
    }

    private bool MarkProvinceCell(int x, int y, int provinceId, int[,] tileMap, List<IntIntPair> neighbors, bool useStricterRules)
    {
        if (tileMap[x, y] > 0 || tileMap[x, y] == provinceId)
        {
            return false;
        }
        tileMap[x, y] = provinceId;
        HexBorder[] borders = HexBorder.GetBorderDirections(x % 2 == 1);
        for (int i = 0; i < borders.Length; i++)
        {
            int neighborX = x + borders[i].GetDeltaX();
            int neighborY = y + borders[i].GetDeltaY();
            IntIntPair neighbor = new IntIntPair(neighborX, neighborY);

            bool passesBasicRules = IsInnerCell(neighborX, neighborY, tileMap) &&
                                    tileMap[neighborX, neighborY] <= 0;

            // stricter rules means having at least two neighboring cells
            // belonging to the same province
            // it's used to avoid serpentine forms
            bool passesStricterRules = false;
            if (useStricterRules)
            {
                int sameProvinceNeighbors = 0;
                HexBorder[] newBorders = HexBorder.GetBorderDirections(neighborX % 2 == 1);
                for (int j = 0; j < newBorders.Length; j++)
                {
                    int xx = neighborX + newBorders[j].GetDeltaX();
                    int yy = neighborY + newBorders[j].GetDeltaY();
                    if (IsInnerCell(xx, yy, tileMap) && tileMap[xx, yy] == provinceId)
                    {
                        sameProvinceNeighbors++;
                        if (sameProvinceNeighbors >= 2)
                        {
                            passesStricterRules = true;
                            break;
                        }
                    }
                }
            }

            // is within the map area, is a water cell, and either doesn't care
            // about stricter rules or satisfies them
            if (passesBasicRules && (!useStricterRules || passesStricterRules))
            {
                neighbors.Add(neighbor);
            }
        }
        return true;
    }

    private IntIntPair AddRandomNeighbor(int provinceId, int[,] tileMap, List<IntIntPair> neighbors, bool useStricterRules, bool towardCenterOnly = false)
    {
        IntIntPair newProvinceCell = null;

        // more accurately, the center of the province #1
        IntIntPair mapCenter = _centers[0];

        bool found = false;
        while (!found)
        {
            // a safety valve - should not happen
            if (neighbors.Count == 0)
            {
                return null;
            }

            int randomIndex = UnityEngine.Random.Range(0, neighbors.Count - 1);
            newProvinceCell = neighbors[randomIndex];

            neighbors.RemoveAt(randomIndex);

            // when deepening the ocean (provinceId = -1), only 0 satisfies the criteria
            // otherwise, both -1 and 0 work
            if (tileMap[newProvinceCell.first, newProvinceCell.second] <= 0 && tileMap[newProvinceCell.first, newProvinceCell.second] != provinceId)
            {
                found = !towardCenterOnly || CalculateDistance(mapCenter, newProvinceCell) <= CalculateDistance(mapCenter, _centers[provinceId - 1]);
            }
        }

        if (MarkProvinceCell(newProvinceCell.first, newProvinceCell.second, provinceId, tileMap, neighbors, useStricterRules))
        {
            return newProvinceCell;
        }
        return null;
    }

    private IntIntPair SelectProvinceCenter(int provinceId, int radius)
    {
        IntIntPair quadrant = null;
        int midX = (int)(0.5 * _provinceMap.GetLength(0));
        int midY = (int)(0.5 * _provinceMap.GetLength(1));

        if (provinceId == 1)
        {
            quadrant = new IntIntPair(midX, midY);
            MarkProvinceCell(quadrant.first, quadrant.second, provinceId, _provinceMap, _quadrants, false);
        }
        else
        {
            quadrant = AddRandomNeighbor(provinceId, _provinceMap, _quadrants, false);
        }

        if (quadrant == null)
        {
            return null;
        }
        _quadrants.Add(quadrant);

        return new IntIntPair(2 * radius * (quadrant.first - midX) + (int)(0.5 * _cellMap.GetLength(0)) + UnityEngine.Random.Range(-2, 2),
                                2 * radius * (quadrant.second - midY) + (int)(0.5 * _cellMap.GetLength(1)) + UnityEngine.Random.Range(-2, 2));
    }

    private int CalculateDistance(IntIntPair one, IntIntPair two)
    {
        // http://www.redblobgames.com/grids/hexagons/
        return Math.Max(Math.Abs(one.first - two.first), Math.Abs(one.second - two.second));
    }

    private void DeepenTheOcean(int startingX, int startingY)
    {
        if (_cellMap[startingX, startingY] != 0)
        {
            return;
        }
        int provinceId = -1;
        List<IntIntPair> neighbors = new List<IntIntPair>();
        MarkProvinceCell(startingX, startingY, provinceId, _cellMap, neighbors, false);
        while (neighbors.Count > 0)
        {
            AddRandomNeighbor(provinceId, _cellMap, neighbors, false);
        }
    }

    private void DryLake(int x, int y)
    {
        List<int> provinceIds = new List<int>();
        HexBorder[] borders = HexBorder.GetBorderDirections(x % 2 == 1);
        for (int i = 0; i < borders.Length; i++)
        {
            int neighborX = x + borders[i].GetDeltaX();
            int neighborY = y + borders[i].GetDeltaY();
            if (neighborX > 0 && neighborX < _cellMap.GetLength(0) &&
                neighborY > 0 && neighborY < _cellMap.GetLength(1) &&
                _cellMap[neighborX, neighborY] > 0)
            {
                provinceIds.Add(_cellMap[neighborX, neighborY]);
            }
        }

        int randomIndex = UnityEngine.Random.Range(0, provinceIds.Count - 1);
        _cellMap[x, y] = provinceIds[randomIndex];
    }

    private bool IsInnerCell(int x, int y, int[,] tileMap)
    {
        return x > 0 && x < tileMap.GetLength(0) &&
               y > 0 && y < tileMap.GetLength(1);
    }

    private void MarkNeighboringProvinces(int first, int second, bool updateDistances = false)
    {
        int matrixSize = _distancesInProvinces.GetLength(0);

        // no province can be its own neighbor
        if (first == second)
        {
            FileLogger.Error("MAP", "Province " + first + " can't be its own neighbor");
            return;

        }

        // province id can't be less than 1 or greater than N
        if (first <= 0 || first > matrixSize || second <= 0 || second > matrixSize)
        {
            FileLogger.Error("MAP", "Expected both " + first + " and " + second +
                                    " be greater than zero and no greater than " + matrixSize);
            return;
        }

        // the distances map's idexes start at 0, province ids start at 1
        int index1 = first - 1;
        int index2 = second - 1;

        // already marked as neighbors
        if (_distancesInProvinces[index1, index2] == 1)
        {
            return;
        }

        //FileLogger.Trace("MAP", "Marking provinces #" + one + " and #" + two + " as neighbors");

        // mark neighbors
        _distancesInProvinces[index1, index2] = 1;
        _distancesInProvinces[index2, index1] = 1;

        // update distances between provinces with new information
        if (updateDistances)
        {
            UpdateDistancesForProvince(index1);
        }
    }

    // OBSOLETE
    private void UpdateDistances()
    {
        int matrixSize = _distancesInProvinces.GetLength(0);

        for (int i = 0; i < matrixSize; i++)
        {
            UpdateDistancesForProvince(i);
        }
    }

    private void UpdateDistancesForProvince(int index)
    {
        int matrixSize = _distancesInProvinces.GetLength(0);
        for (int j = 0; j < matrixSize; j++)
        {
            if (_distancesInProvinces[index, j] > 0 && _distancesInProvinces[index, j] < int.MaxValue)
            {
                for (int k = 0; k < matrixSize; k++)
                {
                    if (_distancesInProvinces[j, k] > 0 && _distancesInProvinces[j, k] < int.MaxValue)
                    {
                        int distance = Math.Min(_distancesInProvinces[index, k], _distancesInProvinces[index, j] + _distancesInProvinces[j, k]);
                        /*
                        if (distance < distances[i, k])
                        {
                            FileLogger.Trace("MAP", "Distance provinces #" + (i + 1).ToString() + " and #" + (k + 1).ToString() + " changes from " + distances[i, k] + " to " + distance);
                        }
                        */
                        _distancesInProvinces[index, k] = distance;
                        _distancesInProvinces[k, index] = distance;
                    }
                }
            }
        }
    }

    private void ConnectIsland(int index)
    {
        FileLogger.Trace("MAP", "Connecting province #" + (index + 1).ToString() + " to the mainland");
        int targetCellsWithNeighbors = 3;
        int addedCells = 0;
        while (addedCells < targetCellsWithNeighbors)
        {
            IntIntPair addition = AddRandomNeighbor(index + 1, _cellMap, _neighbors[index], false, true);
            if (addition == null)
            {
                // nothing can be done - the list of potential neighbors exhausted
                return;
            }
            if (SearchForNeighbors(addition.first, addition.second, true))
            {
                addedCells++;
            }
        }
    }

    private bool SearchForNeighbors(int x, int y, bool updateDistances)
    {
        bool foundNeighboringProvince = false;
        HexBorder[] borders = HexBorder.GetBorderDirections(x % 2 == 1);
        for (int i = 0; i < borders.Length; i++)
        {
            int neighborX = x + borders[i].GetDeltaX();
            int neighborY = y + borders[i].GetDeltaY();
            if (IsInnerCell(neighborX, neighborY, _cellMap) &&
                            _cellMap[neighborX, neighborY] > 0 &&
                            _cellMap[x, y] != _cellMap[neighborX, neighborY])
            {
                MarkNeighboringProvinces(_cellMap[x, y], _cellMap[neighborX, neighborY], updateDistances);
                foundNeighboringProvince = true;
            }
        }
        return foundNeighboringProvince;
    }

    private string CreateProvinceName(int provinceId)
    {
        string provinceName = "Number #" + provinceId;
        if (_humanProvinceNames.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, _humanProvinceNames.Count - 1);
            provinceName = _humanProvinceNames[randomIndex];
            _humanProvinceNames.RemoveAt(randomIndex);
        }

        return provinceName;
    }

}
