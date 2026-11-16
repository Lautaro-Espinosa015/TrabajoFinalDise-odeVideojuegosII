using EcoTilt.Gameplay;
using UnityEngine;

namespace EcoTilt.UI
{
    /// <summary>
    /// Indicador simple de vidas y número de mapa (prototipo). En el Hito 3 se reemplaza por la interfaz final.
    /// </summary>
    public sealed class LivesHud : MonoBehaviour
    {
        [SerializeField] private LivesSystem lives;
        [SerializeField] private LevelRegenerator regenerator;

        private GUIStyle _label;
        private GUIStyle _banner;

        private void OnGUI()
        {
            if (lives == null) return;
            float scale = Screen.height / 720f;
            EnsureStyles(scale);

            string hearts = new string('●', lives.Lives) + new string('○', lives.MaxLives - lives.Lives);
            string map = regenerator != null ? $"   Mapa {regenerator.MapNumber}" : string.Empty;
            GUI.Label(new Rect(20f * scale, 16f * scale, 600f * scale, 50f * scale), $"Vidas  {hearts}{map}", _label);

            if (regenerator != null && regenerator.IsRegenerating)
                GUI.Label(new Rect(0f, Screen.height * 0.4f, Screen.width, 80f * scale),
                          "¡Sin vidas! Armando un laberinto nuevo...", _banner);
        }

        private void EnsureStyles(float scale)
        {
            int size = Mathf.RoundToInt(30f * scale);
            if (_label != null && _label.fontSize == size) return;

            _label = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = FontStyle.Bold };
            _label.normal.textColor = new Color(1f, 0.85f, 0.55f);
            _banner = new GUIStyle(_label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(38f * scale) };
            _banner.normal.textColor = Color.white;
        }
    }
}
