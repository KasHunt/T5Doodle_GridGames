using System;
using System.Collections.Generic;
using System.Linq;
using Code.Scripts.Utils;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using Position = Code.Scripts.GridGameboard.Position;
using PlayerColor = Code.Scripts.GridGameboard.PlayerColor;

namespace Code.Scripts.Reversi
{
    public class Reversi : MonoBehaviour, Actuator.IActuatorMovableProvider
    {
        ///////////////////////////////////
        //////// Public Properties ////////
        ///////////////////////////////////
        
        [Header("Board")]
        public List<GameObject> whitePlayIndicator;
        public List<GameObject> blackPlayIndicator;
        public List<TextMeshPro> scoresWhite;
        public List<TextMeshPro> scoresBlack;
        
        [Header("Geometry")]
        public GameObject pieceTemplate;
        
        [Header("Materials (Board)")]
        public Material tileMaterial;
        public Material tileDisabledMaterial;
        public Material tileHighlightMaterial;
        public Material tileHighlightDisabledMaterial;
        public Material borderMaterial;
        
        [Header("Animation / Movement")]
        public float rotateDuration = 0.3f;
        public float snapDuration = 0.1f;
        public float moveDuration = 1f;
        public float flipDuration = 0.5f;
        public float flipDelay = 0.1f;
        public float moveApexHeight = 2f;
        public float snapDistance = 1.5f;
        
        [Header("Sounds")]
        public AudioClip thudSound;
        
        ////////////////////////////////////
        //////// Private Properties ////////
        ////////////////////////////////////
        
        private readonly List<ReversiPiece> _pieces = new();
        private GameState _gameState;
        private GridGameboard _gameBoard;
        private PlayerColor _placingColor = PlayerColor.Black;
        
        private readonly Dictionary<ReversiPiece, Vector3> _initialMovePosition = new();
        [CanBeNull] private Position _lastHighlightPosition;

        /////////////////////////////////
        //////// Unity Lifecycle ////////
        /////////////////////////////////
        
        // Static instance, accessible from anywhere
        public static Reversi Instance { get; private set; }
        
        private void Awake()
        {
            // Check for existing instances and destroy this instance if we've already got one
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Destroying duplicate Reversi");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void Start()
        {
            CreateBoard();
            MakePieces();
            NewGame();
        }

        private void OnEnable()
        {
            WandManager.Instance.MovableProvider = this;
            SoundManager.Instance.EffectVolume = 1;
        }
        
        ////////////////////////////
        //////// Game Setup ////////
        ////////////////////////////
        
        private void CreateBoard()
        {
            var obj = new GameObject("Reversi Board", new [] { typeof(GridGameboard) });
            var gameBoard = obj.GetComponent<GridGameboard>();
            
            gameBoard.Columns = 8;
            gameBoard.Rows = 8;
            gameBoard.Thickness = 0.4f;
            
            gameBoard.OddTileMaterial = tileMaterial;
            gameBoard.EvenTileMaterial = tileMaterial;
            gameBoard.BorderTileMaterial = borderMaterial;
            gameBoard.OuterBorderTileMaterial = borderMaterial;
            
            gameBoard.BorderThickness = 0.05f;
            gameBoard.BorderHeightFactor = 1.2f;
            gameBoard.OuterBorderThickness = 1f;
            gameBoard.OuterBorderHeightFactor = 1.3f;

            gameBoard.Rebuild();

            gameBoard.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            gameBoard.transform.SetParent(transform);
            _gameBoard = gameBoard;

            WandManager.Instance.yPlane = 0.4f;
            WandManager.Instance.actuatorDefaultHeight = 0.7f;
        }

        private void ResetHighlights() =>
            _gameBoard.GetTiles().ToList().ForEach(position => 
                _gameBoard.SetMaterialForTile(position, 
                    _gameState.TileEnabled(position) ? null : tileDisabledMaterial));
        
        public void NewGame()
        {
            // Create the new board state
            _gameState = GameState.CreateNew();
            _placingColor = PlayerColor.Black;
            
            MovePiecesToInitialPosition();
            ResetHighlights();
            UpdateScores();
            SetPlayIndicator();
        }
        
        private void MakePieces()
        {
            foreach (var piece in _pieces) Destroy(piece);
            
            for (var i = 0; i < 64; i++)
            {
                var piece = Instantiate(pieceTemplate, transform, false);
                var reversiPiece = piece.AddComponent<ReversiPiece>();
                _pieces.Add(reversiPiece);
                piece.transform.position = VectorUtils.RandomVector3(-3f, 3f);
                piece.SetActive(true);
            }
        }

        private void MovePiecesToInitialPosition()
        {
            var newPieceRotation = Quaternion.Euler(90, 0, 0);

            for (var i = 0; i < 64; i++)
            {
                // Move the piece to it's target location
                var row = (i < 32) ? -2f : 9f;
                var column = ((i % 32) / 4f) - 0.5f;
                
                var position = _gameBoard.GetPositionForTile(row, column) + new Vector3(0, 0.5f, 0);

                _pieces[i].AnimateRotationTo(newPieceRotation, flipDuration);
                _pieces[i].AnimatePositionTo(position, moveApexHeight, moveDuration, true);
            }
        }

        private static Quaternion GetRotationForColor(PlayerColor color) =>
            (color == PlayerColor.Black) ? Quaternion.Euler(180, 0, 0): Quaternion.identity;
        
        //////////////////////////
        //////// Movement ////////
        //////////////////////////
        
        public void BeginMove(ReversiPiece piece, Vector3 position)
        {
            piece.IsKinematic = true;
            _initialMovePosition[piece] = position;
            piece.AnimateRotationTo(GetRotationForColor(_placingColor), rotateDuration);
        }

        public void Move(ReversiPiece piece, Vector3 position)
        {
            // Move the piece
            piece.transform.position = position;
            
            var validationResult = IsMoveValid(position, _placingColor, out var gridLocation);
            
            // Clear any previous highlight
            ResetHighlights();

            // If we're out of range, don't set any highlights
            if (validationResult.result == MoveValidationResult.OutOfRange) return;
            
            // Compute the material
            var material = validationResult.result switch
            {
                MoveValidationResult.TileDisabled => tileHighlightDisabledMaterial,
                MoveValidationResult.Occupied => tileHighlightDisabledMaterial,
                MoveValidationResult.Invalid => tileHighlightDisabledMaterial,
                MoveValidationResult.Valid => tileHighlightMaterial,
                MoveValidationResult.OutOfRange => null,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            // Set the new highlight
            _gameBoard.SetMaterialForTile(gridLocation, material);
        }

        private enum MoveValidationResult
        {
            OutOfRange,
            TileDisabled,
            Occupied,
            Invalid,
            Valid,
        }

        private (MoveValidationResult result, IEnumerable<FlipLine> lines) IsMoveValid(Vector3 position, 
            PlayerColor forColor, [CanBeNull] out Position gridLocation)
        {
            gridLocation = null;
            
            // Check we're in range of the tile
            var location = _gameBoard.GetClosestTileForPosition(position);
            var sqrDistance = (_gameBoard.GetPositionForTile(location) - position).sqrMagnitude;
            var inRange = sqrDistance < (snapDistance * snapDistance);
            if (!inRange) return (MoveValidationResult.OutOfRange, new List<FlipLine>());
            gridLocation = location;

            return IsMoveValid(gridLocation, forColor);
        }

        private (MoveValidationResult result, IEnumerable<FlipLine> lines) IsMoveValid(Position gridLocation, 
            PlayerColor forColor)
        {
            IEnumerable<FlipLine> lines = new List<FlipLine>();
            
            // Check the tile is enabled
            var tileEnabled = _gameState.TileEnabled(gridLocation);
            if (!tileEnabled) return (MoveValidationResult.TileDisabled, lines);
            
            // Check the tile is clear
            var tileClear = _gameState.ColorAt(gridLocation) == PlayerColor.None;
            if (!tileClear) return (MoveValidationResult.Occupied, lines);

            // If we're in setup - return valid move...
            if (_gameState.PlacedCount < 4) return (MoveValidationResult.Valid, lines);
            
            // ...otherwise, search for valid flanking maneuvers
            lines = SearchLines(gridLocation, forColor).ToList();
            return (lines.Any() ? MoveValidationResult.Valid : MoveValidationResult.Invalid, lines);
        }
        
        public void EndMove(ReversiPiece piece, Vector3 position)
        {
            // Clear any previous highlight
            ResetHighlights();

            var validMove = IsMoveValid(position, _placingColor, out var gridLocation);
            if (validMove.result != MoveValidationResult.Valid)
            {
                // Can't place - cancel the move
                piece.AnimatePositionTo(_initialMovePosition[piece], moveApexHeight, moveDuration, false);
                piece.IsKinematic = false;
                return;
            }

            // Make the move (set the piece in the game state, flip lines)
            PlacePiece(piece, gridLocation, validMove.lines);
            
            // Snap into place
            piece.AnimatePositionTo(_gameBoard.GetPositionForTile(gridLocation), 0, snapDuration, false);
            piece.transform.rotation = GetRotationForColor(_placingColor);
            
            ResetHighlights();

            UpdateScores();
            
            // Switch to playing the 'other' color
            _placingColor = GridGameboard.OppositeColor(_placingColor);
            
            // Check if no move is available
            CheckForNoMove();

            SetPlayIndicator();
        }

        private void CheckForNoMove()
        {
            var movesAvailable = _gameState.UnPlayedPositions
                .Select(position => IsMoveValid(position, _placingColor))
                .Any(result => result.result == MoveValidationResult.Valid);

            // If no move is available for the player, switch players (again)
            if (!movesAvailable) _placingColor = GridGameboard.OppositeColor(_placingColor);
        }
        
        private void DoLineFlip(FlipLine line)
        {
            var rotation = GetRotationForColor(line.FlipToColor);

            var delay = 0f;
            
            var position = line.Start;
            while (position != line.End)
            {
                delay += flipDelay;
                var tile = _gameState.FlipTo(position, line.FlipToColor);
                tile.AnimateFlip(rotation, moveApexHeight, flipDuration, delay);
                position += line.Step;
            }
        }
        
                private struct FlipLine {
            public Position Start;
            public Position End;
            public Position Step;
            public PlayerColor FlipToColor;
        }

        private FlipLine? SearchLine(Position fromPosition, Position step, PlayerColor selfColor)
        {
            var otherColor = GridGameboard.OppositeColor(selfColor);
            var checkPosition = fromPosition;
            var foundOther = false;
                    
            while (_gameBoard.IsInBounds(checkPosition))
            {
                checkPosition += step;
                var color = _gameState.ColorAt(checkPosition);

                // Break if we reach the end of a test line
                if (color == PlayerColor.None) return null;
                        
                if (!foundOther)
                {
                    // Found ourself before another - stop
                    if (color == selfColor) return null;
                            
                    // Found another piece, move to searching for lines
                    foundOther = true;
                    continue;
                }

                // Found an other piece, and we're still finding them - continue
                if (color == otherColor) continue;
                        
                // Found the end of a line - add to the flip list
                return new FlipLine
                {
                    Start = fromPosition + step,
                    End = checkPosition,
                    Step = step,
                    FlipToColor = selfColor,
                };
            }

            return null;
        }
        
        private IEnumerable<FlipLine> SearchLines(Position position, PlayerColor selfColor) 
            => Position.EightConnectedNeighborOffsets
                .ToList()
                .Select(offset => SearchLine(position, offset, selfColor))
                .Where(line => line != null).Cast<FlipLine>();
        
        private void PlacePiece(ReversiPiece piece, Position gridLocation, IEnumerable<FlipLine> lines)
        {
            // Place the piece
            _gameState.Place(gridLocation, piece, _placingColor);
            
            // Execute the line flips
            foreach (var line in lines) DoLineFlip(line);
        }

        ////////////////////
        //////// UI ////////
        ////////////////////
        
        private void SetPlayIndicator()
        {
            var whiteActive = _placingColor == PlayerColor.White;
            foreach (var obj in whitePlayIndicator) obj.SetActive(whiteActive);
            
            var blackActive = _placingColor == PlayerColor.Black;
            foreach (var obj in blackPlayIndicator) obj.SetActive(blackActive);
        }
        
        private void UpdateScores()
        {
            var (blackScore, whiteScore) = _gameState.GetScores();
            
            scoresWhite.ForEach(tmp => tmp.text = $"{whiteScore}");
            scoresBlack.ForEach(tmp => tmp.text = $"{blackScore}");
        }
        
        //////////////////////
        //////// Misc ////////
        //////////////////////
        
        public IEnumerable<Actuator.IActuatorMovable> GetMovables() => _pieces.ToList().Except(_gameState.PlacedPieces);
    }
}
