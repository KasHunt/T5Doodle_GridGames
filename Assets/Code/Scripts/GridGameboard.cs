using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Code.Scripts
{
    [ExecuteInEditMode]
    public class GridGameboard : MonoBehaviour
    {
        public enum PlayerColor
        {
            Black,
            White,
            None,
        }

        public static PlayerColor OppositeColor(PlayerColor color) => color switch
        {
            PlayerColor.Black => PlayerColor.White,
            PlayerColor.White => PlayerColor.Black,
            _ => PlayerColor.None,
        };

        public class Bounds
        {
            public int LowRow;
            public int HighRow;
            public int LowColumn;
            public int HighColumn;

            public Bounds(int lowRow, int highRow, int lowColumn, int highColumn)
            {
                LowRow = lowRow;
                HighRow = highRow;
                LowColumn = lowColumn;
                HighColumn = highColumn;
            }

            public bool ContainsPosition(Position position) => 
                (position.Row >= LowRow && position.Row < HighRow) && 
                (position.Column >= LowColumn && position.Column < HighColumn);
        }
        
        public class Position
        {
            public readonly int Row;
            public readonly int Column;

            public Position(int row, int column)
            {
                Row = row;
                Column = column;
            }

            protected Position((int row, int column) rowAndColumn)
            {
                Row = rowAndColumn.row;
                Column = rowAndColumn.column;
            }

            public static Position operator +(Position a, Position b)
                => new(a.Row + b.Row, a.Column + b.Column);
            
            public static Position operator -(Position a, Position b)
                => new(a.Row - b.Row, a.Column - b.Column);

            public static Position operator *(Position a, int multiplier)
                => new(a.Row * multiplier, a.Column * multiplier);
            
            public static Position operator /(Position a, int divisor)
                => new(a.Row / divisor, a.Column / divisor);
            
            public int ChebyshevDistance(Position to) => Math.Max(Math.Abs(to.Row - Row), Math.Abs(to.Column - Column));
            
            public float LinearDistance(Position to) => 
                Mathf.Sqrt((to.Row - Row) * (to.Row - Row) + (to.Column - Column) * (to.Column - Column));

            public static bool operator ==(Position a, Position b) => Equals(a, b);

            public static bool operator !=(Position a, Position b) => !(a == b);
            
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.GetType() == this.GetType() && Equals((Position)obj);
            }

            public override int GetHashCode() => HashCode.Combine(Row, Column);

            private bool Equals(Position other) => Row == other.Row && Column == other.Column;

            public override string ToString() => $"{Row}×{Column}";

            public bool IsOdd => ((Row + Column) % 2) == 1;

            public bool IsInBounds(Bounds bounds) => bounds.ContainsPosition(this);
            
            public static IEnumerable<Position> GetRange(int rowStart, int rowCount, int colStart, int colCount)
            {
                return Enumerable
                    .Range(rowStart, rowCount)
                    .SelectMany(row => Enumerable
                        .Range(colStart, colCount)
                        .Select(column => new Position(row, column)));
            }
            
            public IEnumerable<Position> IterateToBounds(Position offset, Bounds bounds)
            {
                var positions = new List<Position>();
                
                var candidate = this + offset;
                while (bounds.ContainsPosition(candidate))
                {
                    positions.Add(candidate);
                    candidate += offset;
                }

                return positions;
            }

            public static IEnumerable<Position> OrthogonalNeighbourOffsets { get; } = new[]
            {
                new Position(-1, 0),
                new Position(0, -1),
                new Position(1, 0),
                new Position(0, 1)
            };
            
            public static IEnumerable<Position> DiagonalNeighbourOffsets { get; }= new[]
            {
                new Position(-1, -1),
                new Position(1, -1),
                new Position(-1, 1),
                new Position(1, 1)
            };

            public static IEnumerable<Position> EightConnectedNeighborOffsets { get; } =
                OrthogonalNeighbourOffsets.Concat(DiagonalNeighbourOffsets).ToArray();
        }
        
        // ReSharper disable CompareOfFloatsByEqualityOperator
        [Header("Board")]
        
        [SerializeField][Range(1, 48)]
        private int rows = 8;
        public int Rows
        {
            set => _rebuild = (rows = value) == value;
            get => rows;
        }
        
        [SerializeField][Range(1, 48)]
        private int columns = 8;
        public int Columns
        {
            set => _rebuild = (columns = value) == value;
            get => columns;
        }
        
        [SerializeField][Min(0.01f)]
        private float thickness = 0.1f;
        public float Thickness
        {
            set => _rebuild = (thickness = value) == value;
            get => thickness;
        }
        
        [Header("Inner Borders")]
        
        [SerializeField][Range(0, 1f)]
        private float borderThickness = 0.05f;
        public float BorderThickness
        {
            set => _rebuild = (borderThickness = value) == value;
            get => borderThickness;
        }
        
        [SerializeField][Range(0, 2f)]
        private float borderHeightFactor = 1f;
        public float BorderHeightFactor
        {
            set => _rebuild = (borderHeightFactor = value) == value;
            get => borderHeightFactor;
        }
        
        [Header("Outer Border")]
        
        [SerializeField][Range(0, 1f)]
        private float outerBorderThickness = 0.05f;
        public float OuterBorderThickness
        {
            set => _rebuild = (outerBorderThickness = value) == value;
            get => outerBorderThickness;
        }
        
        [SerializeField][Range(0, 2f)]
        private float outerBorderHeightFactor = 1f;
        public float OuterBorderHeightFactor
        {
            set => _rebuild = (outerBorderHeightFactor = value) == value;
            get => outerBorderHeightFactor;
        }
        
        [Header("Materials")]
        
        [SerializeField]
        private Material evenTileMaterial;
        public Material EvenTileMaterial
        {
            set => _rebuild = (evenTileMaterial = value) == value;
            get => evenTileMaterial;
        }
        
        [SerializeField]
        private Material oddTileMaterial;
        public Material OddTileMaterial
        {
            set => _rebuild = (oddTileMaterial = value) == value;
            get => oddTileMaterial;
        }
        
        [SerializeField]
        private Material borderMaterial;
        public Material BorderTileMaterial
        {
            set => _rebuild = (borderMaterial = value) == value;
            get => borderMaterial;
        }
        
        [SerializeField]
        private Material outerBorderMaterial;
        public Material OuterBorderTileMaterial
        {
            set => _rebuild = (outerBorderMaterial = value) == value;
            get => outerBorderMaterial;
        }
        // ReSharper restore CompareOfFloatsByEqualityOperator

        // Private variables
        private readonly List<GameObject> _tiles = new();
        private readonly List<Renderer> _tileRenderers = new();
        private bool _rebuild;
        private Material _defaultMaterial;
        private Vector3 _tileScale;
        private Vector3 _tileOffset;
        private GameObject _outerBorderContainer;

        private void OnValidate()
        {
            _rebuild = true;
        }

        private Material GetMaterialForTile(Position position)
        {
            var material = (position.IsOdd ? oddTileMaterial : evenTileMaterial);
            return material ? material : _defaultMaterial;
        }

        private void GetDefaultMaterial()
        {
            var shader = Shader.Find("Standard");
            _defaultMaterial = new Material(shader);   
        }
        
        private (int row, int column) IndexToRowColumn(int index)
        {
            var row = index / columns;
            var column = index % columns;
            return (row, column);
        }

        private int RowColumnToIndex(Position position) => position.Row * columns + position.Column;
        
        private void Awake()
        {
            GetDefaultMaterial();
            Rebuild();
        }
        
        private void Update()
        {
            if (!_rebuild) return;
            Rebuild();
        }

        public void Rebuild()
        {
            ClearTiles();
            RecomputeTileGeometry();
            BuildTiles();
            _rebuild = false;
        }
        
        private void ClearTiles()
        {
            foreach (var tile in _tiles)
            {
#if UNITY_EDITOR
                DestroyImmediate(tile);
#else
                Destroy(tile);
#endif
            }
            _tiles.Clear();
            _tileRenderers.Clear();

            if (_outerBorderContainer)
            {
#if UNITY_EDITOR
                DestroyImmediate(_outerBorderContainer);
#else
                Destroy(_outerBorderContainer);
#endif
            }
        }

        private void RecomputeTileGeometry()
        {
            var localScale = transform.localScale;
            _tileScale = Vector3.Scale(new Vector3(1, thickness, 1), localScale);
            var newOffsetX = -(rows - 1f) / 2f * (_tileScale.x + 2 * borderThickness);
            var newOffsetZ = -(columns - 1f) / 2f * (_tileScale.z + 2 * borderThickness);
            _tileOffset = Vector3.Scale(new Vector3(newOffsetX, 0, newOffsetZ), localScale);
        }
        
        private Vector3 GetTilePosition(float row, float column, float y)
        {
            var localScale = transform.localScale;
            return Vector3.Scale(_tileOffset, localScale) + Vector3.Scale(new Vector3(
                row * (_tileScale.x + 2 * borderThickness), 
                y, 
                column * (_tileScale.z + 2 * borderThickness)
            ),localScale);
        }
        
        private void BuildTiles()
        {
            var tileY = (thickness / 2);
            var borderY = (borderHeightFactor - 1) / 2;
            
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var tile = BuildTile(new Position(row, column), tileY, out var tileRenderer);
                    BuildTileBorders(borderY, tile, tileRenderer);
                }
            }

            BuildOuterBorders();
        }

        private GameObject BuildTile(Position position, float tileY, out Renderer tileRenderer)
        {
            // Calculate new tile position
            var newTilePosition = GetTilePosition(position.Row, position.Column, tileY);

            // Create the new tile
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.transform.localScale = _tileScale;
            tile.transform.position = newTilePosition;
            tile.transform.SetParent(transform);
            tile.name = $"Tile ({position.Row}x{position.Column})";
            _tiles.Add(tile);

            // Set the material
            tileRenderer = tile.GetComponent<Renderer>();
            if (tileRenderer)
            {
                tileRenderer.sharedMaterial = GetMaterialForTile(position);
            }

            _tileRenderers.Add(tileRenderer);
            return tile;
        }

        private void BuildTileBorders(float borderY, GameObject tile, Renderer tileRenderer)
        {
            // Move to the next tile if borders are disabled
            if (borderThickness == 0 || borderHeightFactor == 0) return;

            // Create the borders
            Vector3[] borderPositions =
            {
                new(0, borderY, _tileScale.z / 2 + borderThickness / 2), // Top
                new(0, borderY, -_tileScale.z / 2 - borderThickness / 2), // Bottom
                new(_tileScale.x / 2 + borderThickness / 2, borderY, 0), // Right
                new(-_tileScale.x / 2 - borderThickness / 2, borderY, 0) // Left
            };

            foreach (var pos in borderPositions)
            {
                var borderScale = new Vector3(borderThickness, _tileScale.y + (_tileScale.y * borderY * 2), borderThickness);

                // If it's the top or bottom border, extend it
                if (pos.z != 0)
                {
                    borderScale.x = _tileScale.x + 2 * borderThickness;
                }

                // If it's the left or right border, extend it
                if (pos.x != 0)
                {
                    borderScale.z = _tileScale.z;
                }

                // Create the border primitive
                var border = GameObject.CreatePrimitive(PrimitiveType.Cube);
                border.transform.localScale = borderScale;
                border.transform.SetParent(tile.transform);
                border.transform.localPosition = pos;

                // Set the border material
                var borderRenderer = border.GetComponent<Renderer>();
                if (tileRenderer)
                {
                    borderRenderer.sharedMaterial = borderMaterial ? borderMaterial : _defaultMaterial;
                }
            }
        }

        private void BuildOuterBorders()
        {
            if (outerBorderThickness == 0 || outerBorderHeightFactor == 0) return;

            // Creat ethe outer borders
            _outerBorderContainer = new GameObject("Borders");
            _outerBorderContainer.transform.SetParent(transform);

            // Calculate outer border dimensions
            var outerBorderLengthX = rows * _tileScale.x + (rows - 1) * (borderThickness * 2) + (borderThickness * 2);
            var outerBorderLengthZ = columns * _tileScale.z + (columns - 1) * (borderThickness * 2) + (borderThickness * 2);

            var outerBorderHeight = (outerBorderHeightFactor * thickness);
            var outerBorderPosition = outerBorderHeight / 2;

            // Calculate positions for the outer borders
            Vector3[] outerBorderPositions =
            {
                new(0, outerBorderPosition, (outerBorderLengthZ + outerBorderThickness) / 2), // Top
                new(0, outerBorderPosition, -(outerBorderLengthZ + outerBorderThickness) / 2), // Bottom
                new((outerBorderLengthX + outerBorderThickness) / 2, outerBorderPosition, 0), // Right
                new(-(outerBorderLengthX + outerBorderThickness) / 2, outerBorderPosition, 0) // Left
            };

            // Create the outer borders
            foreach (var pos in outerBorderPositions)
            {
                var outerBorderScale = new Vector3(outerBorderThickness, outerBorderHeight, outerBorderThickness);

                // If it's the top or bottom border, extend it
                if (Math.Abs(pos.z) > 0)
                {
                    outerBorderScale.x = outerBorderLengthX + 2 * outerBorderThickness;
                }

                // If it's the left or right border, extend it
                if (Math.Abs(pos.x) > 0)
                {
                    outerBorderScale.z = outerBorderLengthZ;
                }

                // Create the outer border primitive
                var outerBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                outerBorder.transform.localScale = outerBorderScale;
                outerBorder.transform.SetParent(_outerBorderContainer.transform);
                outerBorder.transform.position = pos;

                // Set the outer border material
                var outerBorderRenderer = outerBorder.GetComponent<Renderer>();
                if (outerBorderRenderer)
                {
                    outerBorderRenderer.sharedMaterial = outerBorderMaterial ? outerBorderMaterial : _defaultMaterial;
                }
            }
        }
        
        public bool SetMaterialForTile(Position position, [CanBeNull] Material newMaterial)
        {
            var index = RowColumnToIndex(position);
            if (index >= _tileRenderers.Count) return false;
            
            _tileRenderers[index].material = newMaterial ? newMaterial : GetMaterialForTile(position);
            return true;
        }

        public bool IsInBounds(Position position) => 
            position.IsInBounds(new Bounds(0, rows, 0, columns));
        
        public IEnumerable<Position> IterateToBounds(Position position, Position offset) => 
            position.IterateToBounds(offset, new Bounds(0, rows, 0, columns));
        
        public Vector3 GetPositionForTile(Position position)
            => GetTilePosition(position.Row, position.Column, thickness);
        
        public Vector3 GetPositionForTile(float row, float column)
            => GetTilePosition(row, column, thickness);

        public Position GetClosestTileForPosition(Vector3 position)
        {
            var localScale = transform.localScale;
            var row = Mathf.RoundToInt((position.x / localScale.x - _tileOffset.x) / (_tileScale.x + 2 * borderThickness));
            var column = Mathf.RoundToInt((position.z / localScale.z - _tileOffset.z) / (_tileScale.z + 2 * borderThickness));

            // Ensure row and column are within valid range
            row = Mathf.Clamp(row, 0, rows - 1);
            column = Mathf.Clamp(column, 0, columns - 1);

            return new Position(row, column);
        }

        public IEnumerable<Position> GetTiles() => Position.GetRange(0, Rows, 0, Columns);
    }
}
