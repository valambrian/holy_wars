using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombatScreen: MonoBehaviour
{
    [SerializeField]
    private UnitStackView _unitStackViewPrefab;

    [SerializeField]
    private UnitTypeUIView _unitTypeView;

    [SerializeField]
    private AttackRollResultsView _attackRollResultsPrefab;

    [SerializeField]
    private AttackResolutionUIView _attackResolutionView;

    [SerializeField]
    private WoundCheckUIView _woundCheckEvent;

    [SerializeField]
    private Button _attackButton;

    [SerializeField]
    private Button _retreatButton;

    [SerializeField]
    private Button _skipStackButton;

    //[SerializeField]
    //private Button _skipPhaseButton;

    [SerializeField]
    private Button _skipTurnButton;

    [SerializeField]
    private Text _phaseDescription;

    [SerializeField]
    private GameObject[] _attackersSpawnPoints;

    [SerializeField]
    private GameObject[] _defendersSpawnPoints;

    [SerializeField]
    private GameObject[] _attackerRollsSpawnPoints;

    [SerializeField]
    private GameObject[] _defenderRollsSpawnPoints;

    [SerializeField]
    private MouseClickListener _attackerLeftArrow;

    [SerializeField]
    private MouseClickListener _attackerRightArrow;

    [SerializeField]
    private MouseClickListener _defenderLeftArrow;

    [SerializeField]
    private MouseClickListener _defenderRightArrow;

    [SerializeField]
    private GameObject _notification;

    private Combat _model;

    private Dictionary<UnitStack, UnitStackView> _attackerStackViews = new Dictionary<UnitStack, UnitStackView>();
    private Dictionary<UnitStack, UnitStackView> _defenderStackViews = new Dictionary<UnitStack, UnitStackView>();
    private Dictionary<UnitStack, AttackRollResultsView> _attackViews = new Dictionary<UnitStack, AttackRollResultsView>();
    private UnitStack _currentAttacker = null;
    private UnitStack _currentDefender = null;
    private int _attackerStackViewsOffset = 0;
    private int _defenderStackViewsOffset = 0;
    private bool _skipStack = false;
    private bool _skipPhase = false;
    private bool _skipTurn = false;

    void Start()
    {
        _model = GameSingleton.Instance.Game.GetCombat();
        if (_model != null)
        {
            _model.AttackResolved += OnAttackResolved;
            _model.WoundChecksStarted += OnWoundChecksStarted;
            _model.Finished += OnCombatOver;

            _unitTypeView.gameObject.SetActive(false);

            _attackResolutionView.gameObject.SetActive(false);
            _attackResolutionView.DisplayCompleted += OnAttackResolutionPlayedOut;

            OnCombatTurnEnd();

            _woundCheckEvent.gameObject.SetActive(false);

            _notification.SetActive(false);

            CreateUnitStackViewsForSide(_attackersSpawnPoints);
            CreateUnitStackViewsForSide(_defendersSpawnPoints);
            _attackerLeftArrow.MouseClickDetected += OnArrowButtonClicked;
            _attackerRightArrow.MouseClickDetected += OnArrowButtonClicked;
            _defenderLeftArrow.MouseClickDetected += OnArrowButtonClicked;
            _defenderRightArrow.MouseClickDetected += OnArrowButtonClicked;

            UpdateNavigationButtonsState();
        }
    }

    private void OnCombatTurnEnd()
    {
        _attackButton.gameObject.SetActive(true);
        _retreatButton.gameObject.SetActive(_model.IsAttackerPC());
        _skipStackButton.gameObject.SetActive(false);
        //_skipPhaseButton.gameObject.SetActive(false);
        _skipTurnButton.gameObject.SetActive(false);
        _phaseDescription.text = "Attack Or Retreat?";
    }

    private void OnCombatTurnStart()
    {
        _attackButton.gameObject.SetActive(false);
        _retreatButton.gameObject.SetActive(false);
        _skipStackButton.gameObject.SetActive(true);
        //_skipPhaseButton.gameObject.SetActive(true);
        _skipTurnButton.gameObject.SetActive(true);
        _skipStack = false;
        _skipPhase = false;
        _skipTurn = false;
        FileLogger.Trace("COMBAT VIEW", "Setting all skips to false");
    }

    private void UpdateUnitStackViews()
    {
        if (_model.GetUnitStacks(true).Count <= _attackersSpawnPoints.Length)
        {
            _attackerStackViewsOffset = 0;
        }
        if (_model.GetUnitStacks(false).Count <= _defendersSpawnPoints.Length)
        {
            _defenderStackViewsOffset = 0;
        }
        // NOTE: potentially, we can be smarter
        // and destroy fewer stack views
        // instead assigning new models to existing stacks
        // but since it's only a prototype, use brute force
        DestroyAllUnitStackViews();
        CreateUnitStackViewsForSide(_attackersSpawnPoints);
        CreateUnitStackViewsForSide(_defendersSpawnPoints);
        UpdateNavigationButtonsState();
    }

    private void DestroyAllUnitStackViews()
    {
        DestroyMultipleUnitStackViews(_attackerStackViews);
        DestroyMultipleUnitStackViews(_defenderStackViews);
    }

    private void DestroyMultipleUnitStackViews(Dictionary<UnitStack, UnitStackView> stackViews)
    {
        List<UnitStack> stacks = new List<UnitStack>(stackViews.Keys);

        for (int i = 0; i < stacks.Count; i++)
        {
            if (stackViews.ContainsKey(stacks[i]))
            {
                DestroyUnitStackView(stacks[i]);
            }
        }
        stackViews.Clear();
    }

    private void DestroyUnitStackView(UnitStack stack)
    {
        FileLogger.Trace("VIEW", "Destroying " + stack.GetUnitType().GetName() + "'s stack view");
        bool isAttackerStackDestroyed = _attackerStackViews.ContainsKey(stack);
        Dictionary<UnitStack, UnitStackView> stackViews = isAttackerStackDestroyed ? _attackerStackViews : _defenderStackViews;

        stackViews[stack].ExplosionAnimationCompleted -= OnExplosionAnimationCompleted;
        stackViews[stack].StackSelected -= OnPlayerSelectedDefendingUnitStack;
        stackViews[stack].StackInspected -= OnStackInspected;
        stackViews[stack].StackInspectionEnded -= OnUnitTypeInspectionEnded;
        Destroy(stackViews[stack].gameObject);
        stackViews.Remove(stack);
    }

    private void CreateUnitStackViewsForSide(GameObject[] spawnPoints)
    {
        bool areProcessingAttackers = spawnPoints == _attackersSpawnPoints;
        List<UnitStack> stacks = _model.GetUnitStacks(areProcessingAttackers);
        Dictionary<UnitStack, UnitStackView> stackViews = areProcessingAttackers ? _attackerStackViews : _defenderStackViews;
        int offset = areProcessingAttackers ? _attackerStackViewsOffset : _defenderStackViewsOffset;

        int count = Mathf.Min(stacks.Count - offset, spawnPoints.Length);
        for (int i = 0; i < count; i++)
        {
            UnitStack currentStack = stacks[i + offset];

            UnitStackView view = (UnitStackView)Instantiate(_unitStackViewPrefab, spawnPoints[i].transform.position, Quaternion.identity);
            view.transform.parent = spawnPoints[i].transform;
            view.SetModel(currentStack);
            if (currentStack.GetProvinceToRetreat().GetOwnersFaction().IsPC())
            {
                view.StackSelected += OnPlayerSelectedDefendingUnitStack;
            }
            view.ExplosionAnimationCompleted += OnExplosionAnimationCompleted;
            view.StackInspected += OnStackInspected;
            view.StackInspectionEnded += OnUnitTypeInspectionEnded;
            stackViews[currentStack] = view;
        }
    }

    private void OnPlayerSelectedDefendingUnitStack(object sender, EventArgs args)
    {
        if (_model.IsPlayerTurn())
        {
            UnitStack stack = ((UnitStackView)sender).GetModel();
            if (_attackViews.Count > 0)
            {
                _currentDefender = stack;
                _model.SetDefendingStack(stack);
                FileLogger.Trace("COMBAT VIEW", "Player selected " + _currentDefender.GetUnitType().GetName() + " as a defender");
                Dictionary<UnitStack, UnitStackView> stackViews = _attackerStackViews.ContainsKey(_currentDefender) ? _attackerStackViews : _defenderStackViews;
                stackViews[_currentDefender].PlayExplosionAnimation();
            }
            else
            {
                FileLogger.Error("COMBAT", "A defending unit stack is chosen, but there are no attacks to resolve.");
            }
        }
    }

    private void OnExplosionAnimationCompleted(object sender, EventArgs args)
    {
        FileLogger.Trace("COMBAT", "OnExplosionAnimationCompleted: resolving the current attack");
        _model.ResolveCurrentAttack(false);
    }

    private void UpdateAttackViews()
    {
        DestroyAttackViews();
        CreateAttackViewsForSide(_attackerRollsSpawnPoints);
        CreateAttackViewsForSide(_defenderRollsSpawnPoints);
        _phaseDescription.text = "Phase: " + _model.GetPhaseDescription();
        if (_model.IsPlayerTurn())
        {
            _phaseDescription.text += " - select defenders";
        }
    }

    private void DestroyAttackViews()
    {
        List<UnitStack> attackingUnitStacks = new List<UnitStack>(_attackViews.Keys);
        for (int i = 0; i < attackingUnitStacks.Count; i++)
        {
            if (_attackViews.ContainsKey(attackingUnitStacks[i]) && _attackViews[attackingUnitStacks[i]] != null)
            {
                _attackViews[attackingUnitStacks[i]].MouseOver -= OnAttackRollsInspected;
                _attackViews[attackingUnitStacks[i]].MouseLeft -= OnUnitTypeInspectionEnded;
                Destroy(_attackViews[attackingUnitStacks[i]].gameObject);
            }
        }
        _attackViews.Clear();
    }

    private void CreateAttackViewsForSide(GameObject[] spawnPoints)
    {
        bool areProcessingAttackers = spawnPoints == _attackerRollsSpawnPoints;
        if (_model.GetTotalUnitsQuantity(!areProcessingAttackers) > 0)
        {
            List<AttackRollResultsCollection> rolls = _model.GetAttackRollResults(areProcessingAttackers);
            int count = Mathf.Min(rolls.Count, spawnPoints.Length);

            for (int i = 0; i < count; i++)
            {
                AttackRollResultsView attackRollResultsView = (AttackRollResultsView)Instantiate(_attackRollResultsPrefab, spawnPoints[i].transform.position, Quaternion.identity);
                attackRollResultsView.transform.parent = spawnPoints[i].transform;
                attackRollResultsView.SetModel(rolls[i]);
                attackRollResultsView.MouseOver += OnAttackRollsInspected;
                attackRollResultsView.MouseLeft += OnUnitTypeInspectionEnded;
                _attackViews[rolls[i].GetUnitStack()] = attackRollResultsView;
            }
        }
    }

    /// <summary>
    /// Called when "Attack" button is pressed
    /// If the are no unresolved attacks this phase,
    /// move forward
    /// </summary>
    public void Attack()
    {
        if (!_model.AreThereUnresolvedAttacks())
        {
            FileLogger.Trace("COMBAT", "Attack: There are no attacks to resolve");
            OnCombatTurnStart();
            ProcessCombatTurn();
        }
    }

    public void Retreat()
    {
        _model.Retreat();
    }

    /// <summary>
    /// Handles interaction with the model to control combat flow
    /// (wait for user input when necessary, move forward when not).
    /// </summary>
    private void ProcessCombatTurn()
    {
        FileLogger.Trace("COMBAT VIEW", "ProcessCombatTurn");
        // multiple phases might be skipped if no action was performed
        _model.PerformPhaseActions();

        switch (_model.GetCurrentTurnPhase())
        {
            case Combat.TurnPhase.START:
                // unit stacks could be created or destroyed - update the stack views
                UpdateUnitStackViews();
                OnCombatTurnEnd();
                //_attackButton.gameObject.SetActive(true);
                break;
            case Combat.TurnPhase.CLEANUP:
                // divine phase just happened
                ProcessDivinePhase();
                break;
            default:
                ProcessPhaseWithAttacks();
                break;
        }
    }

    private void ProcessDivinePhase()
    {
        UnitStackView signalView = null;
        List<UnitStackView> views = new List<UnitStackView>(_attackerStackViews.Values);
        for (int i = 0; i < views.Count; i++)
        {
            if (views[i].GetModel().IsAffectedBy("Heal"))
            {
                signalView = views[i];
                views[i].PlayHealAnimation();
            }
        }
        views = new List<UnitStackView>(_defenderStackViews.Values);
        for (int i = 0; i < views.Count; i++)
        {
            if (views[i].GetModel().IsAffectedBy("Heal"))
            {
                signalView = views[i];
                views[i].PlayHealAnimation();
            }
        }
        if (signalView != null)
        {
            signalView.HealAnimationCompleted += OnHealAnimationPlayedOut;
        }
        else
        {
            ProcessCombatTurn();
        }
    }

    /// <summary>
    /// Handles defender selection and related animations.
    /// </summary>
    private void ProcessPhaseWithAttacks()
    {
        FileLogger.Trace("COMBAT VIEW", "ProcessPhaseWithAttacks");
        // unit stacks could be created or destroyed - update the stack views
        UpdateUnitStackViews();
        if (!_model.AreThereUnresolvedAttacks())
        {
            _skipPhase = false;
            ProcessCombatTurn();
        }
        else
        {
            // show all attacks
            UpdateAttackViews();

            AttackRollResultsCollection currentAttackBatch = _model.SelectAttackRollResultsCollection();
            if (currentAttackBatch != null)
            {
                FileLogger.Trace("COMBAT", "Selected attacks by " + currentAttackBatch.GetUnitStack().GetUnitType().GetName());
                if (currentAttackBatch.GetUnitStack() != _currentAttacker)
                {
                    SetNewStackForCurrentAttackView(currentAttackBatch.GetUnitStack());
                    // a new defender will be selected against a new attack
                    _currentDefender = null;
                    _skipStack = false;
                    FileLogger.Trace("COMBAT VIEW", "ProcessPhaseWithAttacks: Setting skip stack to false");
                }

                if (!_model.IsPlayerTurn())
                {
                    SelectAnNPCStackAsTarget();
                }
                else
                {
                    // if it's the player's turn
                    // and he already selected a defending stack, resolve the attack
                    if (_currentDefender != null && _currentDefender.GetTotalQty() > 0)
                    {
                        FileLogger.Trace("COMBAT", "Resolving an attack against " + _currentDefender.GetUnitType().GetName());
                        _model.ResolveCurrentAttack(false);
                    }
                    // else wait until the player will click on a stack view
                }
            }
            // else panic - there should be no case when there are unresolved attacks
            // but the model can't return an attack colleciton
        }
    }

    /// <summary>
    /// Stops "object selected" animation and probably destroys the current attacks view.
    /// Starts "object selected" animation on the new one.
    /// </summary>
    private void SetNewStackForCurrentAttackView(UnitStack stack)
    {
        // handle the attacks view related to the current attacker
        if (_currentAttacker != null && _attackViews.ContainsKey(_currentAttacker))
        {
            // stop blinking
            _attackViews[_currentAttacker].GetComponent<Selectable>().IsSelected = false;
            // most likely, we exhausted attacks in this bunch - delete the view
            if (_attackViews[_currentAttacker].GetModel().Count == 0)
            {
                Destroy(_attackViews[_currentAttacker].gameObject);
            }
        }
        _currentAttacker = stack;
        // start blinking
        _attackViews[_currentAttacker].GetComponent<Selectable>().IsSelected = true;
    }

    /// <summary>
    /// Stops "object selected" animation and probably destroys the current attacks view
    /// Starts "object selected" animation on the new one
    /// </summary>
    private void SelectAnNPCStackAsTarget()
    {
        FileLogger.Trace("COMBAT VIEW", "SelectAnNPCStackAsTarget");
        UnitStack target = _model.SelectDefendingStack();
        if (target != null)
        {
            FileLogger.Trace("COMBAT", "Selected " + target.GetUnitType().GetName() + " as a target");
            if (_currentDefender != target)
            {
                _currentDefender = target;

                // cover the case when the game selects
                // an NPC defender who's offscreen

                bool areProcessingAttackers = false;
                List<UnitStack> stacks = _model.GetUnitStacks(areProcessingAttackers);
                int index = stacks.FindIndex(a => a == _currentDefender);
                if (index == -1)
                {
                    areProcessingAttackers = true;
                    stacks = _model.GetUnitStacks(areProcessingAttackers);
                    index = stacks.FindIndex(a => a == _currentDefender);
                }
                Dictionary<UnitStack, UnitStackView> stackViews = areProcessingAttackers ? _attackerStackViews : _defenderStackViews;

                // these variables are defined here so that they can be used in an error message below
                int offset = -1;
                int numberOfSpawnPoints = -1;

                if (!stackViews.ContainsKey(_currentDefender))
                {
                    numberOfSpawnPoints = areProcessingAttackers ? _attackersSpawnPoints.Length : _defendersSpawnPoints.Length;
                    offset = index >= numberOfSpawnPoints ? index + 1 - numberOfSpawnPoints : 0;

                    FileLogger.Trace("COMBAT VIEW", "Current defender: " + _currentDefender.GetUnitType().GetName() + ", index = " + index + ", number of spawn points: " + numberOfSpawnPoints + ", offset: " + offset + ")");

                    if (areProcessingAttackers)
                    {
                        _attackerStackViewsOffset = offset;
                    }
                    else
                    {
                        _defenderStackViewsOffset = offset;
                    }
                    UpdateUnitStackViews();
                }

                if (stackViews.ContainsKey(_currentDefender))
                {
                    // the attack will be resoved after the explosion animation plays out
                    stackViews[_currentDefender].PlayExplosionAnimation();
                }
                else
                {
                    FileLogger.Error("COMBAT", "Selected " + _currentDefender.GetUnitType().GetName() + " as a target, but it doesn't have a view! (index = " + index + ", number of spawn points: " + numberOfSpawnPoints + ", offset: " + offset + ")");
                }
            }
            else
            {
                FileLogger.Trace("COMBAT", "SelectAnNPCStackAsTarget: resolving the current attack");
                // explosion animation already played for this defender -
                // go ahead and resolve the attack
                _model.ResolveCurrentAttack(false);
            }
        }
        // else panic - if it's not player's turn select a defender, the model should not return null
    }

    /// <summary>
    /// Starts attack resolution animation
    /// </summary>
    private void OnAttackResolved(object sender, AttackResolutionEvent args)
    {
        if (ShouldPlayAttackResolutionAnimation())
        {
            FileLogger.Trace("COMBAT VIEW", "Playing attack resolution of " + args.GetAttack().GetUnitType().GetName() + " vs " + args.GetTarget().GetUnitType().GetName());
            _attackResolutionView.gameObject.SetActive(true);
            _attackResolutionView.SetModel(args);
        }
        else
        {
            SkipAttackResolutionAnimation();
        }
    }

    private void SkipAttackResolutionAnimation()
    {
        StartCoroutine("WaitingCoroutine");
    }

    private IEnumerator WaitingCoroutine()
    {
        yield return new WaitForSeconds(.1f);
        ProcessAttackResolutionResults();
    }


    public void SkipStack()
    {
        if (_attackResolutionView.gameObject.activeSelf && !_skipStack)
        {
            _skipStack = true;
            _attackResolutionView.StopAnimation();
            FileLogger.Trace("COMBAT VIEW", "Setting skip stack to true");
        }
    }

    public void SkipPhase()
    {
        if (_attackResolutionView.gameObject.activeSelf)
        {
            _skipPhase = true;
            _attackResolutionView.StopAnimation();
            FileLogger.Trace("COMBAT VIEW", "Setting skip phase to true");
        }
    }

    public void SkipTurn()
    {
        if (_attackResolutionView.gameObject.activeSelf)
        {
            _skipTurn = true;
            _attackResolutionView.StopAnimation();
            FileLogger.Trace("COMBAT VIEW", "Setting skip turn to true");
        }
    }

    private bool ShouldPlayAttackResolutionAnimation()
    {
        FileLogger.Trace("COMBAT VIEW", "Skips: stack = " + _skipStack.ToString() + ", phase = " + _skipPhase.ToString() + ", turn = " + _skipTurn.ToString());
        return !_skipStack && !_skipPhase && !_skipTurn;
    }

    /// <summary>
    /// Cleans up results of resolution
    /// </summary>
    private void OnAttackResolutionPlayedOut(object sender, EventArgs args)
    {
        ProcessAttackResolutionResults();
    }

    /// <summary>
    /// Cleans up results of resolution
    /// </summary>
    private void ProcessAttackResolutionResults()
    {
        FileLogger.Trace("COMBAT VIEW", "ProcessAttackResolutionResults");
        if (_attackViews[_currentAttacker].GetModel().Count == 0)
        {
            FileLogger.Trace("COMBAT VIEW", "Destroying " + _currentAttacker.GetUnitType().GetName() + "'s attacks view");
            Destroy(_attackViews[_currentAttacker].gameObject);
            _attackViews.Remove(_currentAttacker);
            _currentAttacker = null;
        }
        else
        {
            FileLogger.Trace("COMBAT VIEW", "Updating " + _currentAttacker.GetUnitType().GetName() + "'s attacks view");
            _attackViews[_currentAttacker].UpdateView();
        }
        if (_currentDefender.GetTotalQty() == 0)
        {
            DestroyUnitStackView(_currentDefender);
            UpdateUnitStackViews();
            _currentDefender = null;
        }
        else
        {
            FileLogger.Trace("COMBAT VIEW", "Updating " + _currentDefender.GetUnitType().GetName() + "'s stack view");
            Dictionary<UnitStack, UnitStackView> stackViews = _attackerStackViews.ContainsKey(_currentDefender) ? _attackerStackViews : _defenderStackViews;
            stackViews[_currentDefender].UpdateView();
        }
        // if the attacker is reset, reset the defender, too
        // because a different defender will likely to be selected
        // against a new attack
        if (_currentAttacker == null)
        {
            _currentDefender = null;
        }
        if (_attackResolutionView.gameObject.activeSelf)
        {
            _attackResolutionView.gameObject.SetActive(false);
        }
        _model.PerformEndOfCombatCheck();
        if (!_model.IsCombatOver())
        {
            ProcessCombatTurn();
        }
    }

    /// <summary>
    /// Updates stack views after heling animation(s) played out
    /// </summary>
    private void OnHealAnimationPlayedOut(object sender, EventArgs args)
    {
        UnitStackView view = (UnitStackView)sender;
        view.HealAnimationCompleted -= OnHealAnimationPlayedOut;
        UpdateUnitStackViews();
        _model.PerformEndOfCombatCheck();
        if (!_model.IsCombatOver())
        {
            ProcessCombatTurn();
        }
    }

    private void OnCombatOver(object sender, EventArgs args)
    {
        _model.Finished -= OnCombatOver;
        StartCoroutine("PlayEndOfCombatAnimations");
    }

    private void OnWoundChecksStarted(object sender, EventArgs args)
    {
        _model.WoundChecksStarted -= OnWoundChecksStarted;
        DestroyAttackViews();
        //UnityEngine.SceneManagement.SceneManager.LoadScene("StrategicMap");
    }

    private IEnumerator PlayEndOfCombatAnimations()
    {
        float pause = 0.5f;
        Dictionary<UnitStack, UnitStackView> stackViews = _model.IsAttackerPC() ? _attackerStackViews : _defenderStackViews;
        List<UnitStack> stacks = new List<UnitStack>(stackViews.Keys);
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i].GetProvinceToRetreat().GetOwnersFaction().IsPC() &&
                stackViews.ContainsKey(stacks[i]) &&
                stackViews[stacks[i]].GetWoundCheckEvents().Count > 0)
            {
                _woundCheckEvent.gameObject.SetActive(true);
                UnitType unitType = stacks[i].GetUnitType();
                List<WoundCheckEvent> events = stackViews[stacks[i]].GetWoundCheckEvents();
                for (int j = 0; j < events.Count; j++)
                {
                    _woundCheckEvent.SetModel(events[j], unitType);
                    _woundCheckEvent.DisplayTargetNumber();
                    yield return new WaitForSeconds(pause);
                    _woundCheckEvent.DisplayRoll();
                    yield return new WaitForSeconds(pause);
                    _woundCheckEvent.DisplayUnitNumbers();
                    yield return new WaitForSeconds(pause);
                }
            }
        }
        _woundCheckEvent.gameObject.SetActive(false);

        UnityEngine.SceneManagement.SceneManager.LoadScene("StrategicMap");
    }

    private void UpdateNavigationButtonsState()
    {
        if (_model.GetUnitStacks(true).Count <= _attackersSpawnPoints.Length)
        {
            DisableAttackerButtons();
        }
        else
        {
            EnableAttackerButtons();
        }
        if (_model.GetUnitStacks(false).Count <= _defendersSpawnPoints.Length)
        {
            DisableDefenderButtons();
        }
        else
        {
            EnableDefenderButtons();
        }
    }

    private void DisableDefenderButtons()
    {
        _defenderLeftArrow.gameObject.SetActive(false);
        _defenderRightArrow.gameObject.SetActive(false);

        _defenderLeftArrow.MouseClickDetected -= OnArrowButtonClicked;
        _defenderRightArrow.MouseClickDetected -= OnArrowButtonClicked;
    }

    private void EnableDefenderButtons()
    {
        _defenderLeftArrow.gameObject.SetActive(true);
        _defenderRightArrow.gameObject.SetActive(true);

        _defenderLeftArrow.MouseClickDetected += OnArrowButtonClicked;
        _defenderRightArrow.MouseClickDetected += OnArrowButtonClicked;
    }

    private void DisableAttackerButtons()
    {
        _attackerLeftArrow.gameObject.SetActive(false);
        _attackerRightArrow.gameObject.SetActive(false);

        _attackerLeftArrow.MouseClickDetected -= OnArrowButtonClicked;
        _attackerRightArrow.MouseClickDetected -= OnArrowButtonClicked;
    }

    private void EnableAttackerButtons()
    {
        _attackerLeftArrow.gameObject.SetActive(true);
        _attackerRightArrow.gameObject.SetActive(true);

        _attackerLeftArrow.MouseClickDetected += OnArrowButtonClicked;
        _attackerRightArrow.MouseClickDetected += OnArrowButtonClicked;
    }

    private void OnArrowButtonClicked(object sender, EventArgs args)
    {
        if (_model.IsPlayerTurn())
        {
            MouseClickListener listener = (MouseClickListener)sender;
            bool needsAnUpdate = false;
            if (listener == _defenderLeftArrow && _defenderStackViewsOffset > 0)
            {
                _defenderStackViewsOffset--;
                needsAnUpdate = true;
            }
            if (listener == _defenderRightArrow && _defenderStackViewsOffset < _model.GetUnitStacks(false).Count - _defendersSpawnPoints.Length)
            {
                _defenderStackViewsOffset++;
                needsAnUpdate = true;
            }
            if (listener == _attackerLeftArrow && _attackerStackViewsOffset > 0)
            {
                _attackerStackViewsOffset--;
                needsAnUpdate = true;
            }
            if (listener == _attackerRightArrow && _attackerStackViewsOffset < _model.GetUnitStacks(true).Count - _attackersSpawnPoints.Length)
            {
                _attackerStackViewsOffset++;
                needsAnUpdate = true;
            }
            if (needsAnUpdate)
            {
                UpdateUnitStackViews();
            }
        }
    }

    void OnDestroy()
    {
        _model.AttackResolved -= OnAttackResolved;
        _model.WoundChecksStarted -= OnWoundChecksStarted;
        _model.Finished -= OnCombatOver;

        _attackResolutionView.DisplayCompleted -= OnAttackResolutionPlayedOut;

        _attackerLeftArrow.MouseClickDetected -= OnArrowButtonClicked;
        _attackerRightArrow.MouseClickDetected -= OnArrowButtonClicked;
        _defenderLeftArrow.MouseClickDetected -= OnArrowButtonClicked;
        _defenderRightArrow.MouseClickDetected -= OnArrowButtonClicked;
    }

    private void OnStackInspected(object sender, EventArgs args)
    {
        UnitStackView view = (UnitStackView)sender;
        _unitTypeView.SetModel(view.GetModel().GetUnitType());
        _unitTypeView.gameObject.SetActive(true);
    }

    private void OnAttackRollsInspected(object sender, EventArgs args)
    {
        AttackRollResultsView view = (AttackRollResultsView)sender;
        _unitTypeView.SetModel(view.GetModel().GetUnitType());
        _unitTypeView.gameObject.SetActive(true);
    }

    private void OnUnitTypeInspectionEnded(object sender, EventArgs args)
    {
        _unitTypeView.gameObject.SetActive(false);
    }

    public void CloseHelpWindow()
    {
        _notification.SetActive(false);
    }

    void Update()
    {
        if (Input.GetButtonUp("Help") && _model.IsPlayerTurn())
        {
            _notification.SetActive(true);
        }
    }

}
