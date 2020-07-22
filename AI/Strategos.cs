/// <summary>
/// Strategic level AI
/// Manages army movements and unit training
/// </summary>

using System;
using System.Collections.Generic;
using UnityEngine;

public class Strategos
{
    private Faction _faction;
	// what loss is the AI ready to suffer
    private const int LOSS_TOLERANCE = 2;
	// what's the base number of simulations to run per attack plan
    private const int COMBATS_TO_RUN = 10;

    /// <summary>
    /// Class constructor
    /// </summary>
    /// <param name="faction">Faction using this AI</param>
    public Strategos(Faction faction)
    {
        _faction = faction;
    }

    #region training

    /// <summary>
    /// Select unit training plans for faction's provinces based on attack plans
    /// </summary>
    /// <param name="plans">Attack plans generated for this turn</param>
    public void SelectTraining(List<ProvinceAttackPlan> plans)
    {
        FileLogger.Trace("AI", "Selecting training");
        Dictionary<int, List<Unit>> dangers = IdentifyDangerousEnemyUnits(plans);

        Dictionary<int, ProvinceTrainingPlan> trainingPlans = new Dictionary<int, ProvinceTrainingPlan>();
        List<Province> provinces = new List<Province>(_faction.GetProvinces().Values);
        int budget = _faction.GetMoneyBalance();

        // if there are enemy untis to counter...
        if (dangers.Count > 0)
        {
            for (int i = 0; i < provinces.Count; i++)
            {
                List<UnitTrainingPlan> newPlans = PlanTrainingCounters(provinces[i], dangers, budget);
                trainingPlans[provinces[i].GetId()] = new ProvinceTrainingPlan(provinces[i], newPlans);
                budget -= GetCost(newPlans);
                if (budget <= 0)
                {
                    break;
                }
            }
        }

        // train fodder units
        if (budget > 0)
        {
            for (int i = 0; i < provinces.Count; i++)
            {
                if (!trainingPlans.ContainsKey(provinces[i].GetId()))
                {
                    trainingPlans[provinces[i].GetId()] = new ProvinceTrainingPlan(provinces[i]);
                }
                List<UnitTrainingPlan> newPlans = PlanTrainingFodder(provinces[i], budget, trainingPlans[provinces[i].GetId()]);
                trainingPlans[provinces[i].GetId()].AddUnitTrainingPlans(newPlans);
                budget -= GetCost(newPlans);
                if (budget <= 0)
                {
                    break;
                }
            }
        }

        // upgrade fodder units to something better, starting with the faction's race
        if (budget > 0)
        {
            Race factionRace = _faction.GetRace();
            for (int i = 0; i < provinces.Count; i++)
            {
                if (provinces[i].GetDwellersRace() == factionRace)
                {
                    UpgradeTrainingFodderPlan(provinces[i], budget, trainingPlans[provinces[i].GetId()]);
                }
            }
        }

        // ... and continuing to other races
        if (budget > 0)
        {
            Race factionRace = _faction.GetRace();
            for (int i = 0; i < provinces.Count; i++)
            {
                if (provinces[i].GetDwellersRace() != factionRace)
                {
                    UpgradeTrainingFodderPlan(provinces[i], budget, trainingPlans[provinces[i].GetId()]);
                }
            }
        }

        // now that training plans have been finalized,
        // queue the training in each province
        for (int i = 0; i < provinces.Count; i++)
        {
            if (trainingPlans.ContainsKey(provinces[i].GetId()))
            {
                ProvinceTrainingPlan plan = trainingPlans[provinces[i].GetId()];
                ProcessProvinceTrainingPlan(provinces[i], plan);
            }
        }
    }

    /// <summary>
    /// Identify enemy units inflicting high losses to our troops
    /// </summary>
    /// <param name="plans">Attack plans generated for this turn</param>
    /// <returns>Hash of provice id => list of units</returns>
   private Dictionary<int, List<Unit>> IdentifyDangerousEnemyUnits(List<ProvinceAttackPlan> plans)
    {
        Dictionary<int, List<Unit>> result = new Dictionary<int, List<Unit>>();
        // identify dangerous enemies if the AI level is higher than easy
        if (_faction.GetAILevel() > 0)
        {
            List<int> reviewedProvinces = new List<int>();

            for (int i = 0; i < plans.Count; i++)
            {
                Province target = plans[i].GetMovementOrders()[0].GetDestination();

                // if this plan was rejected because of high losses and
                // we haven't analyzed the garrison of the target province...
                if (plans[i].IsRejected() && plans[i].GetLoss() >= LOSS_TOLERANCE &&
                    target != null && !reviewedProvinces.Contains(target.GetId()))
                {
                    reviewedProvinces.Add(target.GetId());
                    List<Unit> ourTroops = new List<Unit>();
                    List<ArmyMovementOrder> movementOrders = plans[i].GetMovementOrders();
                    for (int j = 0; j < movementOrders.Count; j++)
                    {
                        ourTroops.AddRange(movementOrders[j].GetUnits());
                    }

                    List<Unit> enemies = target.GetUnits();
                    for (int j = 0; j < enemies.Count; j++)
                    {
                        if (CombatHelper.Instance.GetAttackingCounter(enemies[j], ourTroops) == null &&
                            CombatHelper.Instance.GetDefendingCounter(enemies[j], ourTroops) == null)
                        {
                            if (!result.ContainsKey(target.GetId()))
                            {
                                result[target.GetId()] = new List<Unit>();
                            }
                            if (!result[target.GetId()].Contains(enemies[j]))
                            {
                                result[target.GetId()].Add(new Unit(enemies[j]));
                            }
                        }
                    }
                }
            }

			// put results into the file log
            string message = "Faction " + _faction.GetName() + "'s dangerous enemies: ";
            if (result.Count > 0)
            {
                foreach (KeyValuePair<int, List<Unit>> entry in result)
                {
                    List<Unit> units = entry.Value;
                    if (units.Count > 0)
                    {
                        message += "province #" + entry.Key + " - [" + units[0].GetUnitType().GetName();
                        for (int i = 1; i < units.Count; i++)
                        {
                            message += ", " + units[i].GetUnitType().GetName();
                        }
                        message += "], ";

                    }
                }
            }
            else
            {
                message += "none spotted.";
            }
            FileLogger.Trace("AI", message);
        }
        return result;
    }

    /// <summary>
    /// Prepare additional unit training plans based on the threats identified, the budget, and the existing training plan, if available
    /// </summary>
    /// <param name="province">The province for which to prepare unit training plans</param>
    /// <param name="dangers">Threats identified as a hash of enemy provice id => list of units</param>
    /// <param name="budget">The amount of money available for spending</param>
    /// <param name="existingPlan">The province's current training plan</param>
    /// <returns>List of unit training plans</returns>
    private List<UnitTrainingPlan> PlanTrainingCounters(Province province, Dictionary<int, List<Unit>> dangers, int budget, ProvinceTrainingPlan existingPlan = null)
    {
        List<UnitTrainingPlan> result = new List<UnitTrainingPlan>();
        int manpower = province.GetRemainingManpower();
        if (existingPlan != null)
        {
            manpower -= existingPlan.GetManpowerCost();
        }

        // no dangers, no recruits or no money => good buy!
        if (dangers.Count == 0 || manpower <= 0 || budget <= 0)
        {
            return result;
        }

        List<UnitType> trainable = province.GetTrainableUnits();
        List<Unit> units = new List<Unit>();
        for (int i = 0; i < trainable.Count; i++)
        {
            units.Add(new Unit(trainable[i], 1));
        }

        List<int> targets = new List<int>(dangers.Keys);
        for (int i = 0; i < targets.Count; i++)
        {
            List<Unit> localDangers = dangers[targets[i]];
            for (int j = 0; j < localDangers.Count; j++)
            {
                Unit counter = CombatHelper.Instance.GetAttackingCounter(localDangers[j], units);
                if (counter != null)
                {
                    int cost = counter.GetUnitType().GetTrainingCost();
                    int quantity = Math.Min(budget / cost, Math.Min(province.GetRemainingManpower(), localDangers[j].GetQuantity() / 2));
                    if (quantity > 0)
                    {
                        localDangers[j].AddQuantity(-quantity);
                        UnitTrainingOrder order = new UnitTrainingOrder(counter.GetUnitType(), quantity, false);
                        result.Add(new UnitTrainingPlan(order, UnitTrainingPlan.Reason.COUNTER, localDangers[j].GetUnitType().GetId(), targets[i]));
                        manpower -= quantity;
                        budget -= cost * quantity;

                        FileLogger.Trace("AI", counter.GetUnitType().GetName() + " is an attacking counter to " + localDangers[j].GetUnitType().GetName());
                    }
                    if (manpower <= 0 || budget <= 0)
                    {
                        return result;
                    }
                }
                counter = CombatHelper.Instance.GetDefendingCounter(localDangers[j], units);
                if (counter != null)
                {
                    int cost = counter.GetUnitType().GetTrainingCost();
                    int quantity = Math.Min(budget / cost, Math.Min(province.GetRemainingManpower(), localDangers[j].GetQuantity()));
                    if (quantity > 0)
                    {
                        localDangers[j].AddQuantity(-quantity);
                        UnitTrainingOrder order = new UnitTrainingOrder(counter.GetUnitType(), quantity, false);
                        result.Add(new UnitTrainingPlan(order, UnitTrainingPlan.Reason.COUNTER, localDangers[j].GetUnitType().GetId(), targets[i]));
                        manpower -= quantity;
                        budget -= cost * quantity;
                        FileLogger.Trace("AI", counter.GetUnitType().GetName() + " is an defending counter to " + localDangers[j].GetUnitType().GetName());
                    }
                    if (manpower <= 0 || budget <= 0)
                    {
                        return result;
                    }
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Prepare additional unit training plans by selecting the cheapest units
	/// taking into account the budget and the existing training plan, if any
    /// </summary>
    /// <param name="province">The province for which to prepare unit training plans</param>
    /// <param name="budget">The amount of money available for spending</param>
    /// <param name="existingPlan">The province's current training plan</param>
    /// <returns>List of unit training plans</returns>
    private List<UnitTrainingPlan> PlanTrainingFodder(Province province, int budget, ProvinceTrainingPlan existingPlan = null)
    {
        List<UnitTrainingPlan> result = new List<UnitTrainingPlan>();
        int manpower = province.GetRemainingManpower();
        if (existingPlan != null)
        {
            manpower -= existingPlan.GetManpowerCost();
        }

        // no recruits or no money => good buy!
        if (manpower <= 0 || budget <= 0)
        {
            return result;
        }

        List<UnitType> trainable = province.GetCheapestToTrainUnits();
        Dictionary<UnitType, int> trainingOrder = new Dictionary<UnitType, int>();

        if (trainable.Count > 0)
        {
            if (trainable.Count == 1)
            {
                // simple - train this single cheapest unit
                trainingOrder[trainable[0]] = Mathf.Min(manpower, budget / trainable[0].GetTrainingCost());
            }
            else
            {
                // more complicated - select one of the options randomly
                for (int i = 0; i < trainable.Count; i++)
                {
                    trainingOrder[trainable[i]] = 0;
                }
                for (int i = 0; i < manpower; i++)
                {
                    int randomIndex = UnityEngine.Random.Range(0, trainable.Count);
                    trainingOrder[trainable[randomIndex]] += 1;
                    budget -= trainable[randomIndex].GetTrainingCost();
                    if (budget <= 0)
                    {
                        break;
                    }
                }
            }
        }

        foreach(KeyValuePair<UnitType, int> entry in trainingOrder)
        {
            if (entry.Value > 0)
            {
                UnitTrainingOrder order = new UnitTrainingOrder(entry.Key, entry.Value, false);
                result.Add(new UnitTrainingPlan(order, UnitTrainingPlan.Reason.FODDER));
            }
        }
        return result;
    }

    /// <summary>
    /// Revise training plans of the cheapest units based on the money left and the existing training plan, if available
    /// </summary>
    /// <param name="province">The province for which to prepare unit training plans</param>
    /// <param name="budget">The amount of money available for spending</param>
    /// <param name="existingPlan">The province's current training plan</param>
    private void UpgradeTrainingFodderPlan(Province province, int budget, ProvinceTrainingPlan existingPlan)
    {
        // no money => good buy!
        if (budget <= 0)
        {
            return;
        }

        List<UnitType> trainable = province.GetTrainableUnits();
        Dictionary<UnitType, int> trainingOrder = new Dictionary<UnitType, int>();
        List<UnitType> fodder = province.GetCheapestToTrainUnits();
        List<UnitType> expensives = new List<UnitType>();

        for (int i = 0; i < trainable.Count; i++)
        {
            if (!fodder.Contains(trainable[i]))
            {
                trainingOrder[trainable[i]] = 0;
                expensives.Add(trainable[i]);
            }
        }

        if (trainingOrder.Count > 0)
        {
            List<UnitTrainingPlan> plans = existingPlan.GetUnitTrainingPlans();
            for (int i = 0; i < plans.Count; i++)
            {
                if (plans[i].GetReason() == UnitTrainingPlan.Reason.FODDER)
                {
                    int manpower = plans[i].GetManpowerCost();
                    int fodderCost = plans[i].GetUnitTypeTrainingCost();
                    for (int j = 0; j < manpower; j++)
                    {
                        int randomIndex = UnityEngine.Random.Range(0, expensives.Count);
                        int costIncrease = expensives[randomIndex].GetTrainingCost() - fodderCost;
                        if (budget >= costIncrease)
                        {
                            trainingOrder[expensives[randomIndex]] += 1;
                            plans[i].DecreaseQuantity();
                            budget -= costIncrease;
                        }
                        if (budget <= 0)
                        {
                            break;
                        }
                    }
                }

                if (budget <= 0)
                {
                    break;
                }
            }

            List<UnitTrainingPlan> newPlans = new List<UnitTrainingPlan>();
            foreach (KeyValuePair<UnitType, int> entry in trainingOrder)
            {
                if (entry.Value > 0)
                {
                    UnitTrainingOrder order = new UnitTrainingOrder(entry.Key, entry.Value, false);
                    newPlans.Add(new UnitTrainingPlan(order, UnitTrainingPlan.Reason.CORE));
                }
            }
            if (newPlans.Count > 0)
            {
                existingPlan.AddUnitTrainingPlans(newPlans);
            }
        }

        return;
    }

    /// <summary>
    /// Prepare and queue unit training plans by selecting the cheapest units
    /// </summary>
    /// <param name="province">The province for which to prepare unit training plans</param>
    private void TrainCheapestUnits(Province province)
    {
        Dictionary<UnitType, int> trainingOrder = FillTrainingOrderWithCheapestUnits(province);
        QueueTrainingOrder(province, trainingOrder);
    }

    /// <summary>
    /// Prepare and queue unit training plans by selecting some of the more expensive units
    /// </summary>
    /// <param name="province">The province for which to prepare unit training plans</param>
    private void TrainExpensiveUnits(Province province)
    {
        Dictionary<UnitType, int> trainingOrder = FillTrainingOrderWithCheapestUnits(province);
        int costOfTraining = CalculateUnitTrainingOrderCost(trainingOrder);
        int disposableIncome = province.GetOwnersFaction().GetMoneyBalance();

        if (disposableIncome > costOfTraining)
        {
            Dictionary<UnitType, int> expensiveUnits = new Dictionary<UnitType, int>();
            Dictionary<UnitType, int> cheapUnits = new Dictionary<UnitType, int>();
            List<UnitType> trainable = province.GetTrainableUnits();
            for (int i = 0; i < trainable.Count; i++)
            {
                if (!trainingOrder.ContainsKey(trainable[i]))
                {
                    expensiveUnits[trainable[i]] = 0;
                }
            }

            if (expensiveUnits.Count > 0)
            {
                foreach (KeyValuePair<UnitType, int> entry in trainingOrder)
                {
                    if (entry.Value > 0)
                    {
                        UnitType randomElite = GetRandomUnitType(expensiveUnits);
                        if (randomElite != null)
                        {
                            int costIncreasePerUnit = randomElite.GetTrainingCost() - entry.Key.GetTrainingCost();
                            int slotsReallocated = Mathf.Min(entry.Value, (disposableIncome - costOfTraining) / costIncreasePerUnit);
                            cheapUnits[entry.Key] = entry.Value - slotsReallocated;
                            expensiveUnits[randomElite] += slotsReallocated;
                            costOfTraining += costIncreasePerUnit * slotsReallocated;
                        }
                        else
                        {
                            FileLogger.Trace("AI", "What? No expensive units in " + province.GetName());
                        }
                    }
                }
            }

            trainingOrder = expensiveUnits;
            foreach (KeyValuePair<UnitType, int> entry in cheapUnits)
            {
                trainingOrder[entry.Key] = entry.Value;
            }
        }

        QueueTrainingOrder(province, trainingOrder);
    }

    /// <summary>
    /// Prepare unit training plans by selecting the cheapest units
    /// </summary>
    /// <param name="province">The province for which to prepare unit training plans</param>
    /// <returns>Dictionary of unit type => number of units to train</returns>
    private Dictionary<UnitType, int> FillTrainingOrderWithCheapestUnits(Province province)
    {
        // NOTE: take into account that a faction may not have enough money to train even
        // the cheapest units up to the province's manpower level
        Dictionary<UnitType, int> trainingOrder = new Dictionary<UnitType, int>();
        List<UnitType> chaff = province.GetCheapestToTrainUnits();
        if (chaff.Count > 0)
        {
            if (chaff.Count == 1)
            {
                // simple - train this single cheapest unit
                trainingOrder[chaff[0]] = Mathf.Min(province.GetManpower(), province.GetOwnersFaction().GetMoneyBalance() / chaff[0].GetTrainingCost());
            }
            else
            {
                int availableIncome = province.GetOwnersFaction().GetMoneyBalance();
                // more complicated - select one of the options randomly
                for (int i = 0; i < chaff.Count; i++)
                {
                    trainingOrder[chaff[i]] = 0;
                }
                for (int i = 0; i < province.GetManpower(); i++)
                {
                    int randomIndex = UnityEngine.Random.Range(0, chaff.Count);
                    trainingOrder[chaff[randomIndex]] += 1;
                    availableIncome -= chaff[randomIndex].GetTrainingCost();
                    if (availableIncome <= 0)
                    {
                        break;
                    }
                }
            }
        }
        return trainingOrder;
    }

    /// <summary>
    /// Calculate the total cost of unit training plans
    /// </summary>
    /// <param name="plans">A list of unit training plans for which to calculate cost</param>
    /// <returns>Cost of the unit training plans</returns>
    private int GetCost(List<UnitTrainingPlan> plans)
    {
        int result = 0;
        for (int i = 0; i < plans.Count; i++)
        {
            result += plans[i].GetCost();
        }
        return result;
    }

    /// <summary>
    /// Calculate the cost of training units
    /// </summary>
    /// <param name="order">Hash of unit type => number of units to train</param>
    /// <returns>Cost to train units</returns>
    private int CalculateUnitTrainingOrderCost(Dictionary<UnitType, int> order)
    {
        int result = 0;
        foreach (KeyValuePair<UnitType, int> entry in order)
        {
            result += entry.Key.GetTrainingCost() * entry.Value;
        }
        return result;
    }

    /// <summary>
    /// Queue the training order
    /// </summary>
    /// <param name="province">The province where to place the unit training order</param>
    /// <param name="order">Hash of unit type => number of units to train</param>
    private void QueueTrainingOrder(Province province, Dictionary<UnitType, int> order)
    {
        foreach (KeyValuePair<UnitType, int> entry in order)
        {
            if (entry.Value > 0)
            {
                FileLogger.Trace("AI", "Queueing " + entry.Value + " " + entry.Key.GetName() + "s in " + province.GetName());
                // re-evaluate training orders every turn, don't create standing training orders
                province.QueueTraining(entry.Key, entry.Value, false);
            }
        }
    }

    /// <summary>
    /// Queue the training order
    /// </summary>
    /// <param name="province">The province where to place the unit training order</param>
    /// <param name="order">List of units to train</param>
    private void QueueTrainingOrder(Province province, List<Unit> order)
    {
        for(int i = 0; i < order.Count; i++)
        {
            UnitType unitType = order[i].GetUnitType();
            int quantity = order[i].GetQuantity();
            if (quantity > 0)
            {
                FileLogger.Trace("AI", "Queueing " + quantity + " " + unitType + "s in " + province.GetName());
                // re-evaluate training orders every turn, don't create standing training orders
                province.QueueTraining(unitType, quantity, false);
            }
        }
    }

    /// <summary>
    /// Parse the province training plan and queue its training orders
    /// </summary>
    /// <param name="province">The province where to place the unit training order</param>
    /// <param name="plan">Province training plan to process</param>
    private void ProcessProvinceTrainingPlan(Province province, ProvinceTrainingPlan plan)
    {
        List<UnitTrainingPlan> unitPlans = plan.GetUnitTrainingPlans();
        for (int i = 0; i < unitPlans.Count; i++)
        {
            UnitTrainingOrder order = unitPlans[i].GetUnitTrainingOrder();
            UnitType unitType = order.GetUnitType();
            int quantity = order.GetQuantity();
            if (quantity > 0)
            {
                FileLogger.Trace("AI", "Queueing " + quantity + " " + unitType.GetName() + "s (" + unitPlans[i].GetReason() + ") in " + province.GetName());
                // re-evaluate training orders every turn, don't create standing training orders
                province.QueueTraining(unitType, quantity, false);
            }
        }
    }

    /// <summary>
    /// Get a random unit type out of hash of unit type => number of units
    /// </summary>
    /// <param name="dictionary">The hash of unit type => number of units</param>
    /// <returns>Randomly selected unit type</returns>
    private UnitType GetRandomUnitType(Dictionary<UnitType, int> dictionary)
    {
        UnitType result = null;
        if (dictionary.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, dictionary.Count);
            foreach (KeyValuePair<UnitType, int> entry in dictionary)
            {
                result = entry.Key;
                randomIndex--;
                if (randomIndex < 0)
                {
                    break;
                }
            }
        }
        else
        {
            FileLogger.Trace("AI", "Oh noes! Can't select a random unit type from an empty dictionary");
        }
        return result;
    }

    #endregion

    #region movement

    /// <summary>
    /// Prepare a list of army movement orders for the current turn
    /// </summary>
    /// <returns>List of army movement orders</returns>
    public List<ArmyMovementOrder> SelectMovements()
    {
        // this is our guy to be returned
        List<ArmyMovementOrder> result = new List<ArmyMovementOrder>();

        // this is how many combat simulations we have run for this turn
        int simulationsRan = 0;
        // shouldn't exceed this number
        int maxCombatSimulations = 200;

        // movement orders will depend on the accepted attack plans
        List<ProvinceAttackPlan> plans = new List<ProvinceAttackPlan>();

        // we need to consider all garrisons across our vast empire
        List<Province> provinces = new List<Province>(_faction.GetProvinces().Values);
        // we want to consider the most powerful armies first
        provinces.Sort((x, y) => y.GetUnits().Count.CompareTo(x.GetUnits().Count));
        // no need to pay attention to provinces without garrisons
        provinces.RemoveAll(province => province.GetUnits().Count == 0);

        for (int i = 0; i < provinces.Count; i++)
        {
            if (provinces[i].IsInnerProvince())
            {
                // no enemy neighbors => the move is going to be non-combat
                List<ArmyMovementOrder> orders = SelectNonCombatMoves(provinces[i]);
                if (orders.Count > 0)
                {
                    result.AddRange(orders);
                }
            }
            else
            {
                // plans involving garrison of this particular province
                List<ProvinceAttackPlan> currentPlans = new List<ProvinceAttackPlan>();
                List<Unit> attackers = provinces[i].GetUnits();

                List<Province> targets = provinces[i].GetTargetableNeighbors();
                // consider adding these attackers to an existing invasion plan
                if (_faction.GetAILevel() > 0)
                {
                    for (int j = 0; j < plans.Count; j++)
                    {
                        if (!plans[j].IsIncomplete() && plans[j].GetLoss() > 0)
                        {
                            Province province = plans[j].GetTargetProvince();
                            if (targets.Contains(province))
                            {
                                ProvinceAttackPlan plan = new ProvinceAttackPlan(plans[j]);
                                plan.AddMovementOrder(new ArmyMovementOrder(provinces[i], province, attackers));
                                currentPlans.Add(plan);
                            }
                        }
                    }
                }
                
                // create new incomplete attack plans
                for (int j = 0; j < targets.Count; j++)
                {
                    ProvinceAttackPlan plan = new ProvinceAttackPlan(targets[j],
                                                    GameUtils.CalculateProvinceValue(targets[j], _faction));
                    plan.AddMovementOrder(new ArmyMovementOrder(provinces[i], targets[j], attackers));
                    currentPlans.Add(plan);
                }
                currentPlans.Sort((x, y) => y.GetProvinceValue().CompareTo(x.GetProvinceValue()));

                // evaluate plans that seem promising
                double maxScore = -1;
                for (int j = 0; j < currentPlans.Count; j++)
                {
                    ProvinceAttackPlan currentPlan = currentPlans[j];
                    if (simulationsRan < maxCombatSimulations && currentPlan.GetProvinceValue() + currentPlan.GetDefendersTrainingCost() > maxScore)
                    {
                        int numberOfRuns = currentPlan.AreHeroesInvolved() ? 2 * COMBATS_TO_RUN : COMBATS_TO_RUN;
                        int realRuns = currentPlan.RunCombatSimulations(numberOfRuns, _faction.GetAILevel());
                        simulationsRan += realRuns;
                        maxScore = Math.Max(maxScore, currentPlan.GetScore());
                    }
                }
                plans.AddRange(currentPlans);
            }
        }
        plans.RemoveAll(plan => plan.IsIncomplete());

        FileLogger.Trace("AI", "Reviewing attack plans");
        List<ProvinceAttackPlan> savedPlans = new List<ProvinceAttackPlan>();
        bool havePlansToReview = true;
        int plansReviewed = 0;
        int maxPlansToReview = _faction.GetAILevel() == 0 ? 1 : 10;
        while (havePlansToReview && plansReviewed < maxPlansToReview)
        {
            // select the best plan to implement
            double maxScore = -1;
            int maxScoreIndex = -1;
            for (int i = 0; i < plans.Count; i++)
            {
                double planScore = plans[i].GetScore();
                if (planScore > maxScore)
                {
                    maxScore = planScore;
                    maxScoreIndex = i;
                }
            }

            if (maxScoreIndex >= 0)
            {
                ProvinceAttackPlan currentPlan = plans[maxScoreIndex];
                List<ArmyMovementOrder> movements = currentPlan.GetMovementOrders();
                result.AddRange(movements);
                string attackOriginDescription = movements[0].GetOrigin().GetName();
                for (int j = 1; j < movements.Count; j++)
                {
                    attackOriginDescription += ", " + movements[j].GetOrigin().GetName();
                }
                FileLogger.Trace("AI", _faction.GetName() + ": will attack province " + movements[0].GetDestination().GetName() + " from [" + attackOriginDescription + "]");
                currentPlan.MarkAccepted();
                savedPlans.Add(currentPlan);
                plans.RemoveAt(maxScoreIndex);
                // iterating plans starting with the highest index
                for (int i = plans.Count - 1; i > -1; i--)
                {
                    if (!plans[i].IsCompatibleWith(movements))
                    {
                        plans[i].MarkRejected();
                        savedPlans.Add(plans[i]);
                        plans.RemoveAt(i);
                    }
                }
            }
            else
            {
                if (result.Count == 0)
                {
                    FileLogger.Trace("AI", _faction.GetName() + ": not attacking anyone");
                }
                havePlansToReview = false;
            }
            plansReviewed++;
            FileLogger.Trace("AI", "Selecting attack plans: pass " + plansReviewed.ToString() + " is done");
        }

        // training units may depend on the attack plans
        SelectTraining(savedPlans);

        return result;
    }

    /// <summary>
    /// Prepare a list of army movement orders for the current turn
    /// </summary>
    /// <param name="province">The origin provice for the movement orders</param>
    /// <returns>List of army movement orders not leading to combat</returns>
    private List<ArmyMovementOrder> SelectNonCombatMoves(Province province)
    {
        FileLogger.Trace("AI", "Selecting non-combat moves");
        List<ArmyMovementOrder> result = new List<ArmyMovementOrder>();

        List<Unit> garrison = province.GetUnits();
        // all provinces included into the inner provinces list should have
        // a garrison, but better safe than sorry
        if (garrison.Count > 0)
        {

            List<Province> neighbors = province.GetNeighbors();
            int shortestDistance = int.MaxValue;
            Province favoriteNeighbor = null;
            for (int i = 0; i < neighbors.Count; i++)
            {
                int distanceToEnemy = CalculateDistanceToNearestEnemyProvince(neighbors[i]);
                FileLogger.Trace("AI", "Can reach an enemy in " + distanceToEnemy.ToString() + " turns from " + province.GetName() + " moving through " + neighbors[i].GetName());
                if (distanceToEnemy < shortestDistance)
                {
                    shortestDistance = distanceToEnemy;
                    favoriteNeighbor = neighbors[i];
                }
                // NOTE: if two neighbors have the same distance to an enemy,
                // it's possible to select randomly one of them
            }

            if (favoriteNeighbor != null)
            {
                FileLogger.Trace("AI", "Will move troops from " + province.GetName() + " to " + favoriteNeighbor.GetName());
                result.Add(new ArmyMovementOrder(province, favoriteNeighbor, garrison));
            }
        }
        return result;
    }

    /// <summary>
    /// Get the distance (measured in provinces) to the nearest enemy province
    /// </summary>
    /// <param name="start">The provice from which to calculate the distance</param>
    /// <returns>Number of provinces between this province and the nearest enemy</returns>
    private int CalculateDistanceToNearestEnemyProvince(Province start)
    {
		// TODO: distance between any pair of provinces can be calculated
		// using a similar algorithm at the start of the game and preserved
		// refactor if the prototype moves to the next phase
        Dictionary<int, Province> visited = new Dictionary<int, Province>();
        List<Province> underInspection = new List<Province>();
        underInspection.Add(start);
        int distance = 1;
        // the search is exhausted when there are no more provinces to visit
        bool searchExhausted = false;
        while (!searchExhausted)
        {
            searchExhausted = true;
            distance++;
            List<Province> toBeInspected = new List<Province>();
            for (int i = 0; i < underInspection.Count; i++)
            {
                /* for each province under inspection
                   * mark as visited
                   * for each unvisited neighbor
                     * if it's an enemy, return
                     * if it's not enemy, add to the list of provinces to inspect at the next step
                */
                visited[underInspection[i].GetId()] = underInspection[i];
                List<Province> neighbors = underInspection[i].GetNeighbors();
                for(int j = 0; j < neighbors.Count; j++)
                {
                    Province neighbor = neighbors[j];
                    if(!visited.ContainsKey(neighbor.GetId()))
                    {
                        if(neighbor.GetOwnersFaction() != _faction)
                        {
                            return distance;
                        }
                        else
                        {
                            toBeInspected.Add(neighbor);
                            searchExhausted = false;
                        }
                    }
                }
            }
            underInspection = toBeInspected;
        }
		// if the nearest enemy province could not be found, return "A LOT"
        return int.MaxValue;
    }

    #endregion

    #region events

    /// <summary>
    /// Choose one of the options to react to an in-game event
    /// </summary>
    /// <param name="gameEvent">The in-game event requiring our attention</param>
    /// <returns>Index of the option selected</returns>
    public int ChooseAnOption(GameEvent gameEvent)
    {
        // always accept an offer (for now)
        return 0;
    }

    #endregion

}
