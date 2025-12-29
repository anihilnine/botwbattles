using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Combat
{
    [CreateAssetMenu(fileName = "New Move", menuName = "Combat/Move Data")]
    public class MoveData : ScriptableObject
    {
        [Tooltip("Name identifier for this move")]
        public string moveName;
        
        [Tooltip("Type of move")]
        public MoveType moveType;
        
        [Tooltip("Animation clip to play for this move")]
        public AnimationClip animationClip;
        
        [Tooltip("List of hitbox frames defining when and where hitboxes are active")]
        public List<HitboxFrame> hitboxFrames = new List<HitboxFrame>();
        
        [Tooltip("Total duration of the move in seconds")]
        public float totalDuration;
        
        [Tooltip("Cooldown time between uses (in seconds)")]
        public float cooldown = 0f;
        
        [Tooltip("Full path of bone the hitbox should be offset from (empty for root)")]
        public string boneName = "";

        /// <summary>
        /// Gets all active hitboxes at the given time
        /// </summary>
        /// <param name="currentTime">Current time in the move (in seconds)</param>
        /// <returns>List of active hitbox frames</returns>
        public List<HitboxFrame> GetActiveHitboxes(float currentTime)
        {
            return hitboxFrames.Where(hf => hf.IsActiveAtTime(currentTime)).ToList();
        }

        /// <summary>
        /// Checks if the move has any active hitboxes at the given time
        /// </summary>
        /// <param name="currentTime">Current time in the move (in seconds)</param>
        /// <returns>True if any hitbox is active</returns>
        public bool HasActiveHitboxes(float currentTime)
        {
            return hitboxFrames.Any(hf => hf.IsActiveAtTime(currentTime));
        }
    }
}

