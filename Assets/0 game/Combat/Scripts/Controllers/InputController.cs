using System;
using System.Linq;
using UnityEngine;

public class InputController : MonoBehaviour
{
    public ActionController actionController;
    private ActionData _attackAction;
    private ActionData _dodgeAction;

    private void Start()
    {
        _attackAction = actionController.actions.First(x => x.key == "attack");
        _dodgeAction = actionController.actions.First(x => x.key == "dodge");
    }

    void Update()
    {
        //float t01 = GetCurrentActionNormalizedTime();
        float t01 = Time.time % 1f; // todo: surely going to cause trouble
        actionController.Tick(t01, gotHitStun: false); // replace gotHitStun with your actual hit events

        if (Input.GetKeyDown(KeyCode.Mouse0))
            actionController.RequestAttack(_attackAction);

        if (Input.GetKeyDown(KeyCode.Space))
            actionController.RequestDodge();
    }
}