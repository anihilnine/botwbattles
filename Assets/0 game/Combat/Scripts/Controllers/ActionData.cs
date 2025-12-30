using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[Serializable]
public class ActionData
{
    [Header("Inputs")]
    public string key; // so inputcontroller can find the right actiondata
    public string desc; // just for designer
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
    
    [Header("todo")] 
    public float comboStartNormalized;
    public float comboEndNormalized;
    public float dodgeCancelStartNormalized;
    public float dodgeCancelEndNormalized;
    public float recoveryEndNormalized;


    public void Init(PlayableGraph graph, AnimationLayerMixerPlayable mixer, int index)
    {
        this.index = index;
        this.mixer = mixer;
        sourceClipTime = clip.length;
        playable = AnimationClipPlayable.Create(graph, clip);
        if (this.additive)
        {
            mixer.SetLayerAdditive((uint)index, true);
            // todo: add avatar mask
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