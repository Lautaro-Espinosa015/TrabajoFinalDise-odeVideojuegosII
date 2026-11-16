using System.Collections.Generic;
using System.IO;
using EcoTilt.Board;
using EcoTilt.Controls;
using EcoTilt.Gameplay;
using EcoTilt.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EcoTilt.EditorTools
{
    /// <summary>
    /// Genera la escena jugable a partir del layout. La escena queda guardada en el proyecto y se edita
    /// como cualquier otra; solo se vuelve a generar si se pide desde el menú "EcoTilt".
    /// </summary>
    public static class LevelSceneBuilder
    {
        private const string Root = "Assets/_Project";
        public const string ScenePath = Root + "/Scenes/Nivel1.unity";
        private const string LayoutPath = Root + "/Data/BoardLayout_Nivel1.asset";
        private static readonly string[] ObsoleteScenes = { Root + "/Scenes/Nivel1_Laberinto.unity" };

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
            ShrineTheme theme = ShrineTheme.Create(library);
            var size = new BoardDimensions();
            var prefabs = new PrefabFactory(Root + "/Prefabs", library, theme, size);
            WastePrefabs waste = prefabs.CreateWaste();
            GameObject[] bins = prefabs.CreateBins();

            TiltInputProvider input = new GameObject("Sistemas").AddComponent<TiltInputProvider>();
            GameObject board = CreateBoard(input);

            Mesh cube = GetBuiltinCube();
            var geometry = new BoardGeometryBuilder(layout, size, theme, cube, Root + "/Generated");
            BoardBuildResult result = geometry.Build(board.transform);

            for (int i = 0; i < bins.Length && i < result.BinSlots.Count; i++)
                PrefabUtility.InstantiatePrefab(bins[i], result.BinSlots[i]);

            CreateKillZone();
            CreateTestWaste(waste, result.SpawnPoint);
            SetupCameraAndLight();

            EditorAssetUtility.EnsureFolder(Root + "/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();

            Debug.Log($"EcoTilt: escena lista en {ScenePath}. Play y probá con WASD/flechas o click + mouse.");
            return true;
        }

        private static GameObject CreateBoard(TiltInputProvider input)
        {
            var board = new GameObject("Tablero");
            BoardTiltController controller = board.AddComponent<BoardTiltController>();
            var body = board.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            EditorAssetUtility.SetReference(controller, "inputSource", input);
            return board;
        }

        private static void CreateKillZone()
        {
            var zone = new GameObject("ZonaDeCaida");
            zone.transform.position = new Vector3(0f, -8f, 0f);
            var trigger = zone.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(80f, 2f, 80f);
            zone.AddComponent<KillZone>();
        }

        // Residuos de prueba. En el Hito 2 los reemplaza el spawner.
        private static void CreateTestWaste(WastePrefabs waste, Transform spawnPoint)
        {
            var group = new GameObject("Residuos_Prueba").transform;
            Quaternion lying = Quaternion.Euler(0f, 0f, 90f);
            Place(waste.Bottle, group, spawnPoint, -0.6f, lying);
            Place(waste.Can, group, spawnPoint, 0f, lying);
            Place(waste.Box, group, spawnPoint, 0.6f, Quaternion.identity);
        }

        private static void Place(GameObject prefab, Transform parent, Transform spawnPoint, float offsetX, Quaternion rotation)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.SetPositionAndRotation(spawnPoint.position + new Vector3(offsetX, 0f, 0f), rotation);
            EditorAssetUtility.SetReference(instance.GetComponent<Respawnable>(), "spawnPoint", spawnPoint);
        }

        private static void SetupCameraAndLight()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ShrineTheme.AmbientColor;

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 14.5f, -11.5f);
                camera.transform.LookAt(new Vector3(0f, -1f, 2.8f));
                camera.fieldOfView = 52f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = ShrineTheme.BackgroundColor;
            }

            Light sun = Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.color = ShrineTheme.SunColor;
                sun.intensity = 1.2f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.85f;
                sun.transform.rotation = Quaternion.Euler(58f, -28f, 0f);
            }
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
