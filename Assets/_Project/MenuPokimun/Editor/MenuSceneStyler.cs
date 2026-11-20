using System;
using System.Collections.Generic;
using System.Linq;
using EcoTilt.Board;
using EcoTilt.Level;
using EcoTilt.Waste;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MenuSceneStyler
{
    const string Root = "Assets/_Project/MenuArt";
    const string ScenePath = "Assets/_Project/Scenes/MenuPrincipal.unity";
    static TMP_FontAsset font;
    static Sprite stone;
    static readonly Color Cream = new Color(0.95f, 0.92f, 0.83f);
    static readonly Color Gold = new Color(0.88f, 0.69f, 0.37f);

    [MenuItem("EcoTilt/Actualizar estilo del menú")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AssetDatabase.Refresh();
        PrepareAssets();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.36f, 0.47f, 0.45f);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.075f, 0.079f);
        camera.orthographic = true;
        camera.orthographicSize = 13.3f;
        camera.farClipPlane = 120f;
        var rotation = Quaternion.Euler(66f, -12f, 0f);
        camera.transform.SetPositionAndRotation(new Vector3(-8.5f, 0f, 0f) - rotation * Vector3.forward * 45f, rotation);
        var sun = new GameObject("Luz del tablero", typeof(Light)).GetComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.9f, 0.95f, 0.88f);
        sun.intensity = 1.5f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

        var demo = new GameObject("Demostración EcoTilt").AddComponent<MenuBoardDemo>();
        CreateBoard(scene, demo);
        var canvas = new GameObject("Interfaz EcoTilt", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = 0.5f;
        var ui = canvas.transform;
        var menu = new GameObject("MenuManager", typeof(AudioSource)).AddComponent<MenuManager>();
        var music = menu.GetComponent<AudioSource>();
        music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Music/Musiquita.wav");
        music.playOnAwake = true;
        music.loop = true;
        music.volume = 0.35f;

        Text(ui, "E C O T I L T   /   RECICLAJE EN MOVIMIENTO", 64, 32, 600, 30, 17, Gold);
        Text(ui, "UN TABLERO. TRES DESTINOS.", 670, 70, 780, 36, 22, Cream);
        Text(ui, "DEMOSTRACIÓN EN BUCLE", 670, 110, 780, 28, 14, Gold);
        demo.caption = Text(ui, "Incliná y guiá", 710, 775, 800, 50, 30, Cream);
        Text(ui, "El movimiento del tablero guía cada residuo.", 710, 827, 810, 35, 18, new Color(0.6f, 0.73f, 0.70f));
        Text(ui, "DISEÑO DE VIDEOJUEGOS II  ·  UNJu", 64, 852, 580, 28, 14, new Color(0.6f, 0.73f, 0.70f));

        menu.menuPrincipal = Panel(ui, "Principal", "EcoTilt", "Incliná, separá y reciclá.");
        var main = menu.menuPrincipal.transform;
        Button(main, "Jugar", 40, 258, 430, menu.Jugar, true);
        Button(main, "Opciones", 40, 332, 430, menu.Opciones);
        Button(main, "Cómo jugar", 40, 406, 430, menu.Tutorial);
        Button(main, "Créditos", 40, 480, 430, menu.Creditos);
        Button(main, "Salir", 40, 554, 430, menu.Salir);
        Text(main, "Un pequeño movimiento.\nUn cambio para el planeta.", 46, 645, 420, 60, 20, Cream);

        menu.menuOpciones = Panel(ui, "Opciones", "Opciones", "Ajustá tu experiencia.");
        var options = menu.menuOpciones.AddComponent<OpcionesMenu>();
        var op = menu.menuOpciones.transform;
        Text(op, "Música", 46, 220, 400, 38, 24, Cream);
        options.volumenMusica = Slider(op, "Volumen música", 46, 270);
        options.volumenMusica.SetValueWithoutNotify(0.35f);
        UnityEventTools.AddPersistentListener(options.volumenMusica.onValueChanged, options.changeVolumenMusica);
        Text(op, "Efectos de sonido", 46, 330, 400, 38, 24, Cream);
        options.volumenEfectos = Slider(op, "Volumen efectos", 46, 380);
        options.volumenEfectos.SetValueWithoutNotify(0.85f);
        UnityEventTools.AddPersistentListener(options.volumenEfectos.onValueChanged, options.changeVolumenEfectos);
        var fps = Button(op, "FPS: 60", 40, 465, 430, options.CycleFPS);
        options.fpsText = fps.GetComponentInChildren<TextMeshProUGUI>();
        Button(op, "Volver", 40, 636, 430, menu.Volver);

        menu.menuTutorial = Panel(ui, "Tutorial", "Cómo jugar", "El tablero es tu control.");
        Text(menu.menuTutorial.transform,
            "01   INCLINÁ\nMové suavemente el teléfono para guiar el residuo.\n\n02   SEPARÁ\nLlevá cada residuo al contenedor de su categoría. Cuando corresponda, tomá impulso en la rampa.\n\n03   AVANZÁ\nSepará 6 residuos por nivel. Las caídas restan vidas; los aciertos recuperan una.\n\nAl empezar o continuar, mantené el teléfono quieto para calibrar.",
            46, 215, 418, 405, 21, Cream);
        Button(menu.menuTutorial.transform, "Volver", 40, 636, 430, menu.Volver);
        menu.menuCreditos = Panel(ui, "Créditos", "Créditos", "EcoTilt · Inclina, separá y reciclá");
        Text(menu.menuCreditos.transform,
            "DESARROLLO\n\nWalter Leonel Cussi\nLautaro Eduardo Espinosa\nLuis Brian Gabriel Estrada\n\n\nDiseño de Videojuegos II\nUniversidad Nacional de Jujuy\n\n2026",
            46, 230, 420, 355, 24, Cream);
        Button(menu.menuCreditos.transform, "Volver", 40, 636, 430, menu.Volver);
        menu.Volver();
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("ECOTILT_MENU_STYLE_BUILT");
    }

    static void PrepareAssets()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/Textures/PiedraVerdeBronce.png");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = new Vector4(90, 90, 90, 90);
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        stone = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Textures/PiedraVerdeBronce.png");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/EcoTilt Sans.asset");
        if (font == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/LiberationSans.ttf");
            font = TMP_FontAsset.CreateFontAsset(source, 64, 8, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            font.name = "EcoTilt Sans";
            AssetDatabase.CreateAsset(font, Root + "/Fonts/EcoTilt Sans.asset");
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
        }
        font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789áéíóúÁÉÍÓÚüÜñÑ¡!¿?.,:;/-·() +");
        EditorUtility.SetDirty(font);
        foreach (var atlas in font.atlasTextures) EditorUtility.SetDirty(atlas);
    }

    static GameObject Panel(Transform parent, string name, string title, string subtitle)
    {
        var panel = Rect(parent, name, 44, 90, 510, 740);
        var image = panel.gameObject.AddComponent<Image>();
        image.sprite = stone;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1.8f;
        Text(panel, title, 44, 48, 425, 100, title == "EcoTilt" ? 88 : 52, Cream, true);
        Text(panel, subtitle, 46, 156, 420, 60, 22, Gold);
        return panel.gameObject;
    }

    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(w, h);
        return rect;
    }

    static TextMeshProUGUI Text(Transform parent, string value, float x, float y, float w, float h, float size, Color color, bool bold = false)
    {
        var text = Rect(parent, value.Split('\n')[0], x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    static Button Button(Transform parent, string label, float x, float y, float w, UnityAction action, bool primary = false)
    {
        var rect = Rect(parent, label, x, y, w, 58);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = stone;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 4f;
        image.color = primary ? new Color(1.25f, 1.2f, 0.95f) : Color.white;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.05f);
        colors.pressedColor = new Color(0.7f, 0.85f, 0.75f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        var text = Text(rect, label, 16, 7, w - 32, 44, 25, primary ? Gold : Cream, primary);
        text.alignment = TextAlignmentOptions.Center;
        UnityEventTools.AddPersistentListener(button.onClick, action);
        rect.gameObject.AddComponent<BotonAnimado>();
        return button;
    }

    static Slider Slider(Transform parent, string name, float x, float y)
    {
        var root = Rect(parent, name, x, y, 414, 38);
        var slider = root.gameObject.AddComponent<Slider>();
        var background = Rect(root, "Pista", 0, 13, 414, 10).gameObject.AddComponent<Image>();
        background.color = new Color(0.06f, 0.12f, 0.12f);
        var fillArea = Rect(root, "Área de relleno", 0, 13, 414, 10);
        var fill = Rect(fillArea, "Relleno", 0, 0, 414, 10).gameObject.AddComponent<Image>();
        fill.color = Gold;
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.sizeDelta = Vector2.zero;
        fill.rectTransform.anchoredPosition = Vector2.zero;
        var handleArea = Rect(root, "Área del control", 10, 0, 394, 38);
        var handle = Rect(handleArea, "Control", 0, 2, 24, 34).gameObject.AddComponent<Image>();
        handle.color = Cream;
        handle.rectTransform.anchorMin = Vector2.zero;
        handle.rectTransform.anchorMax = Vector2.up;
        handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        handle.rectTransform.sizeDelta = new Vector2(24f, 0f);
        handle.rectTransform.anchoredPosition = Vector2.zero;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        return slider;
    }

    static void CreateBoard(Scene menuScene, MenuBoardDemo demo)
    {
        var level = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Nivel1.unity", OpenSceneMode.Additive);
        var original = level.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<BoardBuilder>()).Single();
        var board = Object.Instantiate(original.gameObject);
        board.name = "Tablero de demostración";
        SceneManager.MoveGameObjectToScene(board, menuScene);
        board.transform.SetParent(demo.transform, false);
        board.transform.localPosition = Vector3.zero;
        board.transform.localRotation = Quaternion.identity;
        var layout = original.Layout;
        var grid = layout.CreateGrid();
        float step = layout.CellSize + layout.WallThickness;
        float width = grid.Columns * step + layout.WallThickness;
        float depth = grid.Rows * step + layout.WallThickness;
        Vector3 Cell(Vector2Int c) => new Vector3(-width * 0.5f + layout.WallThickness + layout.CellSize * 0.5f + c.x * step,
            0.42f, depth * 0.5f - layout.WallThickness - layout.CellSize * 0.5f - c.y * step);
        var tracks = new List<MenuBoardDemo.Track>();
        foreach (var bin in board.GetComponentsInChildren<RecyclingBin>())
        {
            var category = bin.Category;
            string itemName = category.name.Contains("Metal") ? "Lata" : category.name.Contains("Carton") ? "Caja" : "Botella";
            var item = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Residuo_" + itemName + ".prefab"), menuScene);
            item.transform.SetParent(board.transform, false);
            item.transform.localScale *= 1.8f;
            var destination = board.transform.InverseTransformPoint(bin.transform.position);
            bool outside = Mathf.Abs(destination.x) > width * 0.5f || Mathf.Abs(destination.z) > depth * 0.5f;
            var cell = new Vector2Int(Mathf.RoundToInt((destination.x + width * 0.5f - layout.WallThickness - layout.CellSize * 0.5f) / step),
                Mathf.RoundToInt((depth * 0.5f - layout.WallThickness - layout.CellSize * 0.5f - destination.z) / step));
            var points = grid.FindPath(grid.StartCell, outside ? grid.ExitCell : cell).Select(Cell).ToList();
            if (points.Count < 2) throw new Exception("Ruta demo inválida: " + category.name);
            if (outside)
            {
                Vector3 forward = grid.ExitSide == BoardSide.Right ? Vector3.right : grid.ExitSide == BoardSide.Left ? Vector3.left : grid.ExitSide == BoardSide.Top ? Vector3.forward : Vector3.back;
                var edge = Cell(grid.ExitCell) + forward * (layout.CellSize * 0.5f + layout.WallThickness);
                var startRamp = edge + forward * 2.2f;
                points.Add(edge);
                points.Add(startRamp);
                points.Add(startRamp + forward * 1.6f + Vector3.up * 0.4f);
                points.Add(destination + Vector3.up * 0.2f);
            }
            else points.Add(destination + Vector3.up * 0.2f);
            item.transform.localPosition = points[0];
            tracks.Add(new MenuBoardDemo.Track { item = item.transform, points = points.ToArray(), category = category.DisplayName, jump = outside });
        }
        // La copia del tablero es escenografía: no ejecuta la partida ni reproduce audio de residuos.
        foreach (var item in board.GetComponentsInChildren<WasteItem>(true)) Object.DestroyImmediate(item);
        foreach (var behaviour in board.GetComponentsInChildren<MonoBehaviour>(true))
            if (!(behaviour is EcoTilt.UI.FaceCamera)) Object.DestroyImmediate(behaviour);
        foreach (var collider in board.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        foreach (var body in board.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(body);
        foreach (var audio in board.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(audio);
        foreach (var track in tracks) track.item.gameObject.SetActive(false);
        demo.board = board.transform;
        demo.tracks = tracks.ToArray();
        EditorSceneManager.CloseScene(level, true);
        SceneManager.SetActiveScene(menuScene);
    }
}
