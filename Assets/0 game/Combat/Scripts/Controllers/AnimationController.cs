using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AnimationController : MonoBehaviour
{
    public Animator animator;
    public ActionController actionController;
    
    PlayableGraph graph;

    public ActionData[] actions;
    
    void Awake()
    {
        actions = actionController.actions;
        graph = PlayableGraph.Create("SingleClipGraph");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

        var output = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);
        var mixer = AnimationLayerMixerPlayable.Create(graph, actions.Length);
        for (var i = 0; i < actions.Length; i++)
        {
            actions[i].Init(graph, mixer, i);
        }

        output.SetSourcePlayable(mixer);
        graph.Play();
    }

    private void Update()
    {
        if (actionController.attackStarted)
            actionController.currentAttack.Play();

        // if (actionController.comboStarted)
        //     actions[1].Play();

        if (actionController.dodgeStarted)
            actionController.dodgeAction.Play();

        if (actionController.dodgeEnded)
            actionController.dodgeAction.Stop();
        
        if (actionController.breatheStarted)
            actionController.breatheAction.Play();

        if (actionController.breatheEnded)
            actionController.breatheAction.Stop();
        
        if (actionController.walkStarted)
            actionController.walkAction.Play();

        if (actionController.walkEnded)
            actionController.walkAction.Stop();
        
        if (actionController.idleStarted)
            actionController.idleAction.Play();

        if (actionController.idleEnded)
            actionController.idleAction.Stop();
        
        if (actionController.hitStarted)
            actionController.hitAction.Play();

        // if (actionController.interrupted)
        //     actions[2].Play();
        
        foreach (var clip in actions)
        {
            clip.Update();
        }

        float sumWeight = 0;
        foreach (var clip in actions)
        {
            if (clip.isPlaying)
            {
                sumWeight += clip.fadedWeight;
            }
        }

        // todo: base layer
        // todo: sum primary and secondary animations seperately with budget for secondary
        if (sumWeight > 0)
        {
            foreach (var clip in actions)
            {
                if (clip.isPlaying)
                {
                    //var weight = clip.fadedWeight / sumWeight;
                    var weight = clip.fadedWeight;
                    clip.SetNormalizedWeight(weight);
                }
                else
                {
                    clip.SetNormalizedWeight(0);
                }
            }
        }
        else
        {
            foreach (var clip in actions)
            {
                clip.SetNormalizedWeight(0);
            }
        }

        graph.Evaluate(Time.deltaTime);
        
        // zero out root motion
        //this.transform.position = Vector3.zero;
        
    }
    //
    // void OnAnimatorMove()
    // {
    //     // animator.deltaPosition is the root motion this frame
    //     Vector3 desired = animator.deltaPosition;
    //
    //     // feed into your motor (which does capsule cast)
    //     this.transform.Translate(desired.x, desired.y, -desired.z);
    //     //motor.Move(desired); // should collide & slide
    //     //
    //     // there are issues ....... 
    //     //     1. i think my animations and mesh are kinda shit
    //     //         when i make the rig humanoid things stop to work
    //     //         and when i try and share avatars across animations. one has sword, one doesnt 
    //     //         >>>>>>>>>> i should just use some working pack so i dont have to consider that rn
    //     //     
    //     //     2. i dont have a clear way to ignore root motion and apply it manually. it seems to put it on the bone anyway  
    // }

    void OnDestroy()
    {
        if (graph.IsValid())
            graph.Destroy();
    }
}