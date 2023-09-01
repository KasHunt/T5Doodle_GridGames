using System.Collections.Generic;
using System.Linq;

namespace Code.Scripts.Reversi
{
    public class GameState
    {
        public int PlacedCount { get; private set; }

        public HashSet<GridGameboard.Position> UnPlayedPositions { get; } = new();

        public IEnumerable<ReversiPiece> PlacedPieces => _grid.Values.Select(value => value.piece);

        public (int blackScore, int whiteScore) GetScores() =>
            _grid.Values.Aggregate((black: 0, white: 0), (count, next) => (
                count.black + (next.color == GridGameboard.PlayerColor.Black ? 1 : 0), 
                count.white + (next.color == GridGameboard.PlayerColor.White ? 1 : 0)
            ));

        private readonly Dictionary<GridGameboard.Position, (ReversiPiece piece, GridGameboard.PlayerColor color)> _grid = new();
        
        public static GameState CreateNew()
        {
            var gameState = new GameState();
            gameState.FillUnPlayed();
            return gameState;
        }

        private void FillUnPlayed() =>
            GridGameboard.Position.GetRange(0, 8, 0, 8)
                .ToList()
                .ForEach(position => UnPlayedPositions.Add(position));
        
        public GridGameboard.PlayerColor ColorAt(GridGameboard.Position position) =>
            !_grid.TryGetValue(position, out var pieceColor) ? GridGameboard.PlayerColor.None : pieceColor.color;

        public bool TileEnabled(GridGameboard.Position position) => 
            PlacedCount >= 4 || (position.Column is 3 or 4 && position.Row is 3 or 4);
        
        public void Place(GridGameboard.Position position, ReversiPiece piece, GridGameboard.PlayerColor color)
        {
            _grid[position] = (piece, color);
            PlacedCount++;
        }

        public ReversiPiece FlipTo(GridGameboard.Position position, GridGameboard.PlayerColor color)
        {
            var element = _grid[position];
            element.color = color;
            _grid[position] = element;
            return element.piece;
        }
    }
}