using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Paleta escura de menu de modo carreira: fundo azul-noite, painéis translúcidos,
    /// destaque verde neon e dourado para o geral do jogador.
    /// </summary>
    public static class Theme
    {
        public static Color Hex(string hex) { return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta; }

        // base
        public static readonly Color Bg = Hex("#070B17");
        public static readonly Color BgTop = Hex("#13203F");
        public static readonly Color Card = Hex("#121A30");      // painel / bloco
        public static readonly Color CardHi = Hex("#1A2442");    // painel em destaque
        public static readonly Color Chip = Hex("#1F2A48");      // controles e áreas internas
        public static readonly Color Line = Hex("#2C3858");
        public static readonly Color Bar = Hex("#0A1022");       // barras superior e de abas
        public static readonly Color Ink = Hex("#F2F5FA");
        public static readonly Color Muted = Hex("#8E9AB5");

        // destaques
        public static readonly Color Turf = Hex("#19E68C");      // verde neon (ação principal, aba ativa)
        public static readonly Color TurfInk = Hex("#03140B");
        public static readonly Color Cyan = Hex("#2BD9FE");
        public static readonly Color Gold = Hex("#F5C542");
        public static readonly Color GoldInk = Hex("#221800");
        public static readonly Color Red = Hex("#FF5468");
        public static readonly Color Good = Hex("#19E68C");
        public static readonly Color Purple = Hex("#9B6BFF");
        public static readonly Color Silver = Hex("#C9D1DB");     // pódio
        public static readonly Color Bronze = Hex("#C98B5A");
        public static readonly Color ShirtGreen = Hex("#17502F");

        // partida
        public static readonly Color Pitch = Hex("#081022");
        public static readonly Color Dock = Hex("#121A30");
        public static readonly Color FeedText = Hex("#B9C4DA");
        public static readonly Color FeedGold = Hex("#FFD86B");
        public static readonly Color FeedRed = Hex("#FF9DA3");
        public static readonly Color ChanceHigh = Hex("#1C5A3E");
        public static readonly Color ChanceMid = Hex("#2A3456");
        public static readonly Color ChanceLow = Hex("#5E2433");

        public static Color Alpha(Color c, float a) { c.a = a; return c; }
    }
}
