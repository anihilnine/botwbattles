using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[Serializable]
public class ActionData
{
    [Header("=== Inputs")]
    public string key; // so inputcontroller can find the right actiondata
    public string desc; // just for designer
    public AnimationClip clip;
    public AvatarMask mask;
    public float speed = 1;
    public float weight = 1;
    public float fadeInDuration = 0; // in source clip time
    public float fadeOutDuration = 0; // in source clip time  
    public bool loop;
    public bool isFirstLoop;
    public bool additive;
    public bool log;

    [Header("=== Debug")] 
    public int index;
    public float sourceClipTime;
    public float currentTime;
    public float normalizedTime;
    public bool isPlaying;
    public AnimationClipPlayable playable;
    public AnimationLayerMixerPlayable mixer;
    public float fadedWeight;
    public float normalizedWeight;
    public float endTime;
    public float fadeOutStartTime;
    public bool looping;
    
    [Header("=== Todo")] 
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
        // todo would be good if i could re-init without restart
        // todo would a scriptable object here be easier cos i can make perm changes
        if (mask != null)
        {
            Log("masking");
            mixer.SetLayerMaskFromAvatarMask((uint)index, mask);
        }

        if (this.additive)
        {
            mixer.SetLayerAdditive((uint)index, true);
            // todo: add avatar mask
        }
        
        playable.Pause();
        playable.SetSpeed(speed);
        graph.Connect(playable, 0, mixer, index);
    }

    public void Log(string message)
    {
        if (!log) 
            return;
        
        Debug.Log($"ActionData: {message}");
    }
    
    public void Play()
    {
        Log($"Play");
        fadeOutStartTime = sourceClipTime - fadeOutDuration;
        endTime = sourceClipTime;
        isPlaying = true;
        looping = loop;
        isFirstLoop = looping;
            
        playable.SetTime(0);
        playable.Play();
    }

    public void Stop()
    {
        Log($"Stop");
        looping = false;
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
        normalizedTime = currentTime / endTime; // ? should this be sourceClipTime or endTime
        // Log($"currentTime: {currentTime}");
        // Log($"endTime: {endTime}");
        // Log($"normalizedTime: {normalizedTime}");


        var isFadeIn = currentTime < fadeInDuration;
        var isFadeOut =  currentTime > fadeOutStartTime;

        if (looping)
        {
            isFadeIn = isFirstLoop;
            isFadeOut = false; // looping gets set to false on stop, so this wont happen on final loop
        }
        
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
            // todo we cant blend a clip to itself, so the only loops we support are when the clips are authored as perfect loops, and we hard cut without blend, but it lines up 
            // todo a clip that is fading out might hit the end of the clip and really should loop one last time while finishing the fadeout
            
            if (looping)
            {
                isFirstLoop = false;
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
        Log($"StopImmediate");
        isPlaying = false;
        playable.Pause();
    }

    public void SetNormalizedWeight(float weight)
    {
        Log($"SetNormalizedWeight");
        this.normalizedWeight = weight;
        mixer.SetInputWeight(index, weight);
    }
}