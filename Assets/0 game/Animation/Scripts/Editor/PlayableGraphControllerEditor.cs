using UnityEngine;
using UnityEditor;
using Game.Animation;

namespace Game.Animation.Editor
{
    /// <summary>
    /// Custom editor for PlayableGraphController with runtime controls
    /// </summary>
    [CustomEditor(typeof(PlayableGraphController))]
    public class PlayableGraphControllerEditor : UnityEditor.Editor
    {
        private PlayableGraphController controller;
        private int selectedClipIndex = 0;
        private float timeSlider = 0f;
        private float speedSlider = 1f;

        private void OnEnable()
        {
            controller = (PlayableGraphController)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Runtime controls are only available during Play mode.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            // Clip selection
            if (controller != null && controller.ClipCount > 0)
            {
                var clips = controller.AnimationClips;
                string[] clipNames = new string[clips.Count];
                for (int i = 0; i < clips.Count; i++)
                {
                    AnimationClip clip = clips[i];
                    if (clip != null)
                    {
                        // Include index in name to differentiate clips with same name
                        clipNames[i] = $"[{i}] {clip.name}";
                    }
                    else
                    {
                        clipNames[i] = $"[{i}] None";
                    }
                }

                // Clamp selected index to valid range
                selectedClipIndex = Mathf.Clamp(selectedClipIndex, 0, clips.Count - 1);
                selectedClipIndex = EditorGUILayout.Popup("Select Clip", selectedClipIndex, clipNames);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Play Clip"))
                {
                    controller.PlayClip(selectedClipIndex);
                }
                if (GUILayout.Button("Blend To Clip"))
                {
                    controller.BlendToClip(selectedClipIndex, controller.BlendTime);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // Playback controls
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play"))
            {
                if (controller != null && controller.GetActiveClipIndex() >= 0)
                {
                    controller.Resume();
                }
            }
            if (GUILayout.Button("Pause"))
            {
                controller?.Pause();
            }
            if (GUILayout.Button("Stop"))
            {
                controller?.Stop();
            }
            if (GUILayout.Button("Cut"))
            {
                controller?.Cut();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Speed control
            speedSlider = EditorGUILayout.Slider("Speed", speedSlider, 0f, 3f);
            if (GUILayout.Button("Set Speed"))
            {
                controller?.SetSpeed(speedSlider);
            }

            EditorGUILayout.Space();

            // Time control
            if (controller != null && controller.GetActiveClipIndex() >= 0)
            {
                float currentTime = controller.GetCurrentTime();
                float clipLength = controller.GetCurrentClipLength();

                EditorGUILayout.LabelField($"Time: {currentTime:F2} / {clipLength:F2}");

                timeSlider = EditorGUILayout.Slider("Time Position", currentTime, 0f, clipLength);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Set Time"))
                {
                    controller.SetTime(timeSlider);
                }
                if (GUILayout.Button("0:00"))
                {
                    controller.SetTime(0f);
                    timeSlider = 0f;
                }
                EditorGUILayout.EndHorizontal();

                // Progress bar
                float progress = clipLength > 0 ? currentTime / clipLength : 0f;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, $"{progress * 100:F1}%");
            }

            EditorGUILayout.Space();

            // Status info
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Playing: {controller?.IsPlaying() ?? false}");
            EditorGUILayout.LabelField($"Active Clip: {controller?.GetActiveClipIndex() ?? -1}");
        }
    }
}

