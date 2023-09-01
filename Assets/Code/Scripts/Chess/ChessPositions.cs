using System;

namespace Code.Scripts.Chess
{
    public static class ChessPositions
    {
        public const int QUEENS_ROOK_FILE = 0;
        public const int QUEENS_KNIGHT_FILE = 1;
        public const int QUEENS_BISHOP_FILE = 2;
        public const int QUEEN_FILE = 3;
        public const int KING_FILE = 4;
        public const int KINGS_BISHOP_FILE = 5;
        public const int KINGS_KNIGHT_FILE = 6;
        public const int KINGS_ROOK_FILE = 7;

        public const int WHITE_RANK = 0;
        public const int BLACK_RANK = 7;

        public static readonly GridGameboard.Position blackKing = new(BLACK_RANK, KING_FILE);
        public static readonly GridGameboard.Position whiteKing = new(WHITE_RANK, KING_FILE);

        public static GridGameboard.Position GetCastlePosition(GridGameboard.PlayerColor color, int rookFile) => (color, rookFile) switch
        {
            (GridGameboard.PlayerColor.Black, KINGS_ROOK_FILE) => new GridGameboard.Position(BLACK_RANK, KINGS_BISHOP_FILE),
            (GridGameboard.PlayerColor.Black, QUEENS_ROOK_FILE) => new GridGameboard.Position(BLACK_RANK, QUEEN_FILE),
            (GridGameboard.PlayerColor.White, KINGS_ROOK_FILE) => new GridGameboard.Position(WHITE_RANK, KINGS_BISHOP_FILE),
            (GridGameboard.PlayerColor.White, QUEENS_ROOK_FILE) => new GridGameboard.Position(WHITE_RANK, QUEEN_FILE),
            _ => throw new ArgumentOutOfRangeException()
        };

        public static int RankForColor(GridGameboard.PlayerColor color) => 
            color == GridGameboard.PlayerColor.Black ? BLACK_RANK : WHITE_RANK;
        public static int PawnRankForColor(GridGameboard.PlayerColor color) => 
            color == GridGameboard.PlayerColor.White ? WHITE_RANK + 1 : BLACK_RANK - 1;
    }
}