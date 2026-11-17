using EcoTilt.Gameplay;
using EcoTilt.Waste;
using UnityEngine;

namespace EcoTilt.UI
{
    /// <summary>
    /// Mensajes al separar un residuo: felicita con un consejo (y avisa si devolvió una vida) o recuerda
    /// dónde va. También felicita al completar el nivel. (Prototipo; interfaz final en el Hito 3.)
    /// </summary>
    public sealed class FeedbackBanner : MonoBehaviour
    {
        [SerializeField] private LevelProgress progress;
        [SerializeField, Min(1f)] private float duration = 4f;
        [SerializeField, Min(1f)] private float levelDuration = 5f;

        private static readonly Color Good = new Color(0.15f, 0.55f, 0.25f, 0.92f);
        private static readonly Color Bad = new Color(0.75f, 0.35f, 0.05f, 0.92f);
        private static readonly Color LevelColor = new Color(0.10f, 0.35f, 0.75f, 0.95f);

        private string _title;
        private string _detail;
        private Color _color;
        private float _shownAt = -999f;
        private float _showFor;
        private GUIStyle _titleStyle;
        private GUIStyle _detailStyle;

        private void OnEnable()
        {
            if (progress == null) return;
            progress.ItemSorted += OnSorted;
            progress.LevelCompleted += OnLevelCompleted;
        }

        private void OnDisable()
        {
            if (progress == null) return;
            progress.ItemSorted -= OnSorted;
            progress.LevelCompleted -= OnLevelCompleted;
        }

        private void OnSorted(WasteSortResult result, bool lifeGained)
        {
            WasteCategory category = result.Item.Category;
            string item = result.Item.DisplayName;
            if (result.Correct)
            {
                string detail = category.RandomTip();
                if (lifeGained) detail = "+1 vida   ·   " + detail;
                Show($"¡Muy bien! {item} va en {category.DisplayName}.", detail, Good, duration);
            }
            else
            {
                Show($"¡Casi! {item} no va en {result.ReceivedBy.Category.DisplayName}.",
                     $"Va en el contenedor de {category.DisplayName} ({category.ColorName}).", Bad, duration);
            }
        }

        private void OnLevelCompleted(int level, int correct, int total)
        {
            string praise = correct == total ? "¡Perfecto! Separaste todo bien." : $"Separaste bien {correct} de {total}.";
            Show($"¡Nivel {level} completado!", $"{praise} Vidas recargadas: ¡vamos al próximo laberinto!", LevelColor, levelDuration);
        }

        private void Show(string title, string detail, Color color, float time)
        {
            _title = title;
            _detail = detail;
            _color = color;
            _showFor = time;
            _shownAt = Time.unscaledTime;
        }

        private void OnGUI()
        {
            if (Time.unscaledTime - _shownAt > _showFor || string.IsNullOrEmpty(_title)) return;
            float scale = Screen.height / 720f;
            EnsureStyles(scale);

            var panel = new Rect(Screen.width * 0.5f - 400f * scale, 120f * scale, 800f * scale, 120f * scale);
            Color previous = GUI.color;
            GUI.color = _color;
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(new Rect(panel.x + 16f * scale, panel.y + 8f * scale, panel.width - 32f * scale, 50f * scale), _title, _titleStyle);
            GUI.Label(new Rect(panel.x + 16f * scale, panel.y + 56f * scale, panel.width - 32f * scale, 60f * scale), _detail, _detailStyle);
        }

        private void EnsureStyles(float scale)
        {
            int size = Mathf.RoundToInt(34f * scale);
            if (_titleStyle != null && _titleStyle.fontSize == size) return;
            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _titleStyle.normal.textColor = Color.white;
            _detailStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(24f * scale), alignment = TextAnchor.UpperCenter, wordWrap = true };
            _detailStyle.normal.textColor = Color.white;
        }
    }
}
