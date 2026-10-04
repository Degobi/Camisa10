using System;
using System.Collections.Generic;

namespace Camisa10.Core
{
    /// <summary>Aleatoriedade centralizada (sem dependência da Unity, testável).</summary>
    public static class Rng
    {
        static Random r = new Random();
        public static void Seed(int seed) { r = new Random(seed); }
        public static double Value => r.NextDouble();
        public static int RangeInt(int minInclusive, int maxInclusive) => r.Next(minInclusive, maxInclusive + 1);
        public static double RangeF(double a, double b) => a + r.NextDouble() * (b - a);
        public static bool Chance(double p) => r.NextDouble() < p;
        public static T Pick<T>(IList<T> list) => list[r.Next(list.Count)];

        public static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = r.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>Normal padrão (Box-Muller).</summary>
        public static double Gauss()
        {
            double u1 = 1.0 - r.NextDouble(), u2 = r.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        public static int Poisson(double lambda)
        {
            double limit = Math.Exp(-lambda), p = 1;
            int k = 0;
            do { k++; p *= r.NextDouble(); } while (p > limit);
            return k - 1;
        }

        public static T Weighted<T>(IList<(T item, double weight)> list)
        {
            double total = 0;
            foreach (var x in list) total += x.weight;
            double v = r.NextDouble() * total;
            foreach (var x in list)
            {
                v -= x.weight;
                if (v <= 0) return x.item;
            }
            return list[0].item;
        }
    }
}
