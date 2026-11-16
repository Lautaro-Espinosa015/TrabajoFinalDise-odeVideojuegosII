using System.Collections.Generic;
using EcoTilt.Level;
using UnityEngine;

namespace EcoTilt.EditorTools
{
    /// <summary>Medidas del nivel (en metros). Se ajustan acá durante el balanceo.</summary>
    internal sealed class BoardDimensions
    {
        // Física
        public float FloorThickness = 0.3f;
        public float WallHeight = 0.7f;
        public float WallThickness = 0.5f;   // las paredes ocupan media celda, centradas
        // Visual
        public float SlabDepth = 0.9f;       // espesor de la losa flotante bajo el piso
        public float TileGap = 0.06f;
        public float TileHeight = 0.2f;
        public float PlinthHeight = 0.12f;
        public float RailTop = 0.66f;
        public float PostSpacing = 0.2f;
        public float PostSize = 0.05f;
        // Salto final
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
    }

    /// <summary>
    /// Construye el tablero a partir de un BoardLayout. Separa colisiones (cajas simples e invisibles)
    /// de la parte visual (mallas combinadas con baldosas, barandas, rampa y detalles luminosos).
    /// Todo queda como hijo del tablero, así se inclina en conjunto.
    /// </summary>
    internal sealed class BoardGeometryBuilder
    {
        private readonly BoardLayout _layout;
        private readonly BoardDimensions _size;
        private readonly ShrineTheme _theme;
        private readonly MeshBatch _batch;
        private readonly string _meshFolder;

        private string[] _rows;
        private int _columns;
        private float _cell;
        private float _halfWidth;
        private float _halfLength;
        private Transform _colliders;

        public BoardGeometryBuilder(BoardLayout layout, BoardDimensions size, ShrineTheme theme, Mesh cube, string meshFolder)
        {
            _layout = layout;
            _size = size;
            _theme = theme;
            _batch = new MeshBatch(cube);
            _meshFolder = meshFolder;
        }

        public float FarEdgeZ => _halfLength;

        public BoardBuildResult Build(Transform board)
        {
            _rows = _layout.GetRows();
            _columns = BoardLayout.GetColumns(_rows);
            _cell = _layout.CellSize;
            _halfWidth = _columns * _cell * 0.5f;
            _halfLength = _rows.Length * _cell * 0.5f;
            _colliders = Group(board, "Colisiones");

            var result = new BoardBuildResult();
            BuildFloor();
            BuildWalls();
            BuildRamp();
            BuildBinSlots(Group(board, "Contenedores"), result);
            result.SpawnPoint = BuildSpawn(board);

            _batch.Bake(Group(board, "Visual"), _meshFolder, "Tablero");
            return result;
        }

        // ------------------------------------------------------------------ piso
        private void BuildFloor()
        {
            bool[,] solid = Mask(BoardLayout.IsSolidGround);
            foreach (RectInt rect in RectMerger.Merge(solid))
            {
                Vector3 size = new Vector3(rect.width * _cell, _size.FloorThickness, rect.height * _cell);
                Solid("Piso", RectCenter(rect, -_size.FloorThickness * 0.5f), size, Quaternion.identity);

                // Losa flotante oscura debajo de las baldosas (hace de junta y de "roca").
                float slabTop = -0.03f;
                _batch.AddBox(_theme.SlabStone, RectCenter(rect, slabTop - _size.SlabDepth * 0.5f),
                              new Vector3(size.x, _size.SlabDepth, size.z));
            }

            var random = new System.Random(1234);
            float tile = _cell - _size.TileGap;
            for (int row = 0; row < _rows.Length; row++)
                for (int column = 0; column < _columns; column++)
                {
                    if (!solid[column, row]) continue;
                    Material material = _theme.Tiles[random.Next(_theme.Tiles.Length)];
                    float jitter = (float)random.NextDouble() * 0.015f;
                    _batch.AddBox(material, RectCenter(new RectInt(column, row, 1, 1), -_size.TileHeight * 0.5f - jitter),
                                  new Vector3(tile, _size.TileHeight, tile));
                }
        }

        // ------------------------------------------------------------------ paredes (barandas)
        private void BuildWalls()
        {
            bool[,] walls = Mask(c => c == BoardLayout.Wall);
            foreach (RectInt rect in RectMerger.Merge(walls))
            {
                bool alongX = rect.width >= rect.height;
                float length = (alongX ? rect.width : rect.height) * _cell;
                float thickness = (alongX ? rect.height : rect.width) * _cell - (_cell - _size.WallThickness);
                Quaternion rotation = alongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
                Vector3 center = RectCenter(rect, 0f);

                // Las paredes son más finas que la celda: se alargan hasta tocar la pared vecina en las esquinas.
                float extension = (_cell - _size.WallThickness) * 0.5f;
                if (alongX)
                {
                    float left = IsWall(rect.x - 1, rect.y) ? extension : 0f;
                    float right = IsWall(rect.x + rect.width, rect.y) ? extension : 0f;
                    length += left + right;
                    center.x += (right - left) * 0.5f;
                }
                else
                {
                    float far = IsWall(rect.x, rect.y - 1) ? extension : 0f;           // fila menor = +Z
                    float near = IsWall(rect.x, rect.y + rect.height) ? extension : 0f;
                    length += far + near;
                    center.z += (far - near) * 0.5f;
                }

                Solid("Pared", center + Vector3.up * (_size.WallHeight * 0.5f),
                      new Vector3(length, _size.WallHeight, thickness), rotation);
                BuildRailing(center, rotation, length, thickness);
            }
        }

        private void BuildRailing(Vector3 center, Quaternion rotation, float length, float thickness)
        {
            float plinth = _size.PlinthHeight;
            float top = _size.RailTop;

            // Zócalo claro
            AddLocal(_theme.Plinth, center, rotation, new Vector3(0f, plinth * 0.5f - 0.03f, 0f),
                     new Vector3(length, plinth + 0.06f, thickness));
            // Pasamanos
            AddLocal(_theme.Railing, center, rotation, new Vector3(0f, top - 0.04f, 0f),
                     new Vector3(length - 0.1f, 0.08f, 0.14f));
            // Barrotes
            float postHeight = top - 0.08f - plinth;
            float usable = length - 0.4f;
            int posts = Mathf.Max(2, Mathf.RoundToInt(usable / _size.PostSpacing) + 1);
            for (int i = 0; i < posts; i++)
            {
                float x = -usable * 0.5f + usable * i / (posts - 1);
                AddLocal(_theme.Railing, center, rotation, new Vector3(x, plinth + postHeight * 0.5f, 0f),
                         new Vector3(_size.PostSize, postHeight, _size.PostSize));
            }
            // Pilares en los extremos
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (length * 0.5f - 0.13f);
                AddLocal(_theme.DarkStone, center, rotation, new Vector3(x, (top + 0.06f) * 0.5f, 0f),
                         new Vector3(0.24f, top + 0.06f, 0.24f));
            }
        }

        // ------------------------------------------------------------------ rampa
        private void BuildRamp()
        {
            int first = -1;
            int last = -1;
            for (int column = 0; column < _columns; column++)
            {
                if (!BoardLayout.IsWalkable(BoardLayout.GetCell(_rows, column, 0))) continue;
                if (first < 0) first = column;
                last = column;
            }
            if (first < 0)
            {
                Debug.LogWarning("El layout no tiene salida en la fila 0: no se construyó la rampa.");
                return;
            }

            float width = (last - first + 1) * _cell;
            float centerX = (first + last + 1) * 0.5f * _cell - _halfWidth;
            float angle = Mathf.Atan2(_size.RampRise, _size.RampLength);
            float slope = Mathf.Sqrt(_size.RampLength * _size.RampLength + _size.RampRise * _size.RampRise);
            float thickness = _size.RampThickness;

            Vector3 topCenter = new Vector3(centerX, _size.RampRise * 0.5f, _halfLength + _size.RampLength * 0.5f);
            Quaternion rotation = Quaternion.Euler(-angle * Mathf.Rad2Deg, 0f, 0f);
            Vector3 center = topCenter - rotation * Vector3.up * (thickness * 0.5f);

            Solid("Rampa", center, new Vector3(width, thickness, slope), rotation);

            AddLocal(_theme.RampStone, center, rotation, Vector3.zero, new Vector3(width, thickness, slope));
            for (int side = -1; side <= 1; side += 2)
                AddLocal(_theme.Plinth, center, rotation, new Vector3(side * (width * 0.5f + 0.06f), 0.02f, 0f),
                         new Vector3(0.12f, thickness + 0.06f, slope));
            // Franjas luminosas que marcan el salto
            for (int i = 0; i < 3; i++)
            {
                float z = -slope * 0.3f + i * slope * 0.3f;
                AddLocal(_theme.Glow, center, rotation, new Vector3(0f, thickness * 0.5f + 0.004f, z),
                         new Vector3(width * 0.7f, 0.01f, 0.07f));
            }
            AddLocal(_theme.Glow, center, rotation, new Vector3(0f, thickness * 0.5f + 0.004f, slope * 0.5f - 0.05f),
                     new Vector3(width, 0.012f, 0.08f));
        }

        // ------------------------------------------------------------------ contenedores
        private void BuildBinSlots(Transform parent, BoardBuildResult result)
        {
            float zCenter = _halfLength + _size.RampLength + _size.GapLength + _size.BinDepth * 0.5f;
            const int count = 3;
            for (int i = 0; i < count; i++)
            {
                var slot = new GameObject($"Lugar_{i}").transform;
                slot.SetParent(parent, false);
                slot.localPosition = new Vector3((i - 1) * _size.BinSpacing, _size.BinRimY, zCenter);
                result.BinSlots.Add(slot);
            }

            // Largueros que unen los contenedores al tablero
            float outerX = _size.BinSpacing + _size.BinWidth * 0.5f + 0.2f;
            float startZ = _halfLength - 0.5f;
            float endZ = zCenter + _size.BinDepth * 0.5f;
            float length = endZ - startZ;
            for (int side = -1; side <= 1; side += 2)
            {
                _batch.AddBox(_theme.DarkStone, new Vector3(side * outerX, _size.BinRimY - 0.12f, startZ + length * 0.5f),
                              new Vector3(0.24f, 0.24f, length));
                _batch.AddBox(_theme.Glow, new Vector3(side * outerX, _size.BinRimY + 0.005f, startZ + length * 0.5f),
                              new Vector3(0.05f, 0.01f, length - 0.2f));
            }
        }

        // ------------------------------------------------------------------ inicio
        private Transform BuildSpawn(Transform board)
        {
            Vector2Int startCell = new Vector2Int(1, _rows.Length - 2);
            for (int row = 0; row < _rows.Length; row++)
                for (int column = 0; column < _columns; column++)
                    if (BoardLayout.GetCell(_rows, column, row) == BoardLayout.Start)
                        startCell = new Vector2Int(column, row);

            Vector3 floorCenter = RectCenter(new RectInt(startCell.x, startCell.y, 1, 1), 0f);

            // Marco luminoso cuadrado sobre la baldosa de inicio
            const float frame = 0.8f;
            const float line = 0.05f;
            float y = 0.02f;
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

        private bool IsWall(int column, int row)
        {
            return BoardLayout.GetCell(_rows, column, row) == BoardLayout.Wall;
        }

        private bool[,] Mask(System.Func<char, bool> predicate)
        {
            var cells = new bool[_columns, _rows.Length];
            for (int row = 0; row < _rows.Length; row++)
                for (int column = 0; column < _columns; column++)
                    cells[column, row] = predicate(BoardLayout.GetCell(_rows, column, row));
            return cells;
        }

        private Vector3 RectCenter(RectInt rect, float y)
        {
            float x = (rect.x + rect.width * 0.5f) * _cell - _halfWidth;
            float z = _halfLength - (rect.y + rect.height * 0.5f) * _cell;
            return new Vector3(x, y, z);
        }

        private static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }
    }
}
