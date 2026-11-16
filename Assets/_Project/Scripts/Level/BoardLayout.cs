using UnityEngine;

namespace EcoTilt.Level
{
    /// <summary>
    /// Diseño del tablero: un laberinto dibujado con texto (ver formato en <see cref="MazeGrid"/>)
    /// y sus medidas. Se edita desde el Inspector sin tocar código.
    /// </summary>
    [CreateAssetMenu(menuName = "EcoTilt/Board Layout", fileName = "BoardLayout")]
    public sealed class BoardLayout : ScriptableObject
    {
        public const string DefaultMap =
            "+-+-+-+-+-+-+-+-+\n" +
            "|     |         |\n" +
            "+ + +-+-+ +-+ + +\n" +
            "|S|     |   | | |\n" +
            "+-+ +-+ +-+ +-+ +\n" +
            "|     |O    | | |\n" +
            "+ +-+-+ +-+-+ +-+\n" +
            "| |             |\n" +
            "+-+ + +-+-+ +-+ +\n" +
            "|   | |   | |   |\n" +
            "+ + +-+ +-+ + +-+\n" +
            "| |O    | |   | |\n" +
            "+-+ +-+ + +-+-+ +\n" +
            "|     | |  O     \n" +
            "+ +-+ +-+ + +-+ +\n" +
            "|   |     | |   |\n" +
            "+-+-+-+-+-+-+-+-+";

        [Tooltip("Ancho de cada pasillo, en metros.")]
        [SerializeField, Min(0.5f)] private float cellSize = 1.7f;
        [Tooltip("Espesor de las paredes (barandas), en metros.")]
        [SerializeField, Min(0.05f)] private float wallThickness = 0.25f;
        [Tooltip("Tamaño del pozo respecto al pasillo. El resto queda como borde para esquivarlo.")]
        [SerializeField, Range(0.2f, 0.8f)] private float holeFraction = 0.5f;
        [SerializeField, TextArea(17, 40)] private string map = DefaultMap;

        public float CellSize => cellSize;
        public float WallThickness => wallThickness;
        public float HoleFraction => holeFraction;

        public MazeGrid CreateGrid() => MazeGrid.Parse(map);
    }
}
