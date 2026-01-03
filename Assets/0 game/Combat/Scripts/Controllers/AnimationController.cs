using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AnimationController : MonoBehaviour
{
    public Animator animator;
    public ActionController actionController;
    public float timeScale = 1;
    
    PlayableGraph graph;

    private ActionData[] _actions;
    public AnimLayerData[] animLayers;

    public AnimationController(ActionData[] actions)
    {
        this._actions = actions;
    }

    void Awake()
    {
        _actions = actionController.actions;
        graph = PlayableGraph.Create("SingleClipGraph");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

        var output = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);
        var mixer = AnimationLayerMixerPlayable.Create(graph, _actions.Length);
        for (var i = 0; i < _actions.Length; i++)
        {
            var action = _actions[i];
            action.Init(graph, mixer, i, animLayers[action.layerIndex]);
        }

        output.SetSourcePlayable(mixer);
        graph.Play();
    }

    public void Tick()
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
        
        foreach (var clip in _actions)
        {
            clip.Update();
        }

        foreach (var layer in animLayers)
        {
            layer.sumWeights = 0;
        }
        
        foreach (var clip in _actions)
        {
            if (clip.isPlaying)
            {
                //Debug.Log($"clip weight {clip.fadedWeight}");
                clip.layer.sumWeights += clip.fadedWeight;
            }
        }

        foreach (var layer in animLayers)
        {
            //Debug.Log($"layer weight {layer.key} {layer.sumWeights}");
        }

        // todo: sum primary and secondary animations seperately with budget for secondary
        foreach (var clip in _actions)
        {
            if (clip.isPlaying)
            {
                var weight = clip.fadedWeight / clip.layer.sumWeights;
                //Debug.Log($"clip weight {clip.fadedWeight} / {clip.layer.sumWeights} = {weight}");
                clip.SetNormalizedWeight(weight);
            }
            else
            {
                clip.SetNormalizedWeight(0);
            }
        }

        graph.Evaluate(Time.deltaTime * timeScale);
        
        // zero out root motion
        //this.transform.position = Vector3.zero;
        
    }

    private void OnGUI()
    {
        var y = 50;
        foreach (var action in _actions)
        {
            //if (action.isPlaying)
            {
                var playing = action.isPlaying ? "Y" : "n";
                var text = $"{action.key} ntime={action.normalizedTime:P2} fweight={action.fadedWeight} nfweight={action.normalizedWeight:P2} playing={playing}";
                //Debug.Log($"action.normalizedWeight == {action.normalizedWeight}");
                if (action.looping)
                {
                    text += " looping";
                }
                GUI.Label(new Rect(50, y, 800, 100), text);
                y += 30;
            }
        }
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