using System.Collections.Generic;
using System.Linq;

namespace Code.Scripts.Chess
{
    public class Bishop : ChessPieceSpecialization
    {
        public override IEnumerable<GridGameboard.Position> GetValidMoves(GridGameboard.PlayerColor color, 
            GridGameboard.Position position, GameState gameState) => GetDiagonalMoves(color, position, gameState);
    }
    
    public class Rook : ChessPieceSpecialization
    {
        public override IEnumerable<GridGameboard.Position> GetValidMoves(GridGameboard.PlayerColor color, 
            GridGameboard.Position position, GameState gameState) => GetStraightMoves(color, position, gameState);
    }
    
    public class Queen : ChessPieceSpecialization
    {
        public override IEnumerable<GridGameboard.Position> GetValidMoves(GridGameboard.PlayerColor color, 
            GridGameboard.Position position, GameState gameState) =>
            GetDiagonalMoves(color, position, gameState).Concat(GetStraightMoves(color, position, gameState));
    }
}