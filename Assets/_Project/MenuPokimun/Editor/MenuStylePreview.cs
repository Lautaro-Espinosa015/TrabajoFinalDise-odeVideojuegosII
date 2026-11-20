using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MenuStylePreview
{
    public static void BuildAndPreview()
    {
        MenuSceneStyler.Build();
        Capture();
    }

    [MenuItem("EcoTilt/Guardar vista previa del menú")]
    public static void Capture()
    {
        if (!Application.isPlaying)
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/MenuPrincipal.unity");
        var menu = Object.FindFirstObjectByType<MenuManager>();
        var demo = Object.FindFirstObjectByType<MenuBoardDemo>();
        if (!Application.isPlaying && demo != null && demo.tracks.Length > 0)
        {
            var track = demo.tracks[0];
            track.item.gameObject.SetActive(true);
            track.item.localPosition = track.points[Mathf.Min(4, track.points.Length - 1)];
        }
        var canvas = menu.menuPrincipal.GetComponentInParent<Canvas>();
        var camera = Camera.main;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        camera.aspect = 16f / 9f;
        Directory.CreateDirectory("Logs");
        menu.Volver();
        Render(camera, "Logs/MenuPrincipal.png");
        menu.Opciones();
        Render(camera, "Logs/MenuOpciones.png");
        menu.Tutorial();
        Render(camera, "Logs/MenuTutorial.png");
        menu.Volver();
        Render(camera, "Logs/MenuPrincipal.png");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Debug.Log("ECOTILT_MENU_PREVIEW_SAVED");
    }

    static void Render(Camera camera, string path)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) text.ForceMeshUpdate();
        var rt = RenderTexture.GetTemporary(1600, 900, 24);
        var previous = RenderTexture.active;
        camera.targetTexture = rt;
        camera.Render();
        RenderTexture.active = rt;
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        Object.DestroyImmediate(image);
    }
}
