using System.Collections.Generic;
using System.IO;
using EcoTilt.Board;
using EcoTilt.Controls;
using EcoTilt.Gameplay;
using EcoTilt.Level;
using EcoTilt.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EcoTilt.EditorTools
{
    /// <summary>
    /// Genera la escena jugable a partir del layout. La escena queda guardada en el proyecto y se edita
    /// como cualquier otra; solo se vuelve a generar si cambia LevelVersion o se pide desde el menú "EcoTilt".
    /// </summary>
    public static class LevelSceneBuilder
    {
        private const string Root = "Assets/_Project";
        public const string ScenePath = Root + "/Scenes/Nivel1.unity";
        /// <summary>Subir este número cuando cambia el diseño del nivel: la escena se regenera sola una vez.</summary>
        public const int LevelVersion = 4;
        private const string VersionPath = Root + "/Data/NivelGenerado_version.txt";
        private const string LayoutPath = Root + "/Data/BoardLayout_Laberinto.asset";
        private const string ThemePath = Root + "/Data/Tema_Tablero.asset";
        private static readonly string[] ObsoleteScenes = { Root + "/Scenes/Nivel1_Laberinto.unity" };

        private static readonly Color AmbientColor = new Color(0.30f, 0.40f, 0.44f);
        private static readonly Color BackgroundColor = new Color(0.16f, 0.30f, 0.34f);
        private static readonly Color SunColor = new Color(0.82f, 0.95f, 1.00f);

        [MenuItem("EcoTilt/Regenerar escena del nivel", priority = 0)]
        private static void BuildFromMenu()
        {
            Build(true);
        }

        public static bool Build(bool askBeforeOverwrite)
        {
            if (askBeforeOverwrite && File.Exists(ScenePath) && !EditorUtility.DisplayDialog(
                    "EcoTilt",
                    "La escena del nivel se va a regenerar desde el layout. Los cambios hechos a mano en esa escena se pierden " +
                    "(los prefabs y materiales se conservan).",
                    "Regenerar", "Cancelar"))
                return false;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            BoardLayout layout = EditorAssetUtility.LoadOrCreate<BoardLayout>(LayoutPath);
            var library = new LevelMaterialLibrary(Root + "/Materials", Root + "/Physics");
            BoardTheme theme = BoardThemeFactory.LoadOrCreate(ThemePath, library, GetBuiltinCube());
            var prefabs = new PrefabFactory(Root + "/Prefabs", library, theme, new BoardDimensions());
            WastePrefabs waste = prefabs.CreateWaste();
            GameObject[] bins = prefabs.CreateBins();

            var systems = new GameObject("Sistemas");
            TiltInputProvider input = systems.AddComponent<TiltInputProvider>();
            TiltDebugOverlay overlay = systems.AddComponent<TiltDebugOverlay>();
            EditorAssetUtility.SetReference(overlay, "source", input);

            BoardBuilder builder = CreateBoard(input);
            builder.Configure(layout, theme, bins);
            BoardBuildResult result = builder.Rebuild(layout.CreateGrid(), new EditorBoardSink(Root + "/Generated"));
            EditorUtility.SetDirty(builder);

            KillZone killZone = CreateKillZone();
            Respawnable[] items = CreateTestWaste(waste, result.SpawnPoint);
            CreateGameSystems(systems, builder, killZone, items);

            SetupLight();
            SetupCamera(result.Footprint);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            EditorAssetUtility.EnsureFolder(Root + "/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            UpdateBuildSettings();
            File.WriteAllText(VersionPath, LevelVersion.ToString());
            AssetDatabase.ImportAsset(VersionPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"EcoTilt: escena lista en {ScenePath}. Play y probá con WASD/flechas o click + mouse.");
            return true;
        }

        /// <summary>True si la escena no existe o fue generada con una versión anterior del nivel.</summary>
        public static bool IsOutdated()
        {
            if (!File.Exists(ScenePath) || !File.Exists(VersionPath)) return true;
            return !int.TryParse(File.ReadAllText(VersionPath).Trim(), out int built) || built < LevelVersion;
        }

        private static BoardBuilder CreateBoard(TiltInputProvider input)
        {
            var board = new GameObject("Tablero");
            BoardTiltController controller = board.AddComponent<BoardTiltController>();
            var body = board.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            EditorAssetUtility.SetReference(controller, "inputSource", input);
            return board.AddComponent<BoardBuilder>();
        }

        private static KillZone CreateKillZone()
        {
            var zone = new GameObject("ZonaDeCaida");
            zone.transform.position = new Vector3(0f, -8f, 0f);
            var trigger = zone.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(80f, 2f, 80f);
            return zone.AddComponent<KillZone>();
        }

        // Residuos de prueba (en el Hito 2 los reemplaza el spawner).
        private static Respawnable[] CreateTestWaste(WastePrefabs waste, Transform spawnPoint)
        {
            var group = new GameObject("Residuos_Prueba").transform;
            Quaternion lying = Quaternion.Euler(0f, 0f, 90f);
            return new[]
            {
                Place(waste.Bottle, group, spawnPoint, new Vector3(-0.45f, 0f, 0f), lying),
                Place(waste.Can, group, spawnPoint, new Vector3(0f, 0.3f, 0f), lying),
                Place(waste.Box, group, spawnPoint, new Vector3(0.45f, 0.6f, 0f), Quaternion.identity)
            };
        }

        private static Respawnable Place(GameObject prefab, Transform parent, Transform spawnPoint, Vector3 offset, Quaternion rotation)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.SetPositionAndRotation(spawnPoint.position + offset, rotation);
            var respawnable = instance.GetComponent<Respawnable>();
            EditorAssetUtility.SetReference(respawnable, "spawnPoint", spawnPoint);
            EditorAssetUtility.SetVector3(respawnable, "spawnOffset", offset);
            return respawnable;
        }

        private static void CreateGameSystems(GameObject systems, BoardBuilder board, KillZone killZone, Respawnable[] items)
        {
            LivesSystem lives = systems.AddComponent<LivesSystem>();
            EditorAssetUtility.SetReference(lives, "killZone", killZone);

            LevelRegenerator regenerator = systems.AddComponent<LevelRegenerator>();
            EditorAssetUtility.SetReference(regenerator, "board", board);
            EditorAssetUtility.SetReference(regenerator, "lives", lives);
            EditorAssetUtility.SetReferenceArray(regenerator, "items", items);

            LivesHud hud = systems.AddComponent<LivesHud>();
            EditorAssetUtility.SetReference(hud, "lives", lives);
            EditorAssetUtility.SetReference(hud, "regenerator", regenerator);
        }

        private static void SetupLight()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientColor;

            Light sun = Object.FindFirstObjectByType<Light>();
            if (sun == null) return;
            sun.color = SunColor;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            sun.transform.rotation = Quaternion.Euler(62f, -30f, 0f);
        }

        /// <summary>Cámara alta (casi cenital) que encuadra todo el nivel en un teléfono horizontal.</summary>
        private static void SetupCamera(Bounds footprint)
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            const float pitch = 72f;
            const float fieldOfView = 40f;
            const float aspect = 16f / 9f;
            float halfHeight = Mathf.Max(footprint.size.z, footprint.size.x / aspect) * 0.5f * 1.1f;
            float distance = halfHeight / Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);

            Quaternion rotation = Quaternion.Euler(pitch, 0f, 0f);
            camera.transform.SetPositionAndRotation(footprint.center - rotation * Vector3.forward * distance, rotation);
            camera.fieldOfView = fieldOfView;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.farClipPlane = distance * 3f;
        }

        private static Mesh GetBuiltinCube()
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return mesh;
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath || System.Array.IndexOf(ObsoleteScenes, s.path) >= 0);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
