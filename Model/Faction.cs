/// <summary>
/// Factions (sides) in the game
/// </summary>

using System.Collections.Generic;

public class Faction
{
    // serializable data
    private FactionData _data;

    // cached data - will be re-cached on game load
    private Race _race;
    private Dictionary<int, Province> _provinces;

    // used as a string identifier
    private string _lowercaseName;

    private Strategos _strategicAI;
    private Tactician _tacticalAI;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="data">Serialized faction data</param>
    /// <param name="race">Race of the faction</param>
    /// <param name="unitTypes">Faction's unit types: id => unit type</param>
    public Faction(FactionData data, Race race, Dictionary<int, UnitType> unitTypes)
    {
        _data = data;
        _race = race;
        _lowercaseName = _data.name.Replace(' ', '_').ToLower() + "_faction";
        _provinces = new Dictionary<int, Province>();

        // NOTE: here we can differentiate factions by assigning different AIs
        _strategicAI = new Strategos(this);
        _tacticalAI = new Tactician(_data.level);
    }

	/// <summary>
	/// Get faction's id
	/// </summary>
    /// <returns>Id of the function</returns>
    public int GetId()
    {
        return _data.id;
    }

	/// <summary>
	/// Get faction's name
	/// </summary>
    /// <returns>Name of the function</returns>
    public string GetName()
    {
        return _data.name;
    }

	/// <summary>
	/// Get faction's name in lower case
	/// </summary>
    /// <returns>Lowercased name of the function</returns>
    public string GetNameLowercased()
    {
        return _lowercaseName;
    }

	/// <summary>
	/// Get faction's bonus to province income
	/// </summary>
    /// <returns>Bonus income the faction gets from each province</returns>
    public int GetIncomeBonus()
    {
        return _race.GetIncomeBonus();
    }

	/// <summary>
	/// Get faction's bonus to province manpower
	/// </summary>
    /// <returns>Bonus manpower the faction gets for each province</returns>
    public int GetManpowerBonus()
    {
		/// the bonus is doubled once the faction reaches the Age of`Divine
        return HasReachedAgeOfDivine() ? 2 * _race.GetManpowerBonus() : _race.GetManpowerBonus();
    }

	/// <summary>
	/// Get faction's bonus to province's number of divine favors
	/// </summary>
    /// <returns>Bonus diving favors the faction gets from each province</returns>
    public int GetFavorBonus()
    {
        return _race.GetFavorBonus();
    }

	/// <summary>
	/// Has the faction advanced to the Age of Magic?
	/// </summary>
    /// <returns>Whether the faction advanced to the Age of Magic</returns>
    public bool HasRediscoveredMagic()
    {
        return _data.rediscoveredMagic;
    }

	/// <summary>
	/// Mark the fact that the faction advanced to the Age of Magic
	/// Upgrade relevant unit types
	/// </summary>
    public void RecordMagicRediscovery()
    {
        _data.rediscoveredMagic = true;

        UpgradeUnitTypes(_race.GetAgeOfMagicUnitTypeUpgrades());
    }

    /// <summary>
    /// Upgrade relevant unit types
    /// </summary>
    /// <param name="upgrades">A dictionary of old unit type => new unit type</param>
    private void UpgradeUnitTypes(Dictionary<UnitType, UnitType> upgrades)
    {
        List<Province> provinces = new List<Province>(_provinces.Values);
        for (int i = 0; i < provinces.Count; i++)
        {
            UpgradeUnitTypesInAProvince(provinces[i], upgrades);
        }
    }

    /// <summary>
    /// Upgrade relevant unit types in a province
    /// </summary>
    /// <param name="province">The province in which to upgrade units</param>
    /// <param name="upgrades">A dictionary of old unit type => new unit type</param>
    private void UpgradeUnitTypesInAProvince(Province province, Dictionary<UnitType, UnitType> upgrades)
    {
        List<Unit> garrison = province.GetUnits();
        for (int j = 0; j < garrison.Count; j++)
        {
            UnitType currentUnitType = garrison[j].GetUnitType();
            if (upgrades.ContainsKey(currentUnitType))
            {
                garrison[j].SetUnitType(upgrades[currentUnitType]);
            }
        }
        province.UpgradeTrainableUnits(upgrades);
    }

	/// <summary>
	/// Has the faction advanced to the Age of Divine?
	/// </summary>
    /// <returns>Whether the faction advanced to the Age of Divine</returns>
    public bool HasReachedAgeOfDivine()
    {
        return _data.reachedDivine;
    }

	/// <summary>
	/// Mark the fact that the faction advanced to the Age of Divine
	/// Add divine unit types
	/// Upgrade relevant unit types
	/// </summary>
    public void RecordTheStartOfAgeOfDivine()
    {
        _data.reachedDivine = true;

        Province capital = GetCapital();
        List<UnitType> newUnitTypes = _race.GetUnitTypesJoiningForAgeOfDivine();
        for (int i = 0; i < newUnitTypes.Count; i++)
        {
            capital.AddUnit(new Unit(newUnitTypes[i], 1));
        }

        UpgradeUnitTypes(_race.GetAgeOfDivineUnitTypeUpgrades());
    }

    /// <summary>
    /// Change the province's ownership to this faction
    /// </summary>
    /// <param name="province">The lucky province</param>
    /// <returns>Whether the operation was successful</returns>
    public bool AddProvince(Province province)
    {
        if (_provinces.ContainsKey(province.GetId()))
        {
            return false;
        }
        _provinces[province.GetId()] = province;
        if (_data.capital == 0)
        {
            _data.capital = province.GetId();
        }

        if (HasRediscoveredMagic())
        {
            UpgradeUnitTypesInAProvince(province, _race.GetAgeOfMagicUnitTypeUpgrades());
        }
        if (HasReachedAgeOfDivine())
        {
            UpgradeUnitTypesInAProvince(province, _race.GetAgeOfDivineUnitTypeUpgrades());
        }
        return true;
    }

    /// <summary>
    /// Remove the province's ownership from this faction
	/// In case the province was the faction's capital, choose a new one
    /// </summary>
    /// <param name="province">The unlucky province</param>
    /// <returns>Whether the operation was successful</returns>
    public bool RemoveProvince(Province province)
    {
        int provinceId = province.GetId();
        if (!_provinces.ContainsKey(provinceId))
        {
            return false;
        }
        _provinces.Remove(provinceId);
        if (_data.capital == provinceId)
        {
            SelectNewCapital();
        }
        return true;
    }

    /// <summary>
    /// Get the amount of money the faction has
    /// </summary>
    /// <returns>The amount of money the faction has</returns>
    public int GetMoneyBalance()
    {
        return _data.money;
    }

    /// <summary>
    /// Subtract cost from the faction's money balance
    /// </summary>
    /// <param name="cost">The amount to subtract</param>
    /// <param name="allowNegativeBalance">Whether the money balance can go negative</param>
    /// <returns>Whether the operation was successful</returns>
    public bool SubtractCost(int cost, bool allowNegativeBalance = false)
    {
		/// we can allow the balance go negative if there are enough money sunk into training
		/// so that cancelling some of the training orders will allow the balance get into black
        if (_data.money >= cost || (allowNegativeBalance && HaveSunkenTrainingCosts(cost)))
        {
            _data.money -= cost;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Does the faction have at least this much of sunken training costs?
    /// </summary>
    /// <param name="amount">The amount to match</param>
    /// <returns>Whether the faction has enough money sunk into training</returns>
    private bool HaveSunkenTrainingCosts(int amount)
    {
        int sunkenTrainingCost = 0;
        List<Province> provinces = new List<Province>(_provinces.Values);
        for (int i = 0; i < provinces.Count; i++)
        {
            List<UnitTrainingOrder> orders = new List<UnitTrainingOrder>(provinces[i].GetTrainingQueue().Values);
            for (int j = 0; j < orders.Count; j++)
            {
                sunkenTrainingCost += orders[j].GetCost();
                if (sunkenTrainingCost >= amount)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Get the number of divine favors the faction accumulated
    /// </summary>
    /// <returns>The number of divine favors the faction has</returns>
    public int GetFavors()
    {
        return _data.favors;
    }

    /// <summary>
    /// Collect money and divine favors from the faction's provinces
    /// </summary>
    public void CollectIncome()
    {
        int income = 0;
        int extraFavors = 0;
        foreach (KeyValuePair<int, Province> entry in _provinces)
        {
            income += entry.Value.GetIncome();
            extraFavors += entry.Value.GetFavor();
        }
        _data.money += income;
        _data.favors += extraFavors;
    }

    /// <summary>
    /// Add to the faction's stash of divine favors
    /// </summary>
    /// <param name="amount">The number of divine favors to add</param>
    public void ReceiveFavor(int amount)
    {
        _data.favors += amount;
    }

    /// <summary>
    /// Complete unit training in all faction's provinces
    /// </summary>
    public void CompleteTraining()
    {
        foreach (KeyValuePair<int, Province> entry in _provinces)
        {
            entry.Value.CompleteTraining();
        }
    }

    /// <summary>
    /// Get the province which is the faction's capital
    /// </summary>
    /// <returns>The faction's capital province</returns>
    public Province GetCapital()
    {
        return _provinces[_data.capital];
    }

    /// <summary>
    /// Is the faction playable?
    /// </summary>
    /// <returns>Whether the faction is playable</returns>
    public bool IsPlayable()
    {
        return _data.isPlayable;
    }

    /// <summary>
    /// Allocate money for unit training
	/// If the faction is allowed to get a negative money balance,
	/// the training costs will be subtracted, but the method will return false,
	/// indicating funds shortage
    /// </summary>
    /// <param name="unitType">The type of the units to train</param>
    /// <param name="quantity">The number of units to train</param>
    /// <param name="allowNegativeBalance">Whether the faction can have a negative money balance</param>
    /// <returns>Whether the faction has enough money to cover the cost of training</returns>
    public bool FundUnitTraining(UnitType unitType, int quantity, bool allowNegativeBalance = false)
    {
        int unitCost = unitType.GetTrainingCost() * quantity;
        if (_data.money >= unitCost)
        {
            _data.money -= unitCost;
            return true;
        }
        if (allowNegativeBalance)
        {
            _data.money -= unitCost;
        }
        return false;
    }

    /// <summary>
    /// Get the faction race
    /// </summary>
    /// <returns>The race of the faction's people</returns>
    public Race GetRace()
    {
        return _race;
    }

    /// <summary>
    /// Is this faction a major player in the game?
	/// Major factions train units and wage wars
    /// </summary>
    /// <returns>Whether the faction is a major one</returns>
    public bool IsMajor()
    {
        return _data.isMajor;
    }

    /// <summary>
    /// Is this faction a minor player in the game?
	/// Minor factions are there to be conquered
    /// </summary>
    /// <returns>Whether the faction is a minor one</returns>
    public bool IsMinor()
    {
        return !IsMajor();
    }

    /// <summary>
    /// Is this faction controlled by a human player?
    /// </summary>
    /// <returns>Whether the faction is controlled by a human player</returns>
    public bool IsPC()
    {
        return _data.isPC;
    }

    /// <summary>
    /// Set player's control over the faction
    /// <param name="flag">Whether the faction is controlled by a human player</param>
    /// </summary>
    public void SetIsPC(bool flag)
    {
        _data.isPC = flag;
    }

    /// <summary>
    /// Get combat-level AI the faction uses
    /// </summary>
    /// <returns>Combat-level AI the faction uses</returns>
    public Tactician GetTactician()
    {
        return _tacticalAI;
    }

    /// <summary>
    /// Get strategic-level AI the faction uses
    /// </summary>
    /// <returns>Strategic-level AI the faction uses</returns>
    public Strategos GetStrategos()
    {
        return _strategicAI;
    }

    /// <summary>
    /// Get the level of the stategic AI the faction uses
    /// </summary>
    /// <returns>The level of the stategic AI the faction uses</returns>
    public int GetAILevel()
    {
        return _data.level;
    }

    /// <summary>
    /// Set the level of the stategic AI for the faction to use
    /// </summary>
    /// <param name="level">The level of the stategic AI for the faction to use</param>
    public void SetAILevel(int level)
    {
        _data.level = level;
    }

    /// <summary>
    /// Get the list of provinces the faction controls
    /// </summary>
    /// <returns>The list of provinces the faction controls</returns>
    public Dictionary<int, Province> GetProvinces()
    {
        return _provinces;
    }

    /// <summary>
    /// Get the number of provinces the faction controls
    /// </summary>
    /// <returns>The number of provinces the faction controls</returns>
    public int GetProvinceCount()
    {
        return _provinces.Count;
    }

    /// <summary>
    /// Get the manpower pool's size avalable for training
    /// </summary>
    /// <returns>The manpower pool's size avalable for training</returns>
    public int GetAvailableManpower()
    {
        int result = 0;
        foreach(KeyValuePair<int, Province> entry in _provinces)
        {
            result += entry.Value.GetRemainingManpower();
        }
        return result;
    }

    /// <summary>
    /// Get the total manpower pool's size
    /// </summary>
    /// <returns>The total manpower pool's size</returns>
    public int GetTotalManpower()
    {
        int result = 0;
        foreach (KeyValuePair<int, Province> entry in _provinces)
        {
            result += entry.Value.GetManpower();
        }
        return result;
    }

    /// <summary>
    /// Get the number of expeditions to redizcover magic the faction completed
    /// </summary>
    /// <returns>The number of expeditions to redizcover magic the faction completed</returns>
    public int GetExpeditionsNumber()
    {
        return _data.expeditions;
    }

    /// <summary>
    /// Subtract the cost of funding an expedition from the faction's money balance
    /// </summary>
    /// <param name="cost">The cost of sending the expedition</param>
    /// <returns>Whether the expedition was successfully funded</returns>
    public bool FundExpedition(int cost)
    {
        bool success = SubtractCost(cost, true);
        if (success)
        {
            _data.expeditions++;
        }
        return success;
    }

    /// <summary>
    /// Get the list of arcane spellcasting unit types the faction has
    /// </summary>
    /// <returns>The list of arcane spellcasting unit types the faction has</returns>
    public List<UnitType> GetMagicians()
    {
        List<UnitType> casters = new List<UnitType>();
        List<UnitType> units = GetRace().GetRacialUnits();
        for (int i = 0; i < units.Count; i++)
        {
            if ((units[i].GetSpells().Count > 0 || units[i].GetAttacksForPhase(Combat.TurnPhase.MAGIC).Count > 0) && !units[i].IsHoly())
            {
                casters.Add(units[i]);
            }
        }

        return casters;
    }

    /// <summary>
    /// Get the list of heroic units and provinces where they are located
    /// </summary>
    /// <returns>The list of heroic units and provinces where they are located</returns>
    public List<KeyValuePair<Unit, Province>> GetHeroes()
    {
        List<KeyValuePair<Unit, Province>> heroes = new List<KeyValuePair<Unit, Province>>();
        List<Province> provinces = new List<Province>(_provinces.Values);
        for (int i = 0; i < provinces.Count; i++)
        {
            List<Unit> units = provinces[i].GetUnits();
            for (int j = 0; j < units.Count; j++)
            {
                if (units[j].GetUnitType().IsHero())
                {
                    heroes.Add(new KeyValuePair<Unit, Province>(units[j], provinces[i]));
                }
            }
        }
        return heroes;
    }

    /// <summary>
    /// Create a unit based on its unit type
	/// Take into account unit type upgrades available for the Ages of Magic and Divine
    /// </summary>
    /// <param name="unitType">The unit type to use</param>
    /// <returns>Unit created</returns>
    public Unit CreateUnit(UnitType unitType)
    {
        Unit result = new Unit(unitType, 1);
        Dictionary<UnitType, UnitType> upgrades = _race.GetAgeOfMagicUnitTypeUpgrades();
        if (HasRediscoveredMagic() && upgrades.ContainsKey(unitType))
        {
            unitType = upgrades[unitType];
            result.SetUnitType(unitType);
        }
        upgrades = _race.GetAgeOfDivineUnitTypeUpgrades();
        if (HasReachedAgeOfDivine() && upgrades.ContainsKey(unitType))
        {
            unitType = upgrades[unitType];
            result.SetUnitType(unitType);
        }
        return result;
    }

    /// <summary>
    /// Choose a province to be the new capital
    /// </summary>
    private void SelectNewCapital()
    {
        // there is no need to change the capital
        if (_provinces.ContainsKey(_data.capital))
        {
            return;
        }

        // there is no province to make the capital
        if (_provinces.Count == 0)
        {
            _data.capital = 0;
            return;
        }

        List<Province> provinces = new List<Province>(_provinces.Values);
        // at this point we know that there is at lest one province
        Province bestMatch = provinces[0];

        // chose the one with the highest income
        // NOTE: it's possible to take into account distance from enemies
        // and/or troops on the map as well
        for (int i = 1; i < provinces.Count; i++)
        {
            if (provinces[i].GetBaseIncome() > bestMatch.GetBaseIncome())
            {
                bestMatch = provinces[i];
            }
        }

        _data.capital = bestMatch.GetId();
        return;
    }

}
