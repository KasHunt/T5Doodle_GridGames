using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Code.Scripts.Chess
{
    public class Pawn : ChessPieceSpecialization
    {
        [CanBeNull] private static GridGameboard.Position CheckDoubleMove(GridGameboard.PlayerColor color, GridGameboard.Position position, 
            GameState gameState)
        {
            switch (color)
            {
                case GridGameboard.PlayerColor.White when position.Row != 1:
                case GridGameboard.PlayerColor.Black when position.Row != 6:
                    return null;

                default:
                {
                    var direction = color == GridGameboard.PlayerColor.White ? 1 : -1;    
                    var onPath = position + new GridGameboard.Position(1 * direction, 0);
                    var move = position + new GridGameboard.Position(2 * direction, 0);

                    var result = TestMove(color, onPath, gameState);
                    if (result == MoveTestResult.Invalid) return null;
                    
                    result = TestMove(color, move, gameState);
                    return result == MoveTestResult.Invalid ? null : move;
                }
            }
        }

        private static GridGameboard.Position CheckEnPassant(GridGameboard.Position position, GameState gameState)
        {
            if (!gameState.TryGetLastMovedPiece(out var lastMovePosition, out var lastInfo)) return null;
            if (lastInfo == null) return null;
            if (lastInfo.Type != PieceType.Pawn) return null;
            if (position.Row != lastMovePosition.ToPosition.Row) return null;
            if (Math.Abs(lastMovePosition.FromPosition.Row - lastMovePosition.ToPosition.Row) != 2) return null;
            
            // Last move was a pawn that moved two spaces and is now on the same rank as this (pawn) piece
            return lastMovePosition.ToPosition + new GridGameboard.Position(lastInfo.playerColor == GridGameboard.PlayerColor.White ? -1 : 1, 0);
        }
        
        public override IEnumerable<GridGameboard.Position> GetValidMoves(GridGameboard.PlayerColor color, GridGameboard.Position position, GameState gameState)
        {
            var validMoves = new List<GridGameboard.Position>();
            GridGameboard.Position potentialMove;

            var deltaRank = color == GridGameboard.PlayerColor.White ? 1 : -1;
            for (var deltaFile = -1; deltaFile <= 1; deltaFile++)
            {
                potentialMove = position + new GridGameboard.Position(deltaRank, deltaFile);
                var result = TestMove(color, potentialMove, gameState);

                if ((deltaFile == 0 && result == MoveTestResult.Move) || 
                    (deltaFile != 0 && result == MoveTestResult.Capture))
                {
                    validMoves.Add(potentialMove);
                }
            }

            // Check for double moves
            potentialMove = CheckDoubleMove(color, position, gameState);
            if (potentialMove != null) validMoves.Add(potentialMove);

            // Check for en-passant
            potentialMove = CheckEnPassant(position, gameState);
            if (potentialMove != null) validMoves.Add(potentialMove);
            
            return validMoves;
        }
    }
}