using EcoTilt.Gameplay;
using EcoTilt.Waste;
using UnityEngine;

namespace EcoTilt.UI
{
    /// <summary>
    /// Botón de pausa y menú simple (prototipo; se reemplaza por la interfaz final en el Hito 3).
    /// El menú recuerda qué va en cada contenedor.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private PauseController pause;
        [SerializeField] private WasteCategory[] categories = new WasteCategory[0];

        private GUIStyle _title;
        private GUIStyle _text;
        private GUIStyle _button;

        private void OnGUI()
        {
            if (pause == null) return;
            float scale = Screen.height / 720f;
            EnsureStyles(scale);

            if (!pause.IsPaused)
            {
                var corner = GameplayHudLayout.PauseButton(scale);
                if (GUI.Button(corner, "II", _button)) pause.Pause();
                return;
            }

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            float width = 560f * scale;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height * 0.12f;
            GUI.Label(new Rect(x, y, width, 70f * scale), "PAUSA", _title);
            y += 80f * scale;

            foreach (WasteCategory category in categories)
            {
                if (category == null) continue;
                GUI.color = category.Color;
                GUI.DrawTexture(new Rect(x + 40f * scale, y + 8f * scale, 28f * scale, 28f * scale), Texture2D.whiteTexture);
                GUI.color = previous;
                GUI.Label(new Rect(x + 84f * scale, y, width - 84f * scale, 44f * scale),
                          $"{category.DisplayName}: contenedor {category.ColorName}", _text);
                y += 46f * scale;
            }

            y += 20f * scale;
            if (GUI.Button(new Rect(x, y, width, 76f * scale), "Continuar", _button)) pause.Resume();
            y += 90f * scale;
            if (GUI.Button(new Rect(x, y, width, 76f * scale), "Reiniciar", _button)) pause.Restart();
            y += 90f * scale;
            if (GUI.Button(new Rect(x, y, width, 76f * scale), "Menú principal", _button))
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
                UnityEngine.SceneManagement.SceneManager.LoadScene("Assets/_Project/Scenes/MenuPrincipal.unity");
            }
            y += 90f * scale;
            GUI.Label(new Rect(x, y, width, 60f * scale),
                      "Al continuar, sostené el teléfono quieto un momento para calibrar.", _text);
        }

        private void EnsureStyles(float scale)
        {
            int size = Mathf.RoundToInt(28f * scale);
            if (_text != null && _text.fontSize == size) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(56f * scale), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _title.normal.textColor = Color.white;
            _text = new GUIStyle(GUI.skin.label) { fontSize = size, wordWrap = true };
            _text.normal.textColor = Color.white;
            _button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(32f * scale), fontStyle = FontStyle.Bold };
        }
    }
}
