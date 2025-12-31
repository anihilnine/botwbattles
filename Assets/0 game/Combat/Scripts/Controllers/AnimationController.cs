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
            actionController.attackAction.Play();

        // if (actionController.comboStarted)
        //     actions[1].Play();

        if (actionController.dodgeStarted)
            actionController.dodgeAction.Play();

        if (actionController.dodgeEnded)
            actionController.dodgeAction.Stop();
        
        if (actionController.breatheStarted)
            actionController.breathAction.Play();

        if (actionController.breatheEnded)
            actionController.breathAction.Stop();
        
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
    }

    void OnDestroy()
    {
        if (graph.IsValid())
            graph.Destroy();
    }
}