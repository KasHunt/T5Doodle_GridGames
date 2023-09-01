namespace Code.Scripts.Chess
{
    public struct Move
    {
        public enum MoveType
        {
            Normal,
            QueenSideCastle,
            KingSideCastle,
            EnPassant,
            PawnDouble,
            EndGame,
        };
        
        public Move(ChessPiece piece, GridGameboard.Position fromPosition, GridGameboard.Position toPosition, ChessPiece captured, MoveType type)
        {
            FromPosition = fromPosition;
            ToPosition = toPosition;
            Captured = captured;
            Type = type;
            Piece = piece;
        }
        
        public readonly ChessPiece Piece;
        public readonly GridGameboard.Position FromPosition;
        public readonly GridGameboard.Position ToPosition;
        public readonly ChessPiece Captured;
        public readonly MoveType Type;
    }
}