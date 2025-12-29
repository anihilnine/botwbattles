using UnityEngine;

namespace Game.Combat
{
    public enum HitboxShape
    {
        Sphere,
        Box
    }

    [System.Serializable]
    public class HitboxFrame
    {
        [Tooltip("Start time when hitbox becomes active (in seconds)")]
        public float startTime;
        
        [Tooltip("End time when hitbox deactivates (in seconds)")]
        public float endTime;
        
        [Tooltip("Local position offset from attack origin")]
        public Vector3 positionOffset;
        
        [Tooltip("Size of hitbox - radius for Sphere, dimensions for Box")]
        public Vector3 size;
        
        [Tooltip("Shape of the hitbox")]
        public HitboxShape shape;
        
        [Tooltip("Damage value for this hitbox")]
        public float damage;

        public bool IsActiveAtTime(float currentTime)
        {
            return currentTime >= startTime && currentTime <= endTime;
        }
    }
}

