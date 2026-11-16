using System;
using UnityEngine;

namespace EcoTilt.Level
{
    /// <summary>
    /// Diseño del tablero como mapa de caracteres. Se edita desde el Inspector sin tocar código.
    /// Fila 0 = extremo lejano (donde está la salida hacia la rampa).
    /// '#' pared · '.' piso · 'O' hueco · 'S' inicio (piso con punto de aparición).
    /// Las celdas de piso de la fila 0 forman la salida hacia la rampa.
    /// </summary>
    [CreateAssetMenu(menuName = "EcoTilt/Board Layout", fileName = "BoardLayout")]
    public sealed class BoardLayout : ScriptableObject
    {
        public const char Wall = '#';
        public const char Floor = '.';
        public const char Hole = 'O';
        public const char Start = 'S';

        public const string DefaultMap =
            "##......##\n" +
            "#........#\n" +
            "#.OO.....#\n" +
            "#.OO..####\n" +
            "#........#\n" +
            "#######..#\n" +
            "#........#\n" +
            "#....O...#\n" +
            "#........#\n" +
            "#..#######\n" +
            "#........#\n" +
            "#....OO..#\n" +
            "#....OO..#\n" +
            "#........#\n" +
            "#......S.#\n" +
            "##########";

        [SerializeField, Min(0.25f)] private float cellSize = 1f;
        [SerializeField, TextArea(16, 32)] private string map = DefaultMap;

        public float CellSize => cellSize;

        public string[] GetRows()
        {
            return map.Replace("\r", string.Empty)
                      .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static int GetColumns(string[] rows)
        {
            int columns = 0;
            foreach (string row in rows) columns = Mathf.Max(columns, row.Length);
            return columns;
        }

        /// <summary>Celda en (columna, fila). Fuera del mapa se considera hueco.</summary>
        public static char GetCell(string[] rows, int column, int row)
        {
            if (row < 0 || row >= rows.Length) return Hole;
            string line = rows[row];
            return column >= 0 && column < line.Length ? line[column] : Hole;
        }

        public static bool IsSolidGround(char cell) => cell == Floor || cell == Start || cell == Wall;
        public static bool IsWalkable(char cell) => cell == Floor || cell == Start;
    }
}
