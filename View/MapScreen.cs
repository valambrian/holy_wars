using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MapScreen : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private ProvinceView _provincePrefab;

    [SerializeField]
    private ArmyView _armyPrefab;

    [SerializeField]
    private UnitListUIView _unitListView;

    [SerializeField]
    private ProvinceTrainingUIView _provinceTrainingView;

    [SerializeField]
    private UnitTypeUIView _unitTypeView;

    [SerializeField]
    private GameOverView _gameOverPrefab;

    [SerializeField]
    private ArmyMovementOrderView _armyMovementOrderViewPrefab;

    [SerializeField]
    private Button _doneButton;

    [SerializeField]
    private Button _nextButton;

    // game events
    [SerializeField]
    private GameObject _dialog;

    [SerializeField]
    private Text _dialogText;

    [SerializeField]
    private GameObject _notification;

    [SerializeField]
    private Text _notificationText;

    [SerializeField]
    private GameObject _helpWindow;

    [SerializeField]
    private Text _helpText;

    [SerializeField]
    private GameObject _warning;

    [SerializeField]
    private GameObject _menu;

    [SerializeField]
    private GameObject _briberyWindow;

    [SerializeField]
    private Text _disbandUnitsCostText;

    [SerializeField]
    private Button _disbandUnitsButton;

    [SerializeField]
    private Text _bribeProvinceCostText;

    [SerializeField]
    private Button _bribeProvinceButton;


    [SerializeField]
    private Button _loadButton;

    // (top) resource bar fields
    [SerializeField]
    private Text _phaseDescriptionField;

    [SerializeField]
    private Text _moneyBalanceField;

    [SerializeField]
    private Text _favorsBalanceField;

    [SerializeField]
    private Text _manpowerField;

    [SerializeField]
    private Text _provincesField;

    [SerializeField]
    private Text _turnNumberField;

    [SerializeField]
    private int _width = 16;
    [SerializeField]
    private int _height = 12;
    [SerializeField]
    private int _tileResolution = 32;
    [SerializeField]
    private Color _backgroundColor = new Color(.25f, .25f, .25f);
    [SerializeField]
    private Color _borderColor = new Color(0, 0, 0);

    private List<ProvinceView> _visibleProvinceViews = new List<ProvinceView>();
    private ProvinceView _currentProvinceView = null;
    private List<ArmyView> _armyViews = new List<ArmyView>();
    private List<ArmyMovementOrderView> _armyMovementOrderViews = new List<ArmyMovementOrderView>();
    private ArmyMovementOrderView _currentArmyMovementOrderView;
    private GameObject _currentDialog = null;
    private bool _ignoreMapClicks = false;
    private bool _isViewValid = false;
    private bool _isGameOver = false;
    private bool _isAnArmyViewMoving = false;
    GameOverView _theEnd = null;
    //MouseClickCounter _mouseClickCounter;

    private enum DialogLevels { NONE, UNITS, TRAINING, WARNING, HELP, EVENT, ALL };
    private DialogLevels _currentDialogLevel;

    private Dictionary<Game.TurnPhases, string> _phaseDescriptions = new Dictionary<Game.TurnPhases, string>()
    {
        { Game.TurnPhases.START, "Start Turn" },
        { Game.TurnPhases.TRAINING, "Train Armies" },
        { Game.TurnPhases.MOVEMENT, "Move Armies" },
        { Game.TurnPhases.COMBAT, "Resolve Combat" },
        { Game.TurnPhases.END, "End Turn" }
    };

    // (1 - sqrt(3)/2) * tile resolution
    // if a tile's width is 32, its height is 28 pixels, not 32
    private int _pixelOffset;
    // 3/4 * tile resolution is space for the left trianlge (1/4) plus the central rectangle (1/2) of a hex
    // 1/4 * tile resolution is space for the right triangle of the rightmost hex
    private int _widthInPixels;
    // a hex's height is less than its width
    private int _heightInPixels;

    private int _worldOffsetX = 0;
    private int _worldOffsetY = 0;

    private Game _model;
    private Faction _currentFaction;

    private int _currentPhaseOrdersNumber;
    private int _currentProvinceId;

    void Start()
    {
        // (1 - sqrt(3)/2) * tile resolution
        // because if a tile's width is 32, its height is 28 pixels, not 32
        _pixelOffset = (int)((1 - Mathf.Sqrt(3.0f) / 2) * _tileResolution);
        // 3/4 * tile resolution is space for the left trianlge (1/4) plus the central rectangle (1/2) of a hex
        // 1/4 * tile resolution is space for the right triangle of the rightmost hex
        _widthInPixels = (_width * 3 + 1) * _tileResolution / 4;

        // a hex's height is less than its width
        _heightInPixels = _height * (_tileResolution - _pixelOffset);

        BuildMesh();

        SetModel(GameSingleton.Instance.Game);
    }

    private void CenterMap()
    {
        Province besieged = _model.GetAProvinceUnderSiege();
        if (besieged != null && _model.GetCurrentPhase() == Game.TurnPhases.COMBAT)
        {
            MapCell mapCenter = besieged.GetCenterMapCell();
            SetMapCenter(mapCenter.GetX(), mapCenter.GetY());
        }
    }

    /// <summary>
    /// Build a mesh for the map.
    /// It's just a single rectangle since the map is completely flat.
    /// </summary>
    private void BuildMesh()
    {
        // this is going to be one fat rectangle
        int numVertices = 4;
        int numTriangles = 2;

        // generate the mesh data
        Vector3[] vertices = new Vector3[numVertices];
        Vector3[] normals = new Vector3[numVertices];
        Vector2[] uv = new Vector2[numVertices];

        // lists of vertices creating the triangles
        int[] triangles = new int[numTriangles * 3]; // three points per triangle

        int x, y;
        for (y = 0; y < 2; y++)
        {
            for (x = 0; x < 2; x++)
            {
                vertices[y * 2 + x] = new Vector3(x * _width, y * _height, 0);
                normals[y * 2 + x] = Vector3.back;
                uv[y * 2 + x] = new Vector2(x, y);
            }
        }

        triangles[0] = 0;
        triangles[1] = 2;
        triangles[2] = 3;

        triangles[3] = 0;
        triangles[4] = 3;
        triangles[5] = 1;

        // Create a new Mesh and populate with the data
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uv;

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
    }

    private void UpdateView(bool showResources = true)
    {
        FileLogger.Trace("VIEW", "Updating View");

        // the faction changed - center the view on its capital
        if (_currentFaction != _model.GetCurrentFaction())
        {
            if (_currentFaction != null)
            {
                MapCell mapCenter = _currentFaction.GetCapital().GetCenterMapCell();
                SetMapCenter(mapCenter.GetX(), mapCenter.GetY());
            }
            _currentFaction = _model.GetCurrentFaction();
        }

        BuildTexture();
        UpdateProvinceViews();
        UpdateResourceBar(showResources);
    }

    private void UpdateResourceBar(bool showResources)
    {
        Faction currentFaction = _model.GetCurrentFaction();
        Game.TurnPhases currentPhase = _model.GetCurrentPhase();

        if (_phaseDescriptions.ContainsKey(currentPhase))
        {
            _phaseDescriptionField.text = _phaseDescriptions[currentPhase];
        }
        else
        {
            _phaseDescriptionField.text = "";
        }

        _moneyBalanceField.text = showResources ? currentFaction.GetMoneyBalance().ToString() : "";
        _favorsBalanceField.text = showResources ? currentFaction.GetFavors().ToString() : "";
        _manpowerField.text = showResources ? currentFaction.GetAvailableManpower().ToString() + " / " + currentFaction.GetTotalManpower().ToString() : "";
        _provincesField.text = currentFaction.GetProvinceCount().ToString() + " / " + _model.GetProvinceCount().ToString();
        _turnNumberField.text = _model.GetCurrentTurnNumber().ToString();
    }

    /// <summary>
    /// Build a texture for the world map with the lower left cell being (offsetX, offsetY)
    /// Math is tailored for flat side up hexes.
    /// </summary>
    private void BuildTexture()
    {
        if (!_isViewValid)
        {
            FileLogger.Trace("VIEW", "Building Texture");

            // NOTE: should we used double buffer in the future
            // so that we can recycle a texture instead of creating a new one every time?
            Texture2D mapTexture = new Texture2D(_widthInPixels, _heightInPixels);
            //Debug.Log("Created a texture " + _widthInPixels + " x " + _heightInPixels + " pixels.");
            //Debug.Log("Screen width and height are " + Screen.width + ", " + Screen.height + " pixels.");

            if (_model != null)
            {
                // draw the map texture
                DrawLeftEdge(mapTexture, _worldOffsetX, _worldOffsetY);

                for (int x = 0; x < _width; x++)
                {
                    DrawColumn(mapTexture, _worldOffsetX, _worldOffsetY, x + 1, x % 2 == 0);
                }

                DrawRightEdge(mapTexture, _worldOffsetX, _worldOffsetY);

                // now draw the borders
                DrawLeftEdgeCellBorders(mapTexture, _worldOffsetX, _worldOffsetY);
                for (int x = 0; x < _width; x++)
                {
                    DrawColumnBorders(mapTexture, _worldOffsetX, _worldOffsetY, x + 1, x % 2 == 0);
                }

                DrawRightEdgeCellBorders(mapTexture, _worldOffsetX, _worldOffsetY);
            }
            else
            {
                // apply background color
                Color[] background = mapTexture.GetPixels();
                for (int i = 0; i < background.Length; i++)
                {
                    background[i] = _backgroundColor;
                }
                mapTexture.SetPixels(background);
            }

            mapTexture.filterMode = FilterMode.Point;
            mapTexture.wrapMode = TextureWrapMode.Clamp;
            mapTexture.Apply();

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.material.mainTexture = mapTexture;
        }
    }

    #region functions that draw parts of the map

    private void DrawLeftEdge(Texture2D mapTexture, int offsetX, int offsetY)
    {
        // NOTE: it is assumed that the leftmost column is odd
        // this affects calculations along the axis y

        // all images will start at the left border
        int mapX = 0;
        // all images are going to use the right 1/4 of the cell's texture
        int startX = 3 * _tileResolution / 4;
        int endX = _tileResolution;
        // all map cells are to the left from the leftmost column (if offsetX = 0, it's the first column)
        int worldX = offsetX;

        // the bottom left triangle
        int mapY = 0;
        // top half of the image
        int startY = (_tileResolution - _pixelOffset) / 2;
        int endY = _tileResolution - _pixelOffset;
        // the bottom left cell is below the bottom row (if offsetY = 0, it's the first row)
        int worldY = offsetY;
        DrawCellTexture(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);

        // the triangles in the middle of the left edge
        for (int y = 0; y < _height - 1; y++)
        {
            mapY = y * (_tileResolution - _pixelOffset) + (_tileResolution - _pixelOffset) / 2;
            startY = 0;
            endY = _tileResolution - _pixelOffset;
            worldY = y + offsetY + 1;
            DrawCellTexture(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);
        }

        // the upper left triangle
        mapY = _height * (_tileResolution - _pixelOffset) - (_tileResolution - _pixelOffset) / 2;
        startY = 0;
        endY = (_tileResolution - _pixelOffset) / 2;
        worldY = offsetY + _height;
        DrawCellTexture(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);
    }

    private void DrawRightEdge(Texture2D mapTexture, int offsetX, int offsetY)
    {
        //NOTE: it is assumed that the rightmost column is even
        // this affects calculations along the axis y

        // all images will end at the right border
        int mapX = _width * 3 * _tileResolution / 4;
        // all images are going to use the left 1/4 of the cell's texture
        int startX = 0;
        int endX = _tileResolution / 4;
        // all map cells are to the right from the rightmost column (if offsetX = 0, it's the width's column)
        int worldX = _width + offsetX + 1;

        // the whole image's height
        int startY = 0;
        int endY = _tileResolution - _pixelOffset;
        // the bottom right cell is on the same level as the bottom row (if offsetY = 0, it's the first row)
        int worldY = offsetY + 1;

        for (int y = 0; y < _height; y++)
        {
            int mapY = y * (_tileResolution - _pixelOffset);
            worldY = y + offsetY + 1;
            DrawCellTexture(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);
        }
    }

    private void DrawColumn(Texture2D mapTexture, int offsetX, int offsetY, int x, bool isLong)
    {
        if (isLong)
        {
            DrawLongColumn(mapTexture, offsetX, offsetY, x);
        }
        else
        {
            DrawShortColumn(mapTexture, offsetX, offsetY, x);
        }
    }

    private void DrawLongColumn(Texture2D mapTexture, int offsetX, int offsetY, int x)
    {
        //NOTE: a long column contains only fully visible cells

        // if offsetX == 0, world X = x
        int worldX = x + offsetX;
        // if x is 1, start at the left edge of the screen
        int mapX = (x - 1) * 3 * _tileResolution / 4;
        // all images are going to use the full width of the cell's texture
        int startX = 0;
        int endX = _tileResolution;

        // the whole image's height
        int startY = 0;
        int endY = _tileResolution - _pixelOffset;
        // the bottom right cell is on the same level as the bottom row (if offsetY = 0, it's the first row)
        int worldY = offsetY + 1;

        for (int y = 0; y < _height; y++)
        {
            int mapY = y * (_tileResolution - _pixelOffset);
            worldY = y + offsetY + 1;
            DrawCellTexture(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);
        }
    }

    private void DrawShortColumn(Texture2D mapTexture, int offsetX, int offsetY, int x)
    {
        //NOTE: a short column's top and bottom cells are only half visible

        // if offsetX == 0, world X = x
        int worldX = x + offsetX;
        // if x is 1, start at the left edge of the screen
        int mapX = (x - 1) * 3 * _tileResolution / 4;
        // all images are going to use the full width of the cell's texture
        int startX = 0;
        int endX = _tileResolution;

        // the bottom cell
        int mapY = 0;
        // top half of the image
        int startY = (_tileResolution - _pixelOffset) / 2;
        int endY = _tileResolution - _pixelOffset;
        // the bottom cell is half submerged (if offsetY = 0, it's the first row)
        int worldY = offsetY + 1;
        DrawCellTexture(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);

        // the triangles in the middle of the left edge
        for (int y = 0; y < _height - 1; y++)
        {
            mapY = y * (_tileResolution - _pixelOffset) + (_tileResolution - _pixelOffset) / 2;
            startY = 0;
            endY = _tileResolution - _pixelOffset;
            worldY = y + offsetY + 2;
            DrawCellTexture(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);
        }

        // the upper left triangle
        mapY = _height * (_tileResolution - _pixelOffset) - (_tileResolution - _pixelOffset) / 2;
        startY = 0;
        endY = (_tileResolution - _pixelOffset) / 2;
        worldY = offsetY + _height + 1;
        DrawCellTexture(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);
    }

    private void DrawCellTexture(Texture2D mapTexture, int worldX, int worldY, int mapX, int mapY, int startX, int endX, int startY, int endY)
    {
        MapCell cell = _model.GetCellAt(worldX, worldY);
        string textureName;
        if (cell != null)
        {
            textureName = cell.GetDwellersRace().GetNameLowercased();
        }
        else
        {
            textureName = "none";
        }

        Texture2D cellTexture = SpriteCollectionManager.GetTextureByName(textureName);
        if (cellTexture != null)
        {
            for (int j = 0; j < endY - startY; j++)
            {
                for (int i = 0; i < endX - startX; i++)
                {
                    Color pixelColor = cellTexture.GetPixel(i + startX, j + startY);
                    if (pixelColor.a > 0)
                    {
                        mapTexture.SetPixel(mapX + i, mapY + j, pixelColor);
                    }
                }
            }
        }
        if (cell != null)
        {
            textureName = cell.GetOwnersFaction().GetNameLowercased();
            cellTexture = SpriteCollectionManager.GetTextureByName(textureName);
            if (cellTexture != null)
            {
                for (int j = 0; j < endY - startY; j++)
                {
                    for (int i = 0; i < endX - startX; i++)
                    {
                        Color pixelColor = cellTexture.GetPixel(i + startX, j + startY);
                        if (pixelColor.a > 0)
                        {
                            mapTexture.SetPixel(mapX + i, mapY + j, pixelColor);
                        }
                    }
                }
            }
        }
    }

    private void DrawLeftEdgeCellBorders(Texture2D mapTexture, int offsetX, int offsetY)
    {
        //NOTE: it is assumed that the leftmost column is odd
        // this affects calculations along the axis y

        // all images will start at the left border
        int mapX = 0;
        // all images are going to use the right 1/4 of the cell's texture
        int startX = 3 * _tileResolution / 4;
        int endX = _tileResolution;
        // all map cells are to the left from the leftmost column (if offsetX = 0, it's the first column)
        int worldX = offsetX;

        // the bottom left triangle
        int mapY = 0;
        // top half of the image
        int startY = (_tileResolution - _pixelOffset) / 2;
        int endY = _tileResolution - _pixelOffset;
        // the bottom left cell is below the bottom row (if offsetY = 0, it's the first row)
        int worldY = offsetY;
        DrawCellBorders(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);

        // the triangles in the middle of the left edge
        for (int y = 0; y < _height - 1; y++)
        {
            mapY = y * (_tileResolution - _pixelOffset) + (_tileResolution - _pixelOffset) / 2;
            startY = 0;
            endY = _tileResolution - _pixelOffset;
            worldY = y + offsetY + 1;
            DrawCellBorders(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);
        }

        // the upper left triangle
        mapY = _height * (_tileResolution - _pixelOffset) - (_tileResolution - _pixelOffset) / 2;
        startY = 0;
        endY = (_tileResolution - _pixelOffset) / 2;
        worldY = offsetY + _height;
        DrawCellBorders(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);
    }

    private void DrawColumnBorders(Texture2D mapTexture, int offsetX, int offsetY, int x, bool isLong)
    {
        if (isLong)
        {
            DrawLongColumnBorders(mapTexture, offsetX, offsetY, x);
        }
        else
        {
            DrawShortColumnBorders(mapTexture, offsetX, offsetY, x);
        }
    }

    private void DrawLongColumnBorders(Texture2D mapTexture, int offsetX, int offsetY, int x)
    {
        //NOTE: a long column contains only fully visible cells

        // if offsetX == 0, world X = x
        int worldX = x + offsetX;
        // if x is 1, start at the left edge of the screen
        int mapX = (x - 1) * 3 * _tileResolution / 4;
        // all images are going to use the full width of the cell's texture
        int startX = 0;
        int endX = _tileResolution;

        // the whole image's height
        int startY = 0;
        int endY = _tileResolution - _pixelOffset;
        // the bottom right cell is on the same level as the bottom row (if offsetY = 0, it's the first row)
        int worldY = offsetY + 1;

        for (int y = 0; y < _height; y++)
        {
            int mapY = y * (_tileResolution - _pixelOffset);
            worldY = y + offsetY + 1;
            DrawCellBorders(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);
        }
    }

    private void DrawShortColumnBorders(Texture2D mapTexture, int offsetX, int offsetY, int x)
    {
        //NOTE: a short column's top and bottom cells are only half visible

        // if offsetX == 0, world X = x
        int worldX = x + offsetX;
        // if x is 1, start at the left edge of the screen
        int mapX = (x - 1) * 3 * _tileResolution / 4;
        // all images are going to use the full width of the cell's texture
        int startX = 0;
        int endX = _tileResolution;

        // the bottom cell
        int mapY = 0;
        // top half of the image
        int startY = (_tileResolution - _pixelOffset) / 2;
        int endY = _tileResolution - _pixelOffset;
        // the bottom cell is half submerged (if offsetY = 0, it's the first row)
        int worldY = offsetY + 1;
        DrawCellBorders(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);

        // the triangles in the middle of the left edge
        for (int y = 0; y < _height - 1; y++)
        {
            mapY = y * (_tileResolution - _pixelOffset) + (_tileResolution - _pixelOffset) / 2;
            startY = 0;
            endY = _tileResolution - _pixelOffset;
            worldY = y + offsetY + 2;
            DrawCellBorders(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);
        }

        // the upper left triangle
        mapY = _height * (_tileResolution - _pixelOffset) - (_tileResolution - _pixelOffset) / 2;
        startY = 0;
        endY = (_tileResolution - _pixelOffset) / 2;
        worldY = offsetY + _height + 1;
        DrawCellBorders(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);
    }

    private void DrawRightEdgeCellBorders(Texture2D mapTexture, int offsetX, int offsetY)
    {
        //NOTE: it is assumed that the rightmost column is even
        // this affects calculations along the axis y

        // all images will end at the right border
        int mapX = _width * 3 * _tileResolution / 4;
        // all images are going to use the left 1/4 of the cell's texture
        int startX = 0;
        int endX = _tileResolution / 4;
        // all map cells are to the right from the rightmost column (if offsetX = 0, it's the width's column)
        int worldX = _width + offsetX + 1;

        // the whole image's height
        int startY = 0;
        int endY = _tileResolution - _pixelOffset;
        // the bottom right cell is on the same level as the bottom row (if offsetY = 0, it's the first row)
        int worldY = offsetY + 1;

        for (int y = 0; y < _height; y++)
        {
            int mapY = y * (_tileResolution - _pixelOffset);
            worldY = y + offsetY + 1;
            DrawCellBorders(mapTexture, worldX, worldY, mapX, mapY, startX, endX, startY, endY);
        }
    }

    private void DrawCellBorders(Texture2D mapTexture, int worldX, int worldY, int mapX, int mapY, int startX, int endX, int startY, int endY)
    {
        MapCell cell = _model.GetCellAt(worldX, worldY);

        Texture2D cellTexture = SpriteCollectionManager.GetTextureByName("none");

        bool hasBottomBorder = cell == null ? false : cell.HasBorder(HexBorder.Direction.BOTTOM) && startY == 0;
        bool hasUpperBorder = cell == null ? false : cell.HasBorder(HexBorder.Direction.UP) && endY == (_tileResolution - _pixelOffset);
        bool hasUpperLeftBorder = cell == null ? false : cell.HasBorder(HexBorder.Direction.UPPER_LEFT) && startX == 0;
        bool hasLowerLeftBorder = cell == null ? false : cell.HasBorder(HexBorder.Direction.LOWER_LEFT) && startX == 0;
        bool hasUpperRightBorder = cell == null ? false : cell.HasBorder(HexBorder.Direction.UPPER_RIGHT) && endX > _tileResolution * 3 / 4;
        bool hasLowerRightBorder = cell == null ? false : cell.HasBorder(HexBorder.Direction.LOWER_RIGHT) && endX > _tileResolution * 3 / 4;

        if (cellTexture != null)
        {
            for (int j = 0; j < endY - startY; j++)
            {
                bool markTopOrBottomPixel = (j == 0 && hasBottomBorder) || (j == endY - startY - 1 && hasUpperBorder);
                bool markLeftmostPixel = (hasLowerLeftBorder && j + startY < (_tileResolution - _pixelOffset) / 2)
                                        || (hasUpperLeftBorder && j + startY >= (_tileResolution - _pixelOffset) / 2);
                bool markRightmostPixel = (hasLowerRightBorder && j + startY < (_tileResolution - _pixelOffset) / 2)
                                        || (hasUpperRightBorder && j + startY >= (_tileResolution - _pixelOffset) / 2);
                int rightmostPixelIndex = 0;
                for (int i = 0; i < endX - startX; i++)
                {

                    Color pixelColor = cellTexture.GetPixel(i + startX, j + startY);
                    if (pixelColor.a > 0)
                    {
                        bool useBorderColor = markTopOrBottomPixel || markLeftmostPixel;
                        if (useBorderColor)
                        {
                            mapTexture.SetPixel(mapX + i, mapY + j, _borderColor);
                            markLeftmostPixel = false;
                        }
                        rightmostPixelIndex = i;
                    }
                }
                if (markRightmostPixel)
                {
                    mapTexture.SetPixel(mapX + rightmostPixelIndex, mapY + j, _borderColor);
                }
            }
        }
    }

    #endregion

    private void UpdateProvinceViews()
    {
        Game.TurnPhases currentPhase = _model.GetCurrentPhase();
        Faction currentFaction = _model.GetCurrentFaction();
        if (!_isViewValid)
        {
            FileLogger.Trace("VIEW", "Updating Province Views");
            // first, destroy existing army and province views
            DestroyArmyViews();
            DestroyProvinceViews();

            //Debug.Log("Creating province and army views");

            // and now, create appropriate ones
            if (!_isGameOver)
            {
                List<Province> visibleProvinces = _model.GetVisibleProvinces(_worldOffsetX, _worldOffsetY, _width, _height);
                for (int i = 0; i < visibleProvinces.Count; i++)
                {
                    MapCell location = visibleProvinces[i].GetCenterMapCell();
                    ProvinceView view = (ProvinceView)Instantiate(_provincePrefab,
                                                                  new Vector3(location.GetX() - _worldOffsetX - _width / 2,
                                                                              location.GetY() - _worldOffsetY - _height / 2,
                                                                              -1f),
                                                                  Quaternion.identity);
                    view.SetModel(visibleProvinces[i]);
                    FileLogger.Trace("VIEW", "Creating view for province " + view.GetProvince().GetName());
                    view.SetCurrentPhase(currentPhase);
                    view.SetCurrentFaction(currentFaction);
                    view.TrainingInitiated += OnTrainingPopupCreated;
                    _visibleProvinceViews.Add(view);

                    CreateAndAddArmyView(visibleProvinces[i]);
                }
                _isViewValid = true;
            }
        }

        // this should be done anyway - the phase could change and the animations should be updated
        for (int i = 0; i < _visibleProvinceViews.Count; i++)
        {
            _visibleProvinceViews[i].SetCurrentPhase(currentPhase);
            Province province = _visibleProvinceViews[i].GetProvince();
            if (_model.IsProvinceUnderSiege(province))
            {
                MarkProvinceUnderSiege(province, true);
            }
        }

        DestroyArmyMovementOrderViews();
        List<ArmyMovementOrder> orders = _model.GetArmyMovementOrders();
        for (int i = 0; i < orders.Count; i++)
        {
            CreateArmyMovementOrderView(orders[i]);
        }

    }

    private void CreateAndAddArmyView(Province province)
    {
        if (province.GetUnits().Count > 0)
        {
            MapCell location = province.GetCenterMapCell();

            ArmyView armyView = (ArmyView)Instantiate(_armyPrefab,
                                                          new Vector3(location.GetX() - _worldOffsetX - _width / 2,
                                                                      location.GetY() - _worldOffsetY - _height / 2,
                                                                      -1f),
                                                          Quaternion.identity);
            armyView.SetModel(province);
            armyView.AllowMovement(_model.GetCurrentPhase() == Game.TurnPhases.MOVEMENT &&
                                    armyView.GetModel().GetOwnersFaction() == _model.GetCurrentFaction());
            armyView.ImageClicked += OnGarrisonClicked;
            armyView.ViewDragged += OnArmyViewDragged;
            armyView.MouseUp += OnArmyViewDropped;
            _armyViews.Add(armyView);
        }
    }

    private void DestroyProvinceViews()
    {
        FileLogger.Trace("VIEW", "Destroying " + _visibleProvinceViews.Count.ToString() + " province views");
        for (int i = 0; i < _visibleProvinceViews.Count; i++)
        {
            if (_visibleProvinceViews[i] != null)
            {
                _visibleProvinceViews[i].TrainingInitiated -= OnTrainingPopupCreated;
                _visibleProvinceViews[i].Destruct();
                //FileLogger.Trace("GAME", "Destroying view for province " + _visibleProvinceViews[i].GetProvince().GetName());
            }
        }
        _visibleProvinceViews.Clear();
    }

    private void DestroyArmyViews()
    {
        //Debug.Log("Destroying " + _armyViews.Count.ToString() + " army views");
        for (int i = 0; i < _armyViews.Count; i++)
        {
            if (_armyViews[i] != null)
            {
                Destroy(_armyViews[i].gameObject);
            }
        }
        _armyViews.Clear();
    }

    private void InvalidateView()
    {
        _isViewValid = false;
    }

    private void DestroyArmyMovementOrderViews()
    {
        for (int i = 0; i < _armyMovementOrderViews.Count; i++)
        {
            if (_armyMovementOrderViews[i].gameObject != null)
            {
                Destroy(_armyMovementOrderViews[i].gameObject);
            }
        }
        _armyMovementOrderViews.Clear();
    }

    private void CreateArmyMovementOrderView(ArmyMovementOrder order)
    {
        ArmyMovementOrderView view = (ArmyMovementOrderView)Instantiate(_armyMovementOrderViewPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        Vector3 start = new Vector3(order.GetOrigin().GetCenterMapCell().GetX() - _worldOffsetX - _width / 2 + .2f,
                                    order.GetOrigin().GetCenterMapCell().GetY() - _worldOffsetY - _height / 2 - .2f,
                                    -5f);
        Vector3 end = new Vector3(order.GetDestination().GetCenterMapCell().GetX() - _worldOffsetX - _width / 2,
                                    order.GetDestination().GetCenterMapCell().GetY() - _worldOffsetY - _height / 2,
                                    -5f);
        view.SetModel(order, start, end);
        view.ImageClicked += OnMovingArmyImageClicked;
        _armyMovementOrderViews.Add(view);
    }

    private bool SetMapCenter(int x, int y)
    {
        FileLogger.Trace("VIEW", "Set Map Center: x = " + x + ", y = " + y);
        // we want x to be in the center, which means x offset should be about x - width / 2
        // if x is too small, let's limit the offset to -6 in order to show some ocean tiles to the left
        // same if x is too large - the exact value (+8) is selected empirically
        int newWorldOffsetX = Mathf.Max(-6, Mathf.Min(x - 1 - _width / 2, _model.GetMaxX() - _width + 8));
        if (Math.Abs(newWorldOffsetX % 2) == 1)
        {
            newWorldOffsetX++;
        }
        int newWorldOffsetY = Mathf.Max(-6, Mathf.Min(y - 1 - _height / 2, _model.GetMaxY() - _height + 8));

        if (_worldOffsetX == newWorldOffsetX && _worldOffsetY == newWorldOffsetY)
        {
            return false;
        }

        FileLogger.Trace("VIEW", "World Offset: x = " + newWorldOffsetX + ", y = " + newWorldOffsetY);

        _worldOffsetX = newWorldOffsetX;
        _worldOffsetY = newWorldOffsetY;
        InvalidateView();
        return true;
    }

    void Update()
    {
        if (_model.GetCurrentFaction().IsPC())
        {
            if (Input.GetButtonUp("Help"))
            {
                ShowHelp();
            }
            if (Input.GetButtonUp("Menu"))
            {
                FlipMenuDialog();
            }
        }
    }

    private void ShowHelp()
    {
        IgnoreMouseClicks();
        string helpText = "Ran out of things to say. Try again later.";
        switch (_model.GetCurrentPhase())
        {
            case Game.TurnPhases.TRAINING:
                helpText = "Click on a meeple image to bring up troops training screen.\nProvinces where more troops can be trained are marked with an exclamation point over the meeple image.\nUnit training takes one turn.\n\nClick on an army to inspect it.\nMouse over a unit to see its statistics.";
                break;
            case Game.TurnPhases.MOVEMENT:
                helpText = "Drag and drop an army into a neighboring province.\nClick on the army's image afterwards to split troops.\nMore than one army can move into the same province.";
                break;
            case Game.TurnPhases.COMBAT:
                helpText = "Click on the flashing swords icon or the defending garrison in order to start combat.";
                break;
            case Game.TurnPhases.END:
                helpText = "Click on the map to look around.\nAfter you enjoyed the sightseeing, click on Done button to end your turn.\nYou'll receive income and will be able to train more troops the next turn.";
                break;
        }
        _helpText.text = helpText;
        _helpWindow.SetActive(true);
        _currentDialogLevel = DialogLevels.HELP;
        CloseDialogsUpToLevel(_currentDialogLevel);
    }

    public void CloseHelpWindow()
    {
        _helpWindow.SetActive(false);
        _currentDialogLevel = DialogLevels.NONE;
        WatchMouseClicks();
    }

    private void ShowWarningDialog()
    {
        IgnoreMouseClicks();
        _currentDialogLevel = DialogLevels.WARNING;
        CloseDialogsUpToLevel(_currentDialogLevel);
        _warning.SetActive(true);
    }

    public void CloseWarningDailog()
    {
        _warning.SetActive(false);
        _currentDialogLevel = DialogLevels.NONE;
        WatchMouseClicks();
    }

    public void IgnoreWarning()
    {
        CloseWarningDailog();
        DoAdvanceModel();
    }

    public void BribeEnemyTroops()
    {
        _briberyWindow.SetActive(true);
        int funds = _model.GetCurrentFaction().GetMoneyBalance();
        IntIntPair briberyCosts = _model.CalculateBriberyCosts(_unitListView.GetProvince());
        _disbandUnitsCostText.text = PrepareBribeCostText(briberyCosts.first);
        _disbandUnitsButton.interactable = funds >= briberyCosts.first;
        _bribeProvinceCostText.text = PrepareBribeCostText(briberyCosts.second);
        _bribeProvinceButton.interactable = funds >= briberyCosts.second;
    }

    private string PrepareBribeCostText(int cost)
    {
        return "It would cost " + cost + " gold pieces for this army to";
    }

    public void DisbandEnemyTroops()
    {
        _model.DisbandGarrison(_unitListView.GetProvince());
        CloseBriberyWindow();
        _unitListView.gameObject.SetActive(false);
        UpdateResourceBar(true);
    }

    public void BribeProvince()
    {
        if (_model.BribeProvince(_unitListView.GetProvince()))
        {
            for (int i = 0; i < _visibleProvinceViews.Count; i++)
            {
                if (_visibleProvinceViews[i].GetProvince() == _unitListView.GetProvince())
                {
                    _visibleProvinceViews[i].UpdateView();
                    break;
                }
            }
            UpdateResourceBar(true);
        }
        CloseBriberyWindow();
        _unitListView.gameObject.SetActive(false);
    }

    public void CloseBriberyWindow()
    {
        _briberyWindow.SetActive(false);
    }

    #region game events
    private void DisplayGameEvent()
    {
        GameEvent toShow = _model.GetCurrentEvents()[0];
        if (toShow != null)
        {
            // just in case - we don't want to leave an open window
            CloseCurrentDialog();

            if (toShow.GetNumberOfOptions() == 1)
            {
                _currentDialog = _notification;
                _notificationText.text = toShow.GetText();
            }
            else
            {
                _currentDialog = _dialog;
                _dialogText.text = toShow.GetText();
            }
            _currentDialog.SetActive(true);
            _currentDialogLevel = DialogLevels.EVENT;
            CloseDialogsUpToLevel(_currentDialogLevel);
            IgnoreMouseClicks();
        }
    }

    private void CloseCurrentDialog()
    {
        if (_currentDialog != null)
        {
            _currentDialog.SetActive(false);
            _currentDialog = null;
            _currentDialogLevel = DialogLevels.NONE;
            WatchMouseClicks();
        }
    }

    public void OfferAccepted()
    {
        if (_model.GetCurrentEvents().Count > 0)
        {
            _model.ReactToAnEvent(_model.GetCurrentEvents()[0], 0);
        }
        CloseCurrentDialog();
        InvalidateView();
        UpdateProvinceViews();
        UpdateResourceBar(true);
    }

    public void OfferRejected()
    {
        if (_model.GetCurrentEvents().Count > 0)
        {
            _model.ReactToAnEvent(_model.GetCurrentEvents()[0], 1);
        }
        CloseCurrentDialog();
    }
    #endregion

    public void AdvanceModel()
    {
        if (_currentPhaseOrdersNumber == 0 && 
            ((_model.GetCurrentPhase() == Game.TurnPhases.TRAINING  && _model.GetCurrentFaction().GetAvailableManpower() > 0) ||
            _model.GetCurrentPhase() == Game.TurnPhases.MOVEMENT))
        {
            ShowWarningDialog();
        }
        else
        {
            DoAdvanceModel();
        }
    }

    private void DoAdvanceModel()
    {
        FileLogger.Trace("GAME", "Advancing Model From " + _model.GetCurrentPhase());
        if (_currentDialog == null && _model.Advance())
        {
            ProcessCurrentTurnPhase();
        }
    }

    private void ProcessCurrentTurnPhase()
    {
        FileLogger.Trace("GAME", "Processing Current Turn Phase " + _model.GetCurrentPhase());

        if (!_model.GetCurrentFaction().IsPC())
        {
            FileLogger.Trace("GAME", "Not a PC Turn - will process without player's input");
            DisableDoneButton();

            if (_model.GetCurrentPhase() == Game.TurnPhases.START && !_isGameOver)
            {
                FileLogger.Trace("GAME", "Start of a turn and the game is not over");
                _model.Advance();
            }

            /*
            if (_model.GetCurrentPhase() == Game.TurnPhases.COMBAT)
            {
                // invalidate the view to trigger the full update
                // otherwise, movement order views will disappear
                // but provinces' ownership won't be updated
            }
            */

            InvalidateView();
            StartCoroutine("UpdateViewCoroutine");
            return;
        }

        if (_model.GetCurrentEvents().Count > 0)
        {
            UpdateResourceBar(true);
            DisplayGameEvent();
        }
        else
        {
            // NOTE: potentially, we can show a start of the turn screen here
            if (_model.GetCurrentPhase() == Game.TurnPhases.START && !_isGameOver)
            {
                FileLogger.Trace("GAME", "Start of a turn and the game is not over");
                DisableDoneButton();
                InvalidateView();
                _model.Advance();
            }

            // we may have just loaded the strategic map screen after a combat
            // if it was the last combat this turn, move forward to the end of turn phase
            if (_model.GetCurrentPhase() == Game.TurnPhases.COMBAT && !_model.AreThereAnyUnresolvedCombats())
            {
                FileLogger.Trace("GAME", "Combat phase, but there are no unresolved battles");
                _model.Advance();
            }
            // at the end of combat phase, new events could be generated
            if (_model.GetCurrentEvents().Count > 0)
            {
                UpdateResourceBar(true);
                DisplayGameEvent();
            }

            CenterMapOnAnInterestingProvince();
            UpdateView();

            EnableDoneButtonMaybe();
        }
    }

    private void OnTrainingPopupCreated(object sender, System.EventArgs args)
    {
        if (!_ignoreMapClicks && _currentDialogLevel <= DialogLevels.TRAINING)
        {
            IgnoreMouseClicks();
            ProvinceView view = (ProvinceView)sender;
            _currentProvinceView = view;
            _provinceTrainingView.gameObject.SetActive(true);
            _provinceTrainingView.SetModel(view.GetProvince());
            _currentDialogLevel = DialogLevels.TRAINING;
        }
    }

    private void OnTrainingPopupDestroyed(object sender, System.EventArgs args)
    {
        ProvinceTrainingUIView view = (ProvinceTrainingUIView)sender;
        if (view.WasOrderPlaced())
        {
            _currentPhaseOrdersNumber++;
        }
        _currentProvinceView.UpdateManpowerView();
        _currentProvinceView = null;
        _provinceTrainingView.gameObject.SetActive(false);
        _currentDialogLevel = DialogLevels.NONE;
        WatchMouseClicks();
        UpdateResourceBar(true);
    }

    private void OnGarrisonClicked(object sender, System.EventArgs args)
    {
        ArmyView view = (ArmyView)sender;
        Province province = view.GetModel();
        Game.TurnPhases currentPhase = _model.GetCurrentPhase();

        bool showUnitList = true;
        switch(currentPhase)
        {
            case Game.TurnPhases.COMBAT:
                if (_model.IsProvinceUnderSiege(province))
                {
                    showUnitList = false;
                    StartCombat(province);
                }
                break;
            case Game.TurnPhases.MOVEMENT:
                if (province.GetOwnersFaction() == _model.GetCurrentFaction())
                {
                    showUnitList = false;
                }
                break;
        }

        if (_unitListView.gameObject.activeSelf && _unitListView.GetProvince().GetId() == province.GetId())
        {
            _unitListView.gameObject.SetActive(false);
            WatchMouseClicks();
        }
        else
        {
            if (!_ignoreMapClicks && showUnitList && _currentDialogLevel <= DialogLevels.UNITS)
            {
                CreateUnitListView(province, province.GetUnits(), false);
            }
        }
    }

    private void OnMovingArmyImageClicked(object sender, System.EventArgs args)
    {
        if (!_ignoreMapClicks && _currentDialogLevel <= DialogLevels.UNITS)
        {
            ArmyMovementOrderView view = (ArmyMovementOrderView)sender;
            CreateUnitListView(view.GetOriginProvince(), view.GetUnits(), true);
            _currentArmyMovementOrderView = view;
        }
    }

    private void OnUnitListWindowClosed(object sender, System.EventArgs args)
    {
        // just in case
        CloseBriberyWindow();
        UnitListUIView view = (UnitListUIView)sender;
        if (view.HasMovementOrderBeenEdited())
        {
            List<Unit> units = view.GetUnits();
            List<Unit> stillMovingUnits = new List<Unit>();
            int totalQty = 0;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].GetQuantity() > 0)
                {
                    stillMovingUnits.Add(units[i]);
                    totalQty += units[i].GetQuantity();
                }
            }
            _model.UpdateArmyMovementOrderUnits(_currentArmyMovementOrderView.GetModel(), stillMovingUnits, totalQty);
            if (totalQty == 0)
            {
                _currentPhaseOrdersNumber--;
                MarkProvinceUnderSiege(_currentArmyMovementOrderView.GetModel().GetDestination(), false);
                _armyMovementOrderViews.Remove(_currentArmyMovementOrderView);
                Destroy(_currentArmyMovementOrderView.gameObject);
            }

            Province origin = _currentArmyMovementOrderView.GetModel().GetOrigin();
            int armyViewIndex = _armyViews.FindIndex(av => av.GetModel().GetId() == origin.GetId());
            if (armyViewIndex == -1)
            {
                CreateAndAddArmyView(origin);
            }

            _currentArmyMovementOrderView = null;
        }

        _currentDialogLevel = DialogLevels.NONE;
        WatchMouseClicks();
        _unitListView.gameObject.SetActive(false);
    }

    private void CreateUnitListView(Province source, List<Unit> units, bool isMovementOrder)
    {
        if (units.Count > 0)
        {
            _unitListView.gameObject.SetActive(true);

            _unitListView.SetModel(source,
                            units,
                            isMovementOrder && _model.GetCurrentPhase() == Game.TurnPhases.MOVEMENT,
                            _model.CanBribeGarrison(source));

            _currentDialogLevel = DialogLevels.UNITS;
            if (isMovementOrder && _model.GetCurrentPhase() == Game.TurnPhases.MOVEMENT)
            {
                IgnoreMouseClicks();
            }
        }
    }

    private void OnArmyViewDragged(object sender, System.EventArgs args)
    {
        _isAnArmyViewMoving = true;
    }

    private void OnArmyViewDropped(object sender, MouseUpEvent args)
    {
        if (_model.GetCurrentPhase() == Game.TurnPhases.MOVEMENT && _isAnArmyViewMoving)
        {
            // converting view's position to world coordinates
            int x = Mathf.RoundToInt(args.GetPosition().x) + _width / 2 + _worldOffsetX;
            int y = Mathf.RoundToInt(args.GetPosition().y) + _height / 2 + _worldOffsetY;

            ArmyView view = (ArmyView)sender;
            Province start = view.GetModel();
            MapCell endPoint = _model.GetCellAt(x, y);

            List<Unit> garrison = view.GetModel().GetUnits();
            if (garrison.Count > 0 && endPoint != null && endPoint.GetProvince().IsNeighbor(start))
            {
                // if it's a new order (AddArmyMovementOrder returns true), create a view
                ArmyMovementOrder order = new ArmyMovementOrder(start, endPoint.GetProvince(), garrison);
                if (_model.AddArmyMovementOrder(order))
                {
                    CreateArmyMovementOrderView(order);
                    _currentPhaseOrdersNumber++;

                    if (order.IsCombatMove())
                    {
                        MarkProvinceUnderSiege(endPoint.GetProvince(), true);
                    }
                }
            }

            _unitListView.gameObject.SetActive(false);
            _isAnArmyViewMoving = false;
            WatchMouseClicks();
        }
    }

    private void MarkProvinceUnderSiege(Province province, bool flag)
    {
        for (int i = 0; i < _visibleProvinceViews.Count; i++)
        {
            if (_visibleProvinceViews[i].GetProvince().GetId() == province.GetId())
            {
                if (flag)
                {
                    _visibleProvinceViews[i].ShowSiegeIcon(_model.GetCurrentPhase() == Game.TurnPhases.COMBAT);
                    _visibleProvinceViews[i].SiegeImageClicked += OnSiegeIconClicked;
                }
                else
                {
                    _visibleProvinceViews[i].HideSiegeIcon();
                    _visibleProvinceViews[i].SiegeImageClicked -= OnSiegeIconClicked;
                }
                break;
            }
        }
    }

    private void OnSiegeIconClicked(object sender, System.EventArgs args)
    {
        ProvinceView view = (ProvinceView)sender;
        StartCombat(view.GetProvince());
    }

    private void StartCombat(Province province)
    {
        if (_model.GetCurrentPhase() == Game.TurnPhases.COMBAT)
        {
            if (_model.SetUpCombat(province))
            {
                LoadCombatScene();
            }
        }
    }

    private void OnCombatStarted(object sender, EventArgs args)
    {
        LoadCombatScene();
    }

    private void LoadCombatScene()
    {
        DestroyArmyViews();
        DestroyProvinceViews();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Combat");
    }

    private void OnGamePhaseChanged(object sender, System.EventArgs args)
    {
        // allow an army view to be dragged if the current turn phase is MOVEMENT
        // and the owner of the army is the current player
        for (int i = 0; i < _armyViews.Count; i++)
        {
            _armyViews[i].AllowMovement(_model.GetCurrentPhase() == Game.TurnPhases.MOVEMENT &&
                _armyViews[i].GetModel().GetOwnersFaction() == _model.GetCurrentFaction());
        }
        _currentPhaseOrdersNumber = 0;
        _currentProvinceId = 0;
        CenterMapOnAnInterestingProvince();
    }

    private void OnGameWon(object sender, System.EventArgs args)
    {
        //_model.GameWon -= OnGameWon;
        if (!_isGameOver)
        {
            ShowGameOverMessage(true);
        }
    }

    private void OnGameLost(object sender, System.EventArgs args)
    {
        //_model.GameLost -= OnGameLost;
        if (!_isGameOver)
        {
            ShowGameOverMessage(false);
        }
    }

    private void ShowGameOverMessage(bool gameWon)
    {
        _isGameOver = true;
        if (_theEnd == null)
        {
            DestroyArmyViews();
            DestroyProvinceViews();
            DisableDoneButton();
            _theEnd = (GameOverView)Instantiate(_gameOverPrefab, new Vector3(0, 0, -5f), Quaternion.identity);
            _theEnd.SetGameWon(gameWon);
            _theEnd.MessageAcknowledged += OnGameOverMessageAcknowledged;
        }
    }

    private void OnGameOverMessageAcknowledged(object sender, System.EventArgs args)
    {
        ToStart();
    }

    public void SaveAndQuit()
    {
        GameSingleton.Instance.SaveGame();
        Quit();
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void ToStart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Start");
    }

    public void FlipMenuDialog()
    {
        _menu.SetActive(!_menu.activeSelf);
        if (_menu.activeSelf)
        {
            _loadButton.interactable = GameSingleton.Instance.DoesSaveFileExist();
        }
    }

    public void SaveGame()
    {
        GameSingleton.Instance.SaveGame();
        FlipMenuDialog();
    }

    public void LoadGame()
    {
        GameSingleton.Instance.LoadSavedGame();
        if (GameSingleton.Instance.Game != null)
        {
            UnsetModel();
            SetModel(GameSingleton.Instance.Game);
        }
    }

    void OnDestroy()
    {
        UnsetModel();
    }

    void SetModel(Game model)
    {
        _model = model;
        _model.PhaseChanged += OnGamePhaseChanged;
        _model.CombatStarted += OnCombatStarted;
        _model.GameWon += OnGameWon;
        _model.GameLost += OnGameLost;

        //_mouseClickCounter = new MouseClickCounter();

        _provinceTrainingView.gameObject.SetActive(false);
        _provinceTrainingView.UnitTypeInspected += OnUnitTypeInspected;
        _provinceTrainingView.UnitTypeInspectionEnded += OnUnitTypeInspectionEnded;
        _provinceTrainingView.SelectionDone += OnTrainingPopupDestroyed;

        _unitListView.gameObject.SetActive(false);
        _unitListView.WindowClosed += OnUnitListWindowClosed;
        _unitListView.UnitInspected += OnUnitTypeInspected;
        _unitListView.UnitInspectionEnded += OnUnitTypeInspectionEnded;

        _unitTypeView.gameObject.SetActive(false);

        _currentDialogLevel = DialogLevels.NONE;
        _dialog.SetActive(false);
        _notification.SetActive(false);
        _helpWindow.SetActive(false);
        _warning.SetActive(false);

        _menu.SetActive(false);

        _briberyWindow.SetActive(false);

        // in case we are reloading after combat
        _model.ResolveCombatResults();

        InvalidateView();

        // select an appropriate place to center the map
        // the faction's capital or a besieged province, for example
        CenterMapOnAnInterestingProvince();
        //CenterMap();

        // the main loop
        ProcessCurrentTurnPhase();
    }

    void UnsetModel()
    {
        _provinceTrainingView.ForgetModel();
        _provinceTrainingView.UnitTypeInspected -= OnUnitTypeInspected;
        _provinceTrainingView.UnitTypeInspectionEnded -= OnUnitTypeInspectionEnded;
        _provinceTrainingView.SelectionDone -= OnTrainingPopupDestroyed;
        _unitListView.WindowClosed -= OnUnitListWindowClosed;
        _unitListView.UnitInspected -= OnUnitTypeInspected;
        _unitListView.UnitInspectionEnded -= OnUnitTypeInspectionEnded;
        _model.PhaseChanged -= OnGamePhaseChanged;
        _model.CombatStarted -= OnCombatStarted;
        _model.GameWon -= OnGameWon;
        _model.GameLost -= OnGameLost;
        StopAllCoroutines();
    }

    private void DisableDoneButton()
    {
        if (_doneButton != null)
        {
            FileLogger.Trace("VIEW", "Disabling Done Button");
            _doneButton.gameObject.SetActive(false);
        }
    }

    private void EnableDoneButtonMaybe()
    {
        if (_doneButton != null)
        {
            FileLogger.Trace("VIEW", "Enabling Done Button: " + !_isGameOver);
            _doneButton.gameObject.SetActive(!_isGameOver);
        }
    }

    private void IgnoreMouseClicks()
    {
       //Debug.Log("Ignoring the mouse!");
        _ignoreMapClicks = true;
    }

    private void WatchMouseClicks()
    {
        StartCoroutine("EnableMouseClicksCoroutine");
    }

    private IEnumerator EnableMouseClicksCoroutine()
    {
        //Debug.Log("Wait for it...");
        float pause = 0.2f;
        yield return new WaitForSeconds(pause);
        //Debug.Log("Watching the mouse!");
        _ignoreMapClicks = false;
    }

    private void OnUnitTypeInspected(object sender, System.EventArgs args)
    {
        UnitType unitType = (UnitType)sender;
        _unitTypeView.SetModel(unitType);
        _unitTypeView.gameObject.SetActive(true);
    }

    private void OnUnitTypeInspectionEnded(object sender, System.EventArgs args)
    {
        _unitTypeView.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        int x = (int)((Input.mousePosition.x * _widthInPixels / Screen.width - _tileResolution / 4) / _tileResolution * 4 / 3) + _worldOffsetX + 1;
        int y = (int)(Input.mousePosition.y * _heightInPixels / Screen.height / (_tileResolution - _pixelOffset)) + _worldOffsetY + 1;
        if (SetMapCenter(x, y))
        {
            UpdateView();
        }
    }

    private void CloseDialogsUpToLevel(DialogLevels topLevel)
    {
        switch(topLevel)
        {
            case DialogLevels.EVENT:
                _helpWindow.SetActive(false);
                goto case DialogLevels.HELP;
            case DialogLevels.HELP:
                _warning.SetActive(false);
                goto case DialogLevels.WARNING;
            case DialogLevels.WARNING:
                _provinceTrainingView.ForgetModel();
                _provinceTrainingView.gameObject.SetActive(false);
                goto case DialogLevels.TRAINING;
            case DialogLevels.TRAINING:
                _unitListView.gameObject.SetActive(false);
                break;
        }
    }

    private void CenterMapOnAnInterestingProvince()
    {
        // poi = provinces of interest
        List<Province> poi = _model.GetInterestingProvinces();

        // next button is disabled unless there are at least
        // two interesting provinces
        _nextButton.gameObject.SetActive(_model.GetCurrentFaction().IsPC() && poi.Count > 1);
        MapCell mapCenter = null;
        if (poi.Count > 0)
        {
            if (_currentProvinceId >= poi[poi.Count - 1].GetId())
            {
                _currentProvinceId = 0;
            }
            for (int i = 0; i < poi.Count; i++)
            {
                if (poi[i].GetId() > _currentProvinceId)
                {
                    _currentProvinceId = poi[i].GetId();
                    mapCenter = poi[i].GetCenterMapCell();
                    break;
                }
            }
        }
        else
        {
            Province capital = _model.GetCurrentFaction().GetCapital();
            if (capital != null)
            {
                mapCenter = capital.GetCenterMapCell();
            }
        }

        if (mapCenter != null)
        {
            SetMapCenter(mapCenter.GetX(), mapCenter.GetY());
            UpdateView();
        }
    }

    public void Next()
    {
        CenterMapOnAnInterestingProvince();
    }

    private IEnumerator UpdateViewCoroutine()
    {
        CenterMapOnAnInterestingProvince();
        UpdateView(false);
        yield return null;
        DoAdvanceModel();
    }

}
