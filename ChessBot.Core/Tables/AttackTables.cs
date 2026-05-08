namespace ChessBot.Core.Tables;

public static class AttackTables
{
    public static readonly ulong[] KnightAttacks;
    public static readonly ulong[] KingAttacks;
    public static readonly ulong[,] PawnAttacks;

    static AttackTables()
    {
        KnightAttacks = Tables.KnightAttacks.InitTable();
        KingAttacks = Tables.KingAttacks.InitTable();
        PawnAttacks = Tables.PawnAttacks.InitTable();
    }
}