using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Protocolo.Framework.Utils
{
    public sealed class FastRandom
    {
        private const double UIntToDouble = 1.0 / (uint.MaxValue + 1.0);
        private const uint IntMask = 0x7FFFFFFF;
        private const ulong SplitMixIncrement = 0x9E3779B97F4A7C15UL;
        private const ulong SplitMixMultiplier1 = 0xBF58476D1CE4E5B9UL;
        private const ulong SplitMixMultiplier2 = 0x94D049BB133111EBUL;
        private const int SeedIncrement = unchecked((int)0x9E3779B9);

        private static int s_seedCounter = Environment.TickCount;

        [ThreadStatic]
        private static FastRandom s_shared;

        private uint m_state0;
        private uint m_state1;
        private uint m_state2;
        private uint m_state3;
        private uint m_bitBuffer;
        private int m_bitsRemaining;

        public static FastRandom Shared => s_shared ??= new FastRandom(CreateSeed());

        public FastRandom() : this(CreateSeed()) {}

        public FastRandom(int seed)
        {
            Reinitialise(seed);
        }

        public void Reinitialise(int seed)
        {
            var seedState = unchecked((ulong)(uint)seed) + SplitMixIncrement;
            var first = SplitMix64(ref seedState);
            var second = SplitMix64(ref seedState);

            m_state0 = (uint)first;
            m_state1 = (uint)(first >> 32);
            m_state2 = (uint)second;
            m_state3 = (uint)(second >> 32);

            if ((m_state0 | m_state1 | m_state2 | m_state3) == 0)
                m_state3 = 1;

            m_bitBuffer = 0;
            m_bitsRemaining = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Next()
        {
            var value = (int)(Advance() & IntMask);
            while (value == int.MaxValue)
                value = (int)(Advance() & IntMask);

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Next(int upperBound)
        {
            if (upperBound < 0)
                throw new ArgumentOutOfRangeException(nameof(upperBound), upperBound, "upperBound must be >= 0");

            return upperBound <= 1 ? 0 : (int)NextUIntBounded((uint)upperBound);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Next(int lowerBound, int upperBound)
        {
            if (lowerBound > upperBound)
                throw new ArgumentOutOfRangeException(nameof(upperBound), upperBound, "upperBound must be >= lowerBound");

            if (lowerBound == upperBound)
                return lowerBound;

            var range = (uint)((long)upperBound - lowerBound);
            return (int)(lowerBound + (long)NextUIntBounded(range));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double NextDouble()
        {
            return Advance() * UIntToDouble;
        }

        public void NextBytes(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            NextBytes(buffer.AsSpan());
        }

        public void NextBytes(Span<byte> buffer)
        {
            var state0 = m_state0;
            var state1 = m_state1;
            var state2 = m_state2;
            var state3 = m_state3;
            var offset = 0;
            var completeBytes = buffer.Length & ~3;

            while (offset < completeBytes)
            {
                var value = AdvanceState(ref state0, ref state1, ref state2, ref state3);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(offset, sizeof(uint)), value);
                offset += sizeof(uint);
            }

            if (offset < buffer.Length)
            {
                var value = AdvanceState(ref state0, ref state1, ref state2, ref state3);
                while (offset < buffer.Length)
                {
                    buffer[offset++] = (byte)value;
                    value >>= 8;
                }
            }

            m_state0 = state0;
            m_state1 = state1;
            m_state2 = state2;
            m_state3 = state3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint NextUInt()
        {
            return Advance();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NextInt()
        {
            return (int)(Advance() & IntMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool NextBool()
        {
            if (m_bitsRemaining == 0)
            {
                m_bitBuffer = Advance();
                m_bitsRemaining = sizeof(uint) * 8;
            }

            var result = (m_bitBuffer & 1) != 0;
            m_bitBuffer >>= 1;
            m_bitsRemaining--;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint NextUIntBounded(uint upperBound)
        {
            var product = (ulong)Advance() * upperBound;
            var low = (uint)product;

            if (low < upperBound)
            {
                var threshold = unchecked(0u - upperBound) % upperBound;
                while (low < threshold)
                {
                    product = (ulong)Advance() * upperBound;
                    low = (uint)product;
                }
            }

            return (uint)(product >> 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint Advance()
        {
            return AdvanceState(ref m_state0, ref m_state1, ref m_state2, ref m_state3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint AdvanceState(ref uint state0, ref uint state1, ref uint state2, ref uint state3)
        {
            var result = BitOperations.RotateLeft(state1 * 5, 7) * 9;
            var shifted = state1 << 9;

            state2 ^= state0;
            state3 ^= state1;
            state1 ^= state2;
            state0 ^= state3;
            state2 ^= shifted;
            state3 = BitOperations.RotateLeft(state3, 11);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong SplitMix64(ref ulong state)
        {
            var value = state += SplitMixIncrement;
            value = (value ^ (value >> 30)) * SplitMixMultiplier1;
            value = (value ^ (value >> 27)) * SplitMixMultiplier2;
            return value ^ (value >> 31);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CreateSeed()
        {
            return Interlocked.Add(ref s_seedCounter, SeedIncrement);
        }
    }
}
