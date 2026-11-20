using EcoTilt.Controls;
using EcoTilt.Gameplay;
using EcoTilt.Waste;
using UnityEngine;

namespace EcoTilt.UI
{
    /// <summary>
    /// Botón de pausa y menú simple (prototipo; se reemplaza por la interfaz final en el Hito 3).
    /// El menú recuerda qué va en cada contenedor y permite elegir la respuesta del tablero (Fiel / Suave).
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private PauseController pause;
        [SerializeField] private WasteCategory[] categories = new WasteCategory[0];
        [Tooltip("Componente que implemente ITiltResponseSelector (por ejemplo, TiltInputProvider). " +
                 "Si queda vacío se busca en la escena.")]
        [SerializeField] private MonoBehaviour responseSource;

        private const string MainMenuScene = "Assets/_Project/Scenes/MenuPrincipal.unity";
        private ITiltResponseSelector _response;
        private TiltInputProvider _provider;

        private void Awake()
        {
            _response = responseSource as ITiltResponseSelector;
            if (_response == null)
                foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                    if (behaviour is ITiltResponseSelector selector) { _response = selector; break; }
            _provider = _response as TiltInputProvider;
        }

        private GUIStyle _title;
        private GUIStyle _text;
        private GUIStyle _button;
        private GUIStyle _small;

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

            // Botones en dos columnas para que entren en un teléfono apaisado.
            y += 16f * scale;
            float gap = 16f * scale;
            float wide = Mathf.Min(Screen.width - 32f * scale, 800f * scale);
            float left = (Screen.width - wide) * 0.5f;
            float half = (wide - gap) * 0.5f;
            float height = 70f * scale;
            float step = height + 12f * scale;
            Rect Cell(int column, int row) => new Rect(left + column * (half + gap), y + row * step, half, height);

            if (GUI.Button(Cell(0, 0), "Continuar", _button)) pause.Resume();
            if (GUI.Button(Cell(1, 0), "Reiniciar", _button)) pause.Restart();

            if (_response != null)
            {
                bool faithful = _response.ResponseMode == TiltResponseMode.Fiel;
                if (GUI.Button(Cell(0, 1), faithful ? "Respuesta: Fiel" : "Respuesta: Suave", _button))
                    _response.ResponseMode = faithful ? TiltResponseMode.Suave : TiltResponseMode.Fiel;
            }
            if (GUI.Button(_response != null ? Cell(1, 1) : Cell(0, 1), "Menú principal", _button))
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
                UnityEngine.SceneManagement.SceneManager.LoadScene(MainMenuScene);
            }
            if (GUI.Button(Cell(0, 2), "Salir del juego", _button)) QuitGame();

            string hint = _response == null ? string.Empty
                : _response.ResponseMode == TiltResponseMode.Fiel
                    ? "Fiel: el tablero copia tu teléfono al instante."
                    : "Suave: movimientos más tranquilos y limitados.";
            string sensor = _provider != null ? $"\nControl: {_provider.SensorName}" : string.Empty;
            GUI.Label(new Rect(left + half + gap, y + 2f * step - 6f * scale, half, height + 60f * scale),
                      hint + sensor, _small);
            y += 3f * step;
            GUI.Label(new Rect(left, y, wide, 60f * scale),
                      "Al continuar, sostené el teléfono quieto un momento para calibrar.", _text);
        }

        private static void QuitGame()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void EnsureStyles(float scale)
        {
            int size = Mathf.RoundToInt(28f * scale);
            if (_text != null && _text.fontSize == size) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(56f * scale), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _title.normal.textColor = Color.white;
            _text = new GUIStyle(GUI.skin.label) { fontSize = size, wordWrap = true };
            _text.normal.textColor = Color.white;
            _button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(30f * scale), fontStyle = FontStyle.Bold };
            _small = new GUIStyle(_text) { fontSize = Mathf.RoundToInt(20f * scale) };
            _small.normal.textColor = new Color(0.85f, 0.92f, 0.88f);
        }
    }
}
