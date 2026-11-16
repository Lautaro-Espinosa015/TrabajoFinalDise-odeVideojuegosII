using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoTilt.EditorTools
{
    /// <summary>
    /// Si la escena del nivel no existe o quedó desactualizada (LevelSceneBuilder.LevelVersion), la genera
    /// automáticamente después de compilar y la abre. Si está al día no hace nada: la escena se edita a mano.
    /// </summary>
    [InitializeOnLoad]
    internal static class LevelAutoBuilder
    {
        private static readonly string AttemptKey = "EcoTilt.LevelAutoBuilder.Attempted.v" + LevelSceneBuilder.LevelVersion;

        static LevelAutoBuilder()
        {
            EditorApplication.delayCall += TryBuild;
        }

        private static void TryBuild()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!LevelSceneBuilder.IsOutdated() || SessionState.GetBool(AttemptKey, false)) return;

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
