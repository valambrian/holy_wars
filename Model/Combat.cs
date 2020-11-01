/// <summary>
/// Class reponsible for supporting combat resolution
/// </summary>

using System;
using System.Collections.Generic;

public class Combat
{
    public enum TurnPhase { START = 0, MAGIC, RANGED, SKIRMISH, CHARGE, MELEE, DIVINE, CLEANUP };

    public event EventHandler<AttackResolutionEvent> AttackResolved;
    public event EventHandler<EventArgs> WoundChecksStarted;
    public event EventHandler<EventArgs> Finished;

    // the province where combat is happening
    private Province _province;
    private List<UnitStack> _attackers;
    private List<UnitStack> _defenders;

    private List<AttackRollResultsCollection> _attackerRollResults;
    private List<AttackRollResultsCollection> _defenderRollResults;

    private int _currentTurn;
    private TurnPhase _currentPhase;
    private Spell.SpellType _currentSpellType;
    private string _phaseDescription;

    private bool _isAttackerPC;
    private bool _isDefenderPC;

    private Tactician _attackingTactician = null;
    private Tactician _defendingTactician = null;

    private const int _diceSides = 6;

    private AttackRollResultsCollection _currentAttacks = null;
    private UnitStack _currentTarget = null;
    private bool _isCombatOver = false;

    /// <summary>
    /// Sets up combat participants and parameters
    /// </summary>
    /// <param name="province">Combat location</param>
    /// <param name="attackers">A list of attacking unit stacks</param>
    /// <param name="defenders">A list of defending unit stacks</param>
    /// <param name="isRealCombat">Whether the combat takes place or whether a Strategos simulates it during planning phase</param>
    public Combat(Province province, List<UnitStack> attackers, List<UnitStack> defenders, bool isRealCombat)
    {
        _province = province;
        _attackers = attackers;
        _defenders = defenders;

        _attackerRollResults = new List<AttackRollResultsCollection>();
        _defenderRollResults = new List<AttackRollResultsCollection>();

        _currentTurn = 1;
        _currentPhase = 0;
        _currentSpellType = Spell.SpellType.UNIT_CREATION;

        _isDefenderPC = isRealCombat && province.GetOwnersFaction().IsPC();
        _isAttackerPC = isRealCombat && _attackers[0].GetProvinceToRetreat().GetOwnersFaction().IsPC();

        _attackingTactician = _attackers[0].GetProvinceToRetreat().GetOwnersFaction().GetTactician();
        _defendingTactician = province.GetOwnersFaction().GetTactician();
    }

    #region getters

    /// <summary>
    /// Get the province where the combat is taking place
    /// </summary>
    /// <returns>Battlefield province</returns>
    public Province GetProvince()
    {
        return _province;
    }

    /// <summary>
    /// Get unit stack participating in combat on the attacker's side
    /// </summary>
    /// <returns>List of unit stacks belonging to the attacker</returns>
    public List<UnitStack> GetAttackers()
    {
        return _attackers;
    }

    /// <summary>
    /// Get unit stack participating in combat on the defender's side
    /// </summary>
    /// <returns>List of unit stacks belonging to the defender</returns>
    public List<UnitStack> GetDefenders()
    {
        return _defenders;
    }

    /// <summary>
    /// Get attack roll result collections
    /// </summary>
    /// <param name="byAttacker">Whether to get attackers' or defenders' rolls</param>
    /// <returns>List of attack roll result collections for the selected side</returns>
    public List<AttackRollResultsCollection> GetAttackRollResults(bool byAttacker)
    {
        if (byAttacker)
        {
            return _attackerRollResults;
        }
        else
        {
            return _defenderRollResults;
        }
    }

    /// <summary>
    /// Get unit stack participating in combat on one of the sides
    /// </summary>
    /// <param name="attackers">Whether to get attackers or defenders </param>
    /// <returns>List of unit stacks belonging to the selected side</returns>
    public List<UnitStack> GetUnitStacks(bool attackers)
    {
        RemoveEmptyUnitStacks();

        if (attackers)
        {
            return _attackers;
        }
        return _defenders;
    }

    /// <summary>
    /// Count units in all stacks participating in combat on one of the sides
    /// </summary>
    /// <param name="attackers">Whether to count attackers or defenders </param>
    /// <returns>Count of units in stacks belonging to the selected side</returns>
    public int GetTotalUnitsQuantity(bool attacking)
    {
        List<UnitStack> source = attacking ? _attackers : _defenders;

        int result = 0;
        for (int i = 0; i < source.Count; i++)
        {
            result += source[i].GetTotalQty();
        }

        return result;
    }

    /// <summary>
    /// Get current turn phase
    /// </summary>
    /// <returns>Current turn phase</returns>
    public TurnPhase GetCurrentTurnPhase()
    {
        return (TurnPhase)_currentPhase;
    }

    /// <summary>
    /// Get current attack roll results collection
    /// </summary>
    /// <returns>Current attack roll results collection</returns>
    public AttackRollResultsCollection GetCurrentAttacksCollection()
    {
        return _currentAttacks;
    }

    /// <summary>
    /// Get current defending unit stack
    /// </summary>
    /// <returns>Current target unit stack</returns>
    public UnitStack GetCurentTargetUnitStack()
    {
        return _currentTarget;
    }

    /// <summary>
    /// Get current turn phase description
    /// </summary>
    /// <returns>Current turn phase description</returns>
    public string GetPhaseDescription()
    {
        return _phaseDescription;
    }

    /// <summary>
    /// Is the combat over?
    /// </summary>
    /// <returns>Whether the combat is over</returns>
    public bool IsCombatOver()
    {
        return _isCombatOver;
    }

    /// <summary>
    /// Is a human player attacking?
    /// </summary>
    /// <returns>Whether the attacker is a human player</returns>
    public bool IsAttackerPC()
    {
        return _isAttackerPC;
    }

    /// <summary>
    /// Is a human player player defending?
    /// </summary>
    /// <returns>Whether the defender is a human player</returns>
    public bool IsDefenderPC()
    {
        return _isDefenderPC;
    }

    /// <summary>
    /// Is the combat happening on a human player's turn?
    /// </summary>
    /// <returns>Whether the combat falls on a human player's turn</returns>
    public bool IsPlayerTurn()
    {
        if (_isAttackerPC)
        {
            return _isDefenderPC || _attackerRollResults.Count == 0;
        }
        else
        {
            return _isDefenderPC && _defenderRollResults.Count == 0;
        }
    }

    /// <summary>
    /// Are there more attacks to resolve?
    /// </summary>
    /// <returns>Whether there are attacks to resolve</returns>
    public bool AreThereUnresolvedAttacks()
    {
        return _attackerRollResults.Count + _defenderRollResults.Count > 0;
    }

    #endregion

    #region public functionality

    /// <summary>
    /// Performs actions appropriate for the phase, like generating attack roll results or casting spells
    /// </summary>
    public void PerformPhaseActions()
    {
        if (_attackerRollResults.Count == 0 && _defenderRollResults.Count == 0)
        {
            PerformEndOfCombatCheck();
            bool noPlayerAttentionNeeded = true;
            while (noPlayerAttentionNeeded)
            {
                // setting phase description before advancing to the next phase
                _phaseDescription = _currentPhase.ToString();

                if (_currentPhase == TurnPhase.START)
                {
                    for (int i = 0; i < _attackers.Count; i++)
                    {
                        _attackers[i].ActivateSpellLikeAbilities();
                    }
                    for (int i = 0; i < _defenders.Count; i++)
                    {
                        _defenders[i].ActivateSpellLikeAbilities();
                    }
                }

                if (_currentPhase == TurnPhase.MAGIC)
                {
                    if (_currentSpellType == Spell.SpellType.UNIT_CREATION)
                    {
                        bool unitsSummoned = CastUnitCreationSpells();
                        _currentSpellType = Spell.SpellType.DEFENSIVE;
                        if (unitsSummoned)
                        {
                            return;
                        }
                    }
                    if (_currentSpellType == Spell.SpellType.DEFENSIVE)
                    {
                        bool spellsCast = CastDefensiveSpells();
                        _currentSpellType = Spell.SpellType.OFFENSIVE;
                        if (spellsCast)
                        {
                            return;
                        }
                    }
                    if (_currentSpellType == Spell.SpellType.OFFENSIVE)
                    {
                        CastOffensiveSpells();
                        _currentSpellType = Spell.SpellType.UNIT_CREATION;
                    }
                }

                bool unitsHealed = false;
                if (_currentPhase == TurnPhase.DIVINE)
                {
                    unitsHealed = CastRestorativeSpells(_attackers);
                    unitsHealed = CastRestorativeSpells(_defenders) || unitsHealed;
                }

                RollAttacks(_attackers, _attackerRollResults);
                RollAttacks(_defenders, _defenderRollResults);
                AdvancePhase();
                noPlayerAttentionNeeded = _attackerRollResults.Count == 0 &&
                                            _defenderRollResults.Count == 0 &&
                                            _currentPhase != 0 &&
                                            !unitsHealed;
            }
        }
    }

    /// <summary>
    /// Get an attack roll result colleciton to resolve
    /// </summary>
    /// <returns>Attack roll result colleciton to resolve</returns>
    public AttackRollResultsCollection SelectAttackRollResultsCollection()
    {
        if (_currentAttacks != null && _currentAttacks.Count > 0)
        {
            return _currentAttacks;
        }

        AttackRollResultsCollection result = GetAnAttackRollResultsCollection();
        _currentAttacks = result;
        return result;
    }

    /// <summary>
    /// Get the defending unit stack
    /// </summary>
    /// <returns>Defending unit stack</returns>
    public UnitStack SelectDefendingStack()
    {
        if (_currentTarget != null && _currentTarget.GetTotalHealth() > 0)
        {
            return _currentTarget;
        }

        UnitStack result = null;
        if (_currentAttacks != null && _currentAttacks.Count > 0)
        {
            if (_attackerRollResults.Contains(_currentAttacks) && !_isDefenderPC)
            {
                result = _defendingTactician.SelectDefendingUnitStack(_defenders, _currentAttacks.GetUnitStack(), _currentPhase - 1);
            }
            if (_defenderRollResults.Contains(_currentAttacks) && !_isAttackerPC)
            {
                result = _attackingTactician.SelectDefendingUnitStack(_attackers, _currentAttacks.GetUnitStack(), _currentPhase - 1);
            }
            _currentTarget = result;
        }

        return result;
    }

    /// <summary>
    /// Set the defending unit stack
    /// </summary>
    /// <param name="stack">Unit stack that will be defending against attacks</param>
    public void SetDefendingStack(UnitStack stack)
    {
        _currentTarget = stack;
        FileLogger.Trace("COMBAT", "SetDefendingStack: set " + _currentTarget.GetUnitType().GetName() + " as a target.");
    }

    /// <summary>
    /// Resolve current attack
    /// </summary>
    /// <param name="useEstimates">Whether estimated results will be used or honest rolls will be made</param>
    public void ResolveCurrentAttack(bool useEstimates)
    {
        bool ok = false;
        if (_currentTarget != null && _currentAttacks != null && _currentAttacks.Count > 0)
        {
            ok = ResolveAnAttackAgainstTarget(_currentTarget, _currentAttacks.GetAt(_currentAttacks.Count - 1), useEstimates);
        }
        if (!ok || _currentAttacks == null || _currentAttacks.Count == 0 ||
            _currentTarget == null || _currentTarget.GetTotalQty() == 0)
        {
            _currentTarget = null;
            _currentAttacks = null;
            FileLogger.Trace("COMBAT", "ResolveCurrentAttack: reset current target.");
        }
        else
        {
            FileLogger.Trace("COMBAT", "ResolveCurrentAttack: " + _currentAttacks.Count + " attacks and " + _currentTarget.GetTotalQty() + " targets left.");
        }
    }

    /// <summary>
    /// Check if the combat has ended
    /// </summary>
    public void PerformEndOfCombatCheck()
    {
        RemoveEmptyUnitStacks();

        int attackersCount = GetTotalUnitsQuantity(true);
        int defendersCount = GetTotalUnitsQuantity(false);

        // if there are no attackers left, there is no need in defenders' attack rolls
        if (attackersCount == 0)
        {
            for(int i = 0; i < _defenderRollResults.Count; i++)
            {
                _defenderRollResults[i].Clear();
            }
            _defenderRollResults.Clear();
        }

        // if there are no defenders left, there is no need in attackers' attack rolls
        if (defendersCount == 0)
        {
            for (int i = 0; i < _attackerRollResults.Count; i++)
            {
                _attackerRollResults[i].Clear();
            }
            _attackerRollResults.Clear();
        }

        // if there are some unresolved attack rolls, do not proceed to cleanup
        if (_attackerRollResults.Count > 0 || _defenderRollResults.Count > 0)
        {
            return;
        }

        // or if there are some combatants on both sides, just return
        if (attackersCount > 0 && defendersCount > 0)
        {
            return;
        }

        // otherwise, cleanup
        EndCombat();
    }


    /// <summary>
    /// Resolve combat
    /// Used by the game to resolve NPC-to-NPC battles
    /// and by Strategos to plan invasions
    /// </summary>
    /// <param name="useEstimates">Whether estimated results will be used or honest rolls will be made</param>
    public void ResolveCombat(bool useEstimates = false)
    {
        while (!IsCombatOver())
        {
            PerformPhaseActions();
            AttackRollResultsCollection currentAttackBatch = SelectAttackRollResultsCollection();
            if (currentAttackBatch != null)
            {
                FileLogger.Trace("COMBAT", "Selected attacks by " + currentAttackBatch.GetUnitStack().GetUnitType().GetName());
                UnitStack target = SelectDefendingStack();
                if (target != null)
                {
                    FileLogger.Trace("COMBAT", "Selected " + target.GetUnitType().GetName() + " as a target");
                    ResolveCurrentAttack(useEstimates);
                }
                else
                {
                    PerformEndOfCombatCheck();
                }
            }
        }
    }

    /// <summary>
    /// Execute the decision to retreat
    /// </summary>
    public void Retreat()
    {
        RemoveEmptyUnitStacks();
        EndCombat();
    }

    #endregion

    #region private functionality

    /// <summary>
    /// Get an attack roll result collection
    /// </summary>
    /// <returns>An attack roll result collection selected</returns>
    private AttackRollResultsCollection GetAnAttackRollResultsCollection()
    {
        if (_isCombatOver)
        {
            return null;
        }

        if (!_isDefenderPC || _defenderRollResults.Count == 0)
        {
            AttackRollResultsCollection result = GetFirstNonEmptyCollection(_attackerRollResults);
            if (result != null)
            {
                return result;
            }
        }

        return GetFirstNonEmptyCollection(_defenderRollResults);
    }

    /// <summary>
    /// Go through attack roll result collections and select the first non-empty one
    /// </summary>
    /// <param name="allAttacks">All remaining roll result collections</param>
    /// <returns>A non-empty attack roll result collection or null</returns>
    private AttackRollResultsCollection GetFirstNonEmptyCollection(List<AttackRollResultsCollection> allAttacks)
    {
        for (int i = 0; i < allAttacks.Count; i++)
        {
            if (allAttacks[i].Count > 0)
            {
                return allAttacks[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Rolls attacks for either attackers or defenders
    /// </summary>
    /// <param name="units">Unit stacks making attacks</param>
    /// <param name="results">List storing attack roll results</param>
    private void RollAttacks(List<UnitStack> units, List<AttackRollResultsCollection> results)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (!units[i].IsAffectedBy("Confusion"))
            {
                List<Attack> phaseAttacks = units[i].GetUnitType().GetAttacksForPhase(_currentPhase);
                if (phaseAttacks.Count > 0)
                {
                    AttackRollResultsCollection unitStackAttackRolls = new AttackRollResultsCollection();
                    for (int j = 0; j < units[i].GetTotalQty(); j++)
                    {
                        for (int k = 0; k < phaseAttacks.Count; k++)
                        {
                            for (int l = 0; l < phaseAttacks[k].GetNumberOfAttacks(); l++)
                            {
                                AttackRollResult result = CombatHelper.Instance.CreateAnAttackRollResult(units[i], phaseAttacks[k]);
                                unitStackAttackRolls.AddAttackRollResult(result);
                            }
                        }
                    }
                    results.Add(unitStackAttackRolls);
                }
            }
        }
        results.Sort((x, y) => x.GetAt(0).AttackSkill.CompareTo(y.GetAt(0).AttackSkill));
    }

    /// <summary>
    /// Prompts both attackers and defenders to cast unit creation spells
    /// </summary>
    /// <returns>Whether a unit creation spell was cast</returns>
    private bool CastUnitCreationSpells()
    {
        bool attackersDidIt = CastUnitCreationSpells(_attackers);
        bool defendersDidIt = CastUnitCreationSpells(_defenders);
        return attackersDidIt || defendersDidIt;
    }

    /// <summary>
    /// Prompts unit stacks capable of casting unit creation spells to do so
    /// </summary>
    /// <param name="summoners">Unit stacks capable of casting unit creation spells</param>
    /// <returns>Whether a unit creation spell was cast</returns>
    private bool CastUnitCreationSpells(List<UnitStack> summoners)
    {
        bool result = false;
        List<UnitStack> monsters = new List<UnitStack>();
        for (int i = 0; i < summoners.Count; i++)
        {
            List<Spell> conjurations = summoners[i].GetUnitType().GetSpellsOfType(Spell.SpellType.UNIT_CREATION);
            Province origin = summoners[i].GetProvinceToRetreat();
            for (int j = 0; j < conjurations.Count; j++)
            {
                UnitStack summonedStack = conjurations[j].Create(summoners);
                if (summonedStack != null)
                {
                    result = true;
                    summonedStack.SetProvinceToRetreat(origin);
                    monsters.Add(summonedStack);
                }
            }
        }
        summoners.AddRange(monsters);
        return result;
    }

    /// <summary>
    /// Prompts both attackers and defenders to cast defensive spells
    /// </summary>
    /// <returns>Whether a defensive spell was cast</returns>
    private bool CastDefensiveSpells()
    {
        bool attackersDidIt = CastDefensiveSpells(_attackers);
        bool defendersDidIt = CastDefensiveSpells(_defenders);
        return attackersDidIt || defendersDidIt;
    }

    /// <summary>
    /// Prompts unit stacks capable of casting defensive spells to do so
    /// </summary>
    /// <param name="casters">Unit stacks capable of casting defensive spells</param>
    /// <returns>Whether a defensive spell was cast</returns>
    private bool CastDefensiveSpells(List<UnitStack> casters)
    {
        bool result = false;
        for (int i = 0; i < casters.Count; i++)
        {
            List<Spell> spells = casters[i].GetUnitType().GetSpellsOfType(Spell.SpellType.DEFENSIVE);
            for (int j = 0; j < spells.Count; j++)
            {
                spells[j].CastOn(casters);
                result = true;
            }
        }
        return result;
    }

    /// <summary>
    /// Prompts both attackers and defenders to cast restorative spells
    /// </summary>
    /// <param name="casters">Unit stacks capable of casting restorative spells</param>
    /// <returns>Whether a restorative spell was cast</returns>
    private bool CastRestorativeSpells(List<UnitStack> casters)
    {
        bool result = false;
        for (int i = 0; i < casters.Count; i++)
        {
            List<Spell> spells = casters[i].GetUnitType().GetSpellsOfType(Spell.SpellType.RESTORATIVE);
            for (int j = 0; j < spells.Count; j++)
            {
                spells[j].CastOn(casters);
                result = true;
            }
        }
        return result;
    }

    /// <summary>
    /// Prompts both attackers and defenders to cast offensive spells
    /// </summary>
    private void CastOffensiveSpells()
    {
        CastOffensiveSpells(_attackers, _defenders);
        CastOffensiveSpells(_defenders, _attackers);
    }

    /// <summary>
    /// Prompts unit stacks capable of casting offensive spells to do so
    /// </summary>
    /// <param name="casters">Unit stacks capable of casting offensive spells</param>
    /// <param name="targets">Unit stacks that are valid targets for offensive spells</param>
    private void CastOffensiveSpells(List<UnitStack> casters, List<UnitStack> targets)
    {
        for (int i = 0; i < casters.Count; i++)
        {
            List<Spell> spells = casters[i].GetUnitType().GetSpellsOfType(Spell.SpellType.OFFENSIVE);
            for (int j = 0; j < spells.Count; j++)
            {
                spells[j].CastOn(targets);
            }
        }
    }

    /// <summary>
    /// Advance to the next turn phase
    /// </summary>
    private void AdvancePhase()
    {
        if (_currentPhase == TurnPhase.CLEANUP)
        {
            EndSpells(_attackers);
            EndSpells(_defenders);
            _currentPhase = 0;
            _currentTurn++;
        }
        else
        {
            _currentPhase = (TurnPhase)((int)_currentPhase + 1);
        }
    }

    /// <summary>
    /// Is the unit stack a valid target for the attack?
    /// </summary>
    /// <param name="target">Unit stack which is a potential target for the attack</param>
    /// <param name="attackRollResult">Attack roll result representing the attack</param>
    /// <returns>Whether the unit stack is a valid target for the attack</returns>
    private bool IsValidAttackTarget(UnitStack target, AttackRollResult attackRollResult)
    {
        if (_attackers.Contains(target) && _attackers.Contains(attackRollResult.UnitStack))
        {
            FileLogger.Trace("COMBAT", "Both " + attackRollResult.UnitStack.GetUnitType().GetName() + " and " + target.GetUnitType().GetName() + " belong to the attacker");
            return false;
        }

        if (_defenders.Contains(target) && _defenders.Contains(attackRollResult.UnitStack))
        {
            FileLogger.Trace("COMBAT", "Both " + attackRollResult.UnitStack.GetUnitType().GetName() + " and " + target.GetUnitType().GetName() + " belong to the defender");
            return false;
        }

        if (target.GetTotalQty() == 0)
        {
            FileLogger.Trace("COMBAT", "No defenders left in this stack");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Resolve an attack against the target unit stack
    /// </summary>
    /// <param name="target">Target unit stack</param>
    /// <param name="attackRollResult">Attack roll result representing the attack</param>
    /// <param name="useEstimates">Whether estimated results will be used or honest rolls will be made</param>
    /// <returns>Whether the attack was successfully resolved</returns>
    private bool ResolveAnAttackAgainstTarget(UnitStack target, AttackRollResult attackRollResult, bool useEstimates)
    {
        if (!IsValidAttackTarget(target, attackRollResult))
        {
            return false;
        }

        int positiveDieRoll = CombatHelper.Instance.RollDie();
        int negativeDieRoll = CombatHelper.Instance.RollDie();
        int defenseRoll = positiveDieRoll - negativeDieRoll;

        int defensiveSkill = CombatHelper.Instance.CalculateDefensiveSkill(attackRollResult.Attack, target);
        int shield = CombatHelper.Instance.CalculateShieldValue(attackRollResult.Attack, target);

        AttackResolutionEvent data = new AttackResolutionEvent(attackRollResult, target, positiveDieRoll, negativeDieRoll, defensiveSkill, shield);
        int totalDefense = defenseRoll + defensiveSkill + shield;

        int armor = CombatHelper.Instance.CalculateArmorValue(attackRollResult.Attack, target, attackRollResult.IsCritical);
        data.SetArmor(armor);

        int damage = 0;
        if (useEstimates)
        {
            damage = CombatHelper.Instance.EstimateAttackDamage(attackRollResult, target);
        }
        else
        {
            damage = CombatHelper.Instance.CalculateDamage(attackRollResult, target, totalDefense, armor);
        }
        data.SetDamage(damage);
        target.TakeDamage(damage);

        int totalAttack = attackRollResult.AttackRoll + attackRollResult.AttackSkill;
        if (useEstimates)
        {
            FileLogger.Trace("COMBAT", "Simulation: estimated damage = " + damage);
        }
        else
        {
            FileLogger.Trace("COMBAT", "Attack: " + totalAttack + ", defense: " + totalDefense + ", weapon damage: " + attackRollResult.FullDamage + ", armor: " + armor + ", resulting damage: " + damage);
        }

        bool deleted = false;
        for (int i = 0; i < _attackerRollResults.Count; i++)
        {
            if (_attackerRollResults[i].Contains(attackRollResult))
            {
                bool removed = _attackerRollResults[i].RemoveAttackRollResult(attackRollResult);
                if (!removed)
                {
                    FileLogger.Trace("COMBAT", "Failed to remove an attack roll result from a collection!");
                }
                if (_attackerRollResults[i].Count == 0)
                {
                    _attackerRollResults.RemoveAt(i);
                }
                deleted = true;
                break;
            }
        }

        if (!deleted)
        {
            for (int i = 0; i < _defenderRollResults.Count; i++)
            {
                if (_defenderRollResults[i].Contains(attackRollResult))
                {
                    bool removed = _defenderRollResults[i].RemoveAttackRollResult(attackRollResult);
                    if (!removed)
                    {
                        FileLogger.Trace("COMBAT", "Failed to remove an attack roll result from a collection!");
                    }
                    if (_defenderRollResults[i].Count == 0)
                    {
                        _defenderRollResults.RemoveAt(i);
                    }
                    break;
                }
            }
        }

        if (AttackResolved != null)
        {
            AttackResolved(this, data);
        }

        return true;
    }

    /// <summary>
    /// Remove empty unit stacks on both sides
    /// </summary>
    private void RemoveEmptyUnitStacks()
    {
        _attackers.RemoveAll(stack => stack.GetTotalQty() == 0);
        _defenders.RemoveAll(stack => stack.GetTotalQty() == 0);
    }

    /// <summary>
    /// End spells
    /// Remove summoned units, remove spell effects from regular ones
    /// </summary>
    private void EndSpells(List<UnitStack> units)
    {
        for (int i = units.Count - 1; i > -1; i--)
        {
            int unitTypeId = units[i].GetUnitType().GetId();
            if (units[i].IsAffectedBy(Spell.SpellType.UNIT_CREATION))
            {
				// summoned creatures' unity type id range
                if (unitTypeId >= 26 && unitTypeId <= 28)
                {
                    FileLogger.Trace("SPAWN", units[i].GetUnitType().GetName() + " is summoned. Removing it.");
                }
                units.RemoveAt(i);
            }
            else
            {
                if (unitTypeId >= 26 && unitTypeId <= 28)
                {
                    FileLogger.Error("SPAWN", units[i].GetUnitType().GetName() + " is pretending to be a regular unit. Removing it.");
                    units.RemoveAt(i);
                }
                else
                {
                    units[i].RemoveAllSpells();
                }
            }
        }
    }

    /// <summary>
    /// End combat
    /// Perform cleanup, run survival checks on wounded units
    /// </summary>
    private void EndCombat()
    {
        FileLogger.Trace("COMBAT", "Combat is over, cleaning up");

        // cleanup
        _attackerRollResults.Clear();
        _defenderRollResults.Clear();

        EndSpells(_attackers);
        EndSpells(_defenders);

        // combat never ends during the divine or cleanup phase,
        // so healing is bypassed - call the spells directly
        // unless the current phase is the start of the turn,
        // which means that the attacker is retreating
        if (_currentPhase != TurnPhase.START)
        {
            CastRestorativeSpells(_attackers);
            CastRestorativeSpells(_defenders);
        }

        if (WoundChecksStarted != null)
        {
            WoundChecksStarted(this, EventArgs.Empty);
        }

        FileLogger.Trace("COMBAT", "Wound checks started");

        int woundCheckBonus = 0;
        for (int i = 0; i < _attackers.Count; i++)
        {
            List<Spell> spells = _attackers[i].GetUnitType().GetSpellsOfType(Spell.SpellType.RESTORATIVE);
            if (spells.Count > 0)
            {
                woundCheckBonus = 2;
                break;
            }
        }
        for (int i = 0; i < _attackers.Count; i++)
        {
            _attackers[i].PerformWoundChecks(woundCheckBonus);
        }

        woundCheckBonus = 0;
        for (int i = 0; i < _defenders.Count; i++)
        {
            List<Spell> spells = _defenders[i].GetUnitType().GetSpellsOfType(Spell.SpellType.RESTORATIVE);
            if (spells.Count > 0)
            {
                woundCheckBonus = 2;
                break;
            }
        }
        for (int i = 0; i < _defenders.Count; i++)
        {
            _defenders[i].PerformWoundChecks(woundCheckBonus);
        }

        _attackers.RemoveAll(stack => stack.GetTotalQty() == 0);
        _defenders.RemoveAll(stack => stack.GetTotalQty() == 0);

        _isCombatOver = true;

        FileLogger.Trace("COMBAT", "Combat is finished");
        if (Finished != null)
        {
            Finished(this, EventArgs.Empty);
        }
    }

    #endregion
}
