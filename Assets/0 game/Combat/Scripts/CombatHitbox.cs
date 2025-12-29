using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Component attached to active hitbox GameObjects during move execution.
    /// Handles hit detection using Physics.OverlapSphere/OverlapBox.
    /// </summary>
    public class CombatHitbox : MonoBehaviour
    {
        [SerializeField] private HitboxFrame hitboxFrame;
        [SerializeField] private LayerMask hitLayers = -1;
        [SerializeField] private string[] hitTags = { "Enemy" };

        private HashSet<Collider> hitTargets = new HashSet<Collider>();
        private CombatController owner;
        private float moveStartTime;
        private bool isActive = false;

        public void Initialize(HitboxFrame frame, CombatController combatController, float moveStart, LayerMask layers, string[] tags)
        {
            hitboxFrame = frame;
            owner = combatController;
            moveStartTime = moveStart;
            hitLayers = layers;
            hitTags = tags;
            hitTargets.Clear();
            isActive = true;
        }

        private void Update()
        {
            if (!isActive || hitboxFrame == null) return;

            float elapsedTime = Time.time - moveStartTime;
            
            // Check if hitbox should still be active
            if (!hitboxFrame.IsActiveAtTime(elapsedTime))
            {
                isActive = false;
                Destroy(gameObject);
                return;
            }

            DetectHits();
        }

        private void DetectHits()
        {
            Collider[] colliders = null;
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;

            if (hitboxFrame.shape == HitboxShape.Sphere)
            {
                float radius = hitboxFrame.size.x; // Use x component as radius
                colliders = Physics.OverlapSphere(position, radius, hitLayers);
            }
            else if (hitboxFrame.shape == HitboxShape.Box)
            {
                colliders = Physics.OverlapBox(position, hitboxFrame.size * 0.5f, rotation, hitLayers);
            }

            if (colliders != null)
            {
                foreach (Collider col in colliders)
                {
                    // Skip if already hit
                    if (hitTargets.Contains(col))
                        continue;

                    // Check if collider has a valid tag
                    bool validTag = hitTags.Length == 0 || System.Array.IndexOf(hitTags, col.tag) >= 0;
                    if (!validTag)
                        continue;

                    // Don't hit the owner
                    if (col.transform == owner.transform || col.transform.IsChildOf(owner.transform))
                        continue;

                    // Mark as hit and report to owner
                    hitTargets.Add(col);
                    owner.OnHitDetected(col, hitboxFrame);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (hitboxFrame == null || !isActive) return;

            Gizmos.color = Color.red;
            Vector3 position = transform.position;

            if (hitboxFrame.shape == HitboxShape.Sphere)
            {
                float radius = hitboxFrame.size.x;
                Gizmos.DrawWireSphere(position, radius);
            }
            else if (hitboxFrame.shape == HitboxShape.Box)
            {
                Gizmos.DrawWireCube(position, hitboxFrame.size);
            }
        }
    }
}

