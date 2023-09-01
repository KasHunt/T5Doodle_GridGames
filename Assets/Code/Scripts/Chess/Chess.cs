using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Position = Code.Scripts.GridGameboard.Position;
using PlayerColor = Code.Scripts.GridGameboard.PlayerColor;

namespace Code.Scripts.Chess
{
    public class Chess : MonoBehaviour, Actuator.IActuatorMovableProvider
    {
        ///////////////////////////////////
        //////// Public Properties ////////
        ///////////////////////////////////
        
        [Header("Board")]
        public float gridOffsetHeight = 0.05f;
        public List<GameObject> whitePlayIndicator;
        public List<GameObject> blackPlayIndicator;
        
        [Header("Geometry")]
        public GameObject kingPrefab;
        public GameObject queenPrefab;
        public GameObject bishopPrefab;
        public GameObject knightPrefab;
        public GameObject rookPrefab;
        public GameObject pawnPrefab;
        
        [Header("Materials (Pieces)")]
        public Material whiteMaterial;
        public Material whiteSelectedMaterial;
        public Material whiteFragmentMaterial;
        public Color whiteFragmentColor;
        public Material blackMaterial;
        public Material blackSelectedMaterial;
        public Material blackFragmentMaterial;
        public Color blackFragmentColor;
        
        [Header("Materials (Board)")]
        public Material whiteTileMaterial;
        public Material whiteTileSelectedMaterial;
        public Material whiteTileValidMoveMaterial;
        public Material blackTileMaterial;
        public Material blackTileSelectedMaterial;
        public Material blackTileValidMoveMaterial;
        public Material borderMaterial;
        public Material checkMaterial;
        public Material checkSelectedMaterial;
        
        [Header("Sounds")]
        public AudioClip captureSound;
        public List<AudioClip> destructionSounds = new();
        public AudioClip checkSound;
        public AudioClip thudSound;
        public AudioClip gameOverSound;
        
        [Header("Piece Creation")]
        public float appearDuration = 2.0f;
        public float appearHeight = 10f;
        public float appearStagger = 0.1f;
        
        [Header("Piece Movement")]
        public float snapAnimationDuration = 0.2f;
        public float moveAnimationDuration = 1.5f;
        public float moveAnimationArcHeight = 4f;
        
        [Header("Piece Destruction")]
        public float explosionPower = 200f;
        public float explosionRadius = 2f;
        public float explosionPositionRange = 0.25f;
        [Min(0f)]
        public float maxAudioVelocity = 10f;
        public float fadeDelay = 2.0f;
        public float fadeDuration = 2.0f;
        public ParticleSystem explosionParticleSystem;

        public static readonly GridGameboard.Bounds gameboardBounds = new(0, 8, 0, 8);
        
        ////////////////////////////////////
        //////// Private Properties ////////
        ////////////////////////////////////
        
        private GameState _gameState;
        private GridGameboard _gameBoard;
        [CanBeNull] private ChessPiece _promotingPiece;
        
        private enum BoardHighlights
        {
            CurrentMove,
            ValidMove,
            Check,
        }
        private Dictionary<Position, HashSet<BoardHighlights>> _gameboardHighlights = new();
                
        /////////////////////////////////
        //////// Unity Lifecycle ////////
        /////////////////////////////////
        
        // Static instance, accessible from anywhere
        public static Chess Instance { get; private set; }
        
        private void Awake()
        {
            // Check for existing instances and destroy this instance if we've already got one
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Destroying duplicate Chess");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            CreateBoard();
            NewGame();
            SetPlayIndicator();
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
            var obj = new GameObject("Chess Board", new [] { typeof(GridGameboard) });
            var gameBoard = obj.GetComponent<GridGameboard>();
            
            gameBoard.Columns = 8;
            gameBoard.Rows = 8;
            gameBoard.Thickness = 0.1f;
            
            gameBoard.OddTileMaterial = blackTileMaterial;
            gameBoard.EvenTileMaterial = whiteTileMaterial;
            gameBoard.BorderTileMaterial = whiteTileMaterial;
            gameBoard.OuterBorderTileMaterial = borderMaterial;
            
            gameBoard.BorderThickness = 0f;
            gameBoard.BorderHeightFactor = 0;
            gameBoard.OuterBorderThickness = 0.35f;
            gameBoard.OuterBorderHeightFactor = 1;

            gameBoard.Rebuild();
            
            gameBoard.transform.SetParent(transform);
            _gameBoard = gameBoard;
            
            WandManager.Instance.yPlane = 0.1f;
            WandManager.Instance.actuatorDefaultHeight = 1f;
        }
        
        public void NewGame()
        {
            // Create an empty highlight dictionary
            _gameboardHighlights = new Dictionary<Position, HashSet<BoardHighlights>>();
            _gameBoard.GetTiles().ToList()
                .ForEach(position => _gameboardHighlights[position] = new HashSet<BoardHighlights>());
            
            ApplyBoardMaterials();
            
            // Explode the existing pieces
            if (_gameState != null)
            {
                foreach (var piece in _gameState.AllPieces)
                {
                    piece.Explode(false, 0);
                }               
            }
            
            // Create the new board state, and instantiate the pieces
            _gameState = new GameState();
            StandardBoardSetup();
        }

        private void StandardBoardSetup()
        {
            StandardBoardSetup(PlayerColor.White);
            StandardBoardSetup(PlayerColor.Black);
        }
        
        private void StandardBoardSetup(PlayerColor color)
        {
            // Add non-pawns
            _gameState.AddPiece(MakePiece(PieceType.Rook, color, ChessPositions.QUEENS_ROOK_FILE));
            _gameState.AddPiece(MakePiece(PieceType.Knight, color, ChessPositions.QUEENS_KNIGHT_FILE));
            _gameState.AddPiece(MakePiece(PieceType.Bishop, color, ChessPositions.QUEENS_BISHOP_FILE));
            _gameState.AddPiece(MakePiece(PieceType.Queen, color, ChessPositions.QUEEN_FILE));
            _gameState.AddPiece(MakePiece(PieceType.King, color, ChessPositions.KING_FILE));
            _gameState.AddPiece(MakePiece(PieceType.Bishop, color, ChessPositions.KINGS_BISHOP_FILE));
            _gameState.AddPiece(MakePiece(PieceType.Knight, color, ChessPositions.KINGS_KNIGHT_FILE));
            _gameState.AddPiece(MakePiece(PieceType.Rook, color, ChessPositions.KINGS_ROOK_FILE));
         
            // Add pawns
            var pawnRank = ChessPositions.PawnRankForColor(color);
            for (var i = 0; i < 8; i++)
            {
                _gameState.AddPiece(MakePiece(PieceType.Pawn, color, new Position(pawnRank, i)));
            }
        }
        
        internal GameObject GeometryForType(PieceType type)
        {
            return type switch
            {
                PieceType.King => kingPrefab,
                PieceType.Queen => queenPrefab,
                PieceType.Bishop => bishopPrefab,
                PieceType.Knight => knightPrefab,
                PieceType.Rook => rookPrefab,
                PieceType.Pawn => pawnPrefab,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        internal static Quaternion RotationForType(PieceType type, PlayerColor color)
        {
            if (type != PieceType.Knight) return Quaternion.identity;
            return (color == PlayerColor.White)  ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
        }

        internal static ChessPieceSpecialization SpecializationForType(PieceType type)
        {
            return type switch
            {
                PieceType.King => new King(),
                PieceType.Queen => new Queen(),
                PieceType.Bishop => new Bishop(),
                PieceType.Knight => new Knight(),
                PieceType.Rook => new Rook(),
                PieceType.Pawn => new Pawn(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private Material MaterialForColor(PlayerColor color)
        {
            return color switch
            {
                PlayerColor.Black => blackMaterial,
                PlayerColor.White => whiteMaterial,
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
            };
        }
        
        private Material MaterialForColorSelected(PlayerColor color)
        {
            return color switch
            {
                PlayerColor.Black => blackSelectedMaterial,
                PlayerColor.White => whiteSelectedMaterial,
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
            };
        }
        
        private Material MaterialForColorFragment(PlayerColor color)
        {
            return color switch
            {
                PlayerColor.Black => blackFragmentMaterial,
                PlayerColor.White => whiteFragmentMaterial,
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
            };
        }

        private ChessPiece MakePiece(PieceType type, PlayerColor color, int file) =>
            MakePiece(type, color, new Position(ChessPositions.RankForColor(color), file));
        
        private ChessPiece MakePiece(PieceType type, PlayerColor color, Position position)
        {
            var pieceName = ChessPiece.NameForPiece(type, color, position);
            
            // Instantiate and set up piece
            var instance = new GameObject(pieceName);
            instance.transform.SetParent(transform);
            
            // Add the chess piece
            var chessPiece = instance.AddComponent<ChessPiece>();
            chessPiece.material = MaterialForColor(color);
            chessPiece.selectedMaterial = MaterialForColorSelected(color);
            chessPiece.fragmentMaterial = MaterialForColorFragment(color);
            chessPiece.transform.position = GetGridPosition(position);
            chessPiece.InitialPosition = position;
            chessPiece.pieceType = type;
            chessPiece.playerColor = color;

            return chessPiece;
        }
        
        //////////////////////////////////
        //////// Movement/Capture ////////
        //////////////////////////////////
        
        private IEnumerable<Position> GetValidKingMoves(ChessPiece piece, bool checkForCheck)
        {
            var position = _gameState.GetPosition(piece);
            var validMoves = King.GetValidMoves(piece.playerColor, position, _gameState, false);
            return !checkForCheck ? validMoves : FilterCheckMoves(piece, validMoves);
        }

        private IEnumerable<Position> GetValidMoves(ChessPiece piece, bool checkForCheck)
        {
            var position = _gameState.GetPosition(piece);
            var validMoves = piece.GetValidMoves(position, _gameState);
            return !checkForCheck ? validMoves : FilterCheckMoves(piece, validMoves);
        }

        public List<Position> CanBeTaken(PlayerColor color, Position position, GameState gameState, bool checkForCheck)
        {
            var threateningPieces = new List<Position>();
            
            // Iterate all pieces on the board
            foreach (var otherPieceInfo in gameState.AlivePieces)
            {
                var otherPiecePosition = _gameState.GetPosition(otherPieceInfo);
                
                // Ensure they're the opponents color
                if (otherPieceInfo.playerColor == color) continue;
                
                // Get the valid moves for each piece
                var validMoves = otherPieceInfo.Type == PieceType.King ?
                    GetValidKingMoves(otherPieceInfo, checkForCheck) : GetValidMoves(otherPieceInfo, checkForCheck);

                // If the valid moves of the opponent piece hit this piece, then this piece can be taken
                if (validMoves.Contains(position)) threateningPieces.Add(otherPiecePosition);
            }

            return threateningPieces;
        }
        
        [CanBeNull]
        private ChessPiece MaybeTakeEnPassant(ChessPiece chessPiece, Position fromPosition, Position toPosition)
        {
            if (chessPiece.Type != PieceType.Pawn) return null; // Not a pawn
            if (fromPosition.Column == toPosition.Column) return null; // Not attacking

            // We're a pawn, making an attacking move but we've got no target at the square
            // we're moving to, so this must be en-passant. Find the target and collapse it
            var enPassantTarget = new Position(fromPosition.Row, toPosition.Row);
            return _gameState.GetByCurrentPosition(enPassantTarget);
        }

        private static Move.MoveType MaybeCastle(ChessPiece chessPiece, Position from, Position to)
        {
            if (chessPiece.Type != PieceType.King) return Move.MoveType.Normal; // Not a king
            var moveDistance = to.Column - from.Column;
            if (Math.Abs(moveDistance) != 2) return Move.MoveType.Normal; // Not moving 2 squares
            
            // Record the move
            return moveDistance > 0 ? Move.MoveType.KingSideCastle : Move.MoveType.QueenSideCastle;
        }

        private Move BuildMove(ChessPiece piece, Position moveTo)
        {
            var currentPosition = _gameState.GetPosition(piece);

            // Check if we're capturing
            var capturingPiece = _gameState.GetByCurrentPosition(moveTo);
            var moveType = Move.MoveType.Normal;
            
            // Check for pawn special moves
            if (piece!.Type == PieceType.Pawn)
            {
                // Check for pawn double
                if (Math.Abs(currentPosition.Row - moveTo.Row) == 2)
                {
                    moveType = Move.MoveType.PawnDouble;
                }
                
                // Check for en-passant
                if (capturingPiece == null)
                {
                    capturingPiece = MaybeTakeEnPassant(piece, currentPosition, moveTo);
                    if (capturingPiece != null) moveType = Move.MoveType.EnPassant;
                }
            } 
            else if (piece!.Type == PieceType.King)
            {
                // Check for king castling
                moveType = MaybeCastle(piece, currentPosition, moveTo);
            }

            if (capturingPiece is { Type: PieceType.King })
            {
                moveType = Move.MoveType.EndGame;
            }
            
            return new Move(piece, currentPosition, moveTo, capturingPiece, moveType);
        }
        
        public Position MakeMove(ChessPiece chessPiece, Position moveTo)
        {
            var piece = _gameState.GetByInitialPosition(chessPiece.InitialPosition);
            var moveFrom = _gameState.GetPosition(piece);

            // Cancel the move if no moves are allowed (because we're promoting)
            if (_promotingPiece) return moveFrom;
            
            // Cancel the move if no moves are allowed (because it's not our turn)
            if (piece!.playerColor != _gameState.PlayingColor) return moveFrom;
            
            // Cancel the move if it's invalid
            var validMoves = GetValidMoves(piece, true);
            if (!validMoves.Contains(moveTo)) return moveFrom;
            
            // Build the move
            var move = BuildMove(piece, moveTo);
            
            // Record the move
            _gameState.CommitMove(move);
            
            // Maybe moved the castles
            if (move.Type == Move.MoveType.KingSideCastle)
            {
                var rank = move.FromPosition.Row;
                var rook = _gameState.GetByInitialPosition(new Position(rank, ChessPositions.KINGS_ROOK_FILE));
                if (rook) rook.AnimatePositionTo(
                    ChessPositions.GetCastlePosition(piece.playerColor, ChessPositions.KINGS_ROOK_FILE),
                    Instance.moveAnimationDuration, Instance.moveAnimationArcHeight);
            }
            else if (move.Type == Move.MoveType.QueenSideCastle)
            {
                var rank = move.FromPosition.Row;
                var rook = _gameState.GetByInitialPosition(new Position(rank, ChessPositions.QUEENS_ROOK_FILE));
                if (rook) rook.AnimatePositionTo(
                    ChessPositions.GetCastlePosition(piece.playerColor, ChessPositions.QUEENS_ROOK_FILE), 
                    Instance.moveAnimationDuration, Instance.moveAnimationArcHeight);  
            }
                
            // 'Explode' the captured piece
            if (move.Captured)
            {
                var takenPieceIndex = _gameState.Captured.Count(info => info.playerColor == move.Captured.playerColor) - 1;
                move.Captured.Explode(true, takenPieceIndex);
                
                // If it's a king, play the game over sound
                if (move.Captured.Type == PieceType.King) SoundManager.Instance.PlaySound(gameOverSound, 1);
            }
            
            // Don't check for checks if we've ended the game
            if (_gameState.IsGameOver) return moveTo;
            
            // Alternate turns unless it's checkmate
            AlternateTurns();
            SetPlayIndicator();
            
            // Check for promotion
            if ((piece.Type == PieceType.Pawn) && 
                ((piece.playerColor == PlayerColor.Black && moveTo.Row == ChessPositions.WHITE_RANK) || 
                 (piece.playerColor == PlayerColor.White && moveTo.Row == ChessPositions.BLACK_RANK)))
            {
                _promotingPiece = piece;
                InGameUi.Instance.ShowPawnPromotionScreen();
            }
            
            // Update the checks
            ShowCheck();
            
            return moveTo;
        }

        private void AlternateTurns()
        {
            // Alternate turns...
            _gameState.PlayingColor = _gameState.PlayingColor == PlayerColor.Black
                ? _gameState.PlayingColor = PlayerColor.White
                : _gameState.PlayingColor = PlayerColor.Black;
            
            // ...unless it's checkmate
            CheckForCheckmate();
        }

        ///////////////////////
        //////// Check ////////
        ///////////////////////
        
        private bool MoveResultsInCheck(PlayerColor playingColor, Move move)
        {
            _gameState.PreviewMove(move);
            var checkers = GetCheckers(playingColor, out _);
            _gameState.CancelPreview();

            return checkers.Count > 0;
        }

        private IEnumerable<Position> FilterCheckMoves(ChessPiece piece, IEnumerable<Position> candidates)
        {
            var filteredMoves = new List<Position>();
            foreach (var candidateMove in candidates)
            {
                var move = BuildMove(piece, candidateMove);
                if (MoveResultsInCheck(piece.playerColor, move)) continue;
                filteredMoves.Add(candidateMove);
            }

            return filteredMoves;
        }

        private void CheckForCheckmate()
        {
            if (CheckForCheckmate(PlayerColor.Black)) _gameState.PlayingColor = PlayerColor.White;
            if (CheckForCheckmate(PlayerColor.White)) _gameState.PlayingColor = PlayerColor.Black;
        }
        
        private List<Position> GetCheckers(PlayerColor forColor, out Position kingPosition)
        {
            var king = _gameState.GetByInitialPosition(
                forColor == PlayerColor.White ? ChessPositions.whiteKing : ChessPositions.blackKing);
            kingPosition = _gameState.GetPosition(king);
            return CanBeTaken(forColor, kingPosition, _gameState, false);
        }
        
        private bool CheckForCheckmate(PlayerColor color) =>
            _gameState.AlivePieces
                .Where(piece => piece.playerColor == color)
                .All(piece => !GetValidMoves(piece, true)
                    .Any());
        
        ////////////////////
        //////// UI ////////
        ////////////////////
        
        private void SetPlayIndicator()
        {
            var whiteActive = _gameState.PlayingColor == PlayerColor.White;
            foreach (var obj in whitePlayIndicator) obj.SetActive(whiteActive);
            
            var blackActive = _gameState.PlayingColor == PlayerColor.Black;
            foreach (var obj in blackPlayIndicator) obj.SetActive(blackActive);
        }

        private void ApplyBoardMaterials()
        {
            foreach (var (position, highlights) in _gameboardHighlights)
            {
                var isCheck = highlights.Contains(BoardHighlights.Check);
                var isCurrent = highlights.Contains(BoardHighlights.CurrentMove);
                var isValid = highlights.Contains(BoardHighlights.ValidMove);
                
                var material = (isCheck, isCurrent) switch
                {
                    (true, true) => checkSelectedMaterial,
                    (true, false) => checkMaterial,
                    (false, true) when position.IsOdd => whiteTileSelectedMaterial,
                    (false, true) => blackTileSelectedMaterial,
                    (false, false) when isValid && position.IsOdd => whiteTileValidMoveMaterial,
                    (false, false) when isValid => blackTileValidMoveMaterial,
                    (false, false) => null,
                };
                _gameBoard.SetMaterialForTile(position, material);
            }
        }
        
        public void SetCurrentMoveBoardTile(Position position, bool highlighted)
        {
            // Remove any existing 'current move' highlights
            foreach (var (_, value) in _gameboardHighlights) value.Remove(BoardHighlights.CurrentMove);
            
            // Add the 'current move' highlights
            if (highlighted) _gameboardHighlights[position].Add(BoardHighlights.CurrentMove);
            
            // Apply the materials to the board
            ApplyBoardMaterials();
        }
        
        public void SetValidMovesBoardTiles([CanBeNull] ChessPiece chessPiece)
        {
            // Remove any existing 'valid move' highlights
            foreach (var (_, value) in _gameboardHighlights) value.Remove(BoardHighlights.ValidMove);

            // Add any required 'valid move' highlights
            IEnumerable<Position> validMoves = new List<Position>(); 
            if (chessPiece)
            {
                var piece = _gameState.GetByInitialPosition(chessPiece.InitialPosition);
                if (piece && piece.playerColor == _gameState.PlayingColor) validMoves = GetValidMoves(piece, true);                
            }
            foreach (var validMove in validMoves) _gameboardHighlights[validMove].Add(BoardHighlights.ValidMove);
            
            // Apply the materials to the board
            ApplyBoardMaterials();
        }

        private void ClearCheckHighlights()
        {
            foreach (var (_, value) in _gameboardHighlights) value.Remove(BoardHighlights.Check);
        }
        
        private void ShowCheck()
        {
            // Remove any existing 'check' highlights
            ClearCheckHighlights();
            
            var blackCheckers = GetCheckers(PlayerColor.Black, out var blackKingPosition);
            var whiteCheckers = GetCheckers(PlayerColor.White, out var whiteKingPosition);

            if (blackCheckers.Count > 0) _gameboardHighlights[blackKingPosition].Add(BoardHighlights.Check);
            if (whiteCheckers.Count > 0) _gameboardHighlights[whiteKingPosition].Add(BoardHighlights.Check);
            
            foreach (var checker in blackCheckers) _gameboardHighlights[checker].Add(BoardHighlights.Check);
            foreach (var checker in whiteCheckers) _gameboardHighlights[checker].Add(BoardHighlights.Check);

            if ((blackCheckers.Count > 0) || (whiteCheckers.Count > 0))
            {
                SoundManager.Instance.PlaySound(checkSound, 1);
            }
            
            ApplyBoardMaterials();
        }

        //////////////////////
        //////// Misc ////////
        //////////////////////
        
        public void PromotePawn(PieceType toType)
        {
            if (_promotingPiece == null) return;
            
            // Promote the piece
            _promotingPiece.PromoteTo(toType);
            _promotingPiece = null;
            
            // Update the checks
            CheckForCheckmate();
            ShowCheck();
        }
                
        public Vector3 GetGridPosition(Position gridPosition) => 
            _gameBoard.GetPositionForTile(gridPosition.Row, gridPosition.Column);
        
        public Vector3 GetGridPosition(float rank, float file) => 
            _gameBoard.GetPositionForTile(rank, file);
        
        public Position GetClosesGridPosition(Vector3 worldPosition) =>
            _gameBoard.GetClosestTileForPosition(worldPosition);
        
        public IEnumerable<Actuator.IActuatorMovable> GetMovables() => 
            _gameState.AlivePieces;
    }
}

