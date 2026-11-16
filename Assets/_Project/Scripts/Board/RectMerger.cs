using System.Collections.Generic;
using UnityEngine;

namespace EcoTilt.Board
{
    /// <summary>
    /// Une celdas contiguas en rectángulos grandes (greedy meshing).
    /// Menos colliders = menos "escalones" invisibles entre cajas cuando un residuo rueda.
    /// </summary>
    public static class RectMerger
    {
        /// <param name="cells">[columna, fila] = true si la celda debe cubrirse.</param>
        public static List<RectInt> Merge(bool[,] cells)
        {
            int columns = cells.GetLength(0);
            int rows = cells.GetLength(1);
            var used = new bool[columns, rows];
            var result = new List<RectInt>();

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if (!cells[column, row] || used[column, row]) continue;

                    int width = 1;
                    while (column + width < columns && cells[column + width, row] && !used[column + width, row])
                        width++;

                    int height = 1;
                    while (row + height < rows && RowIsFree(cells, used, column, row + height, width))
                        height++;

                    for (int dy = 0; dy < height; dy++)
                        for (int dx = 0; dx < width; dx++)
                            used[column + dx, row + dy] = true;

                    result.Add(new RectInt(column, row, width, height));
                }
            }

            return result;
        }

        private static bool RowIsFree(bool[,] cells, bool[,] used, int column, int row, int width)
        {
            for (int i = 0; i < width; i++)
                if (!cells[column + i, row] || used[column + i, row]) return false;
            return true;
        }
    }
}
