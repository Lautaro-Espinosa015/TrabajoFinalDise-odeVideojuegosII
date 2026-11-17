using UnityEngine;

namespace EcoTilt.Waste
{
    /// <summary>
    /// Categoría de reciclaje (Plástico, Metal, Cartón): nombre, color del contenedor y consejos.
    /// Agregar una categoría nueva = crear otro asset, sin tocar código (OCP).
    /// </summary>
    [CreateAssetMenu(menuName = "EcoTilt/Waste Category", fileName = "Categoria")]
    public sealed class WasteCategory : ScriptableObject
    {
        [SerializeField] private string displayName = "Plástico";
        [Tooltip("Cómo se nombra el color en los mensajes (por ejemplo, \"amarillo\").")]
        [SerializeField] private string colorName = "amarillo";
        [SerializeField] private Color color = Color.yellow;
        [SerializeField, TextArea(2, 4)] private string[] tips = new string[0];

        public string DisplayName => displayName;
        public string ColorName => colorName;
        public Color Color => color;

        public string RandomTip()
        {
            return tips == null || tips.Length == 0 ? string.Empty : tips[Random.Range(0, tips.Length)];
        }

        /// <summary>Lo usa el editor al crear las categorías por defecto.</summary>
        public void Setup(string name, string colorLabel, Color categoryColor, string[] categoryTips)
        {
            displayName = name;
            colorName = colorLabel;
            color = categoryColor;
            tips = categoryTips;
        }
    }
}
