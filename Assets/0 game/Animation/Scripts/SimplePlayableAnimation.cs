using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class SimplePlayableAnimation : MonoBehaviour
{
    public Animator animator;

    PlayableGraph graph;

    public SimpleClipData[] clips;

    void Awake()
    {
        graph = PlayableGraph.Create("SingleClipGraph");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

        var output = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);
        var mixer = AnimationLayerMixerPlayable.Create(graph, clips.Length);
        for (var i = 0; i < clips.Length; i++)
        {
            clips[i].Init(graph, mixer, i);
        }

        output.SetSourcePlayable(mixer);
        graph.Play();
    }

    private void Update()
    {
        // if (IsFinished())
        // {
        //     Play();
        // }
        //
        foreach (var clip in clips)
        {
            clip.Update();
        }

        float sumWeight = 0;
        foreach (var clip in clips)
        {
            if (clip.isPlaying)
            {
                sumWeight += clip.fadedWeight;
            }
        }

        if (sumWeight > 0)
        {
            foreach (var clip in clips)
            {
                if (clip.isPlaying)
                {
                    var weight = clip.fadedWeight / sumWeight;
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
            foreach (var clip in clips)
            {
                clip.SetNormalizedWeight(0);
            }
        }
        

        graph.Evaluate(Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            clips[0].Play();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            clips[1].Play();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            clips[2].Play();
        }
    }

    void OnDestroy()
    {
        if (graph.IsValid())
            graph.Destroy();
    }

    // Optional helpers ↓↓↓

    // public void Play()
    // {
    //     graph.Play();
    // }

    // public void Stop()
    // {
    //     playable.SetSpeed(0f);
    // }
    //
    // public void SetSpeed(float speed)
    // {
    //     playable.SetSpeed(speed);
    // }
    //
    //  public bool IsFinished()
    //  {
    //      return clip.GetTime() >= clip.length;
    // }
}

[Serializable]
public class SimpleClipData
{
    [Header("Inputs")] 
    public string desc; // just for designera
    public AnimationClip clip;
    public float speed = 1;
    public float weight = 1;
    public float fadeInDuration = 0; // in source clip time
    public float fadeOutDuration = 0; // in source clip time  
    public bool loop;
    public bool additive;

    [Header("Debug")] 
    public int index;
    public float sourceClipTime;
    public float currentTime;
    public bool isPlaying;
    public AnimationClipPlayable playable;
    public AnimationLayerMixerPlayable mixer;
    public float fadedWeight;
    public float normalizedWeight;
    public float endTime;
    public float fadeOutStartTime;


    public void Init(PlayableGraph graph, AnimationLayerMixerPlayable mixer, int index)
    {
        this.index = index;
        this.mixer = mixer;
        sourceClipTime = clip.length;
        playable = AnimationClipPlayable.Create(graph, clip);
        if (this.additive)
        {
            mixer.SetLayerAdditive((uint)index, true);
        }
        playable.Pause();
        playable.SetSpeed(speed);
        graph.Connect(playable, 0, mixer, index);
    }

    public void Play()
    {
        fadeOutStartTime = sourceClipTime - fadeOutDuration;
        endTime = sourceClipTime;
        isPlaying = true;
        playable.SetTime(0);
        playable.Play();
    }

    public void Stop()
    {
        // should start stopping
        if (fadeOutDuration > 0f)
        {
            fadeOutStartTime = (float)playable.GetTime();
            endTime = Mathf.Min(fadeOutStartTime + fadeOutDuration, sourceClipTime);
        }
        else
        {
            StopImmediate();
        }
    }

    // todo: have stop method with fadeout
    
    public void Update()
    {
        // todo: is there a cost to doing this every frame?
        playable.SetSpeed(speed);
        
        currentTime = (float)playable.GetTime();

        var isFadeIn = currentTime < fadeInDuration;
        var isFadeOut =  currentTime > fadeOutStartTime;

        if (isFadeOut)
        {
            fadedWeight = Mathf.Clamp01(Mathf.InverseLerp(endTime, fadeOutStartTime, currentTime)) * weight;
        }
        else if (isFadeIn)
        {
            fadedWeight = Mathf.Clamp01(Mathf.InverseLerp(0, fadeInDuration, currentTime)) * weight;
        }
        else
        {
            fadedWeight = weight;
        }
        
        var isFinished = currentTime >= endTime;

        if (isFinished)
        {
            if (loop)
            {
                playable.SetTime(0);
            }
            else
            {
                StopImmediate();
            }
        }
    }

    private void StopImmediate()
    {
        isPlaying = false;
        playable.Pause();
    }

    public void SetNormalizedWeight(float weight)
    {
        this.normalizedWeight = weight;
        mixer.SetInputWeight(index, weight);
    }
}