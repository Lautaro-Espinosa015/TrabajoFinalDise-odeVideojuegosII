using System;
using EcoTilt.Board;
using EcoTilt.Gameplay;
using EcoTilt.UI;
using EcoTilt.Waste;
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
            PhysicsMaterial sliding = _library.Physics("PM_CajaRapida", 0.05f, 0.08f, 0.0f, PhysicsMaterialCombine.Minimum);

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
                    box.GetComponent<Rigidbody>().linearDamping = 0.01f;
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
            // SonidoArrastre vive fuera del asmdef: resolver el MonoScript sin duplicar la clase.
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/_Project/Prefabs/SonidoArrastre.cs");
            Type soundType = script != null ? script.GetClass() : null;
            if (soundType != null)
            {
                Component sound = item.GetComponent(soundType) ?? item.AddComponent(soundType);
                var serialized = new SerializedObject(sound);
                serialized.FindProperty("sonido").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Project/Prefabs/{name}.wav");
                serialized.FindProperty("velocidadMinima").floatValue = 0.1f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var audio = item.GetComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.mute = false;
            }
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

        // ------------------------------------------------------------------ preparación (también actualiza prefabs ya existentes)
        /// <summary>Agrega o actualiza WasteItem (categoría, nombre, rotación de aparición) en los prefabs de residuos.</summary>
        public WastePrefabs PrepareWaste(WastePrefabs waste, WasteCategories categories)
        {
            var lying = new Vector3(0f, 0f, 90f);
            return new WastePrefabs
            {
                Bottle = Upgrade(waste.Bottle, root => GetOrAdd<WasteItem>(root).Setup(categories.Plastic, "La botella", lying)),
                Can = Upgrade(waste.Can, root => GetOrAdd<WasteItem>(root).Setup(categories.Metal, "La lata", lying)),
                Box = Upgrade(waste.Box, root => GetOrAdd<WasteItem>(root).Setup(categories.Cardboard, "La caja", Vector3.zero))
            };
        }

        /// <summary>Agrega o actualiza RecyclingBin y su zona de detección (trigger) en los prefabs de contenedores.</summary>
        public GameObject[] PrepareBins(GameObject[] bins, WasteCategories categories, WastePrefabs waste)
        {
            WasteCategory[] order = categories.All;
            GameObject[] icons = { waste.Bottle, waste.Can, waste.Box };
            float w = _size.BinWidth;
            float d = _size.BinDepth;
            float h = _size.BinHeight;
            float t = _size.BinWall;

            var result = new GameObject[bins.Length];
            for (int i = 0; i < bins.Length; i++)
            {
                WasteCategory category = order[i % order.Length];
                GameObject icon = icons[i % icons.Length];
                result[i] = Upgrade(bins[i], root =>
                {
                    BuildSign(root, category, icon, h);
                    GetOrAdd<RecyclingBin>(root).Setup(category,
                        new Vector3(0f, -h + t + 0.2f, 0f),
                        new Vector2(w * 0.5f - t - 0.25f, d * 0.5f - t - 0.25f));
                    BoxCollider trigger = GetOrAdd<BoxCollider>(root);
                    trigger.isTrigger = true;
                    trigger.center = new Vector3(0f, -h * 0.55f, 0f);
                    trigger.size = new Vector3(w - 2f * t, h * 0.7f, d - 2f * t);
                });
            }
            return result;
        }

        /// <summary>
        /// Cartel sobre el contenedor: nombre de la categoría (siempre mirando a la cámara) y una réplica
        /// girando del residuo que va ahí. Se rehace cada vez para reflejar cambios de categoría.
        /// </summary>
        private void BuildSign(GameObject root, WasteCategory category, GameObject iconSource, float binHeight)
        {
            foreach (string old in new[] { "Cartel", "Icono" })
            {
                Transform existing = root.transform.Find(old);
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            // Cartel con el nombre
            var sign = new GameObject("Cartel");
            sign.transform.SetParent(root.transform, false);
            sign.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            sign.AddComponent<FaceCamera>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var label = new GameObject("Texto");
            label.transform.SetParent(sign.transform, false);
            var text = label.AddComponent<TextMesh>();
            text.text = category.DisplayName.Split(' ')[0].ToUpperInvariant(); // "CARTÓN", "PLÁSTICO", "METAL"
            text.font = font;
            text.fontSize = 64;
            text.characterSize = 0.05f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;
            text.color = category.Color;
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plate.name = "Fondo";
            UnityEngine.Object.DestroyImmediate(plate.GetComponent<Collider>());
            plate.transform.SetParent(sign.transform, false);
            plate.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            plate.transform.localScale = new Vector3(Mathf.Max(1.4f, text.text.Length * 0.22f), 0.5f, 1f);
            var plateRenderer = plate.GetComponent<MeshRenderer>();
            plateRenderer.sharedMaterial = _theme.DarkStone;
            plateRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Réplica girando del residuo que va en este contenedor
            GameObject icon = UnityEngine.Object.Instantiate(iconSource, root.transform);
            icon.name = "Icono";
            icon.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            icon.transform.localRotation = iconSource.GetComponent<WasteItem>() != null
                ? iconSource.GetComponent<WasteItem>().RestRotation
                : Quaternion.identity;
            icon.transform.localScale = iconSource.transform.localScale * 1.8f;
            StripPhysics(icon);
            icon.AddComponent<Spinner>();
        }

        /// <summary>Deja solo lo visual: sin scripts de juego, cuerpo rígido ni colisiones.</summary>
        private static void StripPhysics(GameObject go)
        {
            foreach (MonoBehaviour script in go.GetComponentsInChildren<MonoBehaviour>())
                if (script != null && script.GetType().Name == "SonidoArrastre") UnityEngine.Object.DestroyImmediate(script);
            foreach (AudioSource audio in go.GetComponentsInChildren<AudioSource>()) UnityEngine.Object.DestroyImmediate(audio);
            foreach (WasteItem item in go.GetComponentsInChildren<WasteItem>()) UnityEngine.Object.DestroyImmediate(item);
            foreach (Respawnable respawnable in go.GetComponentsInChildren<Respawnable>()) UnityEngine.Object.DestroyImmediate(respawnable);
            foreach (Rigidbody body in go.GetComponentsInChildren<Rigidbody>()) UnityEngine.Object.DestroyImmediate(body);
            foreach (Collider collider in go.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static GameObject Upgrade(GameObject prefab, Action<GameObject> modify)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                modify(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
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
