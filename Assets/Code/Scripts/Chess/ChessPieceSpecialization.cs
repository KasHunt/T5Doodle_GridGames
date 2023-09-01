using System.Collections.Generic;
using System.Linq;
using Code.Scripts.Utils;

namespace Code.Scripts.Chess
{
    public abstract class ChessPieceSpecialization
    {
        protected enum MoveTestResult {
            Capture,
            Move,
            Invalid
        }

        protected static MoveTestResult TestMove(GridGameboard.PlayerColor color, GridGameboard.Position position, GameState gameState)
        {
            if (position.Row is < 0 or > 7 || position.Column is < 0 or > 7) return (MoveTestResult.Invalid);

            var info = gameState.GetByCurrentPosition(position);
            return info switch
            {
                null => MoveTestResult.Move,
                _ when info.playerColor != color => MoveTestResult.Capture,
                _ => MoveTestResult.Invalid,
            };
        }
        
        protected static IEnumerable<GridGameboard.Position> GetDiagonalMoves(GridGameboard.PlayerColor color, GridGameboard.Position position, 
            GameState gameState) => 
            GetMovesInDirections(GridGameboard.Position.DiagonalNeighbourOffsets, color, position, gameState);
        
        protected static IEnumerable<GridGameboard.Position> GetStraightMoves(GridGameboard.PlayerColor color, GridGameboard.Position position, 
            GameState gameState) => 
            GetMovesInDirections(GridGameboard.Position.OrthogonalNeighbourOffsets, color, position, gameState);

        private static IEnumerable<GridGameboard.Position> GetMovesInDirections(IEnumerable<GridGameboard.Position> directions, GridGameboard.PlayerColor color, GridGameboard.Position position, GameState gameState)
        {
            var validMoves = new List<GridGameboard.Position>();
            foreach (var offset in directions)
            {
                validMoves.AddRange(
                    position
                        .IterateToBounds(offset, Chess.gameboardBounds)
                        .Select(candidate => (candidate, TestMove(color, candidate, gameState)))
                        .TakeWhileInclusive(
                            candidate => candidate.Item2 == MoveTestResult.Move,
                            candidate => candidate.Item2 == MoveTestResult.Capture
                        ).Select(validMove => validMove.candidate)
                );
            }

            return validMoves;
        }
        
        public abstract IEnumerable<GridGameboard.Position> GetValidMoves(GridGameboard.PlayerColor color, GridGameboard.Position position, GameState gameState);
    }
}