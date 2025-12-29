using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Game.Animation
{
    /// <summary>
    /// Standalone component for experimenting with Unity's Playable Graph system.
    /// Allows launching animations, blending between them, changing timing, and cutting animations off.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayableGraphController : MonoBehaviour
    {
        [Header("Animation Clips")]
        [SerializeField] private List<AnimationClip> animationClips = new List<AnimationClip>();

        public List<AnimationClip> AnimationClips => animationClips;
        public int ClipCount => animationClips != null ? animationClips.Count : 0;

        [Header("Playback Settings")]
        [SerializeField] private bool autoPlay = false;
        [SerializeField] private float defaultSpeed = 1f;

        [Header("Blend Settings")]
        [SerializeField] private float blendTime = 0.25f;
        [SerializeField] private bool useCrossfade = true;

        public float BlendTime => blendTime;

        private PlayableGraph playableGraph;
        private AnimationPlayableOutput playableOutput;
        private AnimationMixerPlayable mixerPlayable;
        private List<AnimationClipPlayable> clipPlayables = new List<AnimationClipPlayable>();
        private int activeClipIndex = -1;
        private bool isInitialized = false;

        // Runtime controls
        private float currentTime = 0f;
        private bool isPlaying = false;
        private float currentSpeed = 1f;

        private void Awake()
        {
            InitializeGraph();
        }

        private void OnEnable()
        {
            if (isInitialized)
            {
                playableGraph.Play();
            }
        }

        private void OnDisable()
        {
            if (playableGraph.IsValid())
            {
                playableGraph.Stop();
            }
        }

        private void OnDestroy()
        {
            CleanupGraph();
        }

        private void Update()
        {
            if (!isInitialized || !isPlaying) return;

            // Update graph time
            playableGraph.Evaluate(Time.deltaTime);
            currentTime += Time.deltaTime * currentSpeed;

            // Auto-stop when clip finishes (if not looping)
            if (activeClipIndex >= 0 && activeClipIndex < clipPlayables.Count)
            {
                AnimationClipPlayable clipPlayable = clipPlayables[activeClipIndex];
                if (clipPlayable.IsValid())
                {
                    AnimationClip clip = animationClips[activeClipIndex];
                    if (!clip.isLooping && currentTime >= clip.length)
                    {
                        Stop();
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the Playable Graph with an Animation Mixer
        /// </summary>
        private void InitializeGraph()
        {
            if (animationClips == null || animationClips.Count == 0)
            {
                Debug.LogWarning("PlayableGraphController: No animation clips assigned.");
                return;
            }

            // Create graph
            playableGraph = PlayableGraph.Create("AnimationPlayableGraph");
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            // Create output
            Animator animator = GetComponent<Animator>();
            playableOutput = AnimationPlayableOutput.Create(playableGraph, "AnimationOutput", animator);

            // Create mixer with enough inputs for all clips
            int clipCount = animationClips.Count;
            mixerPlayable = AnimationMixerPlayable.Create(playableGraph, clipCount);

            // Create clip playables
            clipPlayables.Clear();
            for (int i = 0; i < clipCount; i++)
            {
                if (animationClips[i] != null)
                {
                    AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(playableGraph, animationClips[i]);
                    clipPlayable.SetSpeed(defaultSpeed);
                    clipPlayables.Add(clipPlayable);
                    playableGraph.Connect(clipPlayable, 0, mixerPlayable, i);
                    mixerPlayable.SetInputWeight(i, 0f); // Start with all weights at 0
                }
                else
                {
                    clipPlayables.Add(new AnimationClipPlayable());
                    mixerPlayable.SetInputWeight(i, 0f);
                }
            }

            // Connect mixer to output
            playableOutput.SetSourcePlayable(mixerPlayable);

            isInitialized = true;

            if (autoPlay && animationClips.Count > 0)
            {
                PlayClip(0);
            }
        }

        /// <summary>
        /// Plays a specific animation clip by index
        /// </summary>
        public void PlayClip(int clipIndex)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("PlayableGraphController: Graph not initialized.");
                return;
            }

            if (clipIndex < 0 || clipIndex >= animationClips.Count || animationClips[clipIndex] == null)
            {
                Debug.LogWarning($"PlayableGraphController: Invalid clip index {clipIndex}");
                return;
            }

            // If currently playing, blend; otherwise just start
            if (activeClipIndex >= 0 && useCrossfade)
            {
                BlendToClip(clipIndex, blendTime);
            }
            else
            {
                // Stop all clips first
                for (int i = 0; i < clipPlayables.Count; i++)
                {
                    if (clipPlayables[i].IsValid())
                    {
                        clipPlayables[i].SetTime(0f);
                        mixerPlayable.SetInputWeight(i, 0f);
                    }
                }

                // Start new clip
                activeClipIndex = clipIndex;
                if (clipPlayables[clipIndex].IsValid())
                {
                    clipPlayables[clipIndex].SetTime(0f);
                    mixerPlayable.SetInputWeight(clipIndex, 1f);
                }

                currentTime = 0f;
                isPlaying = true;
                playableGraph.Play();
            }
        }

        /// <summary>
        /// Blends from current clip to a new clip over the specified duration
        /// </summary>
        public void BlendToClip(int clipIndex, float blendDuration)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("PlayableGraphController: Graph not initialized.");
                return;
            }

            if (clipIndex < 0 || clipIndex >= animationClips.Count || animationClips[clipIndex] == null)
            {
                Debug.LogWarning($"PlayableGraphController: Invalid clip index {clipIndex}");
                return;
            }

            if (activeClipIndex == clipIndex)
            {
                return; // Already playing this clip
            }

            int previousIndex = activeClipIndex;
            activeClipIndex = clipIndex;

            // Reset new clip to start if it's not already playing
            if (clipPlayables[clipIndex].IsValid())
            {
                float currentWeight = mixerPlayable.GetInputWeight(clipIndex);
                if (currentWeight < 0.01f) // Not already playing
                {
                    clipPlayables[clipIndex].SetTime(0f);
                }
            }

            // Start blend coroutine
            StartCoroutine(BlendCoroutine(previousIndex, clipIndex, blendDuration));
            currentTime = 0f;
            isPlaying = true;
            playableGraph.Play();
        }

        /// <summary>
        /// Coroutine that handles smooth blending between two clips
        /// </summary>
        private System.Collections.IEnumerator BlendCoroutine(int fromIndex, int toIndex, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth blend (could use different curves)
                float toWeight = t;
                float fromWeight = 1f - t;

                if (fromIndex >= 0 && fromIndex < clipPlayables.Count && clipPlayables[fromIndex].IsValid())
                {
                    mixerPlayable.SetInputWeight(fromIndex, fromWeight);
                }

                if (toIndex >= 0 && toIndex < clipPlayables.Count && clipPlayables[toIndex].IsValid())
                {
                    mixerPlayable.SetInputWeight(toIndex, toWeight);
                }

                yield return null;
            }

            // Finalize blend
            if (fromIndex >= 0 && fromIndex < clipPlayables.Count)
            {
                mixerPlayable.SetInputWeight(fromIndex, 0f);
            }

            if (toIndex >= 0 && toIndex < clipPlayables.Count)
            {
                mixerPlayable.SetInputWeight(toIndex, 1f);
            }
        }

        /// <summary>
        /// Stops the current animation and cuts it off immediately
        /// </summary>
        public void Stop()
        {
            if (!isInitialized) return;

            if (activeClipIndex >= 0 && activeClipIndex < clipPlayables.Count)
            {
                mixerPlayable.SetInputWeight(activeClipIndex, 0f);
            }

            isPlaying = false;
            activeClipIndex = -1;
            currentTime = 0f;
            playableGraph.Stop();
        }

        /// <summary>
        /// Pauses the current animation
        /// </summary>
        public void Pause()
        {
            if (isInitialized)
            {
                playableGraph.Stop();
                isPlaying = false;
            }
        }

        /// <summary>
        /// Resumes a paused animation
        /// </summary>
        public void Resume()
        {
            if (isInitialized && activeClipIndex >= 0)
            {
                playableGraph.Play();
                isPlaying = true;
            }
        }

        /// <summary>
        /// Sets the playback speed for all clips (1.0 = normal speed, 2.0 = double speed, etc.)
        /// </summary>
        public void SetSpeed(float speed)
        {
            currentSpeed = speed;
            if (isInitialized)
            {
                foreach (var clipPlayable in clipPlayables)
                {
                    if (clipPlayable.IsValid())
                    {
                        clipPlayable.SetSpeed(speed);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the time position of the currently playing clip
        /// </summary>
        public void SetTime(float time)
        {
            if (isInitialized && activeClipIndex >= 0 && activeClipIndex < clipPlayables.Count)
            {
                AnimationClipPlayable clipPlayable = clipPlayables[activeClipIndex];
                if (clipPlayable.IsValid())
                {
                    AnimationClip clip = animationClips[activeClipIndex];
                    float clampedTime = Mathf.Clamp(time, 0f, clip.length);
                    clipPlayable.SetTime(clampedTime);
                    currentTime = clampedTime;
                }
            }
        }

        /// <summary>
        /// Gets the current playback time of the active clip
        /// </summary>
        public float GetCurrentTime()
        {
            if (isInitialized && activeClipIndex >= 0 && activeClipIndex < clipPlayables.Count)
            {
                AnimationClipPlayable clipPlayable = clipPlayables[activeClipIndex];
                if (clipPlayable.IsValid())
                {
                    return (float)clipPlayable.GetTime();
                }
            }
            return currentTime;
        }

        /// <summary>
        /// Gets the length of a clip by index
        /// </summary>
        public float GetClipLength(int clipIndex)
        {
            if (clipIndex >= 0 && clipIndex < animationClips.Count && animationClips[clipIndex] != null)
            {
                return animationClips[clipIndex].length;
            }
            return 0f;
        }

        /// <summary>
        /// Gets the length of the currently playing clip
        /// </summary>
        public float GetCurrentClipLength()
        {
            if (activeClipIndex >= 0 && activeClipIndex < animationClips.Count && animationClips[activeClipIndex] != null)
            {
                return animationClips[activeClipIndex].length;
            }
            return 0f;
        }

        /// <summary>
        /// Cuts off the animation at the current time (stops it immediately without blending)
        /// </summary>
        public void Cut()
        {
            Stop();
        }

        /// <summary>
        /// Gets the current active clip index
        /// </summary>
        public int GetActiveClipIndex()
        {
            return activeClipIndex;
        }

        /// <summary>
        /// Checks if an animation is currently playing
        /// </summary>
        public bool IsPlaying()
        {
            return isPlaying && isInitialized;
        }

        private void CleanupGraph()
        {
            if (playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }
            isInitialized = false;
        }

        private void OnValidate()
        {
            // Reinitialize if clips change in editor
            if (Application.isPlaying && isInitialized)
            {
                CleanupGraph();
                InitializeGraph();
            }
        }
    }
}

