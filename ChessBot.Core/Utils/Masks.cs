namespace ChessBot.Core.Utils;

public class Masks
{
    public const ulong FileA = 0x0101010101010101UL;
    public const ulong FileB = 0x0202020202020202UL;
    public const ulong FileC = 0x0404040404040404UL;
    public const ulong FileD = 0x0808080808080808UL;
    public const ulong FileE = 0x1010101010101010UL;
    public const ulong FileF = 0x2020202020202020UL;
    public const ulong FileG = 0x4040404040404040UL;
    public const ulong FileH = 0x8080808080808080UL;

    public const ulong Rank1 = 0x00000000000000FFUL;
    public const ulong Rank2 = 0x000000000000FF00UL;
    public const ulong Rank3 = 0x0000000000FF0000UL;
    public const ulong Rank4 = 0x00000000FF000000UL;
    public const ulong Rank5 = 0x000000FF00000000UL;
    public const ulong Rank6 = 0x0000FF0000000000UL;
    public const ulong Rank7 = 0x00FF000000000000UL;
    public const ulong Rank8 = 0xFF00000000000000UL;

    public static readonly ulong[] FileMask = [FileA, FileB, FileC, FileD, FileE, FileF, FileG, FileH];
    public static readonly ulong[] RankMask = [Rank1, Rank2, Rank3, Rank4, Rank5, Rank6, Rank7, Rank8];
}