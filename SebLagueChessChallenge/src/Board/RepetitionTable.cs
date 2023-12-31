﻿using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ChessChallenge.Chess
{
    public struct RepetitionTable
    {
        readonly ulong[] hashes;
        readonly int[] startIndices;
        int count;

        public RepetitionTable()
        {
            const int size = 1024;
            hashes = new ulong[size - 1];
            startIndices = new int[size];
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Init(Board board)
        {
            ulong[] initialHashes = board.RepetitionPositionHistory.Reverse().ToArray();
            count = initialHashes.Length;

            for (int i = 0; i < initialHashes.Length; i++)
            {
                hashes[i] = initialHashes[i];
                startIndices[i] = 0;
            }
            startIndices[count] = 0;
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(ulong hash, bool reset)
        {
            // Check bounds just in case
            if (count < hashes.Length)
            {
                hashes[count] = hash;
                startIndices[count + 1] = reset ? count : startIndices[count];
                count++;
            }
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryPop()
        {
            count = Math.Max(0, count - 1);
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(ulong h)
        {
            int s = startIndices[count];
            // up to count-1 so that curr position is not counted
            for (int i = s; i < count - 1; i++)
            {
                if (hashes[i] == h)
                {
                    return true;
                }
            }
            return false;
        }
    }
}