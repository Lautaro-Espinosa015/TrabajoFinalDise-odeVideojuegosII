using System.Collections.Generic;
using EcoTilt.Level;
using UnityEngine;

namespace EcoTilt.Board
{
    public sealed class BoardBuildResult
    {
        public Transform SpawnPoint;
        /// <summary>Lugares para contenedores: primero los hundidos dentro del laberinto, después los de la rampa.</summary>
        public readonly List<Transform> BinSlots = new List<Transform>();
        /// <summary>Área que ocupa todo el nivel (para encuadrar la cámara).</summary>
        public Bounds Footprint;
    }

    /// <summary>
    /// Construye la geometría del tablero a partir de un laberinto de paredes finas (MazeGrid).
    /// Funciona igual en el editor y durante el juego (para regenerar el mapa).
    /// Las colisiones son cajas simples e invisibles; lo visual son mallas combinadas
    /// (baldosas, barandas, rampa y detalles luminosos). Todo queda como hijo del tablero.
    /// </summary>
    public sealed class BoardGeometry
    {
        private readonly MazeGrid _grid;
        private readonly float _cell;
        private readonly float _wall;
        private readonly float _holeFraction;
        private readonly BoardDimensions _size;
        private readonly BoardTheme _theme;
        private readonly MeshBatch _batch;
        private readonly IBoardBuildSink _sink;

        private float _width;   // extensión en X
        private float _length;  // extensión en Z
        private Transform _colliders;
        private System.Random _random;
        private readonly System.Random _layoutRandom;
        private readonly int _binsInsideMaze;
        private readonly int _totalBins;
        private readonly HashSet<Vector2Int> _binCells = new HashSet<Vector2Int>();

        /// <param name="binsInsideMaze">Cuántos contenedores van hundidos en callejones del laberinto (el resto, tras la rampa).</param>
        /// <param name="totalBins">Cantidad total de contenedores.</param>
        /// <param name="layoutRandom">Azar para ubicar los contenedores del laberinto.</param>
        public BoardGeometry(MazeGrid grid, BoardLayout layout, BoardDimensions size, BoardTheme theme, IBoardBuildSink sink,
                             int binsInsideMaze, int totalBins, System.Random layoutRandom)
        {
            _binsInsideMaze = Mathf.Clamp(binsInsideMaze, 0, totalBins);
            _totalBins = totalBins;
            _layoutRandom = layoutRandom;
            _grid = grid;
            _cell = layout.CellSize;
            _wall = layout.WallThickness;
            _holeFraction = layout.HoleFraction;
            _size = size;
            _theme = theme;
            _batch = new MeshBatch(theme.Cube);
            _sink = sink;
        }

        public BoardBuildResult Build(Transform board)
        {
            _width = _grid.Columns * _cell + (_grid.Columns + 1) * _wall;
            _length = _grid.Rows * _cell + (_grid.Rows + 1) * _wall;
            _colliders = Group(board, "Colisiones");
            _random = new System.Random(1234);

            var result = new BoardBuildResult
            {
                Footprint = new Bounds(Vector3.zero, new Vector3(_width, 1f, _length))
            };

            ChooseBinCells();
            Transform bins = Group(board, "Contenedores");

            BuildFloor();
            BuildHoles();
            BuildWalls();
            BuildMazeBins(bins, result);
            BuildExit(bins, result);
            result.SpawnPoint = BuildSpawn(board);

            _batch.Bake(Group(board, "Visual"), "Tablero", _sink);
            return result;
        }

        // ------------------------------------------------------------------ piso
        private void BuildFloor()
        {
            int gw = _grid.GridWidth;
            int gh = _grid.GridHeight;
            var solid = new bool[gw, gh];
            for (int gy = 0; gy < gh; gy++)
                for (int gx = 0; gx < gw; gx++)
                    solid[gx, gy] = !(gx % 2 == 1 && gy % 2 == 1 && IsOpenCell(gx / 2, gy / 2));

            foreach (RectInt rect in RectMerger.Merge(solid))
            {
                ElementRect(rect, out Vector3 center, out Vector2 size);
                FloorPiece(center, size, false);
            }

            float tile = _cell + _wall - _size.TileGap;
            for (int y = 0; y < _grid.Rows; y++)
                for (int x = 0; x < _grid.Columns; x++)
                    if (!IsOpenCell(x, y))
                        Tile(CellCenter(x, y), new Vector2(tile, tile));
        }

        /// <summary>Celdas sin piso: pozos y contenedores hundidos.</summary>
        private bool IsOpenCell(int x, int y) => _grid.IsHole(x, y) || _binCells.Contains(new Vector2Int(x, y));

        // ------------------------------------------------------------------ contenedores dentro del laberinto
        /// <summary>
        /// Elige callejones sin salida que no estén en el camino correcto (así no se cae ahí por accidente):
        /// para llegar hay que desviarse a propósito.
        /// </summary>
        private void ChooseBinCells()
        {
            _binCells.Clear();
            if (_binsInsideMaze <= 0) return;

            var path = new HashSet<Vector2Int>(_grid.FindPath(_grid.StartCell, _grid.ExitCell));
            var deadEnds = new List<Vector2Int>();
            var others = new List<Vector2Int>();
            for (int y = 0; y < _grid.Rows; y++)
                for (int x = 0; x < _grid.Columns; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (path.Contains(cell) || _grid.IsHole(x, y) || cell == _grid.StartCell) continue;
                    if (Mathf.Abs(cell.x - _grid.StartCell.x) + Mathf.Abs(cell.y - _grid.StartCell.y) < 3) continue;
                    if (_grid.OpenSides(cell) == 1) deadEnds.Add(cell);
                    else others.Add(cell);
                }

            for (int i = 0; i < _binsInsideMaze; i++)
            {
                List<Vector2Int> pool = deadEnds.Count > 0 ? deadEnds : others;
                if (pool.Count == 0) break;
                int index = _layoutRandom.Next(pool.Count);
                _binCells.Add(pool[index]);
                pool.RemoveAt(index);
            }
        }

        private void BuildMazeBins(Transform parent, BoardBuildResult result)
        {
            float scale = (_cell - 0.08f) / Mathf.Max(_size.BinWidth, _size.BinDepth);
            int index = 0;
            foreach (Vector2Int cell in _binCells)
            {
                Vector3 center = CellCenter(cell.x, cell.y);
                var slot = new GameObject($"Lugar_Laberinto_{index++}").transform;
                slot.SetParent(parent, false);
                slot.localPosition = center + Vector3.up * -0.02f;
                slot.localScale = Vector3.one * scale;
                result.BinSlots.Add(slot);

                // Marco luminoso alrededor del contenedor hundido
                float half = _cell * 0.5f;
                const float line = 0.05f;
                _batch.AddBox(_theme.Glow, center + new Vector3(0f, 0.005f, half - line * 0.5f), new Vector3(_cell, 0.01f, line));
                _batch.AddBox(_theme.Glow, center + new Vector3(0f, 0.005f, -half + line * 0.5f), new Vector3(_cell, 0.01f, line));
                _batch.AddBox(_theme.Glow, center + new Vector3(half - line * 0.5f, 0.005f, 0f), new Vector3(line, 0.01f, _cell));
                _batch.AddBox(_theme.Glow, center + new Vector3(-half + line * 0.5f, 0.005f, 0f), new Vector3(line, 0.01f, _cell));
            }
        }

        // Pozo más chico que el pasillo: queda un borde de piso alrededor para esquivarlo.
        private void BuildHoles()
        {
            float pit = _cell * _holeFraction;
            float margin = (_cell - pit) * 0.5f;
            float offset = (pit + margin) * 0.5f;

            for (int y = 0; y < _grid.Rows; y++)
                for (int x = 0; x < _grid.Columns; x++)
                {
                    if (!_grid.IsHole(x, y)) continue;
                    Vector3 c = CellCenter(x, y);
                    FloorPiece(c + new Vector3(0f, 0f, offset), new Vector2(_cell, margin), true);
                    FloorPiece(c + new Vector3(0f, 0f, -offset), new Vector2(_cell, margin), true);
                    FloorPiece(c + new Vector3(offset, 0f, 0f), new Vector2(margin, pit), true);
                    FloorPiece(c + new Vector3(-offset, 0f, 0f), new Vector2(margin, pit), true);

                    // Borde luminoso del pozo
                    const float line = 0.04f;
                    float half = pit * 0.5f;
                    _batch.AddBox(_theme.Glow, c + new Vector3(0f, 0.005f, half + line * 0.5f), new Vector3(pit + 2f * line, 0.01f, line));
                    _batch.AddBox(_theme.Glow, c + new Vector3(0f, 0.005f, -half - line * 0.5f), new Vector3(pit + 2f * line, 0.01f, line));
                    _batch.AddBox(_theme.Glow, c + new Vector3(half + line * 0.5f, 0.005f, 0f), new Vector3(line, 0.01f, pit));
                    _batch.AddBox(_theme.Glow, c + new Vector3(-half - line * 0.5f, 0.005f, 0f), new Vector3(line, 0.01f, pit));
                }
        }

        private void FloorPiece(Vector3 center, Vector2 size, bool withTile)
        {
            Solid("Piso", center + Vector3.down * (_size.FloorThickness * 0.5f),
                  new Vector3(size.x, _size.FloorThickness, size.y), Quaternion.identity);
            _batch.AddBox(_theme.SlabStone, center + Vector3.down * (0.03f + _size.SlabDepth * 0.5f),
                          new Vector3(size.x, _size.SlabDepth, size.y));
            if (withTile)
                Tile(center, new Vector2(size.x - _size.TileGap, size.y - _size.TileGap));
        }

        private void Tile(Vector3 center, Vector2 size)
        {
            Material material = _theme.Tiles[_random.Next(_theme.Tiles.Length)];
            float sink = (float)_random.NextDouble() * 0.015f;
            _batch.AddBox(material, center + Vector3.down * (_size.TileHeight * 0.5f + sink),
                          new Vector3(size.x, _size.TileHeight, size.y));
        }

        // ------------------------------------------------------------------ paredes (barandas)
        private void BuildWalls()
        {
            int gw = _grid.GridWidth;
            int gh = _grid.GridHeight;
            var walls = new bool[gw, gh];
            for (int gy = 0; gy < gh; gy++)
                for (int gx = 0; gx < gw; gx++)
                    walls[gx, gy] = _grid.IsWall(gx, gy);

            foreach (RectInt rect in RectMerger.Merge(walls))
            {
                ElementRect(rect, out Vector3 center, out Vector2 size);
                bool alongX = size.x >= size.y;
                float length = alongX ? size.x : size.y;
                float thickness = alongX ? size.y : size.x;
                Quaternion rotation = alongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
                Railing(center, rotation, length, thickness);
            }
        }

        /// <summary>Pared con colisión + baranda visual a lo largo del eje X local.</summary>
        private void Railing(Vector3 center, Quaternion rotation, float length, float thickness)
        {
            Solid("Pared", center + Vector3.up * (_size.WallHeight * 0.5f),
                  new Vector3(length, _size.WallHeight, thickness), rotation);

            float plinth = _size.PlinthHeight;
            float top = _size.RailTop;
            AddLocal(_theme.Plinth, center, rotation, new Vector3(0f, plinth * 0.5f - 0.03f, 0f),
                     new Vector3(length, plinth + 0.06f, thickness));
            AddLocal(_theme.Railing, center, rotation, new Vector3(0f, top - 0.04f, 0f),
                     new Vector3(length, 0.08f, Mathf.Min(0.14f, thickness)));

            float postHeight = top - 0.08f - plinth;
            float usable = Mathf.Max(0f, length - 0.4f);
            int posts = Mathf.Max(1, Mathf.RoundToInt(usable / _size.PostSpacing) + 1);
            for (int i = 0; i < posts; i++)
            {
                float x = posts == 1 ? 0f : -usable * 0.5f + usable * i / (posts - 1);
                AddLocal(_theme.Railing, center, rotation, new Vector3(x, plinth + postHeight * 0.5f, 0f),
                         new Vector3(_size.PostSize, postHeight, _size.PostSize));
            }

            float pillar = thickness + 0.07f;
            if (length < pillar * 2.5f)
            {
                AddLocal(_theme.DarkStone, center, rotation, new Vector3(0f, (top + 0.06f) * 0.5f, 0f),
                         new Vector3(Mathf.Min(length, pillar), top + 0.06f, pillar));
                return;
            }
            for (int side = -1; side <= 1; side += 2)
                AddLocal(_theme.DarkStone, center, rotation,
                         new Vector3(side * (length * 0.5f - pillar * 0.5f), (top + 0.06f) * 0.5f, 0f),
                         new Vector3(pillar, top + 0.06f, pillar));
        }

        /// <summary>Muralla recta entre dos puntos (en coordenadas de la salida), con un poco de solape en las uniones.</summary>
        private void FenceSegment(System.Func<Vector3, Vector3> local, Quaternion frame, Vector3 from, Vector3 to,
                                  float thickness, float bottom, float top)
        {
            Vector3 direction = to - from;
            float length = direction.magnitude;
            if (length < 0.05f) return;
            Quaternion rotation = frame * Quaternion.LookRotation(direction / length, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);
            Fence(local((from + to) * 0.5f), rotation, length + thickness, thickness, bottom, top);
        }

        /// <summary>
        /// Muralla calada: colisión maciza de <paramref name="bottom"/> a <paramref name="top"/> (altura local),
        /// pero se ve como zócalo, columnas y baranda superior para no tapar los contenedores.
        /// El largo va sobre el eje X local de <paramref name="rotation"/>.
        /// </summary>
        private void Fence(Vector3 center, Quaternion rotation, float length, float thickness, float bottom, float top)
        {
            float height = top - bottom;
            Solid("Muralla", center + Vector3.up * (bottom + height * 0.5f), new Vector3(length, height, thickness), rotation);

            float plinth = Mathf.Min(0.35f, height * 0.25f);
            AddLocal(_theme.Plinth, center, rotation, new Vector3(0f, bottom + plinth * 0.5f, 0f),
                     new Vector3(length, plinth, thickness));
            AddLocal(_theme.Railing, center, rotation, new Vector3(0f, top - 0.05f, 0f),
                     new Vector3(length, 0.1f, Mathf.Min(0.16f, thickness)));
            AddLocal(_theme.Glow, center, rotation, new Vector3(0f, top + 0.005f, 0f),
                     new Vector3(length - 0.1f, 0.01f, 0.05f));

            float postHeight = height - plinth - 0.1f;
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 0.45f) + 1);
            for (int i = 0; i < posts; i++)
            {
                float x = -length * 0.5f + 0.06f + (length - 0.12f) * i / (posts - 1);
                bool pillar = i == 0 || i == posts - 1;
                float size = pillar ? thickness + 0.06f : _size.PostSize * 1.6f;
                AddLocal(pillar ? _theme.DarkStone : _theme.Railing, center, rotation,
                         new Vector3(x, bottom + plinth + postHeight * 0.5f, 0f),
                         new Vector3(size, postHeight, size));
            }
        }

        // ------------------------------------------------------------------ salida: explanada, rampa y contenedores
        private void BuildExit(Transform binsParent, BoardBuildResult result)
        {
            if (_grid.ExitSide == BoardSide.None)
            {
                Debug.LogWarning("El laberinto no tiene salida en el borde: no se construyó la rampa.");
                return;
            }

            Vector3 forward;
            float edge;
            float lateralHalf;
            Vector3 opening;
            switch (_grid.ExitSide)
            {
                case BoardSide.Right:
                    forward = Vector3.right; edge = _width * 0.5f; lateralHalf = _length * 0.5f;
                    opening = CellCenter(_grid.Columns - 1, _grid.ExitIndex); break;
                case BoardSide.Left:
                    forward = Vector3.left; edge = _width * 0.5f; lateralHalf = _length * 0.5f;
                    opening = CellCenter(0, _grid.ExitIndex); break;
                case BoardSide.Top:
                    forward = Vector3.forward; edge = _length * 0.5f; lateralHalf = _width * 0.5f;
                    opening = CellCenter(_grid.ExitIndex, 0); break;
                default:
                    forward = Vector3.back; edge = _length * 0.5f; lateralHalf = _width * 0.5f;
                    opening = CellCenter(_grid.ExitIndex, _grid.Rows - 1); break;
            }

            Quaternion frame = Quaternion.LookRotation(forward, Vector3.up);
            Vector3 right = frame * Vector3.right;
            float limit = Mathf.Max(0f, lateralHalf - _size.ApronWidth * 0.5f);
            float lateral = Mathf.Clamp(Vector3.Dot(opening, right), -limit, limit);
            Vector3 origin = forward * edge + right * lateral;
            Vector3 Local(Vector3 p) => origin + frame * p;

            // Explanada: piso fuera del laberinto donde el residuo se acomoda antes de saltar
            float apron = _size.ApronLength;
            float apronWidth = _size.ApronWidth;
            Solid("Explanada", Local(new Vector3(0f, -_size.FloorThickness * 0.5f, apron * 0.5f)),
                  new Vector3(apronWidth, _size.FloorThickness, apron), frame);
            AddLocal(_theme.SlabStone, Local(new Vector3(0f, -0.03f - _size.SlabDepth * 0.5f, apron * 0.5f)), frame,
                     Vector3.zero, new Vector3(apronWidth, _size.SlabDepth, apron));
            int tilesX = Mathf.Max(1, Mathf.RoundToInt(apronWidth / (_cell * 0.6f)));
            int tilesZ = Mathf.Max(1, Mathf.RoundToInt(apron / (_cell * 0.6f)));
            float tw = apronWidth / tilesX;
            float tz = apron / tilesZ;
            for (int i = 0; i < tilesX; i++)
                for (int k = 0; k < tilesZ; k++)
                {
                    Vector3 p = Local(new Vector3(-apronWidth * 0.5f + tw * (i + 0.5f), 0f, tz * (k + 0.5f)));
                    Material material = _theme.Tiles[_random.Next(_theme.Tiles.Length)];
                    AddLocal(material, p, frame, Vector3.down * (_size.TileHeight * 0.5f),
                             new Vector3(tw - _size.TileGap, _size.TileHeight, tz - _size.TileGap));
                }
            // Barandas laterales de la explanada
            Quaternion alongForward = frame * Quaternion.Euler(0f, 90f, 0f);
            for (int side = -1; side <= 1; side += 2)
                Railing(Local(new Vector3(side * (apronWidth * 0.5f - _wall * 0.5f), 0f, apron * 0.5f)),
                        alongForward, apron, _wall);

            // Rampa
            float angle = Mathf.Atan2(_size.RampRise, _size.RampLength);
            float slope = Mathf.Sqrt(_size.RampLength * _size.RampLength + _size.RampRise * _size.RampRise);
            float thick = _size.RampThickness;
            float rampWidth = apronWidth - 2f * _wall;
            Quaternion rampRotation = frame * Quaternion.Euler(-angle * Mathf.Rad2Deg, 0f, 0f);
            Vector3 rampCenter = Local(new Vector3(0f, _size.RampRise * 0.5f, apron + _size.RampLength * 0.5f))
                                 - rampRotation * Vector3.up * (thick * 0.5f);
            Solid("Rampa", rampCenter, new Vector3(rampWidth, thick, slope), rampRotation);
            AddLocal(_theme.RampStone, rampCenter, rampRotation, Vector3.zero, new Vector3(rampWidth, thick, slope));
            for (int side = -1; side <= 1; side += 2)
                AddLocal(_theme.Plinth, rampCenter, rampRotation, new Vector3(side * (rampWidth * 0.5f + 0.06f), 0.02f, 0f),
                         new Vector3(0.12f, thick + 0.06f, slope));
            for (int i = 0; i < 3; i++)
                AddLocal(_theme.Glow, rampCenter, rampRotation,
                         new Vector3(0f, thick * 0.5f + 0.004f, -slope * 0.3f + i * slope * 0.3f),
                         new Vector3(rampWidth * 0.7f, 0.01f, 0.07f));
            AddLocal(_theme.Glow, rampCenter, rampRotation, new Vector3(0f, thick * 0.5f + 0.004f, slope * 0.5f - 0.05f),
                     new Vector3(rampWidth, 0.012f, 0.08f));

            // Barandas de la rampa: continúan las de la explanada para que el residuo no se caiga por los costados.
            Quaternion alongRamp = rampRotation * Quaternion.Euler(0f, 90f, 0f);
            for (int side = -1; side <= 1; side += 2)
                Railing(rampCenter + rampRotation * new Vector3(side * (rampWidth * 0.5f + _wall * 0.5f), thick * 0.5f, 0f),
                        alongRamp, slope, _wall);

            // Contenedores (lugares donde se colocan los prefabs)
            float binZ = apron + _size.RampLength + _size.GapLength + _size.BinDepth * 0.5f;
            int exitBins = Mathf.Max(1, _totalBins - _binCells.Count);
            for (int i = 0; i < exitBins; i++)
            {
                var slot = new GameObject($"Lugar_Rampa_{i}").transform;
                slot.SetParent(binsParent, false);
                slot.localPosition = Local(new Vector3((i - (exitBins - 1) * 0.5f) * _size.BinSpacing, _size.BinRimY, binZ));
                slot.localRotation = frame;
                result.BinSlots.Add(slot);
            }

            // Largueros que unen los contenedores al tablero
            float outerX = (exitBins - 1) * 0.5f * _size.BinSpacing + _size.BinWidth * 0.5f + 0.2f;
            float startZ = apron - 0.3f;
            float endZ = binZ + _size.BinDepth * 0.5f;
            float beam = endZ - startZ;
            for (int side = -1; side <= 1; side += 2)
            {
                AddLocal(_theme.DarkStone, Local(new Vector3(side * outerX, _size.BinRimY - 0.12f, startZ + beam * 0.5f)),
                         frame, Vector3.zero, new Vector3(0.24f, 0.24f, beam));
                AddLocal(_theme.Glow, Local(new Vector3(side * outerX, _size.BinRimY + 0.005f, startZ + beam * 0.5f)),
                         frame, Vector3.zero, new Vector3(0.05f, 0.01f, beam - 0.2f));
            }

            // Muralla alrededor de los contenedores: costados y fondo. El salto sigue siendo necesario
            // (el vacío entre la rampa y los contenedores queda abierto), pero lo que se pasa de largo
            // o sale torcido rebota y cae en un contenedor en vez de perderse.
            float t = _size.EnclosureThickness;
            float bottom = _size.BinRimY - _size.BinHeight;
            float top = _size.RampRise + _size.EnclosureHeight;
            float rampSide = rampWidth * 0.5f + _wall + t * 0.5f;   // por fuera de la baranda de la rampa
            float binSide = outerX + t * 0.5f;                      // por fuera de los contenedores
            float rampEnd = apron + _size.RampLength;
            float binFront = binZ - _size.BinDepth * 0.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                // Embudo sobre el vacío: si la rampa es más ancha que los contenedores, desvía hacia adentro.
                Vector3 a = new Vector3(side * rampSide, 0f, rampEnd - t);
                Vector3 b = new Vector3(side * binSide, 0f, binFront);
                FenceSegment(Local, frame, a, b, t, bottom, top);
                FenceSegment(Local, frame, b, new Vector3(side * binSide, 0f, endZ + t), t, bottom, top);
            }
            Fence(Local(new Vector3(0f, 0f, endZ + t * 0.5f)), frame, 2f * binSide + t, t, bottom, top);

            float binsHalf = Mathf.Max(_size.BinSpacing + _size.BinWidth * 0.5f, Mathf.Max(binSide, rampSide) + t);
            result.Footprint.Encapsulate(Local(new Vector3(-binsHalf, 0f, endZ + t)));
            result.Footprint.Encapsulate(Local(new Vector3(binsHalf, 0f, endZ + t)));
        }

        // ------------------------------------------------------------------ inicio
        private Transform BuildSpawn(Transform board)
        {
            Vector3 floorCenter = CellCenter(_grid.StartCell.x, _grid.StartCell.y);

            float frame = _cell * 0.7f;
            const float line = 0.05f;
            const float y = 0.02f;
            _batch.AddBox(_theme.Glow, floorCenter + new Vector3(0f, y, frame * 0.5f), new Vector3(frame, 0.01f, line));
            _batch.AddBox(_theme.Glow, floorCenter + new Vector3(0f, y, -frame * 0.5f), new Vector3(frame, 0.01f, line));
            _batch.AddBox(_theme.Glow, floorCenter + new Vector3(frame * 0.5f, y, 0f), new Vector3(line, 0.01f, frame));
            _batch.AddBox(_theme.Glow, floorCenter + new Vector3(-frame * 0.5f, y, 0f), new Vector3(line, 0.01f, frame));

            var lightGo = new GameObject("LuzInicio");
            lightGo.transform.SetParent(board, false);
            lightGo.transform.localPosition = floorCenter + Vector3.up * 0.6f;
            var glow = lightGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = _theme.GlowColor;
            glow.range = 3f;
            glow.intensity = 2f;

            var spawn = new GameObject("PuntoDeInicio").transform;
            spawn.SetParent(board, false);
            spawn.localPosition = floorCenter + Vector3.up * _size.SpawnHeight;
            return spawn;
        }

        // ------------------------------------------------------------------ coordenadas de la grilla
        private float ElementStartX(int gx) => -_width * 0.5f + ((gx + 1) / 2) * _wall + (gx / 2) * _cell;
        private float ElementTopZ(int gy) => _length * 0.5f - ((gy + 1) / 2) * _wall - (gy / 2) * _cell;
        private float ElementSize(int g) => g % 2 == 0 ? _wall : _cell;

        private void ElementRect(RectInt rect, out Vector3 center, out Vector2 size)
        {
            float x0 = ElementStartX(rect.x);
            float x1 = ElementStartX(rect.xMax - 1) + ElementSize(rect.xMax - 1);
            float z0 = ElementTopZ(rect.y);
            float z1 = ElementTopZ(rect.yMax - 1) - ElementSize(rect.yMax - 1);
            center = new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f);
            size = new Vector2(x1 - x0, z0 - z1);
        }

        private Vector3 CellCenter(int cellX, int cellY)
        {
            ElementRect(new RectInt(cellX * 2 + 1, cellY * 2 + 1, 1, 1), out Vector3 center, out _);
            return center;
        }

        // ------------------------------------------------------------------ utilidades
        private void AddLocal(Material material, Vector3 center, Quaternion rotation, Vector3 localOffset, Vector3 size)
        {
            _batch.AddBox(material, center + rotation * localOffset, size, rotation);
        }

        private void Solid(string name, Vector3 center, Vector3 size, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_colliders, false);
            go.transform.localPosition = center;
            go.transform.localRotation = rotation;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.sharedMaterial = _theme.Surface;
        }

        private static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }
    }
}
