using System.Collections.Generic;
using EcoTilt.Level;
using UnityEngine;

namespace EcoTilt.EditorTools
{
    /// <summary>Medidas del nivel (en metros). Las del laberinto (pasillo y pared) están en el BoardLayout.</summary>
    internal sealed class BoardDimensions
    {
        // Física
        public float FloorThickness = 0.3f;
        public float WallHeight = 0.7f;
        // Visual
        public float SlabDepth = 0.9f;
        public float TileGap = 0.06f;
        public float TileHeight = 0.2f;
        public float PlinthHeight = 0.12f;
        public float RailTop = 0.66f;
        public float PostSpacing = 0.2f;
        public float PostSize = 0.05f;
        // Explanada de salida y salto
        public float ApronLength = 2.2f;
        public float ApronWidth = 6.4f;
        public float RampLength = 1.6f;
        public float RampRise = 0.4f;
        public float RampThickness = 0.25f;
        public float GapLength = 1.1f;
        // Contenedores
        public float BinWidth = 1.8f;
        public float BinDepth = 1.8f;
        public float BinHeight = 1.0f;
        public float BinWall = 0.08f;
        public float BinSpacing = 2.0f;
        public float BinRimY = -0.6f;
        // Inicio
        public float SpawnHeight = 1.2f;
    }

    internal sealed class BoardBuildResult
    {
        public Transform SpawnPoint;
        public readonly List<Transform> BinSlots = new List<Transform>();
        /// <summary>Área que ocupa todo el nivel (para encuadrar la cámara).</summary>
        public Bounds Footprint;
    }

    /// <summary>
    /// Construye el tablero a partir de un laberinto de paredes finas (BoardLayout / MazeGrid).
    /// Las colisiones son cajas simples e invisibles; lo visual son mallas combinadas
    /// (baldosas, barandas, rampa y detalles luminosos). Todo queda como hijo del tablero.
    /// </summary>
    internal sealed class BoardGeometryBuilder
    {
        private readonly BoardLayout _layout;
        private readonly BoardDimensions _size;
        private readonly ShrineTheme _theme;
        private readonly MeshBatch _batch;
        private readonly string _meshFolder;

        private MazeGrid _grid;
        private float _cell;
        private float _wall;
        private float _width;   // extensión en X
        private float _length;  // extensión en Z
        private Transform _colliders;
        private System.Random _random;

        public BoardGeometryBuilder(BoardLayout layout, BoardDimensions size, ShrineTheme theme, Mesh cube, string meshFolder)
        {
            _layout = layout;
            _size = size;
            _theme = theme;
            _batch = new MeshBatch(cube);
            _meshFolder = meshFolder;
        }

        public BoardBuildResult Build(Transform board)
        {
            _grid = _layout.CreateGrid();
            _cell = _layout.CellSize;
            _wall = _layout.WallThickness;
            _width = _grid.Columns * _cell + (_grid.Columns + 1) * _wall;
            _length = _grid.Rows * _cell + (_grid.Rows + 1) * _wall;
            _colliders = Group(board, "Colisiones");
            _random = new System.Random(1234);

            var result = new BoardBuildResult
            {
                Footprint = new Bounds(Vector3.zero, new Vector3(_width, 1f, _length))
            };

            BuildFloor();
            BuildHoles();
            BuildWalls();
            BuildExit(Group(board, "Contenedores"), result);
            result.SpawnPoint = BuildSpawn(board);

            _batch.Bake(Group(board, "Visual"), _meshFolder, "Tablero");
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
                    solid[gx, gy] = !(gx % 2 == 1 && gy % 2 == 1 && _grid.IsHole(gx / 2, gy / 2));

            foreach (RectInt rect in RectMerger.Merge(solid))
            {
                ElementRect(rect, out Vector3 center, out Vector2 size);
                FloorPiece(center, size, false);
            }

            float tile = _cell + _wall - _size.TileGap;
            for (int y = 0; y < _grid.Rows; y++)
                for (int x = 0; x < _grid.Columns; x++)
                    if (!_grid.IsHole(x, y))
                        Tile(CellCenter(x, y), new Vector2(tile, tile));
        }

        // Pozo más chico que el pasillo: queda un borde de piso alrededor para esquivarlo.
        private void BuildHoles()
        {
            float pit = _cell * _layout.HoleFraction;
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

            // Contenedores (lugares donde se colocan los prefabs)
            float binZ = apron + _size.RampLength + _size.GapLength + _size.BinDepth * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                var slot = new GameObject($"Lugar_{i}").transform;
                slot.SetParent(binsParent, false);
                slot.localPosition = Local(new Vector3((i - 1) * _size.BinSpacing, _size.BinRimY, binZ));
                slot.localRotation = frame;
                result.BinSlots.Add(slot);
            }

            // Largueros que unen los contenedores al tablero
            float outerX = _size.BinSpacing + _size.BinWidth * 0.5f + 0.2f;
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

            float binsHalf = _size.BinSpacing + _size.BinWidth * 0.5f;
            result.Footprint.Encapsulate(Local(new Vector3(-binsHalf, 0f, endZ)));
            result.Footprint.Encapsulate(Local(new Vector3(binsHalf, 0f, endZ)));
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
            glow.color = ShrineTheme.GlowColor;
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
