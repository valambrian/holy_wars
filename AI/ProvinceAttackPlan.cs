/// <summary>
/// Representation of an attack plan
/// Contains the target province, the list of army movement orders, and derived data items
/// </summary>

using System.Collections.Generic;

public class ProvinceAttackPlan
{
    public enum State { INCOMPLETE, NOT_REVIEWED, ACCEPTED, REJECTED };

	// the target of the attack
    private Province _province;
	// how valuable is the target province to AI?
    private int _provinceValue;
	// how much money will the defender lose if all troops will be wiped out?
    private int _defendersTrainingCost;
	// expected gain (based on the province value and the win ratio)
    private double _gain;
	// expected enemy loss (based on the defenders' training cost and the win ratio)
    private double _enemyLoss;
	// expected attacker's loss (based on the attackers' training cost and the win ratio)
    private double _loss;
	// combat win ratio (based on the combat simulations run during the analysis)
    private double _winRatio;
	// a list of movement (read: attack) orders targeting the province
    private List<ArmyMovementOrder> _orders;
	// a list of combat simulations run for the plan evaluation
    private List<Combat> _combatSimulations;
	// state of the attack plan (see the enum's values)
    private State _state;
	// attacks involving heroes require more simulations to run
    private bool _areHeroesInvolved;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="province">The target province</param>
    /// <param name="provinceValue">Value of the province</param>
    public ProvinceAttackPlan(Province province, int provinceValue)
    {
        _province = province;
        _provinceValue = provinceValue;
        _orders = new List<ArmyMovementOrder>();
        _combatSimulations = new List<Combat>();
        _state = State.INCOMPLETE;
        _areHeroesInvolved = false;

        // we are not interested in minor factions' troops losses,
        // only the major players'
        if (_province.GetOwnersFaction().IsPlayable())
        {
            List<Unit> defenders = _province.GetUnits();
            for (int i = 0; i < defenders.Count; i++)
            {
                _areHeroesInvolved = _areHeroesInvolved || defenders[i].GetUnitType().IsHero();
                _defendersTrainingCost += defenders[i].GetTrainingCost();
            }
        }
    }

    /// <summary>
    /// Copy constructor
    /// </summary>
    /// <param name="toClone">The original plan to be copied</param>
    public ProvinceAttackPlan(ProvinceAttackPlan toClone)
    {
        _province = toClone._province;
        _provinceValue = toClone._provinceValue;
        _orders = new List<ArmyMovementOrder>();
        for (int i = 0; i < toClone._orders.Count; i++)
        {
            _orders.Add(toClone._orders[i]);
        }
        _combatSimulations = new List<Combat>();
        _state = State.INCOMPLETE;
        _areHeroesInvolved = toClone._areHeroesInvolved;
        _defendersTrainingCost = toClone._defendersTrainingCost;
    }

    /// <summary>
    /// Target province getter
    /// </summary>
    /// <returns>The target of the attack plan</returns>
    public Province GetTargetProvince()
    {
        return _province;
    }

    /// <summary>
    /// Target province value getter
    /// </summary>
    /// <returns>The value of the target province</returns>
    public int GetProvinceValue()
    {
        return _provinceValue;
    }

    /// <summary>
    /// Province attack plan's score (numeric value)
	/// Used for choosing the best plan out of all available
    /// </summary>
    /// <returns>The value of the target province</returns>
    public double GetScore()
    {
        return _gain + _enemyLoss - _loss;
    }

    /// <summary>
    /// Defenders' training cost getter
    /// </summary>
    /// <returns>Money spent on training defenders</returns>
    public int GetDefendersTrainingCost()
    {
        return _defendersTrainingCost;
    }

    /// <summary>
    /// Win ratio getter
    /// </summary>
    /// <returns>Percentage of the won combat simulations</returns>
    public double GetWinRatio()
    {
        return _winRatio;
    }

    /// <summary>
    /// Attacker losses getter
	/// Based on combat simulations run
    /// </summary>
    /// <returns>Training costs of attackers expected to be lost</returns>
    public double GetLoss()
    {
        return _loss;
    }

    /// <summary>
    /// Was the plan selected for execution?
    /// </summary>
    /// <returns>Whether the plan was evaluated and accepted</returns>
    public bool IsSelected()
    {
        return _state == State.ACCEPTED;
    }

    /// <summary>
    /// Was the plan rejected?
    /// </summary>
    /// <returns>Whether the plan was evaluated and rejected</returns>
    public bool IsRejected()
    {
        return _state == State.REJECTED;
    }

    /// <summary>
    /// Is the plan still waiting to be evaluated?
    /// </summary>
    /// <returns>Whether the plan wasn't evaluated yet</returns>
    public bool IsIncomplete()
    {
        return _state == State.INCOMPLETE;
    }

    /// <summary>
    /// Movement orders getter
    /// </summary>
    /// <returns>List of army movement orders comprising the attack plan</returns>
    public List<ArmyMovementOrder> GetMovementOrders()
    {
        return _orders;
    }

    /// <summary>
    /// Combat simulations getter
    /// </summary>
    /// <returns>List of combat simulations ran during plan evaluation</returns>
    public List<Combat> GetCombatSimulations()
    {
        return _combatSimulations;
    }

    /// <summary>
    /// Attack plan's gain setter
    /// </summary>
    /// <param name="gain">Gain value to be used in score calculation</param>
    public void SetGain(double gain)
    {
        _gain = gain;
    }

    /// <summary>
    /// Attack plan's loss setter
    /// </summary>
    /// <param name="loss">Loss value to be used in score calculation</param>
    public void SetLoss(double loss)
    {
        _loss = loss;
    }

    /// <summary>
    /// Attack plan's enemy loss setter
    /// </summary>
    /// <param name="loss">Enemy's loss value to be used in score calculation</param>
    public void SetEnemyLoss(double loss)
    {
        _enemyLoss = loss;
    }

    /// <summary>
    /// Win ration setter
    /// </summary>
    /// <param name="winRatio">Combat simulations' win ratio</param>
    public void SetWinRatio(double winRatio)
    {
        _winRatio = winRatio;
    }

    /// <summary>
    /// Mark the attack plan as accepted
    /// </summary>
    public void MarkAccepted()
    {
        _state = State.ACCEPTED;
    }

    /// <summary>
    /// Mark the attack plan as rejected
    /// </summary>
    public void MarkRejected()
    {
        _state = State.REJECTED;
    }

    /// <summary>
    /// Add an army movement order to the attack plan
    /// </summary>
    /// <param name="order">Movement order for an army joining the attack</param>
    public void AddMovementOrder(ArmyMovementOrder order)
    {
        if (_province == order.GetDestination())
        {
            _orders.Add(order);
            if (!_areHeroesInvolved)
            {
                List<Unit> attackers = order.GetUnits();
                for (int i = 0; i < attackers.Count; i++)
                {
                    _areHeroesInvolved = _areHeroesInvolved || attackers[i].GetUnitType().IsHero();
                }
            }
        }
        else
        {
            FileLogger.Error("AI", "Trying to add an order to move to " + order.GetDestination().GetName() + " to a plan to attack " + _province.GetName() );
        }
    }

    /// <summary>
    /// Add a combat simulation to the collection
    /// </summary>
    /// <param name="simulation">Combat simulation ran for the attack plan</param>
    public void AddCombatSimulation(Combat simulation)
    {
        _combatSimulations.Add(simulation);
    }

    /// <summary>
    /// Is the plan compatible with the list of army movements under consideration?
    /// </summary>
    /// <returns>Whether the plan can coexist with the list of army movement orders</returns>
    public bool IsCompatibleWith(List<ArmyMovementOrder> movements)
    {
        // NOTE: for now, it's assumed that all troops from the origin province
        // participate in the attack, so if there is a common origin province
        // between two attack plans, they are incompatible

        // NOTE: or the target province, for this matter
        // because if another attack plan has the best score,
        // there is probably no need to add troops to it,
        // and they can be used in another attack

        bool result = true;
        List<Province> origins = new List<Province>();
        Dictionary<int, Province> destinations = new Dictionary<int, Province>();
		// parse origin and destination provinces out of the orders being considered
        for (int i = 0; i < movements.Count; i++)
        {
            origins.Add(movements[i].GetOrigin());
            destinations[movements[i].GetDestination().GetId()] = movements[i].GetDestination();
        }
		// check if there is an overlap between the plan's own orders
		// and the orders being considered
        for (int i = 0; i < _orders.Count; i++)
        {
            if (origins.Contains(_orders[i].GetOrigin()) || destinations.ContainsKey(_orders[i].GetDestination().GetId()))
            {
                result = false;
                break;
            }
        }
        return result;
    }

    /// <summary>
    /// Is the plan still waiting to be evaluated?
    /// </summary>
    /// <returns>Whether the plan wasn't evaluated yet</returns>
    public bool AreHeroesInvolved()
    {
        return _areHeroesInvolved;
    }

    /// <summary>
    /// Run combat simulations
    /// </summary>
    /// <param name="numberOfRuns">The number of simulations to run</param>
    /// <param name="simulationLevel">Sumulation complexity level</param>
    /// <returns>Number of simulations that actually ran</returns>
    public int RunCombatSimulations(int numberOfRuns, int simulationLevel)
    {
        FileLogger.Trace("AI", "Running combat simulations for " + _province.GetName());
        _state = State.NOT_REVIEWED;

        List<Unit> units = _province.GetUnits();
        // if nobody's here to defend the province attacked, then
        // attackers always win and there are no losses
        if (units.Count == 0 && simulationLevel == 0)
        {
            _winRatio = 1;
            _gain = _provinceValue;
            return 0;
        }

        // looks like there will be a fight after all
        // set up unit stacks for defenders
        List<UnitStack> defenders = new List<UnitStack>();
        // not using _defendersTrainingCost directly because of a hacky way
		// to simulate enemy retaliation implemented below
        // remainder: _defendersTrainingCost is zero for non-playable functions
        // because, frankly, who cares about them?
        int defendersTrainingCost = _defendersTrainingCost;
        for (int j = 0; j < units.Count; j++)
        {
            defenders.Add(new UnitStack(units[j], _province));
        }

        // now do the same for the attackers
        List<UnitStack> attackers = new List<UnitStack>();
        List<Province> attackingProvinces = new List<Province>();
        int attackersTrainingCost = 0;
        for (int i = 0; i < _orders.Count; i++)
        {
            units = _orders[i].GetUnits();
            for (int j = 0; j < units.Count; j++)
            {
                attackers.Add(new UnitStack(units[j], _orders[i].GetOrigin()));
                if (!attackingProvinces.Contains(_orders[i].GetOrigin()))
                {
                    attackingProvinces.Add(_orders[i].GetOrigin());
                }
                attackersTrainingCost += units[j].GetTrainingCost();
            }
        }

        // that was the easy part
        // now let's consider possible enemy's retalliation (in a hacky way)
        // btw, the easy AI doesn't do this
        if (simulationLevel > 0)
        {
            Faction attackingFaction = _orders[0].GetOrigin().GetOwnersFaction();
            List<Province> neighbors = _province.GetNeighbors();
            for (int i = 0; i < neighbors.Count; i++)
            {
                Faction opponent = neighbors[i].GetOwnersFaction();
                if (opponent.IsPlayable() && opponent != attackingFaction)
                {
                    units = neighbors[i].GetUnits();
                    for (int j = 0; j < units.Count; j++)
                    {
                        defenders.Add(new UnitStack(units[j], neighbors[i]));
                        defendersTrainingCost += units[j].GetTrainingCost();
                    }
                }
            }
        }

        // initialize cumulative statistics
        int numberOfWins = 0;
        // what's the replacement cost for the losses suffered?
        double cumulativeLoss = 0;
        // if fighting a playable faction, same for the enemy
        // because we want to inflict losses on them
        double cumulativeEnemyLoss = 0;

        for (int k = 0; k < numberOfRuns; k++)
        {
            Combat combat = new Combat(_province, UnitStack.Clone(attackers), UnitStack.Clone(defenders), false);
            combat.ResolveCombat();

            _combatSimulations.Add(combat);

            List<UnitStack> remainingAttackers = combat.GetAttackers();
            if (remainingAttackers.Count > 0)
            {
                numberOfWins++;

                // count the losses, ours...
                int remainingAttackersTrainingCost = 0;
                for (int j = 0; j < remainingAttackers.Count; j++)
                {
                    remainingAttackersTrainingCost += remainingAttackers[j].GetBaseUnit().GetTrainingCost();
                }
                cumulativeLoss += attackersTrainingCost - remainingAttackersTrainingCost;

                // ... and our enemies'
                cumulativeEnemyLoss += defendersTrainingCost;
            }
            else
            {
                // we lost the battle and all of our units!
                cumulativeLoss += attackersTrainingCost;

                // but maybe the enemy lost some too?
                List<UnitStack> remainingDefenders = combat.GetDefenders();
                int remainingDefendersTrainingCost = 0;
                for (int j = 0; j < remainingDefenders.Count; j++)
                {
                    Faction opponent = remainingDefenders[j].GetProvinceToRetreat().GetOwnersFaction();
                    if (opponent.IsPlayable())
                    {
                        remainingDefendersTrainingCost += remainingDefenders[j].GetBaseUnit().GetTrainingCost();
                    }
                }
                cumulativeEnemyLoss += defendersTrainingCost - remainingDefendersTrainingCost;
            }
        }

        _winRatio = (double)numberOfWins / numberOfRuns;
        _gain = (double)_provinceValue * numberOfWins / numberOfRuns;
        _loss = cumulativeLoss / numberOfRuns;
        _enemyLoss = cumulativeEnemyLoss / numberOfRuns;

        string attackOriginDescription = GameUtils.DescribeProvinceList(attackingProvinces);

        FileLogger.Trace("AI", _province.GetName() + " can be conquered " + numberOfWins + " out of " + numberOfRuns + " times if attacked from [" + attackOriginDescription + "] (value: " + _provinceValue + ", loss: " + _loss + ", enemy loss: " + _enemyLoss + ", gain: " + _gain + ", score: " + GetScore() + ")");

        return numberOfRuns;
    }

}
