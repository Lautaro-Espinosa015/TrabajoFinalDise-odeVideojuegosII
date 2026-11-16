using System;
using EcoTilt.Board;
using EcoTilt.Gameplay;
using UnityEditor;
using UnityEngine;

namespace EcoTilt.EditorTools
{
    internal sealed class WastePrefabs
    {
        public GameObject Bottle;
        public GameObject Can;
        public GameObject Box;
    }

    /// <summary>
    /// Crea los prefabs de residuos y contenedores la primera vez. Si ya existen no los toca,
    /// así se pueden editar libremente en Unity (modo prefab) sin que el generador los pise.
    /// </summary>
    internal sealed class PrefabFactory
    {
        public static readonly string[] BinNames = { "Contenedor_Plastico", "Contenedor_Metal", "Contenedor_Carton" };

        private readonly string _folder;
        private readonly LevelMaterialLibrary _library;
        private readonly BoardTheme _theme;
        private readonly BoardDimensions _size;

        public PrefabFactory(string folder, LevelMaterialLibrary library, BoardTheme theme, BoardDimensions size)
        {
            _folder = folder;
            _library = library;
            _theme = theme;
            _size = size;
            EditorAssetUtility.EnsureFolder(folder);
        }

        // ------------------------------------------------------------------ residuos
        public WastePrefabs CreateWaste()
        {
            PhysicsMaterial rolling = _library.Physics("PM_Rueda", 0.5f, 0.5f, 0.1f);
            PhysicsMaterial sliding = _library.Physics("PM_Desliza", 0.15f, 0.2f, 0.0f);

            return new WastePrefabs
            {
                Bottle = GetOrCreate("Residuo_Botella", () =>
                {
                    GameObject bottle = Waste("Residuo_Botella", PrimitiveType.Capsule, new Vector3(0.3f, 0.3f, 0.3f), 0.3f,
                        _library.Lit("M_Botella", new Color(0.55f, 0.88f, 0.95f), 0.7f), rolling);
                    Decor(bottle.transform, PrimitiveType.Cylinder, new Vector3(0f, 1.02f, 0f), new Vector3(0.38f, 0.08f, 0.38f),
                        _library.Lit("M_Botella_Tapa", new Color(0.10f, 0.35f, 0.85f)));
                    Decor(bottle.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(1.03f, 0.28f, 1.03f),
                        _library.Lit("M_Botella_Etiqueta", new Color(0.20f, 0.70f, 0.35f)));
                    return bottle;
                }),
                Can = GetOrCreate("Residuo_Lata", () =>
                {
                    GameObject can = Waste("Residuo_Lata", PrimitiveType.Cylinder, new Vector3(0.3f, 0.2f, 0.3f), 0.2f,
                        _library.Lit("M_Lata", new Color(0.82f, 0.13f, 0.13f), 0.6f), rolling);
                    Material metal = _library.Lit("M_Lata_Metal", new Color(0.80f, 0.82f, 0.85f), 0.8f);
                    Decor(can.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.97f, 0f), new Vector3(0.94f, 0.04f, 0.94f), metal);
                    Decor(can.transform, PrimitiveType.Cylinder, new Vector3(0f, -0.97f, 0f), new Vector3(0.94f, 0.04f, 0.94f), metal);
                    return can;
                }),
                Box = GetOrCreate("Residuo_Caja", () =>
                {
                    GameObject box = Waste("Residuo_Caja", PrimitiveType.Cube, new Vector3(0.4f, 0.4f, 0.4f), 0.4f,
                        _library.Lit("M_Caja", new Color(0.72f, 0.53f, 0.32f)), sliding);
                    Decor(box.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.22f, 1.02f, 1.02f),
                        _library.Lit("M_Caja_Cinta", new Color(0.86f, 0.78f, 0.58f)));
                    return box;
                })
            };
        }

        private static GameObject Waste(string name, PrimitiveType shape, Vector3 scale, float mass,
                                        Material material, PhysicsMaterial physics)
        {
            GameObject item = GameObject.CreatePrimitive(shape);
            item.name = name;
            item.transform.localScale = scale;
            item.GetComponent<MeshRenderer>().sharedMaterial = material;
            item.GetComponent<Collider>().sharedMaterial = physics;

            var body = item.AddComponent<Rigidbody>();
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping = 0.05f;
            body.angularDamping = 0.05f;

            item.AddComponent<Respawnable>();
            return item;
        }

        // ------------------------------------------------------------------ contenedores
        public GameObject[] CreateBins()
        {
            Color[] colors =
            {
                new Color(0.98f, 0.78f, 0.12f), // plástico
                new Color(0.72f, 0.80f, 0.88f), // metal
                new Color(0.22f, 0.52f, 1.00f)  // cartón
            };

            var bins = new GameObject[BinNames.Length];
            for (int i = 0; i < BinNames.Length; i++)
            {
                string name = BinNames[i];
                Color color = colors[i];
                bins[i] = GetOrCreate(name, () => Bin(name, color));
            }
            return bins;
        }

        private GameObject Bin(string name, Color color)
        {
            var root = new GameObject(name);
            float w = _size.BinWidth;
            float d = _size.BinDepth;
            float h = _size.BinHeight;
            float t = _size.BinWall;

            // Paredes con colisión (el borde superior del contenedor está en y = 0)
            Wall(root.transform, "Fondo", new Vector3(0f, -h + t * 0.5f, 0f), new Vector3(w, t, d));
            Wall(root.transform, "Lado_Izq", new Vector3(-w * 0.5f + t * 0.5f, -h * 0.5f, 0f), new Vector3(t, h, d));
            Wall(root.transform, "Lado_Der", new Vector3(w * 0.5f - t * 0.5f, -h * 0.5f, 0f), new Vector3(t, h, d));
            Wall(root.transform, "Frente", new Vector3(0f, -h * 0.5f, -d * 0.5f + t * 0.5f), new Vector3(w, h, t));
            Wall(root.transform, "Trasera", new Vector3(0f, -h * 0.5f, d * 0.5f - t * 0.5f), new Vector3(w, h, t));

            // Borde luminoso y placa frontal del color de la categoría
            Material glow = _library.Emissive($"M_{name}_Brillo", color, 2.5f);
            Material tint = _library.Lit($"M_{name}_Interior", color * 0.7f);
            const float rim = 0.06f;
            Decor(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.01f, -d * 0.5f + t * 0.5f), new Vector3(w, 0.03f, rim), glow);
            Decor(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.01f, d * 0.5f - t * 0.5f), new Vector3(w, 0.03f, rim), glow);
            Decor(root.transform, PrimitiveType.Cube, new Vector3(-w * 0.5f + t * 0.5f, 0.01f, 0f), new Vector3(rim, 0.03f, d), glow);
            Decor(root.transform, PrimitiveType.Cube, new Vector3(w * 0.5f - t * 0.5f, 0.01f, 0f), new Vector3(rim, 0.03f, d), glow);
            Decor(root.transform, PrimitiveType.Cube, new Vector3(0f, -h * 0.45f, -d * 0.5f - 0.012f), new Vector3(w * 0.55f, h * 0.45f, 0.02f), glow);
            Decor(root.transform, PrimitiveType.Cube, new Vector3(0f, -h + t + 0.01f, 0f), new Vector3(w - 2f * t, 0.02f, d - 2f * t), tint);
            return root;
        }

        private void Wall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = size;
            wall.GetComponent<MeshRenderer>().sharedMaterial = _theme.DarkStone;
            wall.GetComponent<Collider>().sharedMaterial = _theme.Surface;
        }

        // ------------------------------------------------------------------ utilidades
        private static void Decor(Transform parent, PrimitiveType shape, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = "Detalle";
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private GameObject GetOrCreate(string name, Func<GameObject> build)
        {
            string path = $"{_folder}/{name}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject temp = build();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            UnityEngine.Object.DestroyImmediate(temp);
            return prefab;
        }
    }
}
