using System;
using System.Linq;
using UnityEngine;

public class InputController : MonoBehaviour
{
    public ActionController actionController;

    void Update()
    {
        actionController.Tick(gotHitStun: false);

        if (Input.GetKeyDown(KeyCode.Mouse0))
            actionController.RequestAttack(actionController.attackAction);

        if (Input.GetKeyDown(KeyCode.Mouse1))
            actionController.RequestHit();

        if (Input.GetKeyDown(KeyCode.Space))
            actionController.RequestDodge();
        
        if (Input.GetKeyUp(KeyCode.Space))
            actionController.RequestStopDodge();
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
            actionController.RequestBreathe();
        
        if (Input.GetKeyUp(KeyCode.Alpha1))
            actionController.RequestStopBreathe();
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
            actionController.RequestAttack(actionController.attackAction2);
    }
}