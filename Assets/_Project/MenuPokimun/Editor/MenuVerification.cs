using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class MenuVerification
{
    private const string MenuPath = "Assets/_Project/Scenes/MenuPrincipal.unity";

    [MenuItem("EcoTilt/Verificar menú principal")]
    public static void Verify()
    {
        var scene = EditorSceneManager.OpenScene(MenuPath);
        var objects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (var t in objects)
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0)
                throw new Exception("Script faltante: " + t.name);

        var menu = UnityEngine.Object.FindFirstObjectByType<MenuManager>();
        if (menu == null) throw new Exception("Falta MenuManager");
        var panels = new[] { menu.menuPrincipal, menu.menuOpciones, menu.menuTutorial, menu.menuCreditos };
        if (panels.Any(p => p == null)) throw new Exception("Falta un panel");
        foreach (var action in new Action[] { menu.Volver, menu.Opciones, menu.Tutorial, menu.Creditos })
        {
            action();
            if (panels.Count(p => p.activeSelf) != 1) throw new Exception("Paneles superpuestos");
        }
        menu.Volver();
        foreach (var button in objects.SelectMany(t => t.GetComponents<Button>()))
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                var target = button.onClick.GetPersistentTarget(i);
                var method = button.onClick.GetPersistentMethodName(i);
                if (target == null || target.GetType().GetMethod(method) == null)
                    throw new Exception("Botón desconectado: " + button.name + "/" + method);
            }
        var options = menu.menuOpciones.GetComponentInChildren<OpcionesMenu>(true);
        if (options == null || options.volumenMusica == null || options.volumenEfectos == null ||
            options.fpsText == null) throw new Exception("Opciones incompletas");
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>()?.GetComponent<InputSystemUIInputModule>() == null)
            throw new Exception("Falta entrada UI");
        var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
        if (enabled[0].path != MenuPath || !enabled.Any(s => s.path == "Assets/_Project/Scenes/Nivel1.unity"))
            throw new Exception("Orden de escenas incorrecto");

        // Música nativa de EcoTilt; no se importan bancos ni componentes de Wwise.
        var audio = menu.GetComponent<AudioSource>();
        if (audio == null) audio = menu.gameObject.AddComponent<AudioSource>();
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Music/Musiquita.wav");
        audio.loop = true;
        audio.playOnAwake = true;
        audio.spatialBlend = 0f;
        audio.volume = 0.35f;
        if (audio.clip == null) throw new Exception("Falta música");
        if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null)
            Camera.main.gameObject.AddComponent<AudioListener>();
        // Los objetos globales del middleware quedaron vacíos al retirar sus componentes.
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "WwiseGlobal") UnityEngine.Object.DestroyImmediate(root);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ECOTILT_MENU_VERIFIED: scripts, paneles, botones, opciones, entrada, música y escenas OK.");
    }
}
