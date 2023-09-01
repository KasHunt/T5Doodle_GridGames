using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Position = Code.Scripts.GridGameboard.Position;
using PlayerColor = Code.Scripts.GridGameboard.PlayerColor;

namespace Code.Scripts.Checkers
{
    public class Checkers : MonoBehaviour, Actuator.IActuatorMovableProvider
    {
        ///////////////////////////////////
        //////// Public Properties ////////
        ///////////////////////////////////
        
        [Header("Board")]
        public float boardThickness = 0.1f;
        public List<GameObject> whitePlayIndicator;
        public List<GameObject> blackPlayIndicator;
        
        [Header("Geometry")]
        public GameObject pieceTemplate;
        public GameObject crownTemplate;
        
        [Header("Materials (Pieces)")]
        public Material blackPieceMaterial;
        public Material whitePieceMaterial;
        
        [Header("Materials (Board)")]
        public Material whiteTileMaterial;
        public Material blackTileMaterial;
        public Material blackTileRequiredMoveMaterial;
        public Material blackTileValidMoveMaterial;
        public Material blackTileSelectedInValidMoveMaterial;
        public Material blackTileSelectedValidMoveMaterial;
        public Material outerBorder;
        
        [Header("Sounds")]
        public AudioClip thudSound;
        public AudioClip dissolveSound;
        public AudioClip promoteSound;
        public AudioClip gameOverSound;

        [Header("Piece Timings")]
        public float promotionDuration = 1f;
        public float dissolveDuration = 1f;
        public float appearStagger = 0.05f;
        public float flipDelay = 0.1f;
        public float flipDuration = 0.5f;
        
        [Header("Piece Movement")]
        public float snapDuration = 0.1f;
        public float moveDuration = 1f;
        public float moveApexHeight = 2f;
        public float snapDistance = 1.5f;
        public float moveHeight = 1f;
        
        ////////////////////////////////////
        //////// Private Properties ////////
        ////////////////////////////////////
        
        private readonly List<CheckersPiece> _pieces = new();
        private IEnumerable<CheckersPiece> ActivePieces => _pieces.Where(piece => piece.Shown).ToList();
        
        internal GridGameboard GameBoard;
        
        private Dictionary<Position, MoveValidationResult> _currentValidMoves = new();
        
        private PlayerColor _requiredColor = PlayerColor.Black;
        [CanBeNull] private CheckersPiece _lastPieceToCapture;
        [CanBeNull] private CheckersPiece _requiredPiece;
        [CanBeNull] private Position _lastHighlightPosition;
        private List<CheckersPiece> _piecesThatCanCapture = new();
        private bool _firstReset = true;
        private bool _won;

        internal const int BLACK_PROMOTION_ROW = 0;
        internal const int WHITE_PROMOTION_ROW = 7;
        
        /////////////////////////////////
        //////// Unity Lifecycle ////////
        /////////////////////////////////
        
        // Static instance, accessible from anywhere
        public static Checkers Instance { get; private set; }
        
        private void Awake()
        {
            // Check for existing instances and destroy this instance if we've already got one
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Destroying duplicate Checkers");
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
            var obj = new GameObject("Checkers Board", new [] { typeof(GridGameboard) });
            var gameBoard = obj.GetComponent<GridGameboard>();
            
            gameBoard.Columns = 8;
            gameBoard.Rows = 8;
            gameBoard.Thickness = boardThickness;
            
            gameBoard.OddTileMaterial = blackTileMaterial;
            gameBoard.EvenTileMaterial = whiteTileMaterial;
            gameBoard.BorderTileMaterial = whiteTileMaterial;
            gameBoard.OuterBorderTileMaterial = outerBorder;
            
            gameBoard.BorderThickness = 0f;
            gameBoard.BorderHeightFactor = 0;
            gameBoard.OuterBorderThickness = 0.04f;
            gameBoard.OuterBorderHeightFactor = 0.5f;

            gameBoard.Rebuild();
            
            gameBoard.transform.SetParent(transform);
            GameBoard = gameBoard;
            
            WandManager.Instance.yPlane = 0.1f;
            WandManager.Instance.actuatorDefaultHeight = moveHeight;
        }
        
        public void NewGame()
        {
            ResetGame();
            
            ApplyBoardHighlights();
            
            SetPlayIndicator();
        }

        private void MakePiece(Position position, PlayerColor color)
        {
            var piece = Instantiate(pieceTemplate, transform, false);
            var checkersPiece = piece.AddComponent<CheckersPiece>();
            checkersPiece.color = color;
            checkersPiece.InitialPosition = position;
            checkersPiece.CurrentPosition = position;
            _pieces.Add(checkersPiece);
            
            piece.SetActive(true);

            var render = piece.GetComponentInChildren<MeshRenderer>();
            render.material = Instantiate(color == PlayerColor.Black ? blackPieceMaterial : whitePieceMaterial);
        }

        private void ResetGame()
        {
            // Create the new board state
            _piecesThatCanCapture.Clear();
            _currentValidMoves.Clear();
            
            _requiredPiece = null;
            _requiredColor = PlayerColor.Black;
            _won = false;
            
            if (_firstReset)
            {
                _firstReset = false;
                return;
            }
            
            // Reset the pieces, staggering their creating in a random order
            var random = new System.Random();
            _pieces
                .OrderBy(_ => random.Next())
                .Select((piece, index) => (piece, index))
                .ToList()
                .ForEach(ele => ele.piece.ResetPiece(ele.index));
        }
        
        private void MakePieces() => new List<PlayerColor>{PlayerColor.White, PlayerColor.Black}
            .ForEach(color => Position
                .GetRange(color == PlayerColor.White ? 0 : 5, 3, 0, 8)
                .Where(pos => pos.IsOdd).ToList().ForEach(pos => MakePiece(pos, color)));
        
        //////////////////////////////////
        //////// Movement/Capture ////////
        //////////////////////////////////
        
        public void BeginMove(CheckersPiece piece)
        {
            _currentValidMoves.Clear();

            // Cancel the move if: The game has been won
            if (_won) return;
            
            // Cancel the move if: It's not of the required color
            if (_requiredColor != PlayerColor.None && piece.color != _requiredColor) return;
            
            // Cancel the move if: It's not the required piece
            // (Because a piece has captured, and has further captures available)
            if (_requiredPiece && piece != _requiredPiece) return;

            // Cancel the move if: Pieces can capture, but this isn't one of them
            if (_piecesThatCanCapture.Count > 0 && !_piecesThatCanCapture.Contains(piece)) return;

            // Get the available moves for the given piece
            _currentValidMoves = ValidateMoves(piece);
            
            ApplyBoardHighlights();
        }

        public void Move(CheckersPiece piece, Vector3 position)
        {
            // Move the piece
            piece.transform.position = position;
            
            // Reset the currently highlighted valid moves
            ApplyBoardHighlights();
            
            // Get the validity of the 'current' tile, and set the appropriate material
            var toPosition = GetClosestValidTile(position);
            if (toPosition == null) return;
            
            // Don't change material if we're on the 'starting' tile
            if (toPosition == piece.CurrentPosition) return;
            
            // Set the materials
            var validationResult = _currentValidMoves.GetValueOrDefault(toPosition);
            GameBoard.SetMaterialForTile(toPosition, GetMaterialForValidity(validationResult));
        }

        private Material GetMaterialForValidity(MoveValidationResult validationResult)
        {
            return validationResult switch
            {
                MoveValidationResult.Occupied => blackTileSelectedInValidMoveMaterial,
                MoveValidationResult.Invalid => blackTileSelectedInValidMoveMaterial,
                MoveValidationResult.Valid => blackTileSelectedValidMoveMaterial,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        private void ApplyBoardHighlights()
        {
            // Clear any existing highlights
            GameBoard.GetTiles().ToList().ForEach(position => GameBoard.SetMaterialForTile(position, null));
            
            // Highlight any valid moves
            _currentValidMoves
                .Where(pair => pair.Value == MoveValidationResult.Valid)
                .ToList().ForEach(pair => GameBoard.SetMaterialForTile(pair.Key, blackTileValidMoveMaterial));
            
            // Highlight any pieces that can capture
            _piecesThatCanCapture
                .ForEach(piece => GameBoard.SetMaterialForTile(piece.CurrentPosition, blackTileRequiredMoveMaterial));
        }

        private enum MoveValidationResult
        {
            Invalid = 0,    // Default
            Occupied,
            Valid,
        }

        private readonly IEnumerable<Position> _possibleMoveOffsets =
            Position.DiagonalNeighbourOffsets.Concat(Position.DiagonalNeighbourOffsets.Select(value => value * 2));

        [CanBeNull]
        private Position GetClosestValidTile(Vector3 position)
        {
            // Check we're in range of an (odd) tile
            var toPosition = GameBoard.GetClosestTileForPosition(position);
            if (!toPosition.IsOdd) return null;
            
            var sqrDistance = (GameBoard.GetPositionForTile(toPosition) - position).sqrMagnitude;
            var inRange = sqrDistance < (snapDistance * snapDistance);
            if (!inRange) return null;

            return (GameBoard.IsInBounds(toPosition)) ? toPosition : null;
        }
        
        private Dictionary<Position, MoveValidationResult> ValidateMoves(CheckersPiece piece)
        {
            var position = piece.CurrentPosition;
            return _possibleMoveOffsets
                .Where(candidate => GameBoard.IsInBounds(position + candidate))
                .ToDictionary(
                    candidate => position + candidate, 
                    candidate => ValidateMove(piece, position + candidate, piece.color)
                );
        }

        private CheckersPiece JumpedPiece(Position fromPosition, Position toPosition)
        {
            var jumpPosition = fromPosition + (toPosition - fromPosition) / 2;
            return ActivePieces.FirstOrDefault(other => other.CurrentPosition == jumpPosition);
        }
        
        private MoveValidationResult ValidateMove(CheckersPiece piece, Position toPosition, PlayerColor forColor)
        {
            // Check the target tile is clear
            if (ActivePieces.Any(other => other.CurrentPosition == toPosition)) return MoveValidationResult.Occupied;
            
            // Check the direction is valid for the current piece
            if (!piece!.IsDirectionAllowed(toPosition)) return MoveValidationResult.Invalid;
            
            // Determine if the move is a capture (distance 2) or non-capture move (distance 1)
            var distance = piece.CurrentPosition.ChebyshevDistance(toPosition);

            // Allow non-capture moves if we've not taken this turn, and no captures are available
            var haveTakenThisTurn = _lastPieceToCapture == piece;
            var canCapture = _piecesThatCanCapture.Any();
            if (distance == 1)
            {
                return (!haveTakenThisTurn && !canCapture) ? MoveValidationResult.Valid : MoveValidationResult.Invalid;
            }

            // Allow capture moves that take a piece of the opposite color
            var jumpedPiece = JumpedPiece(piece.CurrentPosition, toPosition);
            return (jumpedPiece && jumpedPiece.color == GridGameboard.OppositeColor(forColor))
                ? MoveValidationResult.Valid
                : MoveValidationResult.Invalid;
        }
        
        public void EndMove(CheckersPiece piece, Vector3 position)
        {
            // Get the validity of the move
            var fromPosition = piece.CurrentPosition;
            var toPosition = GetClosestValidTile(position);
            
            // Cancel the move if it's invalid or if it's not actually a move
            if ((toPosition == null) ||
                (fromPosition == toPosition) || 
                (_currentValidMoves.GetValueOrDefault(toPosition) != MoveValidationResult.Valid))
            {
                _currentValidMoves.Clear();
                ApplyBoardHighlights();
                
                piece.AnimateCancelMove();
                return;
            }
            
            // Snap into place (promoting if necessary)
            piece.AnimateApplyMove(toPosition);
            
            var oppositeColor = GridGameboard.OppositeColor(piece.color);
            
            // After a single move, we force change of control
            // (IE No more moves for the same color)
            if (fromPosition.ChebyshevDistance(toPosition) == 1)
            {
                _lastPieceToCapture = null;
                
                _requiredColor = oppositeColor;
                _requiredPiece = null;
                _piecesThatCanCapture = GetPiecesThatCanCapture(_requiredColor);
                _currentValidMoves.Clear();
            }
            else
            {
                JumpedPiece(fromPosition, toPosition).Capture();
                
                _lastPieceToCapture = piece;
                
                if (HasAnyValidMoves(piece))
                {
                    // If we've captured, but the piece still have valid
                    // moves (captures), the player /must/ take (one of) them
                    _requiredPiece = piece;
                    _requiredColor = piece.color;
                    _piecesThatCanCapture = new List<CheckersPiece> { piece };
                    _currentValidMoves = ValidateMoves(piece);
                }
                else
                {
                    // If we've captured, and have no more valid moves
                    // (captures), force a change of control.
                    _requiredPiece = null;
                    _requiredColor = oppositeColor;
                    _piecesThatCanCapture = GetPiecesThatCanCapture(_requiredColor);
                    _currentValidMoves.Clear();
                }
            }
            
            // Check if the player has won (because the opponent can't move)
            if (!HasAnyValidMoves(_requiredColor))
            {
                ShowWin(toPosition, GridGameboard.OppositeColor(_requiredColor));
                SoundManager.Instance.PlaySound(gameOverSound, 1);
                _won = true;
            }
            
            ApplyBoardHighlights();
            
            SetPlayIndicator();
        }

        private bool HasAnyValidMoves(PlayerColor color) => ActivePieces
            .Any(piece => piece.color == color && HasAnyValidMoves(piece));
        
        private bool HasAnyValidMoves(CheckersPiece piece) => 
            ValidateMoves(piece).Any(ele => ele.Value ==MoveValidationResult.Valid);

        private static bool ContainsValidCapture(Position fromPosition, 
            Dictionary<Position, MoveValidationResult> moves) => moves
                .Any(pair => pair.Value == MoveValidationResult.Valid &&
                             pair.Key.ChebyshevDistance(fromPosition) == 2);
        
        private List<CheckersPiece> GetPiecesThatCanCapture(PlayerColor color)
        {
            return ActivePieces
                .Where(piece => piece.color == color && 
                                ContainsValidCapture(piece.CurrentPosition, ValidateMoves(piece)))
                .ToList();
        }
        
        ////////////////////
        //////// UI ////////
        ////////////////////

        private void SetPlayIndicator()
        {
            var whiteActive = _requiredColor == PlayerColor.White;
            foreach (var obj in whitePlayIndicator) obj.SetActive(whiteActive);
            
            var blackActive = _requiredColor == PlayerColor.Black;
            foreach (var obj in blackPlayIndicator) obj.SetActive(blackActive);
        }
        
        // If player can't move, then the opponent has won
        private void ShowWin(Position lastMove, PlayerColor winnerColor) => 
            ActivePieces.Where(piece => piece.color == winnerColor)
                .Select(piece => (piece, delay: moveDuration + piece.CurrentPosition.LinearDistance(lastMove) * flipDelay))
                .ToList().ForEach(ele => ele.piece.AnimateFlip(moveApexHeight, flipDuration, ele.delay));
        
        //////////////////////
        //////// Misc ////////
        //////////////////////
        
        public IEnumerable<Actuator.IActuatorMovable> GetMovables() => _pieces;
    }
}
