using System;
using System.Collections.Generic;
using System.Linq;

namespace Code.Scripts.Chess
{
    public class King : ChessPieceSpecialization
    {
        private static bool CastlePossibleAt(GridGameboard.PlayerColor color, int rank, 
            int startFile, int endFile, GameState gameState)
        {
            // Exclude file 1 from check check
            var lowerFile = Math.Min(startFile, endFile);
            var upperFile = Math.Max(startFile, endFile);

            for (var file = lowerFile + 1; file < upperFile; file++)
            {
                var testPosition = new GridGameboard.Position(rank, file);
                
                // If any piece obstructs the move, castling is not possible
                var result = TestMove(color, testPosition, gameState);
                if (result != MoveTestResult.Move) return false;
                
                // Kings may not move into check while castling
                // Note we don't need to check file 1 since the king doesn't move there during castling
                if ((file != 1) && Chess.Instance.CanBeTaken(color, testPosition, gameState, true).Count > 0) return false;
            }

            return true;
        }
        
        private static IEnumerable<GridGameboard.Position> CheckCastle(GridGameboard.PlayerColor color, GameState gameState)
        {
            // Find the King
            var castleMoves = new List<GridGameboard.Position>();
            var kingPosition = (color == GridGameboard.PlayerColor.Black) ? ChessPositions.blackKing : ChessPositions.whiteKing;
            var info = gameState.GetByInitialPosition(kingPosition);
            
            // Can't castle if we've moved the king
            if (gameState.HasMoved(info)) return castleMoves;
            
            // Can't castle if in check
            if (Chess.Instance.CanBeTaken(color, kingPosition, gameState, true).Count > 0)
            {
                return castleMoves;
            }

            // Check the rooks
            for (var direction = -1; direction <= 1; direction += 2)
            {
                var rookPosition = new GridGameboard.Position(kingPosition.Row, 
                    direction == -1 ? ChessPositions.QUEENS_ROOK_FILE : ChessPositions.KINGS_ROOK_FILE);
                var rook = gameState.GetByInitialPosition(rookPosition);
                if ((rook == null) || gameState.HasMoved(rook)) continue;
                if (!CastlePossibleAt(color, kingPosition.Row, rookPosition.Column, kingPosition.Column, gameState)) continue;
                castleMoves.Add(kingPosition + new GridGameboard.Position(0, 2 * direction));
            }
            
            return castleMoves;
        }

        public override IEnumerable<GridGameboard.Position> GetValidMoves(GridGameboard.PlayerColor color, GridGameboard.Position position, GameState gameState)
        {
            return GetValidMoves(color, position, gameState, true);
        }

        public static IEnumerable<GridGameboard.Position> GetValidMoves(GridGameboard.PlayerColor color, 
            GridGameboard.Position position, GameState gameState, bool enableCastling)
        {
            var validMoves = GridGameboard.Position.EightConnectedNeighborOffsets
                .Select(offset => position + offset)
                .Where(candidate => TestMove(color, candidate, gameState) != MoveTestResult.Invalid)
                .ToList();
            
            // Check for castling (if permitted)
            if (!enableCastling) return validMoves;
            var castleMoves = CheckCastle(color, gameState);
            validMoves.AddRange(castleMoves);

            return validMoves;
        }
    }
}