using System.IO;
using UnityEditor;
using UnityEngine;
using VoidEater.Core;
using VoidEater.Hole;
using VoidEater.Objects;
using VoidEater.Player;

namespace VoidEater.Editor
{
    public static class PhaseTestSceneBuilder
    {
        private const float InitialPlayerHoleRadius = 0.5f;
        private const string SettingsPath = "Assets/GameSettings.asset";
        private const string MaterialsFolder = "Assets/Materials";
        private const string HoleMaterialPath = MaterialsFolder + "/M_HoleVisual.mat";
        private const string HoleProgressMaterialPath = MaterialsFolder + "/M_HoleProgress.mat";
        private const string GroundMaterialPath = MaterialsFolder + "/M_TestGround.mat";
        private const string SwallowableMaterialPath = MaterialsFolder + "/M_TestSwallowable.mat";

        [MenuItem("Void Eater/Setup Core Test Scene")]
        [MenuItem("Void Eater/Setup Phase 2 Test Scene")]
        public static void SetupPhase2TestScene()
        {
            EnsureMaterialsFolder();

            GameSettings settings = EnsureGameSettings();
            Material holeMaterial = EnsureMaterial(HoleMaterialPath, new Color(0.005f, 0.005f, 0.008f));
            Material progressMaterial = EnsureLineMaterial(HoleProgressMaterialPath, new Color(0.3f, 0.95f, 1f));
            Material groundMaterial = EnsureMaterial(GroundMaterialPath, new Color(0.2f, 0.24f, 0.22f));
            Material swallowableMaterial = EnsureMaterial(SwallowableMaterialPath, new Color(0.85f, 0.68f, 0.38f));

            GameObject ground = EnsureGround(groundMaterial);
            GameObject player = EnsurePlayerHole(settings, holeMaterial, progressMaterial);
            EnsureCamera(player.GetComponent<PlayerHole>());
            EnsureSampleSwallowables(swallowableMaterial);

            Selection.activeGameObject = player;
            EditorGUIUtility.PingObject(player);
            EditorUtility.SetDirty(ground);
            EditorUtility.SetDirty(player);
        }

        private static GameSettings EnsureGameSettings()
        {
            GameSettings settings = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<GameSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            settings.requireFullContainment = false;
            settings.swallowTolerance = 0.1f;
            settings.passThroughClearance = 0.05f;
            settings.baseGrowthRequired = 35f;
            settings.growthRequirementMultiplier = 1.22f;
            settings.objectVolumeProgressWeight = 8f;
            settings.objectScoreProgressWeight = 1f;
            settings.radiusGainPerLevel = 0.25f;
            settings.maximumRadius = 12f;
            settings.scoreMultiplier = 1f;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static GameObject EnsureGround(Material material)
        {
            GameObject ground = GameObject.Find("Test Ground");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Undo.RegisterCreatedObjectUndo(ground, "Create Test Ground");
                ground.name = "Test Ground";
            }

            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            AssignMaterial(ground, material);
            return ground;
        }

        private static GameObject EnsurePlayerHole(
            GameSettings settings,
            Material holeMaterial,
            Material progressMaterial)
        {
            GameObject player = GameObject.Find("Player Hole");
            if (player == null)
            {
                player = new GameObject("Player Hole");
                Undo.RegisterCreatedObjectUndo(player, "Create Player Hole");
            }

            player.transform.position = new Vector3(0f, 0.05f, 0f);
            player.transform.rotation = Quaternion.identity;

            Rigidbody body = EnsureComponent<Rigidbody>(player);
            body.isKinematic = true;
            body.useGravity = false;

            SphereCollider trigger = EnsureComponent<SphereCollider>(player);
            trigger.isTrigger = true;
            trigger.radius = InitialPlayerHoleRadius;

            PlayerHole playerHole = EnsureComponent<PlayerHole>(player);

            Transform visualRoot = player.transform.Find("visualRoot");
            if (visualRoot == null)
            {
                visualRoot = new GameObject("visualRoot").transform;
                Undo.RegisterCreatedObjectUndo(visualRoot.gameObject, "Create Hole Visual Root");
                visualRoot.SetParent(player.transform);
            }

            visualRoot.localPosition = new Vector3(0f, 0.02f, 0f);
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;

            Transform holeVisual = visualRoot.Find("HoleVisual");
            if (holeVisual == null)
            {
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Undo.RegisterCreatedObjectUndo(visual, "Create Hole Visual");
                visual.name = "HoleVisual";
                holeVisual = visual.transform;
                holeVisual.SetParent(visualRoot);
            }

            holeVisual.localPosition = Vector3.zero;
            holeVisual.localRotation = Quaternion.identity;
            holeVisual.localScale = new Vector3(1f, 0.01f, 1f);
            Object.DestroyImmediate(holeVisual.GetComponent<Collider>());
            AssignMaterial(holeVisual.gameObject, holeMaterial);

            SerializedObject serializedPlayer = new SerializedObject(playerHole);
            serializedPlayer.FindProperty("settings").objectReferenceValue = settings;
            serializedPlayer.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            serializedPlayer.FindProperty("radius").floatValue = InitialPlayerHoleRadius;
            serializedPlayer.ApplyModifiedProperties();

            EnsureProgressRing(player.transform, playerHole, progressMaterial);

            return player;
        }

        private static void EnsureProgressRing(
            Transform player,
            PlayerHole playerHole,
            Material progressMaterial)
        {
            Transform rings = player.Find("Rings");
            if (rings == null)
            {
                rings = new GameObject("Rings").transform;
                Undo.RegisterCreatedObjectUndo(rings.gameObject, "Create Hole Rings");
                rings.SetParent(player, false);
            }

            rings.localPosition = Vector3.zero;
            rings.localRotation = Quaternion.identity;
            rings.localScale = Vector3.one;

            HoleProgressRing progressRing = EnsureComponent<HoleProgressRing>(rings.gameObject);
            progressRing.SetTarget(playerHole);

            SerializedObject serializedRing = new SerializedObject(progressRing);
            serializedRing.FindProperty("target").objectReferenceValue = playerHole;
            serializedRing.FindProperty("progressMaterial").objectReferenceValue = progressMaterial;
            serializedRing.FindProperty("segments").intValue = 128;
            serializedRing.FindProperty("progressRadiusOffset").floatValue = 0.08f;
            serializedRing.FindProperty("progressWidth").floatValue = 0.09f;
            serializedRing.FindProperty("ringHeight").floatValue = 2.5f;
            serializedRing.ApplyModifiedProperties();
            progressRing.SetTarget(playerHole);
            EditorUtility.SetDirty(progressRing);
        }

        private static void EnsureCamera(PlayerHole target)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                Undo.RegisterCreatedObjectUndo(cameraObject, "Create Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 12f;
            camera.transform.position = new Vector3(0f, 18f, -14f);
            camera.transform.LookAt(target.transform.position);

            HoleCameraFollow follow = EnsureComponent<HoleCameraFollow>(camera.gameObject);
            SerializedObject serializedFollow = new SerializedObject(follow);
            serializedFollow.FindProperty("target").objectReferenceValue = target;
            serializedFollow.FindProperty("targetCamera").objectReferenceValue = camera;
            serializedFollow.FindProperty("offset").vector3Value = new Vector3(0f, 18f, -14f);
            serializedFollow.FindProperty("followSharpness").floatValue = 8f;
            serializedFollow.FindProperty("baseOrthographicSize").floatValue = 10f;
            serializedFollow.FindProperty("sizePerRadius").floatValue = 1.25f;
            serializedFollow.FindProperty("minOrthographicSize").floatValue = 8f;
            serializedFollow.FindProperty("maxOrthographicSize").floatValue = 28f;
            serializedFollow.FindProperty("swallowPulseSize").floatValue = 1.1f;
            serializedFollow.FindProperty("swallowPulseDuration").floatValue = 0.22f;
            serializedFollow.ApplyModifiedProperties();

            HoleDebugHUD debugHud = EnsureComponent<HoleDebugHUD>(camera.gameObject);
            SerializedObject serializedHud = new SerializedObject(debugHud);
            serializedHud.FindProperty("target").objectReferenceValue = target;
            serializedHud.FindProperty("fontSize").intValue = 82;
            serializedHud.FindProperty("topPositionRatio").floatValue = 0.99f;
            serializedHud.FindProperty("widthRatio").floatValue = 0.9f;
            serializedHud.ApplyModifiedProperties();
        }

        private static void EnsureSampleSwallowables(Material material)
        {
            CreateSampleCube("Sample Tiny Cube", new Vector3(2.5f, 0.15f, -1.5f), Vector3.one * 0.3f, 5, material);
            CreateSampleCube("Sample Small Cube", new Vector3(3.25f, 0.25f, 0f), Vector3.one * 0.5f, 10, material);
            CreateSampleCube("Sample Medium Cube", new Vector3(5f, 0.5f, 1.75f), Vector3.one, 25, material);
            CreateSampleCube("Sample Tall Block", new Vector3(6.5f, 1.5f, -1.75f), new Vector3(0.8f, 3f, 0.8f), 45, material);
            CreateSampleCube("Sample Large Cube", new Vector3(8.25f, 1f, 0f), Vector3.one * 2f, 75, material);
        }

        private static void CreateSampleCube(string name, Vector3 position, Vector3 scale, int score, Material material)
        {
            GameObject cube = GameObject.Find(name);
            if (cube == null)
            {
                cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(cube, "Create " + name);
                cube.name = name;
            }

            cube.transform.position = position;
            cube.transform.rotation = Quaternion.identity;
            cube.transform.localScale = scale;
            AssignMaterial(cube, material);

            Swallowable swallowable = EnsureComponent<Swallowable>(cube);
            SerializedObject serializedSwallowable = new SerializedObject(swallowable);
            serializedSwallowable.FindProperty("size").floatValue = 0f;
            serializedSwallowable.FindProperty("score").intValue = score;
            serializedSwallowable.FindProperty("activatePhysicsOnStart").boolValue = false;
            serializedSwallowable.FindProperty("inwardAcceleration").floatValue = 18f;
            serializedSwallowable.FindProperty("edgeDownAcceleration").floatValue = 34f;
            serializedSwallowable.FindProperty("tumbleTorque").floatValue = 18f;
            serializedSwallowable.FindProperty("consumeDepth").floatValue = 2.5f;
            serializedSwallowable.FindProperty("maxFallSpeed").floatValue = 9f;
            serializedSwallowable.FindProperty("occlusionRevealAlpha").floatValue = 0.18f;
            serializedSwallowable.FindProperty("occlusionOutlineColor").colorValue = Color.white;
            serializedSwallowable.FindProperty("occlusionOutlineWidth").floatValue = 0.04f;
            serializedSwallowable.FindProperty("occlusionMemory").floatValue = 0.08f;
            serializedSwallowable.ApplyModifiedProperties();

            Rigidbody body = EnsureComponent<Rigidbody>(cube);
            body.useGravity = false;
            body.isKinematic = true;
            body.mass = Mathf.Max(0.25f, scale.x * scale.y * scale.z);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = Undo.AddComponent<T>(gameObject);
            }

            return component;
        }

        private static void EnsureMaterialsFolder()
        {
            if (!Directory.Exists(MaterialsFolder))
            {
                Directory.CreateDirectory(MaterialsFolder);
                AssetDatabase.Refresh();
            }
        }

        private static Material EnsureMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureLineMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AssignMaterial(GameObject gameObject, Material material)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}
