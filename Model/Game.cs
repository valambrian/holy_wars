/// <summary>
/// The game model class
/// </summary>

using System;
using System.Collections.Generic;

public class Game
{
    public event EventHandler<EventArgs> PhaseChanged;
    public event EventHandler<EventArgs> CombatStarted;
    public event EventHandler<EventArgs> GameWon;
    public event EventHandler<EventArgs> GameLost;

    public enum SpellId { SUMMON_CHAOS_SPAWN = 1, CONFUSION, MIRROR_IMAGE, MAGIC_SHIELD, STONE_SKIN, HEAL };
    private Dictionary<SpellId, Spell> _spells = new Dictionary<SpellId, Spell>();

    public enum TurnPhases { START, TRAINING, MOVEMENT, COMBAT, END };

    // how many divine favor points does it take to trigger the age of divine
    private const int FAVORS_TO_START_AGE_OF_DIVINE = 200;

    // how many divine favor points does a won battle grant
    private const int COMBAT_WIN_FAVORS = 2;

    // how expensive is it to fund a dungeon exploration expedition
    private const int EXPEDITION_COST = 5;

    // how many expeditions do we plan to have
    private const int EXPEDITION_NUMBER = 2;

    // serializable data
    private GameData _data;

    // game data in a more usable format
    // think of it as a cache
    // unit type id => unit type
    private Dictionary<int, UnitType> _unitTypes;
    // race id => race
    private Dictionary<int, Race> _races;
    private List<Faction> _factions;
    // province id => province
    private Dictionary<int, Province> _provinces;
    // x => { y => map cell }
    private Dictionary<int, Dictionary<int, MapCell>> _cells;

    //NOTE: these guys require a special handling; see GetGameData() for details
    private ArmyMovementOrdersCollection _movementOrders = new ArmyMovementOrdersCollection();
    private KeyValuePair<Unit, Province> _currentExpedition;

    // no need to serialize _currentCombat - no saving is possible during combat
    private Combat _currentCombat;

    // events are also not serialized because it seems to be a hurdle to parse different subtypes
    private List<GameEvent> _currentEvents = new List<GameEvent>();

    private bool _isGameOver = false;

    // max x and y of the map
    // are used by the view
    private int _maxX = int.MinValue;
    private int _maxY = int.MinValue;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="data">Game data loaded from a save</param>
    public Game(GameData data)
    {
        _data = data;
        SetUpSpells();
        SetUpUnitTypes();
        SetUpRaces();
        SetUpFactions();
        SetUpProvinces();
        SetUpMapCells();
        CompleteUnitTypesSetUp(_spells);
        CompleteSpellsSetUp();
        SetUpExpeditionsSchedule();
        SetUpCurrentExpedition();
        SetUpArmyMovementOrders();
    }

    #region setup functions

    /// <summary>
    /// Make the first pass at setting up spells
    /// </summary>
    private void SetUpSpells()
    {
        _spells[SpellId.SUMMON_CHAOS_SPAWN] = new SummonChaosSpawnSpell();
        _spells[SpellId.CONFUSION] = new ConfusionSpell();
        _spells[SpellId.MIRROR_IMAGE] = new MirrorImageSpell();
        _spells[SpellId.MAGIC_SHIELD] = new MagicShieldSpell();
        _spells[SpellId.STONE_SKIN] = new StoneSkinSpell();
        _spells[SpellId.HEAL] = new HealSpell();
    }

    /// <summary>
    /// Finalize spells setup
	/// Depends on finalized unit types
    /// </summary>
    private void CompleteSpellsSetUp()
    {
        ((SummonChaosSpawnSpell)_spells[SpellId.SUMMON_CHAOS_SPAWN]).FinalizeSpell(_unitTypes);
    }

    /// <summary>
    /// Make the first pass at setting up unit types
    /// </summary>
    private void SetUpUnitTypes()
    {
        _unitTypes = new Dictionary<int, UnitType>();
        for (int i = 0; i < _data.units.Length; i++)
        {
            _unitTypes[_data.units[i].id] = new UnitType(_data.units[i], _spells);
        }
    }

    /// <summary>
    /// Finalize unit types setup
	/// Depends on the first pass of spells setup
    /// </summary>
    private void CompleteUnitTypesSetUp(Dictionary<SpellId, Spell> spells)
    {
        List<int> unitTypeIDs = new List<int>(_unitTypes.Keys);
        for (int i = 0; i < unitTypeIDs.Count; i++)
        {
            _unitTypes[unitTypeIDs[i]].SetUpSpellLikeAbilities(spells);
        }
    }

    /// <summary>
    /// Set up races
	/// Depends on the first pass of unit types setup
    /// </summary>
    private void SetUpRaces()
    {
        _races = new Dictionary<int, Race>();
        for (int i = 0; i < _data.races.Length; i++)
        {
            _races[_data.races[i].id] = new Race(_data.races[i], _unitTypes);
        }
    }

    /// <summary>
    /// Set up factions
	/// Depends on races being set up
    /// </summary>
    private void SetUpFactions()
    {
        _factions = new List<Faction>();
        for (int i = 0; i < _data.factions.Length; i++)
        {
            Race race = GetRace(_data.factions[i].race);
            _factions.Add(new Faction(_data.factions[i], race, _unitTypes));
        }
    }

    /// <summary>
    /// Set up provinces
	/// Depends on races and factions being set up
    /// </summary>
    private void SetUpProvinces()
    {
        _provinces = new Dictionary<int, Province>();
        for (int i = 0; i < _data.provinces.Length; i++)
        {
            Race dwellers = GetRace(_data.provinces[i].raceId);
            Faction owners = GetFaction(_data.provinces[i].factionId);
            if (dwellers != null && owners != null)
            {
                Province province = new Province(_data.provinces[i], dwellers, owners, _unitTypes);
                _provinces[_data.provinces[i].id] = province;
                owners.AddProvince(province);
            }
        }
    }

    /// <summary>
    /// Set up map cells
	/// Depends on provinces being set up
    /// </summary>
    private void SetUpMapCells()
    {
        Dictionary<Province, List<MapCell>> provinceCells = new Dictionary<Province, List<MapCell>>();
        _cells = new Dictionary<int, Dictionary<int, MapCell>>();
        for (int i = 0; i < _data.cells.Length; i++)
        {
            Province province = GetProvince(_data.cells[i].provinceId);
            if (province != null)
            {
                int x = _data.cells[i].x;
                int y = _data.cells[i].y;
                if (!_cells.ContainsKey(x))
                {
                    _cells[x] = new Dictionary<int, MapCell>();
                }
                MapCell newCell = new MapCell(_data.cells[i], province);

                // set up cell's neighbors
                HexBorder[] borders = HexBorder.GetBorderDirections(x % 2 == 1);
                for(int j = 0; j < borders.Length; j++)
                {
                    int deltaX = borders[j].GetDeltaX();
                    int deltaY = borders[j].GetDeltaY();
                    MapCell neighbor = GetCellAt(x + deltaX, y + deltaY);
                    if (neighbor != null)
                    {
                        newCell.SetNeighbor(neighbor, deltaX, deltaY);
                        neighbor.SetNeighbor(newCell, -deltaX, -deltaY);
                    }
                }

                _cells[x][y] = newCell;
                _maxX = UnityEngine.Mathf.Max(x, _maxX);
                _maxY = UnityEngine.Mathf.Max(y, _maxY);
                if (newCell.IsCenterOfProvince())
                {
                    province.SetCenterMapCell(newCell);
                }

                if (!provinceCells.ContainsKey(province))
                {
                    provinceCells[province] = new List<MapCell>();
                }
                provinceCells[province].Add(newCell);
            }
        }

        List<Province> provinces = new List<Province>(provinceCells.Keys);
        for (int i = 0; i < provinces.Count; i++)
        {
            Province province = provinces[i];
            double sumX = 0;
            double sumY = 0;
            List<MapCell> cells = provinceCells[province];
            for (int j = 0; j < cells.Count; j++)
            {
                sumX += cells[j].GetX();
                sumY += cells[j].GetY();
            }
            double avgX = sumX / cells.Count;
            double avgY = sumY / cells.Count;
            int index = -1;
            double minDeltaSquared = Double.MaxValue;
            for (int j = 0; j < cells.Count; j++)
            {
                double deltaSquared = (cells[j].GetX() - avgX) * (cells[j].GetX() - avgX) +
                                        (cells[j].GetY() - avgY) * (cells[j].GetY() - avgY);
                if (deltaSquared < minDeltaSquared)
                {
                    index = j;
                    minDeltaSquared = deltaSquared;
                }
            }
            if (index >= 0)
            {
                province.SetCenterMapCell(cells[index]);
            }
        }

    }

    /// <summary>
    /// Set up the schedule for expeditions to rediscover magic
    /// </summary>
    private void SetUpExpeditionsSchedule()
    {
        if (_data.startingExpeditionsTurn == 0)
        {
            _data.startingExpeditionsTurn = Dice.RollDie(3) + 1;
        }

        // now, let's setup expeditions data
        if (_data.expeditions == null || _data.expeditions.Length == 0)
        {
            _data.expeditions = new int[EXPEDITION_NUMBER];
            _data.expeditions[0] = Dice.RollDie(4) + 2;
            _data.expeditions[1] = Dice.RollDie(4) + _data.expeditions[0];

            List<UnitType> casters = new List<UnitType>();
            for (int i = 0; i < _factions.Count; i++)
            {
                if (_factions[i].IsMajor())
                {
                    List<UnitType> factionCasters = _factions[i].GetMagicians();
                    for (int j = 0; j < EXPEDITION_NUMBER; j++)
                    {
                        if (factionCasters.Count > 0)
                        {
                            int index = Dice.RollDie(factionCasters.Count) - 1;
                            casters.Add(factionCasters[index]);
                            factionCasters.RemoveAt(index);
                        }
                    }
                }
            }

            _data.casters = new int[casters.Count];
            for (int i = 0; i < casters.Count; i++)
            {
                _data.casters[i] = casters[i].GetId();
            }
        }
    }

    /// <summary>
    /// Set up the the next expedition
    /// </summary>
    private void SetUpCurrentExpedition()
    {
        // current expedition, if available
        if (_data.currentExpedition != null && _data.currentExpedition.first > 0)
        {
            UnitType heroType = _unitTypes[_data.currentExpedition.first];
            Unit hero = new Unit(heroType, 1);
            Province province = _provinces[_data.currentExpedition.second];
            _currentExpedition = new KeyValuePair<Unit, Province>(hero, province);
        }
    }

    /// <summary>
    /// Set up army movements order based on data loaded from a save
    /// </summary>
    private void SetUpArmyMovementOrders()
    {
        if (_data.movements != null && _data.movements.Length > 0)
        {
            for (int i = 0; i < _data.movements.Length; i++)
            {
                Province start = _provinces[_data.movements[i].start];
                Province end = _provinces[_data.movements[i].end];
                List<Unit> units = new List<Unit>();
                for (int j = 0; j < _data.movements[i].units.Length; j++)
                {
                    int unitTypeId = _data.movements[i].units[j].first;
                    UnitType unitType = _unitTypes[unitTypeId];
                    Unit unit = new Unit(unitType, _data.movements[i].units[j].second);
                    units.Add(unit);
                }
                _movementOrders.AddArmyMovementOrder(new ArmyMovementOrder(start, end, units));
            }
        }
    }

    #endregion

    #region getters

	/// <summary>
	/// Get race based on its id
	/// </summary>
    /// <param name="id">Id of the race</param>
    /// <returns>Race corresponding to the id</returns>
    public Race GetRace(int id)
    {
        if (_races.ContainsKey(id))
        {
            return _races[id];
        }
        return null;
    }

	/// <summary>
	/// Get the list of factions
	/// </summary>
    /// <returns>List of all factions in the game</returns>
    public List<Faction> GetAllFactions()
    {
        return _factions;
    }

	/// <summary>
	/// Get faction based on its id
	/// </summary>
    /// <param name="id">Id of the faction</param>
    /// <returns>Faction corresponding to the id</returns>
    public Faction GetFaction(int id)
    {
        for (int i = 0; i < _factions.Count; i++)
        {
            if (_factions[i].GetId() == id)
            {
                return _factions[i];
            }
        }
        return null;
    }

	/// <summary>
	/// Get province based on its id
	/// </summary>
    /// <param name="id">Id of the province</param>
    /// <returns>Province corresponding to the id</returns>
    public Province GetProvince(int id)
    {
        if (_provinces.ContainsKey(id))
        {
            return _provinces[id];
        }
        return null;
    }

	/// <summary>
	/// Get map cell based on its coordinates
	/// </summary>
    /// <param name="x">X coordinate of the map cell</param>
    /// <param name="y">Y coordinate of the map cell</param>
    /// <returns>Map cell at the (x, y) coordinate (or null if coordinates are invalid)</returns>
    public MapCell GetCellAt(int x, int y)
    {
        if (_cells.ContainsKey(x) && _cells[x].ContainsKey(y))
        {
            return _cells[x][y];
        }
        return null;
    }

	/// <summary>
	/// Get max value of the map's X coordinate
	/// </summary>
    /// <returns>Max value of the map's X coordinate</returns>
    public int GetMaxX()
    {
        return _maxX;
    }

	/// <summary>
	/// Get max value of the map's Y coordinate
	/// </summary>
    /// <returns>Max value of the map's Y coordinate</returns>
    public int GetMaxY()
    {
        return _maxY;
    }

    /// <summary>
    /// Get the list of visible provinces
    /// </summary>
    /// <param name="worldOffsetX">X coordinate of the world offset</param>
    /// <param name="worldOffsetY">Y coordinate of the world offset</param>
    /// <param name="width">Width of the rectangle including visible provinces</param>
    /// <param name="height">Height of the rectangle including visible provinces</param>
    /// <returns>The list of visible provinces</returns>
    public List<Province> GetVisibleProvinces(int worldOffsetX, int worldOffsetY, int width, int height)
    {
        List<Province> result = new List<Province>();
        List<int> provinceIDs = new List<int>(_provinces.Keys);
        for (int i = 0; i < provinceIDs.Count; i++)
        {
            if (_provinces[provinceIDs[i]].IsCenterWithinRectangle(worldOffsetX, worldOffsetY, width, height))
            {
                result.Add(_provinces[provinceIDs[i]]);
            }
        }
        return result;
    }

    /// <summary>
    /// Get the list of provinces relevant for the current turn phase
    /// </summary>
    /// <returns>The list of provinces relevant for the current turn phase</returns>
    public List<Province> GetInterestingProvinces()
    {
        switch(GetCurrentPhase())
        {
            case TurnPhases.TRAINING:
                return GetProvincesWithRemainingManpower();
            case TurnPhases.MOVEMENT:
                return GetGarrisonedProvinces();
            case TurnPhases.COMBAT:
                return GetBesiegedProvinces();
            default:
                return new List<Province>();
        }
    }

    /// <summary>
    /// Get the list of provinces with non-zero available manpower
	/// Relevant during the unit training phase
    /// </summary>
    /// <returns>The list of provinces with non-zero available manpower</returns>
    public List<Province> GetProvincesWithRemainingManpower()
    {
        List<Province> result = new List<Province>();
        List<Province> provinces = new List<Province>(GetCurrentFaction().GetProvinces().Values);
        for (int i = 0; i < provinces.Count; i++)
        {
            if (provinces[i].GetRemainingManpower() > 0 && provinces[i].GetTrainableUnits().Count > 0)
            {
                result.Add(provinces[i]);
            }
        }
        result.Sort((x, y) => x.GetId().CompareTo(y.GetId()));
        return result;
    }

    /// <summary>
    /// Get the list of provinces with armies in them
	/// Relevant during the unit movement phase
    /// </summary>
    /// <returns>The list of provinces with armies in them</returns>
    public List<Province> GetGarrisonedProvinces()
    {
        List<Province> result = new List<Province>();
        List<Province> provinces = new List<Province>(GetCurrentFaction().GetProvinces().Values);
        for (int i = 0; i < provinces.Count; i++)
        {
            if (provinces[i].GetUnits().Count > 0)
            {
                result.Add(provinces[i]);
            }
        }
        result.Sort((x, y) => x.GetId().CompareTo(y.GetId()));
        return result;
    }

    /// <summary>
    /// Get the list of besieged provinces
	/// Relevant during the combat phase
    /// </summary>
    /// <returns>The list of besieged provinces</returns>
    public List<Province> GetBesiegedProvinces()
    {
        List<ArmyMovementOrder> orders = _movementOrders.GetAllOrders();
        Dictionary<int, Province> targets = new Dictionary<int, Province>();
        for (int i = 0; i < orders.Count; i++)
        {
            if (orders[i].IsCombatMove())
            {
                Province destination = orders[i].GetDestination();
                targets[destination.GetId()] = destination;
            }
        }

        List<Province> result = new List<Province>(targets.Values);
        result.Sort((x, y) => x.GetId().CompareTo(y.GetId()));
        return result;
    }

    /// <summary>
    /// Get the current faction
    /// </summary>
    /// <returns>The current faction</returns>
    public Faction GetCurrentFaction()
    {
        return _factions[_data.currentPlayerIndex];
    }

    /// <summary>
    /// Get the current turn number
    /// </summary>
    /// <returns>The current turn number</returns>
    public int GetCurrentTurnNumber()
    {
        return _data.turn;
    }

    /// <summary>
    /// Get the current turn phase
    /// </summary>
    /// <returns>The current turn phase</returns>
    public TurnPhases GetCurrentPhase()
    {
        return (TurnPhases)_data.currentTurnPhase;
    }

    /// <summary>
    /// Get the current combat
    /// </summary>
    /// <returns>The current combat</returns>
    public Combat GetCombat()
    {
        return _currentCombat;
    }

    /// <summary>
    /// Get the list of army movement orders
    /// </summary>
    /// <returns>The list of army movement orders</returns>
    public List<ArmyMovementOrder> GetArmyMovementOrders()
    {
        return _movementOrders.GetAllOrders();
    }

    /// <summary>
    /// Get the number of provinces in the game
    /// </summary>
    /// <returns>The number of provinces in the game</returns>
    public int GetProvinceCount()
    {
        return _provinces.Count;
    }

    /// <summary>
    /// Get game data
	/// It will be serialized and saved
    /// </summary>
    /// <returns>The game data</returns>
    public GameData GetData()
    {
        // provinces need to update training orders in their data
        List<Province> provinces = new List<Province>(_provinces.Values);
        for (int i = 0; i < provinces.Count; i++)
        {
            provinces[i].PrepareForSerialization();
        }

        // current expedition should be converted to a serializable form
        int heroTypeId = _currentExpedition.Key == null ? 0 : _currentExpedition.Key.GetUnitType().GetId();
        int provinceId = _currentExpedition.Value == null ? 0 : _currentExpedition.Value.GetId();
        _data.currentExpedition = new IntIntPair(heroTypeId, provinceId);

        // army movement orders should be converted to a serializable form
        List<ArmyMovementOrder> orders = GetArmyMovementOrders();
        _data.movements = new ArmyMovementOrderData[orders.Count];
        for (int i = 0; i < orders.Count; i++)
        {
            List <Unit> movingTroops = orders[i].GetUnits();
            ArmyMovementOrderData orderData = new ArmyMovementOrderData();
            orderData.start = orders[i].GetOrigin().GetId();
            orderData.end = orders[i].GetDestination().GetId();
            orderData.units = new IntIntPair[movingTroops.Count];
            for (int j = 0; j < movingTroops.Count; j++)
            {
                int unitTypeId = movingTroops[j].GetUnitType().GetId();
                int quantity = movingTroops[j].GetQuantity();
                orderData.units[j] = new IntIntPair(unitTypeId, quantity);
            }
            _data.movements[i] = orderData;
        }

        return _data;
    }

    #endregion

    #region game flow

    /// <summary>
    /// Advance to the next turn phase
    /// </summary>
    /// <returns>Whether advancing to the next turn phase was successful</returns>
    public bool Advance()
    {
        TurnPhases currentPhase = GetCurrentPhase();
        FileLogger.Trace("GAME", "Advancing. Current faction: " + GetCurrentFaction().GetName() + ", phase: " + currentPhase);

        switch (currentPhase)
        {
            case TurnPhases.START:

                if (IsGameOver())
                {
                    return false;
                }

                GetCurrentFaction().CollectIncome();

                FileLogger.Trace("SUMMARY", GetCurrentFaction().GetName() + " can recruit " + GetCurrentFaction().GetTotalManpower() + " units on turn " + _data.turn + ".");
                FileLogger.Trace("SUMMARY", GetCurrentFaction().GetName() + " have " + GetCurrentFaction().GetMoneyBalance() + " gold coins.");
                FileLogger.Trace("SUMMARY", GetCurrentFaction().GetName() + " accumulated " + GetCurrentFaction().GetFavors() + " divine favors.");

                GetCurrentFaction().CompleteTraining();

                GenerateStartOfTurnEvents();

                if (!GetCurrentFaction().IsPC())
                {
                    if (_currentEvents.Count > 0)
                    {
                        while (_currentEvents.Count > 0)
                        {
                            ReactToAnEvent(_currentEvents[0], GetCurrentFaction().GetStrategos().ChooseAnOption(_currentEvents[0]));
                        }
                        _currentEvents.Clear();
                    }
                }
                break;

            case TurnPhases.TRAINING:

                if (!GetCurrentFaction().IsPC())
                {
                    // NOTE: training and movement planning are combined for NPC actions
                    // since this turn's training can depend on the next turn's movement plans
                    List<ArmyMovementOrder> movementOrders = GetCurrentFaction().GetStrategos().SelectMovements();
                    for (int i = 0; i < movementOrders.Count; i++)
                    {
                        AddArmyMovementOrder(movementOrders[i]);
                    }
                }

                if (GetCurrentFaction().GetMoneyBalance() < 0)
                {
                    return false;
                }
                break;

            case TurnPhases.MOVEMENT:
                List<ArmyMovementOrder> orders = _movementOrders.GetAllOrders();
                for (int i = 0; i < orders.Count; i++)
                {
                    if (!orders[i].IsCombatMove())
                    {
                        orders[i].GetDestination().AddUnits(orders[i].GetUnits());
                    }
                }
                _movementOrders.RemoveNonCombatMoveOrders();
                // skip combat phase if there are no combats to resolve
                if (_movementOrders.GetAllOrders().Count == 0)
                {
                    FileLogger.Trace("GAME", "No Combats To Resolve - Skipping The Phase");
                    _data.currentTurnPhase++;

                    // but do generate end of turn events
                    // which are usually created at the end of combat phase
                    GenerateEndOfTurnEvents();
                }
                break;

            case TurnPhases.COMBAT:
                // do not move to the next phase if there are unresolved movement orders
                if (_movementOrders.GetAllOrders().Count > 0)
                {
                    if (!GetCurrentFaction().IsPC())
                    {
                        Province target = _movementOrders.GetAllOrders()[0].GetDestination();
                        SetUpCombat(target);

                        if (!target.GetOwnersFaction().IsPC())
                        {
                            FileLogger.Trace("GAME", "Resolving a NPC-to-NPC combat");
                            // the honest Game doesn't use estimates
                            // it goes through some real dice rolling!
                            _currentCombat.ResolveCombat(false);
                            ResolveCombatResults();
                            return true;
                        }
                        else
                        {
                            if (CombatStarted != null)
                            {
                                CombatStarted(this, EventArgs.Empty);
                            }
                        }
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    GenerateEndOfTurnEvents();
                }
                break;
        }

        if (currentPhase == TurnPhases.END)
        {
            if (_currentEvents.Count > 0)
            {
                if (!GetCurrentFaction().IsPC())
                {
                    while (_currentEvents.Count > 0)
                    {
                        ReactToAnEvent(_currentEvents[0], GetCurrentFaction().GetStrategos().ChooseAnOption(_currentEvents[0]));
                    }
                }
                _currentEvents.Clear();
            }
            else
            {
                _data.currentTurnPhase = 0;
                MoveToTheNextMajorFaction();
            }
        }
        else
        {
            _data.currentTurnPhase++;
        }

        FileLogger.Trace("GAME", "Current faction: " + GetCurrentFaction().GetName() + ", phase: " + GetCurrentPhase());

        if (PhaseChanged != null)
        {
            PhaseChanged(this, new EventArgs());
        }
        return true;
    }

    #endregion

    #region movement orders

    /// <summary>
    /// Add an army movement order to the current collection of orders
    /// </summary>
    /// <param name="order">The new army movement order</param>
    /// <returns>Whether the order was added successfully</returns>
    public bool AddArmyMovementOrder(ArmyMovementOrder order)
    {
        // clone order's units as removing them from their location
        // may result in the order's unit list to become empty
        List<Unit> orderUnits = order.GetUnits();
        List<Unit> movingUnits = new List<Unit>();
        for (int i = 0; i < orderUnits.Count; i++)
        {
            movingUnits.Add(new Unit(orderUnits[i]));
        }

        order.GetOrigin().RemoveUnits(order.GetUnits());

        order.SetUnits(movingUnits);
        return _movementOrders.AddArmyMovementOrder(order);
    }

    /// <summary>
    /// Update an existing army movement order with a new list of units
    /// </summary>
    /// <param name="order">The army movement order</param>
    /// <param name="newUnits">The list of units to include into the army movement order</param>
    /// <param name="totalQty">The total number of combatants among the new units</param>
    /// totalQty is added to save time calculating it - the caller can just pass it in
    public void UpdateArmyMovementOrderUnits(ArmyMovementOrder order, List<Unit> newUnits, int totalQty)
    {
        // return units back to the province they were leaving
        order.GetOrigin().AddUnits(order.GetUnits());

        if (totalQty > 0)
        {
            // remove new units from the provine and add them to the move order
            order.GetOrigin().RemoveUnits(newUnits);
            order.SetUnits(newUnits);
        }
        else
        {
            // no units involved - just nix the order
            _movementOrders.GetAllOrders().Remove(order);
        }
    }

    #endregion

    #region combat

	/// <summary>
	/// Are there any unresolved combats for this turn?
	/// </summary>
    /// <returns>Whether there are any combats to resolve</returns>
    public bool AreThereAnyUnresolvedCombats()
    {
        return _movementOrders.DoesIncludeCombatMoves();
    }

	/// <summary>
	/// Is the province besieged?
	/// </summary>
    /// <param name="province">The province in question</param>
    /// <returns>Whether the province is under siege</returns>
    public bool IsProvinceUnderSiege(Province province)
    {
        List<ArmyMovementOrder> orders = _movementOrders.GetAllOrders();
        for (int i = 0; i < orders.Count; i++)
        {
            if (orders[i].IsCombatMove() && orders[i].GetDestination() == province)
            {
                return true;
            }
        }
        return false;
    }

	/// <summary>
	/// Get a province under siege (any province)
	/// </summary>
    /// <returns>A province is under siege (or null if no provinces are under siege)</returns>
    public Province GetAProvinceUnderSiege()
    {
        Province result = null;
        List<ArmyMovementOrder> orders = _movementOrders.GetAllOrders();
        for (int i = 0; i < orders.Count; i++)
        {
            if (orders[i].IsCombatMove())
            {
                result = orders[i].GetDestination();
                break;
            }
        }
        return result;
    }

	/// <summary>
	/// Set up an instance of Combat class based on the province attacked
	/// </summary>
    /// <param name="province">The province in question</param>
    /// <returns>Whether the combat was set up successfully</returns>
    public bool SetUpCombat(Province province)
    {
        if (_currentCombat != null)
        {
            return false;
        }
        List<UnitStack> attackers = new List<UnitStack>();
        Dictionary<UnitType, int> attackersLookup = new Dictionary<UnitType, int>();

        List<ArmyMovementOrder> orders = _movementOrders.GetAllOrders();
        for (int i = 0; i < orders.Count; i++)
        {
            if (orders[i].GetDestination() == province)
            {
                List<Unit> units = orders[i].GetUnits();
                for (int j = 0; j < units.Count; j++)
                {
                    UnitType attackerUnitType = units[j].GetUnitType();
                    if (attackersLookup.ContainsKey(attackerUnitType))
                    {
                        int stackIndex = attackersLookup[attackerUnitType];
                        attackers[stackIndex].ReinforceStack(units[j].GetQuantity());
                    }
                    else
                    {
                        attackers.Add(new UnitStack(units[j], orders[i].GetOrigin()));
                        attackersLookup[attackerUnitType] = attackers.Count - 1;
                    }
                }
            }
        }

        List<UnitStack> defenders = new List<UnitStack>();

        List<Unit> garrison = province.GetUnits();
        for (int j = 0; j < garrison.Count; j++)
        {
            defenders.Add(new UnitStack(garrison[j], province));
        }

        _currentCombat = new Combat(province, attackers, defenders, true);
        return true;
    }

	/// <summary>
	/// Update the game state based on the results of the combat just resolved
	/// </summary>
    public void ResolveCombatResults()
    {
        if (_currentCombat != null)
        {
            FileLogger.Trace("GAME", "Resolving combat: updating province ownership and unit stacks");
            Province battlefield = _currentCombat.GetProvince();
            List<UnitStack> attackers = _currentCombat.GetAttackers();
            List<UnitStack> defenders = _currentCombat.GetDefenders();
            if (defenders.Count == 0)
            {
                // attackers won
                List<Unit> survivors = new List<Unit>();
                if (attackers.Count > 0)
                {
                    Faction newOwners = attackers[0].GetProvinceToRetreat().GetOwnersFaction();
                    for (int i = 0; i < attackers.Count; i++)
                    {
                        survivors.Add(attackers[i].GetBaseUnit());
                    }
                    ChangeProvinceOwnership(battlefield, newOwners);
                    newOwners.ReceiveFavor(COMBAT_WIN_FAVORS);
                }
                // if both attackers and defenders lists are empty, it's kind of a draw
                battlefield.SetUnits(survivors);
            }
            else
            {
                // attackers lost or retreated
                List<Unit> survivors = new List<Unit>();
                for (int i = 0; i < defenders.Count; i++)
                {
                    survivors.Add(defenders[i].GetBaseUnit());
                }
                battlefield.SetUnits(survivors);

                for (int i = 0; i < attackers.Count; i++)
                {
                    attackers[i].GetProvinceToRetreat().AddUnit(attackers[i].GetBaseUnit());
                }
            }

            // no matter who won, remove movement orders that were targeting the battlefield province
            _movementOrders.RemoveOrdersTargetingProvince(battlefield);

            _currentCombat = null;
        }
    }

    #endregion

    #region game events

	/// <summary>
	/// Generate events that happen at the start of a turn
	/// </summary>
    private void GenerateStartOfTurnEvents()
    {
        _currentExpedition = new KeyValuePair<Unit, Province>(null, null);
        Faction currentFaction = GetCurrentFaction();

        if (currentFaction.GetFavors() >= FAVORS_TO_START_AGE_OF_DIVINE &&
            !currentFaction.HasReachedAgeOfDivine())
        {
            currentFaction.RecordTheStartOfAgeOfDivine();
            string message = "The age of divine has arrived!";
            _currentEvents.Add(new GameEvent(message, 1));
            // no more than one event per turn
            return;
        }

        // if turn is greater than the starting expeditions turn
        // and the faction has at least one hero
        // and the number of funded expeditions is less than max
        // then create a new dungeon exploration game event
        // in 90% of cases

        if (_data.turn > _data.startingExpeditionsTurn &&
            Dice.RollDie(10) > 1 &&
            currentFaction.GetExpeditionsNumber() < _data.expeditions[EXPEDITION_NUMBER - 1])
        {
            List<KeyValuePair<Unit, Province>> heroes = GetCurrentFaction().GetHeroes();
            if (heroes.Count > 0)
            {
                int random = Dice.RollDie(heroes.Count) - 1;
                ExpeditionEvent expedition = new ExpeditionEvent(heroes[random], EXPEDITION_COST);
                _currentEvents.Add(expedition);
            }
        }
    }

	/// <summary>
	/// Generate events that happen at the end of a turn
	/// </summary>
    private void GenerateEndOfTurnEvents()
    {
        if (_currentExpedition.Key != null && _currentExpedition.Value != null)
        {
            Province province = GetCurrentFaction().GetCapital();
            if (province == null)
            {
                province = _currentExpedition.Value;
            }

            // return the exporing hero
            province.AddUnit(_currentExpedition.Key);

            int expeditionsFunded = GetCurrentFaction().GetExpeditionsNumber();

            // index < 0 means mismatch, no cigar
            int index = -1;
            for (int i = 0; i < EXPEDITION_NUMBER; i++)
            {
                if (expeditionsFunded == _data.expeditions[i])
                {
                    index = i;
                    break;
                }
            }

            // the expedition is a success
            // a new hero is joining the worthy cause
            if (index >= 0)
            {
                List<UnitType> candidates = GetCurrentFaction().GetMagicians();

                for (int i = 0; i < _data.casters.Length; i++)
                {
                    UnitType caster = _unitTypes[_data.casters[i]];
                    if (candidates.Contains(caster))
                    {
                        if (index == 0)
                        {
                            Unit newHero = GetCurrentFaction().CreateUnit(caster);
                            province.AddUnit(newHero);
                            string message = newHero.GetUnitType().GetName() + " emerges in " + province.GetName() + "!";
                            _currentEvents.Add(new GameEvent(message, 1));
                            break;
                        }
                        else
                        {
                            index--;
                        }
                    }
                }
                if (!GetCurrentFaction().HasRediscoveredMagic())
                {
                    GetCurrentFaction().RecordMagicRediscovery();
                }
            }
            else
            {
                string message = "This expedition was a waste of money, but the next one will surely be a great success!";
                _currentEvents.Add(new GameEvent(message, 1));
            }

        }
    }

	/// <summary>
	/// Get the list of current events
	/// </summary>
    /// <returns>The list of current events</returns>
    public List<GameEvent> GetCurrentEvents()
    {
        return _currentEvents;
    }

	/// <summary>
	/// Resolve an event based on the option selected
	/// </summary>
    /// <param name="gameEvent">The game event to resolve</param>
    /// <param name="option">Option selected</param>
    public void ReactToAnEvent(GameEvent gameEvent, int option)
    {
        if (gameEvent is ExpeditionEvent && option == 0)
        {
            ExpeditionEvent trueEvent = (ExpeditionEvent)gameEvent;
            if(GetCurrentFaction().FundExpedition(trueEvent.GetCost()))
            {
                KeyValuePair<Unit, Province> details = trueEvent.GetExpeditionDetails();
                Unit exploringHero = new Unit(details.Key);
                _currentExpedition = new KeyValuePair<Unit, Province>(exploringHero, details.Value);
                List<Unit> expeditionMembers = new List<Unit>();
                expeditionMembers.Add(exploringHero);
                details.Value.RemoveUnits(expeditionMembers);
            }
        }

        _currentEvents.Remove(gameEvent);
    }

    #endregion

    #region bribery

	/// <summary>
	/// Can a province garrison be bribed?
	/// </summary>
    /// <param name="province">The garrisoned province</param>
    /// <returns>Whether the current player can bribe garrison of this province</returns>
    public bool CanBribeGarrison(Province province)
    {
        List<Unit> garrison = province.GetUnits();

        // NOTE: may need to add BRIBING phase
        if (GetCurrentPhase() != TurnPhases.TRAINING ||           // only possible during the training phase
            !GetCurrentFaction().GetRace().CanBribe() ||          // current player can't bribe
            GetCurrentFaction() == province.GetOwnersFaction() || // it's an army in a player's province
            garrison.Count == 0)                                  // no units to bribe
        {
            return false;
        }

        // can't bribe heroes
        for (int i = 0; i < garrison.Count; i++)
        {
            if (garrison[i].GetUnitType().IsHero())
            {
                return false;
            }
        }

        // the more calculation intensive check - is this a neighboring province?
        List<Province> neighbors = province.GetNeighbors();
        for (int i = 0; i < neighbors.Count; i++)
        {
            if (neighbors[i].GetOwnersFaction() == GetCurrentFaction())
            {
                // found a neighbor the current player owns
                return true;
            }
        }
        // none of the neighbors belongs to the current player
        return false;
    }

	/// <summary>
	/// Can a province garrison be bribed?
	/// </summary>
    /// <param name="province">The garrisoned province</param>
    /// <returns>A tuple containing the cost of disbanding the garrison and the cost of it changing sides</returns>
    public IntIntPair CalculateBriberyCosts(Province province)
    {
        List<Unit> garrison = province.GetUnits();
        int cost = 0;
        for (int i = 0; i < garrison.Count; i++)
        {
            // NOTE: a better way could be to take into account
            // the race of the unit and its holiness
            cost += garrison[i].GetTrainingCost();
        }

        return new IntIntPair(cost, 2*cost + GameUtils.CalculateProvinceValue(province, GetCurrentFaction()));
    }

	/// <summary>
	/// Bribe a province garrison to disband
	/// </summary>
    /// <param name="province">The garrisoned province</param>
    /// <returns>Whether the opration was successful</returns>
    public bool DisbandGarrison(Province province)
    {
        IntIntPair costs = CalculateBriberyCosts(province);
        if (GetCurrentFaction().SubtractCost(costs.first))
        {
            province.RemoveAllUnits();
            return true;
        }
        return false;
    }

	/// <summary>
	/// Bribe a province garrison to change sides
	/// </summary>
    /// <param name="province">The garrisoned province</param>
    /// <returns>Whether the opration was successful</returns>
    public bool BribeProvince(Province province)
    {
        IntIntPair costs = CalculateBriberyCosts(province);
        if (GetCurrentFaction().SubtractCost(costs.second))
        {
            ChangeProvinceOwnership(province, GetCurrentFaction());
            return true;
        }
        return false;
    }

    #endregion

    #region private functionality
	/// <summary>
	/// Transfer province ownership to a new faction
	/// </summary>
    /// <param name="province">The province in question</param>
    /// <param name="newOwners">The faction which will control the province</param>
    private void ChangeProvinceOwnership(Province province, Faction newOwners)
    {
        FileLogger.Trace("SUMMARY", province.GetName() + "'s ownership changes from " + province.GetOwnersFaction().GetName() + " to " + newOwners.GetName());
        province.GetOwnersFaction().RemoveProvince(province);
        province.ClearTrainingQueue();
        province.SetOwnersFaction(newOwners);
        newOwners.AddProvince(province);

        // NOTE: there may be a better way to prevent Guardians from suddenly taking over all minor human provinces
        if (newOwners.IsPlayable())
        {
            Race newOwnersRace = newOwners.GetRace();
            List<Province> neighbors = province.GetNeighbors();
            for(int i = 0; i < neighbors.Count; i++)
            {
                Province neighbor = neighbors[i];
                if (neighbor.GetDwellersRace() == newOwnersRace && neighbor.GetOwnersFaction().IsMinor())
                {
                    ChangeProvinceOwnership(neighbor, newOwners);
                }
            }
        }

    }

	/// <summary>
	/// Is the game over yet?
	/// </summary>
    /// <returns>Whether the game is over (either won or lost)</returns>
    private bool IsGameOver()
    {
        if (GetCurrentFaction().GetProvinceCount() < 1 && GetCurrentFaction().IsPC())
        {
            if (!_isGameOver && GameLost != null)
            {
                GameLost(this, new EventArgs());
            }
            _isGameOver = true;
            return true;
        }
        if (2 * GetCurrentFaction().GetProvinceCount() >= _provinces.Count && GetCurrentFaction().IsPC()
            && GetCurrentFaction().GetProvinceCount() > 1)
        {
            if (!_isGameOver && GameWon != null)
            {
                GameWon(this, new EventArgs());
            }
            _isGameOver = true;
            return true;
        }
        return false;
    }

	/// <summary>
	/// Move the game turn to the next major faction
	/// </summary>
    private void MoveToTheNextMajorFaction()
    {
        _data.currentPlayerIndex++;
        while (_data.currentPlayerIndex >= _factions.Count
                || !GetCurrentFaction().IsMajor()
                || GetCurrentFaction().GetProvinces().Count == 0)
        {
            if (_data.currentPlayerIndex >= _factions.Count)
            {
                _data.currentPlayerIndex = 0;
                _data.turn++;
            }
            else
            {
                _data.currentPlayerIndex++;
            }
        }
    }

    #endregion
}
