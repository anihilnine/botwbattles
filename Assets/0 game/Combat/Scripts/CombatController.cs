using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Main combat controller that handles input, move execution, animation playback, and hitbox management.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Animation))]
    public class CombatController : MonoBehaviour
    {
        [Header("Move Data")]
        [SerializeField] private MoveData lightAttackMove;
        [SerializeField] private MoveData heavyAttackMove;
        [SerializeField] private MoveData blockMove;
        [SerializeField] private MoveData parryMove;

        [Header("Input Settings")]
        [SerializeField] private string lightAttackInput = "Fire1";
        [SerializeField] private string heavyAttackInput = "Fire2";
        [SerializeField] private string blockInput = "Block";
        [SerializeField] private string parryInput = "Parry";

        [Header("Hit Detection")]
        [SerializeField] private LayerMask hitLayers = -1;
        [SerializeField] private string[] hitTags = { "Enemy" };

        [Header("Bone Reference")]
        [SerializeField] private Transform boneRoot; // Root transform to search for bones

        private UnityEngine.Animation animationComponent;
        private MoveData currentMove;
        private float currentMoveStartTime;
        private bool isExecutingMove = false;
        private Dictionary<HitboxFrame, GameObject> activeHitboxes = new Dictionary<HitboxFrame, GameObject>();
        private float lastMoveEndTime;
        private float currentCooldown = 0f;

        private void Awake()
        {
            animationComponent = GetComponent<UnityEngine.Animation>();
            if (boneRoot == null)
                boneRoot = transform;
        }

        private void Update()
        {
            HandleInput();
            
            if (isExecutingMove)
            {
                UpdateMoveExecution();
            }

            // Update cooldown
            if (currentCooldown > 0f)
            {
                currentCooldown -= Time.deltaTime;
            }
        }

        private void HandleInput()
        {
            // Don't allow new inputs if currently in a move (except maybe block/parry)
            if (isExecutingMove && currentMove != null)
            {
                // Allow block/parry to interrupt (could be refined based on game design)
                if (Input.GetButtonDown(blockInput) && blockMove != null)
                {
                    ExecuteMove(blockMove);
                }
                else if (Input.GetButtonDown(parryInput) && parryMove != null)
                {
                    ExecuteMove(parryMove);
                }
                return;
            }

            // Check cooldown
            if (currentCooldown > 0f)
                return;

            // Handle move inputs
            if (Input.GetButtonDown(lightAttackInput) && lightAttackMove != null)
            {
                ExecuteMove(lightAttackMove);
            }
            else if (Input.GetButtonDown(heavyAttackInput) && heavyAttackMove != null)
            {
                ExecuteMove(heavyAttackMove);
            }
            else if (Input.GetButton(blockInput) && blockMove != null)
            {
                ExecuteMove(blockMove);
            }
            else if (Input.GetButtonDown(parryInput) && parryMove != null)
            {
                ExecuteMove(parryMove);
            }
        }

        private void ExecuteMove(MoveData move)
        {
            if (move == null || move.animationClip == null)
            {
                Debug.LogWarning($"Cannot execute move: {move?.moveName ?? "null"} - missing move data or animation clip");
                return;
            }

            // Stop current move
            if (isExecutingMove)
            {
                CleanupCurrentMove();
            }

            currentMove = move;
            currentMoveStartTime = Time.time;
            isExecutingMove = true;

            // Play animation
            animationComponent.clip = move.animationClip;
            animationComponent.Play();

            // Set cooldown
            currentCooldown = move.cooldown;

            // Start coroutine to end move after duration
            StartCoroutine(EndMoveAfterDuration(move.totalDuration));
        }

        private IEnumerator EndMoveAfterDuration(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (isExecutingMove && currentMove != null)
            {
                CleanupCurrentMove();
                isExecutingMove = false;
                currentMove = null;
                lastMoveEndTime = Time.time;
            }
        }

        private void UpdateMoveExecution()
        {
            if (currentMove == null) return;

            float elapsedTime = Time.time - currentMoveStartTime;
            List<HitboxFrame> activeFrames = currentMove.GetActiveHitboxes(elapsedTime);

            // Create hitboxes for newly active frames
            foreach (HitboxFrame frame in activeFrames)
            {
                if (!activeHitboxes.ContainsKey(frame))
                {
                    CreateHitbox(frame);
                }
            }

            // Remove hitboxes for inactive frames
            List<HitboxFrame> framesToRemove = new List<HitboxFrame>();
            foreach (var kvp in activeHitboxes)
            {
                if (!kvp.Key.IsActiveAtTime(elapsedTime))
                {
                    framesToRemove.Add(kvp.Key);
                }
            }

            foreach (HitboxFrame frame in framesToRemove)
            {
                if (activeHitboxes.TryGetValue(frame, out GameObject hitboxObj))
                {
                    Destroy(hitboxObj);
                    activeHitboxes.Remove(frame);
                }
            }
        }

        private void CreateHitbox(HitboxFrame frame)
        {
            // Find the bone transform if boneName is specified
            Transform parentTransform = transform;
            if (!string.IsNullOrEmpty(currentMove.boneName))
            {
                Transform boneTransform = FindBoneTransform(currentMove.boneName);
                if (boneTransform != null)
                {
                    parentTransform = boneTransform;
                }
                else
                {
                    Debug.LogWarning($"Bone '{currentMove.boneName}' not found for move '{currentMove.moveName}'. Using root transform.");
                }
            }

            // Create hitbox GameObject
            GameObject hitboxObj = new GameObject($"Hitbox_{frame.startTime}_{frame.endTime}");
            hitboxObj.transform.SetParent(parentTransform);
            hitboxObj.transform.localPosition = frame.positionOffset;
            hitboxObj.transform.localRotation = Quaternion.identity;

            // Add CombatHitbox component
            CombatHitbox combatHitbox = hitboxObj.AddComponent<CombatHitbox>();
            combatHitbox.Initialize(frame, this, currentMoveStartTime, hitLayers, hitTags);

            activeHitboxes[frame] = hitboxObj;
        }

        private Transform FindBoneTransform(string bonePath)
        {
            // Search recursively for the bone
            Transform found = boneRoot.Find(bonePath);
            if (found != null)
                return found;

            // If path contains slashes, try to find recursively
            string[] pathParts = bonePath.Split('/');
            Transform current = boneRoot;

            foreach (string part in pathParts)
            {
                if (current == null) return null;
                
                current = current.Find(part);
                if (current == null)
                {
                    // Try deep search
                    current = SearchChildren(boneRoot, part);
                }
            }

            return current;
        }

        private Transform SearchChildren(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;

                Transform found = SearchChildren(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private void CleanupCurrentMove()
        {
            // Cleanup all active hitboxes
            foreach (var kvp in activeHitboxes)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            activeHitboxes.Clear();

            // Stop animation
            if (animationComponent.isPlaying)
            {
                animationComponent.Stop();
            }
        }

        /// <summary>
        /// Called by CombatHitbox when a hit is detected
        /// </summary>
        public void OnHitDetected(Collider hitCollider, HitboxFrame hitboxFrame)
        {
            Debug.Log($"Hit detected: {hitCollider.name} with {currentMove?.moveName} (Damage: {hitboxFrame.damage})");
            
            // TODO: Apply damage, trigger hit reactions, etc.
            // This is where you would integrate with damage systems, enemy AI, etc.
        }

        private void OnDisable()
        {
            CleanupCurrentMove();
            isExecutingMove = false;
        }
    }
}

