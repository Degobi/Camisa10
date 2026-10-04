using System;
using System.Collections.Generic;

namespace Camisa10.UI.Art
{
    /// <summary>Como desenhar o escudo de um clube quando não há imagem oficial em Resources/Escudos.</summary>
    public class CrestDesign
    {
        public string Shape = "shield";   // shield, badge, round, triangle
        public string Pattern = "solid";  // solid, vstripes, hoops, halves, sash, cross, chief, barca
        public string[] Cols;             // cores do campo (chief: a primeira é a faixa de cima)
        public int Count = 5;             // listras
        public string Ring = "#D8DEE4";   // borda
        public string Emblem = "";        // star, stars5, crown, cannon, hammers, maltese, ball, star-top
        public string EmblemCol = "#FFFFFF";
        public string Text = "", TextCol = "#FFFFFF";
        public float TextY = .5f, TextSize = .22f; // posição (0 = topo) e altura relativa do texto
    }

    public static class CrestArt
    {
        public const float DW = 100, DH = 120; // espaço de design

        static CrestDesign D(string shape, string pattern, string cols, string ring, string text = "", string textCol = "#FFFFFF",
            float textY = .5f, float textSize = .22f, string emblem = "", string emblemCol = "#FFFFFF", int count = 5) => new CrestDesign
        {
            Shape = shape, Pattern = pattern, Cols = cols.Split(','), Ring = ring, Text = text, TextCol = textCol,
            TextY = textY, TextSize = textSize, Emblem = emblem, EmblemCol = emblemCol, Count = count,
        };

        const string Gold = "#D4AF37", Silver = "#D8DEE4";

        // Releitura simplificada da identidade de cada clube (cores, listras, símbolos).
        public static readonly Dictionary<string, CrestDesign> Designs = new Dictionary<string, CrestDesign>
        {
            // Brasileirão
            ["Flamengo"] = D("shield", "hoops", "#C8102E,#111111", "#111111", "CRF", "#FFFFFF", .2f, .2f, count: 7),
            ["Palmeiras"] = D("round", "solid", "#006437", "#F2F2F2", "P", "#FFFFFF", .52f, .5f, "star-top", "#FFFFFF"),
            ["Botafogo"] = D("shield", "solid", "#111111", "#F2F2F2", "", emblem: "star", emblemCol: "#FFFFFF"),
            ["Cruzeiro"] = D("round", "solid", "#003DA5", "#F2F2F2", "", emblem: "stars5", emblemCol: "#FFFFFF"),
            ["Fluminense"] = D("badge", "chief", "#F4F4F4,#00613C,#FFFFFF,#7A0026,#FFFFFF,#00613C", "#7A0026", "FFC", "#7A0026", .17f, .2f),
            ["São Paulo"] = D("triangle", "chief", "#111111,#C8102E,#FFFFFF,#111111", "#111111", "SPFC", "#FFFFFF", .15f, .18f, count: 3),
            ["Corinthians"] = D("round", "solid", "#FFFFFF", "#111111", "SCCP", "#111111", .5f, .2f, "anchor", "#111111"),
            ["Bahia"] = D("shield", "vstripes", "#0057B8,#FFFFFF,#E30613,#FFFFFF,#0057B8", "#0057B8", "ECB", "#0057B8", .5f, .22f, "star-top", "#E30613"),
            ["Vasco da Gama"] = D("shield", "sash", "#111111,#FFFFFF", "#F2F2F2", "", emblem: "maltese", emblemCol: "#D7141A"),
            ["Grêmio"] = D("badge", "vstripes", "#0D80BF,#111111,#FFFFFF,#0D80BF,#111111,#FFFFFF,#0D80BF", "#111111", "GFBPA", "#FFFFFF", .5f, .17f, "star-top", Gold, 7),
            // La Liga
            ["Real Madrid"] = D("round", "sash", "#FFFFFF,#3D3B8E", Gold, "MCF", Gold, .5f, .26f, "crown", Gold),
            ["Barcelona"] = D("shield", "barca", "#004D98,#A50044", Gold, "FCB", "#111111", .43f, .12f),
            ["Atlético de Madrid"] = D("shield", "chief", "#272E61,#CB3524,#FFFFFF", Silver, "ATM", "#FFFFFF", .15f, .16f, count: 5),
            ["Athletic Bilbao"] = D("shield", "vstripes", "#EE2523,#FFFFFF", "#111111", "AC", "#111111", .5f, .26f, count: 7),
            ["Villarreal"] = D("round", "solid", "#FFE667", "#005187", "VCF", "#005187", .5f, .3f),
            ["Real Betis"] = D("shield", "vstripes", "#00954C,#FFFFFF", Gold, "RB", "#111111", .5f, .28f, "crown", Gold, 7),
            ["Real Sociedad"] = D("round", "vstripes", "#0067B1,#FFFFFF", "#0067B1", "RS", "#FFFFFF", .5f, .3f, "crown", Gold, 5),
            ["Sevilla"] = D("shield", "chief", "#FFFFFF,#D71920,#FFFFFF", "#D71920", "SFC", "#D71920", .18f, .2f, count: 5),
            ["Valencia"] = D("round", "vstripes", "#F7C600,#D71920", "#111111", "VCF", "#111111", .5f, .28f, "bat", "#111111", 9),
            ["Celta de Vigo"] = D("shield", "solid", "#8AC3EE", "#D71920", "", emblem: "cross", emblemCol: "#D71920"),
            // Premier League
            ["Liverpool"] = D("badge", "solid", "#C8102E", Gold, "LFC", "#F5D58A", .72f, .2f, "bird", "#F5D58A"),
            ["Manchester City"] = D("round", "solid", "#6CABDD", "#1C2C5B", "MCFC", "#1C2C5B", .74f, .16f, "ship", "#F2C230"),
            ["Arsenal"] = D("badge", "solid", "#EF0107", Gold, "", emblem: "cannon", emblemCol: Gold),
            ["Chelsea"] = D("round", "solid", "#034694", Gold, "CFC", "#FFFFFF", .76f, .16f, "lion", Gold),
            ["Manchester United"] = D("badge", "solid", "#DA291C", "#FBE122", "MU", "#FBE122", .74f, .18f, "devil", "#FBE122"),
            ["Newcastle"] = D("round", "vstripes", "#111111,#FFFFFF", "#111111", "NUFC", "#F2C230", .5f, .2f, count: 7),
            ["Tottenham"] = D("badge", "solid", "#FFFFFF", "#132257", "THFC", "#132257", .78f, .15f, "ball", "#132257"),
            ["Aston Villa"] = D("round", "solid", "#670E36", "#95BFE5", "AVFC", "#FBE122", .76f, .16f, "lion", "#FBE122"),
            ["Brighton"] = D("round", "solid", "#FFFFFF", "#0057B8", "BHAFC", "#0057B8", .74f, .14f, "bird", "#0057B8"),
            ["Seleção"] = D("shield", "solid", "#FFDF00", "#009C3B", "BRA", "#002776", .56f, .26f, "star-top", "#009C3B"),
            ["West Ham"] = D("badge", "solid", "#7A263A", "#1BB1E7", "", emblem: "hammers", emblemCol: "#F2C230"),
        };

        public static CrestDesign For(string club, string c1, string c2)
        {
            if (club != null && Designs.TryGetValue(club, out var d)) return d;
            // clube sem desenho próprio: escudo dividido com as cores e as iniciais
            return D("shield", "halves", c1 + "," + c2, Silver, Initials(club), Rgba.Hex(c1).Luma > .6f ? "#111111" : "#FFFFFF", .5f, .26f);
        }

        public static string Initials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (var w in name.Split(' '))
                if (w.Length > 2) sb.Append(char.ToUpperInvariant(w[0]));
            return sb.Length > 0 ? sb.ToString() : name.Substring(0, 1).ToUpperInvariant();
        }

        public static Shape Outline(string shape)
        {
            switch (shape)
            {
                case "round": return Shape.Ellipse(50, 62, 46, 46, 120);
                case "badge":
                    return Shape.M(8, 6).L(92, 6).L(92, 62).C(92, 90, 74, 106, 50, 116).C(26, 106, 8, 90, 8, 62);
                case "triangle":
                    return Shape.M(6, 6).L(94, 6).C(92, 40, 74, 84, 50, 116).C(26, 84, 8, 40, 6, 6);
                default:
                    return Shape.M(6, 12).C(22, 12, 38, 10, 50, 3).C(62, 10, 78, 12, 94, 12).L(94, 56)
                        .C(94, 86, 76, 104, 50, 116).C(24, 104, 6, 86, 6, 56);
            }
        }

        public static Raster Render(CrestDesign d, int w, int h)
        {
            float scale = Math.Min(w / DW, h / DH);
            var r = new Raster(w, h, scale);
            var outer = Outline(d.Shape);
            bool crown = d.Emblem == "crown";
            if (crown) outer = outer.Scaled(50, 116, .86f, .86f); // abre espaço para a coroa em cima
            outer.Bounds(out float ox0, out float oy0, out float ox1, out float oy1);
            float cx = (ox0 + ox1) / 2, cy = (oy0 + oy1) / 2;

            var ring = Rgba.Hex(d.Ring);
            var dark = new Rgba(0, 0, 0, .55f);

            // sombra
            r.Fill(outer.Moved(1.2f, 2.2f), dark.WithA(.35f));
            // borda metálica ou na cor do clube
            r.Fill(outer, (x, y) => Metal(ring, x, y, oy0, oy1));
            // filete interno escuro
            r.Fill(outer.Scaled(cx, cy, .9f, .915f), ring.Mul(.45f));
            // campo
            var field = outer.Scaled(cx, cy, .86f, .88f);
            field.Bounds(out float fx0, out float fy0, out float fx1, out float fy1);
            var cols = Array.ConvertAll(d.Cols, Rgba.Hex);
            r.Fill(field, (x, y) =>
            {
                float u = (x - fx0) / (fx1 - fx0), v = (y - fy0) / (fy1 - fy0);
                var c = PatternAt(d, cols, u, v);
                float light = 1.08f - .26f * v;
                c = c.Mul(light);
                // brilho de verniz no canto superior esquerdo
                float du = u - .3f, dv = v - .18f, dist = (float)Math.Sqrt(du * du + dv * dv * 1.6f);
                float gloss = .2f * Raster.Smooth(.55f, 0, dist);
                return c.Lerp(new Rgba(1, 1, 1), gloss);
            });

            Emblem(r, d, (fx0 + fx1) / 2, fy0, fy1, fx1 - fx0);
            if (crown) Crown(r, 50, oy0 - 1, Rgba.Hex(d.EmblemCol));
            return r;
        }

        static Rgba Metal(Rgba baseCol, float x, float y, float y0, float y1)
        {
            float v = (y - y0) / (y1 - y0);
            float k = .82f + .38f * (float)Math.Cos(v * 5.2f + x * .03f) * .5f + .18f * (1 - v);
            return baseCol.Mul(k);
        }

        static Rgba PatternAt(CrestDesign d, Rgba[] c, float u, float v)
        {
            int n = Math.Max(1, d.Count);
            switch (d.Pattern)
            {
                case "vstripes": return c[Math.Min(n - 1, (int)(u * n)) % c.Length];
                case "hoops": return c[Math.Min(n - 1, (int)(v * n)) % c.Length];
                case "halves": return u < .5f ? c[0] : c[c.Length > 1 ? 1 : 0];
                case "sash": return Math.Abs(u - v * 1.05f) < .16f ? c[1] : c[0];
                case "chief":
                {
                    if (v < .3f) return c[0];
                    if (c.Length == 2) return c[1];
                    int k = Math.Min(n - 1, (int)(u * n));
                    return c[1 + k % (c.Length - 1)];
                }
                case "barca":
                {
                    if (v < .37f)
                    {
                        if (u < .5f) // cruz de São Jorge
                            return Math.Abs(u - .25f) < .06f || Math.Abs(v - .19f) < .06f ? Rgba.Hex("#D7141A") : Rgba.Hex("#FFFFFF");
                        int s = (int)((u - .5f) / .5f * 9); // senyera
                        return s % 2 == 1 ? Rgba.Hex("#D7141A") : Rgba.Hex("#F7C600");
                    }
                    if (v < .49f) return Rgba.Hex("#F3E3B0");
                    return c[(int)(u * 5) % 2];
                }
                default: return c[0];
            }
        }

        static void Emblem(Raster r, CrestDesign d, float cx, float fy0, float fy1, float fw)
        {
            var col = Rgba.Hex(d.EmblemCol);
            var shadow = new Rgba(0, 0, 0, .35f);
            float mid = (fy0 + fy1) / 2, fh = fy1 - fy0;
            Func<float, float, Rgba> lit = (x, y) => col.Mul(1.12f - .3f * (y - fy0) / fh);
            void Draw(Shape s) { r.Fill(s.Moved(.8f, 1.2f), shadow); r.Fill(s, lit); }

            switch (d.Emblem)
            {
                case "star": Draw(Shape.Star(cx, mid + 2, fw * .3f)); break;
                case "star-top": Draw(Shape.Star(cx, fy0 + fh * .12f, fw * .08f)); break;
                case "stars5":
                    Draw(Shape.Star(cx, mid - fh * .26f, fw * .1f));
                    Draw(Shape.Star(cx - fw * .2f, mid - fh * .02f, fw * .1f));
                    Draw(Shape.Star(cx + fw * .22f, mid - fh * .06f, fw * .1f));
                    Draw(Shape.Star(cx + fw * .02f, mid + fh * .28f, fw * .11f));
                    Draw(Shape.Star(cx + fw * .1f, mid + fh * .06f, fw * .06f));
                    break;
                case "maltese":
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 90;
                        double t = a * Math.PI / 180;
                        float dx = (float)Math.Cos(t), dy = (float)Math.Sin(t), px = -dy, py = dx;
                        float s1 = fw * .05f, s2 = fw * .24f, wi = fw * .03f, wo = fw * .14f;
                        Draw(Shape.M(cx + dx * s1 + px * wi, mid + dy * s1 + py * wi).L(cx + dx * s2 + px * wo, mid + dy * s2 + py * wo)
                            .L(cx + dx * (s2 - fw * .05f), mid + dy * (s2 - fw * .05f))
                            .L(cx + dx * s2 - px * wo, mid + dy * s2 - py * wo).L(cx + dx * s1 - px * wi, mid + dy * s1 - py * wi));
                    }
                    break;
                case "cross":
                    Draw(Shape.Box(cx, mid - fh * .08f, fw * .14f, fh * .62f));
                    Draw(Shape.Box(cx, mid - fh * .18f, fw * .6f, fh * .1f));
                    break;
                case "cannon":
                {
                    float y = mid + fh * .05f;
                    Draw(Shape.M(cx - fw * .36f, y - fh * .06f).L(cx + fw * .3f, y - fh * .1f).L(cx + fw * .34f, y - fh * .13f)
                        .L(cx + fw * .38f, y - fh * .08f).L(cx + fw * .3f, y - fh * .03f).L(cx - fw * .36f, y + fh * .06f));
                    Draw(Shape.Ellipse(cx - fw * .1f, y + fh * .12f, fw * .1f, fw * .1f));
                    Draw(Shape.Box(cx - fw * .18f, y + fh * .2f, fw * .34f, fh * .03f));
                    break;
                }
                case "hammers":
                    Draw(Shape.Box(cx, mid, fw * .07f, fh * .62f, 35));
                    Draw(Shape.Box(cx, mid, fw * .07f, fh * .62f, -35));
                    Draw(Shape.Box(cx - fw * .17f, mid - fh * .25f, fw * .24f, fh * .09f, -55));
                    Draw(Shape.Box(cx + fw * .17f, mid - fh * .25f, fw * .24f, fh * .09f, 55));
                    break;
                case "ball":
                    Draw(Shape.Ellipse(cx, mid + fh * .08f, fw * .17f, fw * .17f));
                    Draw(Shape.M(cx - fw * .08f, mid - fh * .06f).C(cx - fw * .02f, mid - fh * .3f, cx + fw * .12f, mid - fh * .34f, cx + fw * .16f, mid - fh * .3f)
                        .L(cx + fw * .1f, mid - fh * .22f).C(cx + fw * .12f, mid - fh * .14f, cx + fw * .06f, mid - fh * .06f, cx + fw * .02f, mid - fh * .04f));
                    break;
                case "anchor":
                    Draw(Shape.Box(cx, mid - fh * .02f, fw * .06f, fh * .5f));
                    Draw(Shape.Box(cx, mid - fh * .2f, fw * .32f, fh * .05f));
                    Draw(Shape.M(cx - fw * .28f, mid + fh * .1f).C(cx - fw * .24f, mid + fh * .3f, cx + fw * .24f, mid + fh * .3f, cx + fw * .28f, mid + fh * .1f)
                        .L(cx + fw * .2f, mid + fh * .14f).C(cx + fw * .14f, mid + fh * .24f, cx - fw * .14f, mid + fh * .24f, cx - fw * .2f, mid + fh * .14f));
                    break;
                case "lion":
                case "bird":
                case "devil":
                case "ship":
                case "bat":
                    SimpleFigure(r, d.Emblem, cx, mid - fh * .08f, fw, fh, Draw);
                    break;
            }
        }

        // Silhuetas simples dos símbolos (leão, pássaro, diabo, navio, morcego).
        static void SimpleFigure(Raster r, string kind, float cx, float cy, float fw, float fh, Action<Shape> draw)
        {
            float s = fw * .01f; // 1 unidade = 1% da largura do campo
            Shape P(params float[] xy)
            {
                var sh = Shape.M(cx + xy[0] * s, cy + xy[1] * s);
                for (int i = 2; i + 1 < xy.Length; i += 2) sh.L(cx + xy[i] * s, cy + xy[i + 1] * s);
                return sh;
            }
            switch (kind)
            {
                case "lion": // leão rampante
                    draw(P(-8, 26, -14, 6, -20, -6, -14, -10, -10, -22, -2, -30, 8, -28, 14, -20, 10, -14, 16, -8, 24, -14, 22, -4, 14, 2, 18, 12, 26, 20, 18, 26, 8, 16, 2, 24, 4, 32, -6, 32));
                    break;
                case "bird": // pássaro de asas abertas
                    draw(P(-30, -6, -12, -2, -4, -14, 4, -18, 8, -12, 4, -6, 12, -2, 30, -10, 20, 4, 6, 8, 4, 22, -2, 26, -6, 12, -18, 6));
                    break;
                case "devil": // diabo com tridente
                    draw(P(-18, 28, -16, 8, -22, -2, -18, -14, -22, -24, -12, -18, -6, -24, 2, -20, 6, -10, 14, -4, 12, 10, 4, 14, 2, 28));
                    draw(Shape.Box(cx + 20 * s, cy + 2 * s, 3 * s, 56 * s));
                    draw(P(12, -22, 14, -32, 16, -24, 20, -34, 24, -24, 26, -32, 28, -22, 24, -18, 16, -18));
                    break;
                case "ship": // navio
                    draw(P(-28, 12, 28, 12, 20, 24, -20, 24));
                    draw(Shape.Box(cx, cy - 8 * s, 3 * s, 40 * s));
                    draw(P(2, -26, 22, 4, 2, 4));
                    draw(P(-2, -20, -18, 6, -2, 6));
                    break;
                case "bat": // morcego
                    draw(P(-36, -14, -22, -8, -14, -16, -6, -10, -4, -22, 0, -16, 4, -22, 6, -10, 14, -16, 22, -8, 36, -14, 30, 0, 22, -2, 16, 8, 8, 4, 0, 14, -8, 4, -16, 8, -22, -2, -30, 0));
                    break;
            }
        }

        static void Crown(Raster r, float cx, float bottom, Rgba col)
        {
            float w = 46, h = 16, top = bottom - h;
            var c = Shape.M(cx - w / 2, bottom).L(cx - w / 2 - 2, top + 5).L(cx - w / 4, top + 9).L(cx - w / 8, top + 1)
                .L(cx, top + 7).L(cx + w / 8, top + 1).L(cx + w / 4, top + 9).L(cx + w / 2 + 2, top + 5).L(cx + w / 2, bottom);
            r.Fill(c.Moved(.8f, 1.2f), new Rgba(0, 0, 0, .35f));
            r.Fill(c, (x, y) => Metal(col, x, y, top, bottom));
            r.Fill(Shape.Box(cx, bottom - 2.5f, w, 4), col.Mul(.7f));
            foreach (float dx in new[] { -w / 2 - 2, -w / 8, w / 8, w / 2 + 2 })
                r.Fill(Shape.Ellipse(cx + dx, top + (Math.Abs(dx) > w / 4 ? 5 : 1), 2.2f, 2.2f), col);
            r.Fill(Shape.Ellipse(cx, top - 3, 3, 3), Rgba.Hex("#D7141A"));
        }
    }

    /// <summary>Taça dourada (títulos) ou prateada (prêmios individuais) com brilho metálico.</summary>
    public static class TrophyArt
    {
        public static Raster Render(bool gold, int w)
        {
            float scale = w / 100f;
            var r = new Raster(w, (int)(120 * scale), scale);
            var baseCol = Rgba.Hex(gold ? "#E2B33C" : "#C9D1DA");
            Func<float, float, Rgba> metal = (x, y) =>
            {
                float band = (float)Math.Cos((x - 38) * .09f);
                float k = .7f + .45f * band * band + .12f * (1 - y / 120f);
                return baseCol.Mul(k);
            };
            var shadow = new Rgba(0, 0, 0, .35f);
            // alças
            foreach (int side in new[] { -1, 1 })
            {
                float cx = 50 + side * 30;
                // anel: contorno externo + interno no mesmo polígono (o preenchimento par-ímpar deixa o furo)
                var ring = Shape.Ellipse(cx, 36, 13, 16, 48);
                var hole = Shape.Ellipse(cx, 36, 7.5f, 10.5f, 48);
                ring.Pts.Add(ring.Pts[0]);
                ring.Pts.AddRange(hole.Pts);
                ring.Pts.Add(hole.Pts[0]);
                r.Fill(ring.Moved(1, 1.5f), shadow);
                r.Fill(ring, metal);
            }
            // copa
            var bowl = Shape.M(20, 14).L(80, 14).C(80, 46, 68, 62, 56, 66).L(44, 66).C(32, 62, 20, 46, 20, 14);
            r.Fill(bowl.Moved(1, 1.5f), shadow);
            r.Fill(bowl, metal);
            r.Fill(Shape.Ellipse(50, 14, 30, 4.5f, 64), (x, y) => baseCol.Mul(.55f));
            // haste, nó e base
            r.Fill(Shape.M(45, 66).L(55, 66).L(53, 84).L(47, 84), metal);
            r.Fill(Shape.Ellipse(50, 84, 9, 3.5f, 32), metal);
            var foot = Shape.M(34, 92).L(66, 92).L(70, 108).L(30, 108);
            r.Fill(foot.Moved(1, 1.5f), shadow);
            r.Fill(foot, (x, y) => Rgba.Hex("#3A2A1A").Mul(1.2f - (y - 92) / 30f));
            r.Fill(Shape.Box(50, 99, 26, 3, 0), metal);
            r.Fill(Shape.Box(50, 89, 30, 5, 0), metal);
            // reflexo
            r.Fill(Shape.M(30, 18).C(30, 36, 34, 48, 40, 56).L(36, 56).C(28, 46, 25, 34, 26, 18), new Rgba(1, 1, 1, .35f));
            return r;
        }
    }
}
