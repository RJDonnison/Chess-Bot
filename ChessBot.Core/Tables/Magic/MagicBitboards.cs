using System.Diagnostics;
using System.Numerics;

namespace ChessBot.Core.Tables;
using static PrecomputedMagics;

public static class MagicBitboards
{
    private static readonly ulong[][] RookTables;
    private static readonly ulong[][] BishopTables;

    private static readonly ulong[] RookMasks;
    private static readonly ulong[] BishopMasks;

    public static ulong GetRookMoves(int sq, ulong occupied)
    {
        ulong key = ((occupied & RookMasks[sq]) * RookMagics[sq]) >> RookShifts[sq];
        return RookTables[sq][key];
    }

    public static ulong GetBishopMoves(int sq, ulong occupied)
    {
        ulong key = ((occupied & BishopMasks[sq]) * BishopMagics[sq]) >> BishopShifts[sq];
        return BishopTables[sq][key];
    }

    public static ulong GetQueenMoves(int sq, ulong occupied) => GetRookMoves(sq, occupied) | GetBishopMoves(sq, occupied);

    static MagicBitboards()
    {
        RookMasks = new ulong[64];
        BishopMasks = new ulong[64];

        for (int squareIndex = 0; squareIndex < 64; squareIndex++)
        {
            RookMasks[squareIndex] = ComputeRookMask(squareIndex);
            BishopMasks[squareIndex] = ComputeBishopMask(squareIndex);
        }

        RookTables = new ulong[64][];
        BishopTables = new ulong[64][];

        for (int i = 0; i < 64; i++)
        {
            RookTables[i] = CreateTable(i, true, RookMagics[i], RookShifts[i]);
            BishopTables[i] = CreateTable(i, false, BishopMagics[i], BishopShifts[i]);
        }
    }

    private static ulong ComputeRookMask(int sq)
    {
        int f = sq % 8, r = sq / 8;
        ulong mask = 0;

        for (int i = f + 1; i < 7; i++) mask |= 1UL << (r * 8 + i); // east
        for (int i = f - 1; i > 0; i--) mask |= 1UL << (r * 8 + i); // west
        for (int i = r + 1; i < 7; i++) mask |= 1UL << (i * 8 + f); // north
        for (int i = r - 1; i > 0; i--) mask |= 1UL << (i * 8 + f); // south
        return mask;
    }

    private static ulong ComputeBishopMask(int sq)
    {
        int f = sq % 8, r = sq / 8;
        ulong mask = 0;

        for (int i = 1; f + i < 7 && r + i < 7; i++) mask |= 1UL << ((r + i) * 8 + (f + i));
        for (int i = 1; f - i > 0 && r + i < 7; i++) mask |= 1UL << ((r + i) * 8 + (f - i));
        for (int i = 1; f + i < 7 && r - i > 0; i++) mask |= 1UL << ((r - i) * 8 + (f + i));
        for (int i = 1; f - i > 0 && r - i > 0; i++) mask |= 1UL << ((r - i) * 8 + (f - i));
        return mask;
    }
    
    private static ulong[] CreateTable(int square, bool isRook, ulong magic, int leftShift)
    {
        int numBits = 64 - leftShift;
        int lookupSize = 1 << numBits;
        ulong[] table = new ulong[lookupSize];

        ulong movementMask = isRook ? ComputeRookMask(square) : ComputeBishopMask(square);
        ulong[] blockerPatterns = CreateAllBlockerBitboards(movementMask);

        foreach (ulong pattern in blockerPatterns)
        {
            ulong index = (pattern * magic) >> leftShift;
            ulong moves = LegalMoveBitboardFromBlockers(square, pattern, isRook);
            table[index] = moves;
        }

        return table;
    }
    
    public static ulong[] CreateAllBlockerBitboards(ulong movementMask)
    {
        List<int> moveSquareIndices = new();
        for (int i = 0; i < 64; i++)
        {
            if (((movementMask >> i) & 1) == 1)
            {
                moveSquareIndices.Add(i);
            }
        }

        int numPatterns = 1 << moveSquareIndices.Count;
        ulong[] blockerBitboards = new ulong[numPatterns];

        for (int patternIndex = 0; patternIndex < numPatterns; patternIndex++)
        {
            for (int bitIndex = 0; bitIndex < moveSquareIndices.Count; bitIndex++)
            {
                int bit = (patternIndex >> bitIndex) & 1;
                blockerBitboards[patternIndex] |= (ulong)bit << moveSquareIndices[bitIndex];
            }
        }

        return blockerBitboards;
    }
    
    private static ulong LegalMoveBitboardFromBlockers(int square, ulong blockers, bool isRook)
    {
        ulong moves = 0;
        int f = square % 8;
        int r = square / 8;

        (int df, int dr)[] dirs = isRook
            ? [(1, 0), (-1, 0), (0, 1), (0, -1)]
            : [(1, 1), (-1, 1), (1, -1), (-1, -1)];

        foreach ((int df, int dr) in dirs)
        {
            int ff = f + df;
            int rr = r + dr;

            while (ff >= 0 && ff < 8 && rr >= 0 && rr < 8)
            {
                ulong bit = 1UL << (rr * 8 + ff);
                moves |= bit;

                if ((blockers & bit) != 0) break;

                ff += df;
                rr += dr;
            }
        }

        return moves;
    }
}