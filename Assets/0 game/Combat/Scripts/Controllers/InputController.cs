using System;
using System.Linq;
using UnityEngine;

public class InputController : MonoBehaviour
{
    public ActionController actionController;
    public AnimationController animationController;

    void Update()
    {
        Debug.Log("input processed");
        
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
        
        if (Input.GetKeyDown(KeyCode.UpArrow))
            actionController.RequestStartWalk();
        
        if (Input.GetKeyUp(KeyCode.UpArrow))
            actionController.RequestStopWalk();
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
            actionController.RequestAttack(actionController.attackAction2);

        animationController.Tick();
        
        actionController.Tick(gotHitStun: false);
    }
}