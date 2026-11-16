using UnityEngine;

namespace EcoTilt.Board
{
    /// <summary>
    /// Materiales del tablero (baldosas, barandas, acentos luminosos) y la malla base de las piezas.
    /// Cambiar el estilo del nivel = asignar otro tema; el código de construcción no cambia.
    /// </summary>
    [CreateAssetMenu(menuName = "EcoTilt/Board Theme", fileName = "BoardTheme")]
    public sealed class BoardTheme : ScriptableObject
    {
        public Material[] Tiles;
        public Material SlabStone;
        public Material Plinth;
        public Material Railing;
        public Material RampStone;
        public Material Glow;
        public Material DarkStone;
        public PhysicsMaterial Surface;
        [Tooltip("Malla de cubo usada para todas las piezas (se combinan en pocas mallas).")]
        public Mesh Cube;
        public Color GlowColor = new Color(1f, 0.52f, 0.14f);
    }
}
