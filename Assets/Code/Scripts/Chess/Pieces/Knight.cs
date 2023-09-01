using System.Collections.Generic;
using System.Linq;

namespace Code.Scripts.Chess
{
    public class Knight : ChessPieceSpecialization
    {
        private static IEnumerable<GridGameboard.Position> KnightMoves { get; }= new[]
        {
            new GridGameboard.Position(2, 1),
            new GridGameboard.Position(1, 2),
            new GridGameboard.Position(-2, 1),
            new GridGameboard.Position(-1, 2),
            
            new GridGameboard.Position(2, -1),
            new GridGameboard.Position(1, -2),
            new GridGameboard.Position(-2, -1),
            new GridGameboard.Position(-1, -2),
        };
        
        public override IEnumerable<GridGameboard.Position> GetValidMoves(GridGameboard.PlayerColor color, GridGameboard.Position position,
            GameState gameState) => KnightMoves
            .Select(candidate => position + candidate)
            .Where(candidate => TestMove(color, candidate, gameState) != MoveTestResult.Invalid);
    }
}