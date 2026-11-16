using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoTilt.EditorTools
{
    /// <summary>
    /// Si la escena del nivel no existe en el proyecto, la genera automáticamente después de compilar
    /// y la abre. Si ya existe, no hace nada: la escena es del equipo y se edita a mano.
    /// </summary>
    [InitializeOnLoad]
    internal static class LevelAutoBuilder
    {
        private const string AttemptKey = "EcoTilt.LevelAutoBuilder.Attempted";

        static LevelAutoBuilder()
        {
            EditorApplication.delayCall += TryBuild;
        }

        private static void TryBuild()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(LevelSceneBuilder.ScenePath) || SessionState.GetBool(AttemptKey, false)) return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryBuild;
                return;
            }

            SessionState.SetBool(AttemptKey, true);
            if (LevelSceneBuilder.Build(false))
                EditorSceneManager.OpenScene(LevelSceneBuilder.ScenePath);
        }
    }
}
