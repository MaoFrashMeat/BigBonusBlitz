using System;

namespace BBB.Core
{
    public interface IRandom
    {
        /// <summary>0 以上 max 未満の整数。</summary>
        int Next(int max);
        /// <summary>0.0 以上 1.0 未満。</summary>
        double NextDouble();
    }

    public sealed class SystemRandom : IRandom
    {
        private readonly Random _r;
        public SystemRandom() { _r = new Random(); }
        public SystemRandom(int seed) { _r = new Random(seed); }
        public int Next(int max) => _r.Next(max);
        public double NextDouble() => _r.NextDouble();
    }
}
