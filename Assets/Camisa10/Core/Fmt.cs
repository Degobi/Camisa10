using System;
using System.Globalization;

namespace Camisa10.Core
{
    /// <summary>Formatação no padrão brasileiro, independente da cultura do aparelho.</summary>
    public static class Fmt
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Money(double v)
        {
            string s = v < 0 ? "-" : "";
            v = Math.Abs(v);
            if (v >= 1e9) return s + "R$ " + (v / 1e9).ToString("0.0", Inv).Replace('.', ',') + " bi";
            if (v >= 1e6) return s + "R$ " + (v / 1e6).ToString("0.0", Inv).Replace('.', ',') + " mi";
            if (v >= 1e4) return s + "R$ " + Math.Round(v / 1e3).ToString("0", Inv) + " mil";
            return s + "R$ " + ((long)Math.Round(v)).ToString("#,0", Inv).Replace(',', '.');
        }

        /// <summary>Preço em centavos, sempre com duas casas: R$ 18,50.</summary>
        public static string Cents(long cents)
        {
            string s = cents < 0 ? "-" : "";
            cents = Math.Abs(cents);
            return s + "R$ " + (cents / 100).ToString("#,0", Inv).Replace(',', '.') + "," + (cents % 100).ToString("00", Inv);
        }

        public static string Pct(double v) => (v >= 0 ? "+" : "") + v.ToString("0.0", Inv).Replace('.', ',') + "%";

        public static string Rating(double r) => r.ToString("0.0", Inv).Replace('.', ',');
    }
}
