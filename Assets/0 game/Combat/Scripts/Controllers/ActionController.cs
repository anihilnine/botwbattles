using UnityEngine;

public sealed class ActionController : MonoBehaviour
{
    public ActionData[] actions;
    
    public ActionType CurrentAction { get; private set; } = ActionType.Locomotion;
    public ActionData CurrentAttack { get; private set; }

    // ───────── Output events (one-frame signals) ─────────
    public bool AttackStarted { get; private set; }
    public bool ComboStarted  { get; private set; }
    public bool DodgeStarted  { get; private set; }
    public bool Interrupted   { get; private set; }

    // ───────── Queue ─────────
    private ActionType _queued = ActionType.None;
    private ActionData _queuedAttack;
    private float _queueUntil;

    private const float QUEUE_TIME = 0.2f;

    // Cached windows
    private bool _canCombo;
    private bool _canDodgeCancel;

    /// Call once per frame
    public void Tick(float attackNormalizedTime, bool gotHitStun)
    {
        // Reset outputs
        AttackStarted = ComboStarted = DodgeStarted = Interrupted = false;

        // Hard interrupt
        if (gotHitStun)
        {
            ClearQueue();
            Interrupt(ActionType.HitStun);
            return;
        }

        // Update timing windows
        if (CurrentAction == ActionType.Attack && CurrentAttack != null)
        {
            _canCombo =
                attackNormalizedTime >= CurrentAttack.comboStartNormalized &&
                attackNormalizedTime <= CurrentAttack.comboEndNormalized;

            _canDodgeCancel =
                attackNormalizedTime >= CurrentAttack.dodgeCancelStartNormalized &&
                attackNormalizedTime <= CurrentAttack.dodgeCancelEndNormalized;

            // Natural completion
            if (attackNormalizedTime >= CurrentAttack.recoveryEndNormalized)
                EndAction();
        }

        TryConsumeQueue();
    }

    // ───────── Requests (from input / AI) ─────────

    public void RequestAttack(ActionData attack)
    {
        if (CanAttackNow())
        {
            if (CurrentAction == ActionType.Attack)
            {
                StartCombo(attack);
            }
            else
            {
                StartAttack(attack);
            }
        }
        else
        {
            QueueAttack(attack);
        }
    }

    public void RequestDodge()
    {
        if (CanDodgeNow())
        {
            StartDodge();
        }
        else
        {
            QueueDodge();
        }
    }

    // ───────── Queue handling ─────────

    private void QueueAttack(ActionData attack)
    {
        _queued = ActionType.Attack;
        _queuedAttack = attack;
        _queueUntil = Time.time + QUEUE_TIME;
    }

    private void QueueDodge()
    {
        _queued = ActionType.Dodge;
        _queueUntil = Time.time + QUEUE_TIME;
    }

    private void TryConsumeQueue()
    {
        if (_queued == ActionType.None)
            return;

        if (Time.time > _queueUntil)
        {
            ClearQueue();
            return;
        }

        if (_queued == ActionType.Attack && CanAttackNow())
        {
            if (CurrentAction == ActionType.Attack)
                StartCombo(_queuedAttack);
            else
                StartAttack(_queuedAttack);

            ClearQueue();
        }
        else if (_queued == ActionType.Dodge && CanDodgeNow())
        {
            StartDodge();
            ClearQueue();
        }
    }

    private void ClearQueue()
    {
        _queued = ActionType.None;
        _queuedAttack = null;
    }

    // ───────── Permissions ─────────

    private bool CanAttackNow()
    {
        return CurrentAction == ActionType.Locomotion ||
              (CurrentAction == ActionType.Attack && _canCombo);
    }

    private bool CanDodgeNow()
    {
        return CurrentAction == ActionType.Locomotion ||
              (CurrentAction == ActionType.Attack && _canDodgeCancel);
    }

    // ───────── Transitions ─────────

    private void StartAttack(ActionData attack)
    {
        CurrentAction = ActionType.Attack;
        CurrentAttack = attack;
        AttackStarted = true;
    }

    private void StartCombo(ActionData attack)
    {
        CurrentAction = ActionType.Attack;
        CurrentAttack = attack;
        ComboStarted = true;
    }

    private void StartDodge()
    {
        CurrentAction = ActionType.Dodge;
        CurrentAttack = null;
        DodgeStarted = true;
    }

    private void EndAction()
    {
        CurrentAction = ActionType.Locomotion;
        CurrentAttack = null;
    }

    private void Interrupt(ActionType action)
    {
        CurrentAction = action;
        CurrentAttack = null;
        Interrupted = true;
    }
}