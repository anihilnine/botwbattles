using UnityEngine;
using UnityEditor;
using Game.Combat;

namespace Game.Combat.Editor
{
    public static class MoveDataCreator
    {
        [MenuItem("Assets/Create/Combat/Light Attack Move", priority = 1)]
        public static void CreateLightAttackMove()
        {
            CreateMoveAsset(MoveType.LightAttack, "LightAttack");
        }

        [MenuItem("Assets/Create/Combat/Heavy Attack Move", priority = 2)]
        public static void CreateHeavyAttackMove()
        {
            CreateMoveAsset(MoveType.HeavyAttack, "HeavyAttack");
        }

        [MenuItem("Assets/Create/Combat/Block Move", priority = 3)]
        public static void CreateBlockMove()
        {
            CreateMoveAsset(MoveType.Block, "Block");
        }

        [MenuItem("Assets/Create/Combat/Parry Move", priority = 4)]
        public static void CreateParryMove()
        {
            CreateMoveAsset(MoveType.Parry, "Parry");
        }

        private static void CreateMoveAsset(MoveType moveType, string defaultName)
        {
            MoveData moveData = ScriptableObject.CreateInstance<MoveData>();
            moveData.moveName = defaultName;
            moveData.moveType = moveType;
            moveData.totalDuration = 1f; // Default 1 second
            moveData.cooldown = 0f;

            // Create default hitbox frame for attacks (not for block/parry)
            if (moveType == MoveType.LightAttack || moveType == MoveType.HeavyAttack)
            {
                HitboxFrame defaultFrame = new HitboxFrame
                {
                    startTime = 0.2f,
                    endTime = 0.4f,
                    positionOffset = new Vector3(0, 0, 1f), // 1 unit forward
                    size = new Vector3(0.5f, 0.5f, 0.5f), // 0.5 radius for sphere
                    shape = HitboxShape.Sphere,
                    damage = moveType == MoveType.LightAttack ? 10f : 20f
                };
                moveData.hitboxFrames.Add(defaultFrame);
            }

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets/0 game/Combat/Data";
            }
            else if (!AssetDatabase.IsValidFolder(path))
            {
                path = System.IO.Path.GetDirectoryName(path);
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{defaultName}.asset");
            AssetDatabase.CreateAsset(moveData, assetPath);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = moveData;
        }
    }
}

