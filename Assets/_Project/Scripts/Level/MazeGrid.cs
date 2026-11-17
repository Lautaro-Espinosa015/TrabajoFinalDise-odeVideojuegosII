using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoTilt.Level
{
    public enum BoardSide { None, Top, Bottom, Left, Right }

    /// <summary>
    /// Laberinto de paredes finas leído desde texto, con el mismo aspecto que un dibujo de laberinto:
    ///
    ///   +-+-+     '+' esquina   '-' pared horizontal   '|' pared vertical
    ///   |S  |     'S' inicio (dentro de una celda)     'O' pozo (dentro de una celda)
    ///   + +-+     Un hueco en el borde exterior es la salida hacia la rampa.
    ///
    /// La grilla de elementos mide (2·columnas+1) × (2·filas+1): índices pares = líneas de pared,
    /// índices impares = pasillos. La fila 0 es el borde lejano (+Z).
    /// </summary>
    public sealed class MazeGrid
    {
        public const char Post = '+';
        public const char HorizontalWall = '-';
        public const char VerticalWall = '|';
        public const char Start = 'S';
        public const char Hole = 'O';

        private readonly char[,] _chars;

        public int Columns { get; }
        public int Rows { get; }
        public int GridWidth => Columns * 2 + 1;
        public int GridHeight => Rows * 2 + 1;
        public Vector2Int StartCell { get; private set; }
        public BoardSide ExitSide { get; private set; }
        /// <summary>Índice de la celda de salida a lo largo de su borde (columna si es arriba/abajo, fila si es izquierda/derecha).</summary>
        public int ExitIndex { get; private set; }

        private MazeGrid(char[,] chars, int columns, int rows)
        {
            _chars = chars;
            Columns = columns;
            Rows = rows;
            FindStart();
            FindExit();
        }

        public static MazeGrid Parse(string text)
        {
            string[] lines = (text ?? string.Empty).Replace("\r", string.Empty)
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            int width = 0;
            foreach (string line in lines) width = Mathf.Max(width, line.Length);
            int columns = Mathf.Max(1, (width - 1) / 2);
            int rows = Mathf.Max(1, (lines.Length - 1) / 2);

            var chars = new char[columns * 2 + 1, rows * 2 + 1];
            for (int gy = 0; gy < rows * 2 + 1; gy++)
                for (int gx = 0; gx < columns * 2 + 1; gx++)
                    chars[gx, gy] = gy < lines.Length && gx < lines[gy].Length ? lines[gy][gx] : ' ';

            return new MazeGrid(chars, columns, rows);
        }

        /// <summary>True si el elemento (gx, gy) es pared. Las esquinas sueltas, sin paredes que lleguen a ellas, no cuentan.</summary>
        public bool IsWall(int gx, int gy)
        {
            char c = Raw(gx, gy);
            if (c == HorizontalWall || c == VerticalWall) return true;
            if (c != Post) return false;
            return IsSegment(gx - 1, gy) || IsSegment(gx + 1, gy) || IsSegment(gx, gy - 1) || IsSegment(gx, gy + 1);
        }

        public bool IsHole(int cellX, int cellY) => Raw(cellX * 2 + 1, cellY * 2 + 1) == Hole;

        private bool IsSegment(int gx, int gy)
        {
            char c = Raw(gx, gy);
            return c == HorizontalWall || c == VerticalWall;
        }

        private char Raw(int gx, int gy)
        {
            if (gx < 0 || gy < 0 || gx >= GridWidth || gy >= GridHeight) return ' ';
            return _chars[gx, gy];
        }

        private void FindStart()
        {
            StartCell = Vector2Int.zero;
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Columns; x++)
                    if (Raw(x * 2 + 1, y * 2 + 1) == Start)
                        StartCell = new Vector2Int(x, y);
        }

        private void FindExit()
        {
            ExitSide = BoardSide.None;
            for (int x = 0; x < Columns && ExitSide == BoardSide.None; x++)
            {
                if (!IsWall(x * 2 + 1, 0)) SetExit(BoardSide.Top, x);
                else if (!IsWall(x * 2 + 1, GridHeight - 1)) SetExit(BoardSide.Bottom, x);
            }
            for (int y = 0; y < Rows && ExitSide == BoardSide.None; y++)
            {
                if (!IsWall(0, y * 2 + 1)) SetExit(BoardSide.Left, y);
                else if (!IsWall(GridWidth - 1, y * 2 + 1)) SetExit(BoardSide.Right, y);
            }
        }

        private void SetExit(BoardSide side, int index)
        {
            ExitSide = side;
            ExitIndex = index;
        }

        // ------------------------------------------------------------------ navegación por celdas
        public static readonly Vector2Int[] Directions =
            { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

        public bool Contains(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < Columns && cell.y < Rows;

        /// <summary>True si desde la celda se puede pasar a la vecina en esa dirección (fila mayor = más cerca de la cámara).</summary>
        public bool IsOpen(Vector2Int cell, Vector2Int dir)
        {
            return Contains(cell + dir) && !IsWall(cell.x * 2 + 1 + dir.x, cell.y * 2 + 1 + dir.y);
        }

        public int OpenSides(Vector2Int cell)
        {
            int open = 0;
            foreach (Vector2Int dir in Directions)
                if (IsOpen(cell, dir)) open++;
            return open;
        }

        /// <summary>Celda del laberinto que da a la salida.</summary>
        public Vector2Int ExitCell
        {
            get
            {
                switch (ExitSide)
                {
                    case BoardSide.Right: return new Vector2Int(Columns - 1, ExitIndex);
                    case BoardSide.Left: return new Vector2Int(0, ExitIndex);
                    case BoardSide.Top: return new Vector2Int(ExitIndex, 0);
                    case BoardSide.Bottom: return new Vector2Int(ExitIndex, Rows - 1);
                    default: return StartCell;
                }
            }
        }

        /// <summary>Camino más corto entre dos celdas (vacío si no hay).</summary>
        public List<Vector2Int> FindPath(Vector2Int from, Vector2Int to)
        {
            var previous = new Dictionary<Vector2Int, Vector2Int> { [from] = from };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                if (cell == to) break;
                foreach (Vector2Int dir in Directions)
                {
                    Vector2Int next = cell + dir;
                    if (!IsOpen(cell, dir) || previous.ContainsKey(next)) continue;
                    previous[next] = cell;
                    queue.Enqueue(next);
                }
            }

            var path = new List<Vector2Int>();
            if (!previous.ContainsKey(to)) return path;
            for (Vector2Int c = to; c != from; c = previous[c]) path.Add(c);
            path.Add(from);
            path.Reverse();
            return path;
        }
    }
}
