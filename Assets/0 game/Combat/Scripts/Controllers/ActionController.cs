using System;
using System.Linq;
using UnityEngine;

public sealed class ActionController : MonoBehaviour
{
    private const float QUEUE_TIME = 0.2f;
    
    public ActionData[] actions;
    
    public ActionType currentAction = ActionType.Locomotion;
    public ActionData currentAttack;

    // ───────── Output events (one-frame signals) ─────────
    public bool attackStarted;
    public bool comboStarted;
    public bool dodgeStarted;
    public bool dodgeEnded;
    public bool interrupted;
    public bool hitStarted;
    public bool breatheStarted;
    public bool breatheEnded;
    public bool idleStarted;
    public bool idleEnded;
    public bool walkStarted;
    public bool walkEnded;

    // ───────── Queue ─────────
    public ActionType queued = ActionType.None;
    public ActionData queuedAttack;
    public float queueUntil;

    // Cached windows
    public bool canCombo;
    public bool canDodgeCancel;
    
    public ActionData attackAction;
    public ActionData attackAction2;
    public ActionData dodgeAction;
    public ActionData hitAction;
    public ActionData breatheAction;
    public ActionData idleAction;
    public ActionData walkAction;

    private void Awake()
    {
        attackAction = actions.FirstOrDefault(x => x.key == "attack");
        attackAction2 = actions.FirstOrDefault(x => x.key == "attack2");
        dodgeAction = actions.FirstOrDefault(x => x.key == "dodge");
        hitAction = actions.FirstOrDefault(x => x.key == "hit");
        breatheAction = actions.FirstOrDefault(x => x.key == "breathe");
        walkAction = actions.FirstOrDefault(x => x.key == "walk");
        idleAction = actions.FirstOrDefault(x => x.key == "idle");
        
        StartIdle();
    }

    /// Call once per frame
    /// 
    public void Tick(bool gotHitStun)
    {
        // Reset outputs
        attackStarted = false;
        comboStarted = false;
        dodgeStarted = false;
        dodgeEnded = false;
        interrupted = false;
        hitStarted = false;
        breatheEnded = false;
        breatheStarted = false;
        walkEnded = false;
        walkStarted = false;
        idleEnded = false;
        idleStarted = false;
        //Debug.Log("flags cleared");

        // Hard interrupt
        if (gotHitStun)
        {
            ClearQueue();
            Interrupt(ActionType.HitStun);
            return;
        }

        // Update timing windows
        if (currentAction == ActionType.Attack && currentAttack != null)
        {
            var attackNormalizedTime = attackAction.normalizedTime;
            
            canCombo =
                attackNormalizedTime >= currentAttack.comboStartNormalized &&
                attackNormalizedTime <= currentAttack.comboEndNormalized;

            canDodgeCancel =
                attackNormalizedTime >= currentAttack.dodgeCancelStartNormalized &&
                attackNormalizedTime <= currentAttack.dodgeCancelEndNormalized;

            // Natural completion
            if (attackNormalizedTime >= currentAttack.recoveryEndNormalized)
                EndAction();
        }

        TryConsumeQueue();
    }

    // ───────── Requests (from input / AI) ─────────

    public void RequestAttack(ActionData attack)
    {
        if (CanAttackNow())
        {
            if (currentAction == ActionType.Attack)
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

    public void RequestBreathe()
    {
        StartBreathe();
    }

    private void StartBreathe()
    {
        breatheStarted = true;
    }

    public void RequestStopBreathe()
    {
        StopBreathe();
    }

    private void StopBreathe()
    {
        breatheEnded = true;
    }
    
    
    public void RequestStartWalk()
    {
        StopIdle();
        StartWalk();
    }

    
    public void RequestStopWalk()
    {
        StartIdle();
        StopWalk();
    }

    private void StartIdle()
    {
        idleStarted = true;
    }

    private void StopIdle()
    {
        idleEnded = true;
    }

    private void StartWalk()
    {
        walkStarted = true;
    }

    private void StopWalk()
    {
        walkEnded = true;
    }

    public void RequestHit()
    {
        hitStarted = true;
    }

    public void RequestStopDodge()
    {
        if (currentAction == ActionType.Dodge)
        {
            EndDodge();
        }
    }

    // ───────── Queue handling ─────────

    private void QueueAttack(ActionData attack)
    {
        queued = ActionType.Attack;
        queuedAttack = attack;
        queueUntil = Time.time + QUEUE_TIME;
    }

    private void QueueDodge()
    {
        queued = ActionType.Dodge;
        queueUntil = Time.time + QUEUE_TIME;
    }

    private void TryConsumeQueue()
    {
        if (queued == ActionType.None)
            return;

        if (Time.time > queueUntil)
        {
            ClearQueue();
            return;
        }

        if (queued == ActionType.Attack && CanAttackNow())
        {
            if (currentAction == ActionType.Attack)
                StartCombo(queuedAttack);
            else
                StartAttack(queuedAttack);

            ClearQueue();
        }
        else if (queued == ActionType.Dodge && CanDodgeNow())
        {
            StartDodge();
            ClearQueue();
        }
    }

    private void ClearQueue()
    {
        queued = ActionType.None;
        queuedAttack = null;
    }

    // ───────── Permissions ─────────

    private bool CanAttackNow()
    {
        return currentAction == ActionType.Locomotion ||
              (currentAction == ActionType.Attack && canCombo);
    }

    private bool CanDodgeNow()
    {
        return currentAction == ActionType.Locomotion ||
              (currentAction == ActionType.Attack && canDodgeCancel);
    }

    // ───────── Transitions ─────────

    private void StartAttack(ActionData attack)
    {
        currentAction = ActionType.Attack;
        currentAttack = attack;
        attackStarted = true;
    }

    private void StartCombo(ActionData attack)
    {
        currentAction = ActionType.Attack;
        currentAttack = attack;
        comboStarted = true;
    }

    private void StartDodge()
    {
        currentAction = ActionType.Dodge;
        currentAttack = null;
        dodgeStarted = true;
    }
    
    private void EndDodge()
    {
        EndAction();
        dodgeEnded = true;
    }

    private void EndAction()
    {
        currentAction = ActionType.Locomotion;
        currentAttack = null;
    }

    private void Interrupt(ActionType action)
    {
        currentAction = action;
        currentAttack = null;
        interrupted = true;
    }
}