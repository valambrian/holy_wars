/// <summary>
/// A geographical unit in the game
/// </summary>

using System;
using System.Collections.Generic;
using UnityEngine;

public class Province
{
    public event EventHandler<EventArgs> GarrisonUpdated;

    //serializabale data
    private ProvinceData _data;

    // cached data
    private Race _dwellers;
    private Faction _owners;
    private MapCell _center;
    private List<Unit> _units;
    private List<Province> _neighbors;

    private Dictionary<UnitType, UnitTrainingOrder> _trainingOrders;

    private List<UnitType> _trainable;
    private List<UnitType> _cheapestToTrainUnits;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="data">Province data loaded from a save</param>
    /// <param name="dwellers">Race that populates the province</param>
    /// <param name="owners">Faction that owns the province</param>
    /// <param name="allUnitTypes">A hash of id => unit type that includes all unit types in the game</param>
    public Province(ProvinceData data, Race dwellers, Faction owners, Dictionary<int, UnitType> allUnitTypes)
    {
        _data = data;
        _dwellers = dwellers;
        _owners = owners;

        if (_data.raceId != dwellers.GetId())
        {
            Debug.Log("Bad race data for province " + _data.id + ": " + _data.raceId + " is changed to " + dwellers.GetId());
            _data.raceId = dwellers.GetId();
        }

        if (_data.factionId != owners.GetId())
        {
            Debug.Log("Bad faction data for province " + _data.id + ": " + _data.factionId + " is changed to " + owners.GetId());
            _data.factionId = owners.GetId();
        }

        _neighbors = new List<Province>();

        _units = new List<Unit>();
        for (int i = 0; i < data.units.Length; i++)
        {
            _units.Add(new Unit(data.units[i], allUnitTypes[data.units[i].id]));
        }

        _trainingOrders = new Dictionary<UnitType, UnitTrainingOrder>();
        if (data.training != null && data.training.Length > 0)
        {
            for (int i = 0; i < data.training.Length; i++)
            {
                UnitTrainingOrderData order = data.training[i];
                // order.id is unit type id just for conspiration
                UnitType unitType = allUnitTypes[order.id];
                _trainingOrders[unitType] = new UnitTrainingOrder(order, allUnitTypes);
            }
        }

        _trainable = new List<UnitType>();
        int minTrainingCost = 100500; // means "a lot"
        // if the province doesn't have defined trainable unit types, use the list of racial units
        if (_data.trainable == null || _data.trainable.Length == 0)
        {
            List<UnitType> racialUnits = _dwellers.GetRacialUnits();
            for (int i = 0; i < racialUnits.Count; i++)
            {
                // heroes can't be trained
                if (racialUnits[i].IsTrainable())
                {
                    _trainable.Add(racialUnits[i]);
                    if (!racialUnits[i].IsHoly())
                    {
                        minTrainingCost = Math.Min(minTrainingCost, racialUnits[i].GetTrainingCost());
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < _data.trainable.Length; i++)
            {
                UnitType unitType = allUnitTypes[_data.trainable[i]];
                if (unitType != null && unitType.IsTrainable())
                {
                    _trainable.Add(unitType);
                    if (!unitType.IsHoly())
                    {
                        minTrainingCost = Math.Min(minTrainingCost, unitType.GetTrainingCost());
                    }
                }
            }
        }

        SelectCheapestToTrainUnits(minTrainingCost);
    }

    #region general

	/// <summary>
	/// Get province id
	/// </summary>
    /// <returns>Id of the province</returns>
    public int GetId()
    {
        return _data.id;
    }

	/// <summary>
	/// Get name of the province
	/// </summary>
    /// <returns>Name of the province</returns>
    public string GetName()
    {
        return _data.name;
    }

	/// <summary>
	/// Get province population's race
	/// </summary>
    /// <returns>Race of the province's population</returns>
    public Race GetDwellersRace()
    {
        return _dwellers;
    }

	/// <summary>
	/// Get the faction that controls the province
	/// </summary>
    /// <returns>The faction that controls the province</returns>
    public Faction GetOwnersFaction()
    {
        return _owners;
    }

    /// <summary>
    /// Set province's new owners
	/// This usually happens upon province conquest
    /// </summary>
    /// <param name="faction">Faction that's taken control over the province</param>
    public void SetOwnersFaction(Faction faction)
    {
        _owners = faction;
        _data.factionId = faction.GetId();
    }

	/// <summary>
	/// Get the province's base income
	/// It can be modified by owners' income bonus
	/// </summary>
    /// <returns>The base income the province generates</returns>
    public int GetBaseIncome()
    {
        return _data.income;
    }

	/// <summary>
	/// Get the base number of divine favor points owning the province generates
	/// It can be modified by owners' bonus to divine favors generation
	/// </summary>
    /// <returns>The base number of divine favor points owning the province generates</returns>
    public int GetBaseFavor()
    {
        return _data.favor;
    }

	/// <summary>
	/// Get the province's income, modified by owners' income bonus
	/// </summary>
    /// <returns>The income the province generates</returns>
    public int GetIncome()
    {
        return _data.income + _owners.GetIncomeBonus();
    }

    public int GetManpower()
    {
        return _data.manpower + _owners.GetManpowerBonus();
    }

	/// <summary>
	/// Get the number of divine favor points owning the province generates
	/// </summary>
    /// <returns>The number of divine favor points owning the province generates</returns>
    public int GetFavor()
    {
        return _data.favor + _owners.GetFavorBonus();
    }

    #endregion

    #region units

    /// <summary>
    /// Adds a unit to the province garrison
    /// </summary>
    /// <param name="unit">The unit to add</param>
    public void AddUnit(Unit unit)
    {
        int unitTypeId = unit.GetUnitType().GetId();
        for (int i = 0; i < _units.Count; i++)
        {
            if (unitTypeId == _units[i].GetUnitType().GetId())
            {
                _units[i].AddQuantity(unit.GetQuantity());
                return;
            }
        }
        _units.Add(unit);

        if (GarrisonUpdated != null)
        {
            GarrisonUpdated(this, new EventArgs());
        }
    }

	/// <summary>
	/// Get the list of units defending the province
	/// </summary>
    /// <returns>The list of units defending the province</returns>
    public List<Unit> GetUnits()
    {
        return _units;
    }

    /// <summary>
    /// Set the list of units defending the province
    /// </summary>
    /// <param name="unit">The unit to use as the province garrison</param>
    public void SetUnits(List<Unit> units)
    {
        _units = units;

        if (GarrisonUpdated != null)
        {
            GarrisonUpdated(this, new EventArgs());
        }
    }

    /// <summary>
    /// Remove units from the province garrison
	/// Either some or all of the units can be removed
    /// </summary>
    /// <param name="unit">The list of units to remove from the province garrison</param>
    public void RemoveUnits(List<Unit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            Unit unit = units[i];
            for (int j = 0; j < _units.Count; j++)
            {
                if (_units[j].GetUnitType() == unit.GetUnitType())
                {
                    _units[j].AddQuantity(-unit.GetQuantity());
                    break;
                }
            }
        }

        _units.RemoveAll(unit => unit.GetQuantity() <= 0);
        if (GarrisonUpdated != null)
        {
            GarrisonUpdated(this, new EventArgs());
        }
    }

    /// <summary>
    /// Add units to the province garrison
    /// </summary>
    /// <param name="unit">The list of units to add to the province garrison</param>
    public void AddUnits(List<Unit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            AddUnit(units[i]);
        }

        if (GarrisonUpdated != null)
        {
            GarrisonUpdated(this, new EventArgs());
        }
    }

	/// <summary>
	/// Get the list of units defending the province as unit stacks
	/// to be used by Combat
	/// </summary>
    /// <returns>The list of unit stacks based on the province garrison</returns>
    public List<UnitStack> GetUnitStacks()
    {
        List<UnitStack> result = new List<UnitStack>();
        for (int i = 0; i < _units.Count; i++)
        {
            result.Add(new UnitStack(new Unit(_units[i]), this));
        }
        return result;
    }

	/// <summary>
	/// Remove all units from the province garrison
	/// </summary>
    public void RemoveAllUnits()
    {
        _units = new List<Unit>();

        if (GarrisonUpdated != null)
        {
            GarrisonUpdated(this, new EventArgs());
        }
    }

    #endregion

    #region training

	/// <summary>
	/// Get the province's remaining manpower
	/// It determines how many units can be added to the training queue
	/// </summary>
    /// <returns>The available manpower pool size</returns>
    public int GetRemainingManpower()
    {
        int result = GetManpower();
        foreach (KeyValuePair<UnitType, UnitTrainingOrder> entry in _trainingOrders)
        {
            result -= entry.Value.GetQuantity();
        }
        return result;
    }

	/// <summary>
	/// Add an order to the training queue
	/// </summary>
    /// <param name="unitType">The type of the unit to train</param>
    /// <param name="quantity">The number of units to train</param>
    /// <param name="standing">Whether the order is standing (repeating)</param>
    /// <returns>Whether the order was successfully processed</returns>
    public bool QueueTraining(UnitType unitType, int quantity, bool standing)
    {
        if (_owners.FundUnitTraining(unitType, quantity))
        {
            _trainingOrders[unitType] = new UnitTrainingOrder(unitType, quantity, standing);
            return true;
        }
        return false;
    }

	/// <summary>
	/// Complete training and add newly trained units to the province garrison
	/// Re-queue standing (repeating) training orders
	/// </summary>
    public void CompleteTraining()
    {
        Dictionary<UnitType, UnitTrainingOrder> _standingOrders = new Dictionary<UnitType, UnitTrainingOrder>();
        foreach (KeyValuePair<UnitType, UnitTrainingOrder> entry in _trainingOrders)
        {
            AddUnit(new Unit(entry.Key, entry.Value.GetQuantity()));

			// note that a unit type could became unavailable since the last turn
			// due to reaching a new Age
            if (entry.Value.IsStanding() && GetTrainableUnits().Contains(entry.Key))
            {
                _standingOrders[entry.Key] = entry.Value;
                _owners.FundUnitTraining(entry.Key, entry.Value.GetQuantity(), true);
            }
        }
        _trainingOrders = _standingOrders;
    }

	/// <summary>
	/// Get a list of unit types that can be trained in the province
	/// </summary>
    /// <returns>The list of unit types that can be trained in the province</returns>
    public List<UnitType> GetTrainableUnits()
    {
        if (_owners.HasReachedAgeOfDivine() && _owners.GetRace() == _dwellers)
        {
            return _trainable;
        }

        List<UnitType> result = new List<UnitType>();
        for (int i = 0; i < _trainable.Count; i++)
        {
            if (!_trainable[i].IsHoly())
            {
                result.Add(_trainable[i]);
            }
        }
        return result;
    }

	/// <summary>
	/// Get a list of least expensive to train unit types
	/// </summary>
    /// <returns>The list of least expensive to train unit types</returns>
    public List<UnitType> GetCheapestToTrainUnits()
    {
        if (_owners.HasReachedAgeOfDivine())
        {
            return _cheapestToTrainUnits;
        }

        List<UnitType> result = new List<UnitType>();
        for (int i = 0; i < _cheapestToTrainUnits.Count; i++)
        {
            if (!_cheapestToTrainUnits[i].IsHoly())
            {
                result.Add(_cheapestToTrainUnits[i]);
            }
        }
        return result;
    }

	/// <summary>
	/// Upgrade the list of trainable unit types
	/// Should be called upon reaching a new Age
	/// </summary>
    /// <param name="upgrades">Hash of existing unit type => upgraded unit type</param>
    public void UpgradeTrainableUnits(Dictionary<UnitType, UnitType> upgrades)
    {
        List<UnitType> cheapest = GetCheapestToTrainUnits();
        if (cheapest.Count > 0)
        {
            int minTrainingCost = GetCheapestToTrainUnits()[0].GetTrainingCost();
            bool hasUpgradeHapened = false;
            for (int i = 0; i < _trainable.Count; i++)
            {
                if (upgrades.ContainsKey(_trainable[i]))
                {
                    _trainable[i] = upgrades[_trainable[i]];
                    minTrainingCost = Math.Min(minTrainingCost, _trainable[i].GetTrainingCost());
                    hasUpgradeHapened = true;
                }
            }
            if (hasUpgradeHapened)
            {
                _trainingOrders = new Dictionary<UnitType, UnitTrainingOrder>();
                SelectCheapestToTrainUnits(minTrainingCost);
            }
        }
    }

	/// <summary>
	/// Populate the list of least expensive to train unit types
	/// </summary>
    /// <param name="cost">The cost of training to match</param>
    private void SelectCheapestToTrainUnits(int cost)
    {
        _cheapestToTrainUnits = new List<UnitType>();
        for (int i = 0; i < _trainable.Count; i++)
        {
            if (_trainable[i].GetTrainingCost() == cost)
            {
                _cheapestToTrainUnits.Add(_trainable[i]);
            }
        }

    }

    /// <summary>
    /// Get province's training orders
    /// </summary>
    /// <returns>Hash of unit type => unit training order</returns>
    public Dictionary<UnitType, UnitTrainingOrder> GetTrainingQueue()
    {
        return _trainingOrders;
    }

    /// <summary>
    /// Cancel unit training orders and get money back
    /// </summary>
    public void RefundTrainingQueue()
    {
        foreach (KeyValuePair<UnitType, UnitTrainingOrder> entry in _trainingOrders)
        {
            _owners.FundUnitTraining(entry.Key, -entry.Value.GetQuantity());
        }
        _trainingOrders.Clear();
    }

    /// <summary>
    /// Cancel unit training orders
    /// </summary>
    public void ClearTrainingQueue()
    {
        _trainingOrders.Clear();
    }

    #endregion

    #region map

    /// <summary>
    /// If the province's center map cell within the rectangle specified?
    /// </summary>
    /// <param name="minX">Min value of the X coordinate of the rectangle</param>
    /// <param name="minY">Min value of the Y coordinate of the rectangle</param>
    /// <param name="width">The width of the rectangle</param>
    /// <param name="height">The height of the rectangle</param>
    /// <returns>Whether the province's center map cell is within the rectangle specified</returns>
    public bool IsCenterWithinRectangle(int minX, int minY, int width, int height)
    {
        if (_center != null
            && _center.GetX() >= minX && _center.GetX() <= minX + width
            && _center.GetY() >= minY && _center.GetY() <= minY + height)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Designate a map cell as the center of the province
    /// </summary>
    /// <param name="centerCell">The map cell to designate as the center of the province</param>
    /// <returns>Whether the operation was successful</returns>
    public bool SetCenterMapCell(MapCell centerCell)
    {
        if (_center != null || centerCell.GetProvinceId() != _data.id)
        {
            return false;
        }
        _center = centerCell;
        return true;
    }

    /// <summary>
    /// Get the center map cell of the province
    /// </summary>
    /// <returns>Central map cell of the province</returns>
    public MapCell GetCenterMapCell()
    {
        return _center;
    }

    /// <summary>
    /// Add a province to the neighbors list
    /// </summary>
    /// <param name="province">A neighboring province</param>
    public void AddNeighbor(Province province)
    {
        if (!_neighbors.Contains(province))
        {
            _neighbors.Add(province);
        }
    }

    /// <summary>
    /// Is the province in question one of our neighbors?
    /// </summary>
    /// <param name="province">Potential neighbor</param>
    /// <returns>Whether the province specified is a neighbor</returns>
    public bool IsNeighbor(Province province)
    {
        return _neighbors.Contains(province);
    }

    /// <summary>
    /// Get the list of neighboring provinces
    /// </summary>
    /// <returns>The list of neighboring provinces</returns>
    public List<Province> GetNeighbors()
    {
        return _neighbors;
    }

    /// <summary>
    /// Get the list of neighboring provinces that can be attacked
	/// At the moment, it's any province that belongs to another faction
    /// </summary>
    /// <returns>The list of neighboring provinces that can be attacked</returns>
    public List<Province> GetTargetableNeighbors()
    {
        List<Province> result = new List<Province>();
        for (int i = 0; i < _neighbors.Count; i++)
        {
            Province neighbor = _neighbors[i];
            if (neighbor.GetOwnersFaction() != _owners)
            {
                result.Add(neighbor);
            }
        }
        return result;
    }

    /// <summary>
    /// Get the list of neighboring provinces that belong to an opposing major faction
    /// </summary>
    /// <returns>The list of neighboring provinces that belong to an opposing major faction</returns>
    public List<Province> GetMajorEnemiesAdjacentProvinces()
    {
        List<Province> result = new List<Province>();
        for (int i = 0; i < _neighbors.Count; i++)
        {
            Province neighbor = _neighbors[i];
            if (neighbor.GetOwnersFaction() != _owners && neighbor.GetOwnersFaction().IsMajor())
            {
                result.Add(neighbor);
            }
        }
        return result;
    }

    /// <summary>
    /// Is this province an inner one?
	/// In other words, do all province's neighbors belong to the same faction?
    /// </summary>
    /// <returns>Whether the province is an inner one</returns>
    public bool IsInnerProvince()
    {
        for (int i = 0; i < _neighbors.Count; i++)
        {
            Province neighbor = _neighbors[i];
            if (neighbor.GetOwnersFaction() != _owners)
            {
                return false;
            }
        }
        return true;
    }


    #endregion

    /// <summary>
    /// Prepare the province data for serialization
    /// </summary>
    public void PrepareForSerialization()
    {
        _data.units = new UnitData[_units.Count];
        for (int i = 0; i < _units.Count; i++)
        {
            _data.units[i] = new UnitData(_units[i].GetUnitType().GetId(), _units[i].GetQuantity());
        }

        List<UnitType> types = new List<UnitType>(_trainingOrders.Keys);
        _data.training = new UnitTrainingOrderData[types.Count];
        for (int i = 0; i < types.Count; i++)
        {
            UnitTrainingOrder order = _trainingOrders[types[i]];
            _data.training[i] = new UnitTrainingOrderData(types[i].GetId(), order.GetQuantity(), order.IsStanding());
        }
    }

}
