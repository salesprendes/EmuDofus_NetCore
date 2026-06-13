using System;

namespace Protocolo.Framework.Utils
{
    public sealed class FastRandom
    {
        private const double RealUnitInt = 1.0 / (int.MaxValue + 1.0);
        private const double RealUnitUInt = 1.0 / (uint.MaxValue + 1.0);
        private const uint SeedY = 842502087;
        private const uint SeedZ = 3579807591;
        private const uint SeedW = 273326509;
        private const uint IntMask = 0x7FFFFFFF;

        private uint x;
        private uint y;
        private uint z;
        private uint w;
        private uint bitBuffer;
        private uint bitMask = 1;

        public FastRandom() : this(Environment.TickCount) {}

        public FastRandom(int seed) => Reinitialise(seed);

        public void Reinitialise(int seed)
        {
            x = (uint)seed;
            y = SeedY;
            z = SeedZ;
            w = SeedW;
            bitBuffer = 0;
            bitMask = 1;
        }

        public int Next()
        {
            var value = NextInt31();
            while (value == int.MaxValue)
                value = NextInt31();

            return value;
        }

        public int Next(int upperBound)
        {
            if (upperBound < 0)
                throw new ArgumentOutOfRangeException("upperBound", upperBound, "upperBound must be >=0");

            return upperBound <= 1 ? 0 : (int)(Sample() * upperBound);
        }

        public int Next(int lowerBound, int upperBound)
        {
            if (lowerBound > upperBound)
                throw new ArgumentOutOfRangeException("upperBound", upperBound, "upperBound must be >=lowerBound");

            if (lowerBound == upperBound)
                return lowerBound;

            int range = upperBound - lowerBound;
            if (range < 0)
                return lowerBound + (int)(SampleLarge() * ((long)upperBound - lowerBound));

            return lowerBound + (int)(Sample() * range);
        }

        public double NextDouble() => Sample();

        public void NextBytes(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            uint x = this.x;
            uint y = this.y;
            uint z = this.z;
            uint w = this.w;
            int i = 0;
            int bound = buffer.Length - 4;

            while (i <= bound)
            {
                var value = AdvanceState(ref x, ref y, ref z, ref w);
                buffer[i++] = (byte)value;
                buffer[i++] = (byte)(value >> 8);
                buffer[i++] = (byte)(value >> 16);
                buffer[i++] = (byte)(value >> 24);
            }

            if (i < buffer.Length)
            {
                var value = AdvanceState(ref x, ref y, ref z, ref w);
                while (i < buffer.Length)
                {
                    buffer[i++] = (byte)value;
                    value >>= 8;
                }
            }

            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public uint NextUInt() => Advance();

        public int NextInt() => NextInt31();

        public bool NextBool()
        {
            if (bitMask == 1)
            {
                bitBuffer = Advance();
                bitMask = 0x80000000;
            }

            return (bitBuffer & (bitMask >>= 1)) == 0;
        }

        private int NextInt31() => (int)(Advance() & IntMask);

        private double Sample() => NextInt31() * RealUnitInt;

        private double SampleLarge() => Advance() * RealUnitUInt;

        private uint Advance() => AdvanceState(ref x, ref y, ref z, ref w);

        private static uint AdvanceState(ref uint x, ref uint y, ref uint z, ref uint w)
        {
            uint shifted = x ^ (x << 11);
            uint next = (w ^ (w >> 19)) ^ (shifted ^ (shifted >> 8));

            x = y;
            y = z;
            z = w;
            w = next;
            return next;
        }
    }
}
