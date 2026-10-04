using System;

namespace Camisa10.UI.Art
{
    /// <summary>Chuteira de perfil (bico para a direita) com cano de malha, cadarço, faixa da marca, solado e cravos.</summary>
    public static class BootArt
    {
        public const float DW = 250, DH = 130;

        public static Raster Render(string c1Hex, string c2Hex, string soleHex, int w)
        {
            float scale = w / DW;
            var r = new Raster(w, (int)Math.Round(DH * scale), scale);
            Rgba c1 = Rgba.Hex(c1Hex), c2 = Rgba.Hex(c2Hex), sole = Rgba.Hex(soleHex);
            var ink = new Rgba(.06f, .06f, .07f);
            bool lightUpper = c1.Luma > .75f;

            // sombra no chão
            r.Fill(Shape.Ellipse(132, 117, 112, 7), (x, y) =>
            {
                float d = Math.Abs(x - 132) / 112f;
                return new Rgba(0, 0, 0, .35f * (1 - d * d));
            });

            // cravos (atrás do solado)
            foreach (float sx in new[] { 48f, 74f, 138f, 162f, 186f, 210f })
            {
                var stud = Shape.M(sx - 5, 100).L(sx + 5, 100).L(sx + 3, 109).C(sx + 2, 111, sx - 2, 111, sx - 3, 109);
                r.Fill(stud, (x, y) => sole.Mul(1.15f - .5f * (y - 100) / 11f));
            }

            // contorno escuro (silhueta levemente maior)
            var upper = Upper();
            r.Fill(upper.Scaled(130, 60, 1.012f, 1.03f), ink);
            var collar = Collar();
            r.Fill(collar.Scaled(75, 30, 1.03f, 1.04f), ink);

            // cabedal com volume: claro em cima, escuro embaixo, textura de malha e reflexo na borda
            Func<float, float, Rgba> leather = (x, y) =>
            {
                float v = (y - 30) / 66f;
                float k = 1.18f - .45f * v;
                k += .05f * (Raster.Noise(x * 1.6f, y * 1.6f) - .5f);
                var c = c1.Mul(k);
                // brilho de verniz ao longo do peito do pé
                float spec = Raster.Smooth(12, 0, Math.Abs(y - (64 + (x - 120) * .16f))) * Raster.Smooth(90, 160, x) * Raster.Smooth(240, 190, x);
                return c.Lerp(new Rgba(1, 1, 1), .28f * spec);
            };
            r.Fill(upper, leather);

            // biqueira levemente mais escura
            r.Fill(Shape.M(200, 68).C(218, 71, 234, 76, 240, 86).C(241, 93, 235, 95, 228, 95).L(204, 95).C(208, 86, 206, 76, 200, 68),
                (x, y) => c1.Mul(.9f - .3f * (y - 68) / 27f).WithA(.5f));

            // contraforte do calcanhar na cor de detalhe
            r.Fill(Shape.M(26, 72).C(27, 60, 32, 52, 40, 47).C(44, 62, 46, 80, 44, 95).L(32, 96).C(28, 88, 25, 80, 26, 72),
                (x, y) => c2.Mul(1.1f - .4f * (y - 47) / 49f));

            // cano de malha (estilo meia), com nervuras
            r.Fill(collar, (x, y) =>
            {
                var knit = lightUpper ? c1.Mul(.8f) : c1.Mul(.7f);
                float rib = .5f + .5f * (float)Math.Sin(x * 1.9f);
                float v = (y - 14) / 32f;
                return knit.Mul(.85f + .2f * rib - .25f * v + .06f * (Raster.Noise(x * 3, y * 3) - .5f));
            });
            r.Fill(Shape.M(55, 14).L(100, 23).L(100, 27).L(54, 18), c2.Mul(.9f)); // barra do cano

            // língua e cadarço
            var tongue = Shape.M(102, 42).C(120, 44, 140, 50, 162, 57).L(158, 63).C(138, 56, 118, 50, 102, 49);
            r.Fill(tongue, (x, y) => c1.Mul(lightUpper ? .78f : .6f));
            var lace = lightUpper ? new Rgba(.15f, .15f, .17f) : new Rgba(.96f, .96f, .96f);
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f, x = 108 + t * 48, y = 45 + t * 13;
                r.Fill(Shape.Box(x, y, 3.2f, 11, -64), (px, py) => lace.Mul(1.05f - .15f * ((py - y) / 6 + .5f)));
                r.Fill(Shape.Ellipse(x - 3.5f, y + 4.5f, 1.4f, 1.4f), ink);
            }

            // faixa da marca (curva ao longo da lateral)
            var stripe = Shape.M(60, 84).C(100, 82, 148, 74, 200, 64).C(204, 64, 206, 68, 202, 70).C(152, 80, 104, 90, 66, 92)
                .C(58, 92, 56, 86, 60, 84);
            r.Fill(stripe.Moved(.6f, 1.2f), new Rgba(0, 0, 0, .3f));
            r.Fill(stripe, (x, y) => c2.Mul(1.12f - .3f * (y - 58) / 34f));

            // costura acima do solado
            for (float x = 34; x < 226; x += 5)
                r.Fill(Shape.Box(x, 92.5f - (x > 200 ? (x - 200) * .1f : 0), 2.6f, .9f), c1.Mul(.55f).WithA(.8f));

            // solado (placa) com brilho
            var plate = Shape.M(28, 94).L(230, 92).C(240, 92, 241, 100, 232, 101).L(38, 103).C(29, 103, 26, 98, 28, 94);
            r.Fill(plate, (x, y) => sole.Mul(1.2f - .55f * (y - 92) / 11f));
            r.Fill(Shape.M(40, 95.5f).L(222, 94).L(222, 95.4f).L(40, 97), new Rgba(1, 1, 1, .25f));
            return r;
        }

        static Shape Upper() =>
            Shape.M(30, 96).C(24, 84, 22, 62, 34, 48).C(40, 42, 48, 40, 56, 41).L(102, 42).C(120, 44, 142, 50, 162, 57)
                .C(188, 66, 218, 70, 234, 78).C(244, 84, 242, 94, 230, 95).L(34, 96);

        static Shape Collar() =>
            Shape.M(44, 44).C(44, 32, 49, 20, 55, 14).L(100, 23).C(103, 30, 105, 38, 105, 46).C(84, 42, 64, 41, 44, 44);
    }
}
