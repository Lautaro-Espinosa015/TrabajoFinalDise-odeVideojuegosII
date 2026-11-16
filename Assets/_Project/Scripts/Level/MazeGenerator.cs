using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EcoTilt.Level
{
    [Serializable]
    public sealed class MazeSettings
    {
        [Min(3)] public int Columns = 8;
        [Min(3)] public int Rows = 8;
        [Tooltip("Pozos que se colocan sobre el camino correcto.")]
        [Min(0)] public int Holes = 3;
        [Tooltip("Proporción de paredes internas que se abren de más para crear algún atajo.")]
        [Range(0f, 0.3f)] public float ExtraOpenings = 0.03f;
        [Tooltip("Largo mínimo (en celdas) del camino entre el inicio y la salida.")]
        [Min(1)] public int MinPathLength = 18;
    }

    /// <summary>
    /// Genera laberintos aleatorios en el mismo formato de texto que <see cref="MazeGrid"/>:
    /// inicio en una celda del borde izquierdo (cerrada), salida abierta en el borde derecho
    /// y pozos sobre tramos rectos del camino correcto.
    /// Usa el algoritmo de Prim aleatorio, que da muchos callejones cortos (como un laberinto dibujado a mano).
    /// </summary>
    public static class MazeGenerator
    {
        private const int MaxAttempts = 60;

        public static string Generate(MazeSettings settings, System.Random random)
        {
            string best = null;
            int bestLength = -1;
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                string map = TryGenerate(settings, random, out int pathLength);
                if (pathLength > bestLength)
                {
                    best = map;
                    bestLength = pathLength;
                }
                if (pathLength >= settings.MinPathLength) break;
            }
            return best;
        }

        private static string TryGenerate(MazeSettings s, System.Random random, out int pathLength)
        {
            int cols = s.Columns;
            int rows = s.Rows;
            // h[x, y]: pared horizontal sobre la fila y (y = rows es el borde inferior)
            // v[x, y]: pared vertical a la izquierda de la columna x (x = cols es el borde derecho)
            var h = new bool[cols, rows + 1];
            var v = new bool[cols + 1, rows];
            for (int x = 0; x < cols; x++) for (int y = 0; y <= rows; y++) h[x, y] = true;
            for (int x = 0; x <= cols; x++) for (int y = 0; y < rows; y++) v[x, y] = true;

            CarvePrim(cols, rows, h, v, random);
            OpenExtraWalls(cols, rows, h, v, s.ExtraOpenings, random);

            var start = new Vector2Int(0, random.Next(rows));
            var exit = new Vector2Int(cols - 1, random.Next(rows));
            v[cols, exit.y] = false; // salida en el borde derecho

            List<Vector2Int> path = FindPath(cols, rows, h, v, start, exit);
            pathLength = path.Count;
            HashSet<Vector2Int> holes = PickHoles(path, s.Holes);
            return ToText(cols, rows, h, v, start, holes);
        }

        // ------------------------------------------------------------------ Prim aleatorio
        private static void CarvePrim(int cols, int rows, bool[,] h, bool[,] v, System.Random random)
        {
            var inMaze = new bool[cols, rows];
            var frontier = new List<(Vector2Int cell, Vector2Int dir)>();
            var first = new Vector2Int(random.Next(cols), random.Next(rows));
            inMaze[first.x, first.y] = true;
            AddFrontier(first, cols, rows, frontier);

            while (frontier.Count > 0)
            {
                int index = random.Next(frontier.Count);
                (Vector2Int cell, Vector2Int dir) = frontier[index];
                frontier[index] = frontier[frontier.Count - 1];
                frontier.RemoveAt(frontier.Count - 1);

                Vector2Int next = cell + dir;
                if (inMaze[next.x, next.y]) continue;

                RemoveWall(cell, dir, h, v);
                inMaze[next.x, next.y] = true;
                AddFrontier(next, cols, rows, frontier);
            }
        }

        private static readonly Vector2Int[] Directions =
            { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

        private static void AddFrontier(Vector2Int cell, int cols, int rows, List<(Vector2Int, Vector2Int)> frontier)
        {
            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = cell + dir;
                if (next.x >= 0 && next.y >= 0 && next.x < cols && next.y < rows)
                    frontier.Add((cell, dir));
            }
        }

        private static void RemoveWall(Vector2Int cell, Vector2Int dir, bool[,] h, bool[,] v)
        {
            if (dir.x == 1) v[cell.x + 1, cell.y] = false;
            else if (dir.x == -1) v[cell.x, cell.y] = false;
            else if (dir.y == 1) h[cell.x, cell.y + 1] = false;
            else h[cell.x, cell.y] = false;
        }

        private static bool HasWall(Vector2Int cell, Vector2Int dir, bool[,] h, bool[,] v)
        {
            if (dir.x == 1) return v[cell.x + 1, cell.y];
            if (dir.x == -1) return v[cell.x, cell.y];
            if (dir.y == 1) return h[cell.x, cell.y + 1];
            return h[cell.x, cell.y];
        }

        private static void OpenExtraWalls(int cols, int rows, bool[,] h, bool[,] v, float ratio, System.Random random)
        {
            int interior = (cols - 1) * rows + cols * (rows - 1);
            int count = Mathf.RoundToInt(interior * ratio);
            for (int i = 0; i < count; i++)
            {
                if (random.Next(2) == 0) v[1 + random.Next(cols - 1), random.Next(rows)] = false;
                else h[random.Next(cols), 1 + random.Next(rows - 1)] = false;
            }
        }

        // ------------------------------------------------------------------ camino y pozos
        private static List<Vector2Int> FindPath(int cols, int rows, bool[,] h, bool[,] v, Vector2Int start, Vector2Int goal)
        {
            var previous = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                if (cell == goal) break;
                foreach (Vector2Int dir in Directions)
                {
                    Vector2Int next = cell + dir;
                    if (next.x < 0 || next.y < 0 || next.x >= cols || next.y >= rows) continue;
                    if (previous.ContainsKey(next) || HasWall(cell, dir, h, v)) continue;
                    previous[next] = cell;
                    queue.Enqueue(next);
                }
            }

            var path = new List<Vector2Int>();
            if (!previous.ContainsKey(goal)) return path;
            for (Vector2Int c = goal; c != start; c = previous[c]) path.Add(c);
            path.Add(start);
            path.Reverse();
            return path;
        }

        /// <summary>Pozos repartidos a lo largo del camino, solo en tramos rectos (se pueden esquivar por el borde).</summary>
        private static HashSet<Vector2Int> PickHoles(List<Vector2Int> path, int count)
        {
            var straight = new List<Vector2Int>();
            for (int i = 2; i < path.Count - 2; i++)
                if (path[i] - path[i - 1] == path[i + 1] - path[i])
                    straight.Add(path[i]);

            var holes = new HashSet<Vector2Int>();
            if (count <= 0 || straight.Count == 0) return holes;
            int n = Mathf.Min(count, straight.Count);
            for (int k = 0; k < n; k++)
                holes.Add(straight[Mathf.Min(straight.Count - 1, (int)((k + 0.5f) * straight.Count / n))]);
            return holes;
        }

        // ------------------------------------------------------------------ texto
        private static string ToText(int cols, int rows, bool[,] h, bool[,] v, Vector2Int start, HashSet<Vector2Int> holes)
        {
            var text = new StringBuilder();
            for (int gy = 0; gy <= rows * 2; gy++)
            {
                for (int gx = 0; gx <= cols * 2; gx++)
                {
                    bool evenX = gx % 2 == 0;
                    bool evenY = gy % 2 == 0;
                    char c;
                    if (evenX && evenY) c = MazeGrid.Post;
                    else if (evenY) c = h[gx / 2, gy / 2] ? MazeGrid.HorizontalWall : ' ';
                    else if (evenX) c = v[gx / 2, gy / 2] ? MazeGrid.VerticalWall : ' ';
                    else
                    {
                        var cell = new Vector2Int(gx / 2, gy / 2);
                        c = cell == start ? MazeGrid.Start : holes.Contains(cell) ? MazeGrid.Hole : ' ';
                    }
                    text.Append(c);
                }
                if (gy < rows * 2) text.Append('\n');
            }
            return text.ToString();
        }
    }
}
