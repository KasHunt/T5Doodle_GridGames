using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Code.Scripts.Chess
{
    public class GameState
    {
        public void AddPiece(ChessPiece piece) => AllPieces.Add(piece);

        public List<ChessPiece> AllPieces { get; } = new();
        public IEnumerable<ChessPiece> AlivePieces => AllPieces.Except(Captured).ToList();

        public GridGameboard.PlayerColor PlayingColor = GridGameboard.PlayerColor.White;

        private readonly Stack<Move> _moves = new();
        private bool _previewing;
        
        // TODO: Cache (and flush when performing a move)
        public bool HasMoved(ChessPiece piece) => 
            _moves.Any(move => Equals(move.FromPosition, piece.InitialPosition));

        // TODO: Cache (and flush when performing a move)
        public IEnumerable<ChessPiece> Captured => 
            (from move in _moves where move.Captured != null select move.Captured).ToList();
        
        public bool IsGameOver => _moves.Reverse().Any(move => move.Type == Move.MoveType.EndGame);
        
        // TODO: Cache (and flush when performing a move)
        public GridGameboard.Position GetPosition(ChessPiece piece)
        {
            var currentPosition = piece.InitialPosition;
            foreach (var move in _moves.Reverse())
            {
                if (move.Piece == piece)
                {
                    currentPosition = move.ToPosition;
                }

                // Check for castling
                if (piece.Type != PieceType.Rook) continue;
                if (move.Piece.playerColor != piece.playerColor) continue;
                currentPosition = move.Type switch
                {
                    Move.MoveType.KingSideCastle when piece.InitialPosition.Column == ChessPositions.KINGS_ROOK_FILE
                        => ChessPositions.GetCastlePosition(piece.playerColor, ChessPositions.KINGS_ROOK_FILE),
                    Move.MoveType.QueenSideCastle when piece.InitialPosition.Column == ChessPositions.QUEENS_ROOK_FILE
                        => ChessPositions.GetCastlePosition(piece.playerColor, ChessPositions.QUEENS_ROOK_FILE),
                    _ => currentPosition,
                };
            }
            return currentPosition;
        }
        
        [CanBeNull]
        public ChessPiece GetByInitialPosition(GridGameboard.Position position) =>
            AllPieces.FirstOrDefault(candidate => candidate.InitialPosition == position);
        
        [CanBeNull]
        public ChessPiece GetByCurrentPosition(GridGameboard.Position position) =>
            AlivePieces.FirstOrDefault(piece => GetPosition(piece) == position);
        
        public bool TryGetLastMovedPiece(out Move lastMove, [CanBeNull] out ChessPiece positionInfo)
        {
            lastMove = new Move();
            positionInfo = null;
            if (_moves.Count == 0) return false;

            lastMove = _moves.Peek();
            positionInfo = GetByCurrentPosition(lastMove.ToPosition);
            return true;
        }

        public void CancelPreview()
        {
            if (!_previewing) return;
            _moves.Pop();
            _previewing = false;
        }
        
        public void PreviewMove(Move move)
        {
            CancelPreview();
            _previewing = true;
            _moves.Push(move);
        }
        
        public void CommitMove(Move move)
        {
            CancelPreview();
            
            _moves.Push(move);
        }
    }
}