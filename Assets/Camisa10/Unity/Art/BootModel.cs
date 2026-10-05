using System;
using System.Collections.Generic;
using Camisa10.Core;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Chuteira modelada em código, em alta resolução e separada da malha do corpo: cano aberto com borda acolchoada e
    /// forro, contraforte alto, lingueta, cadarço cruzado em relevo, bico arredondado com costuras, solado rígido com o arco
    /// suspenso e travas cônicas, e textura nas cores escolhidas (faixa lateral, respiros, costura junto ao solado).
    /// A mesma malha veste os jogadores em campo e é fotografada em estúdio para a tela da chuteira.
    /// </summary>
    public static class BootModel
    {
        // ---------- forma (medidas em metros, pé esquerdo, bico para +z) ----------
        // valores do calcanhar (s = 0) ao bico (s = 1), espaçados por igual
        static readonly float[] HalfWidth = { .032f, .041f, .045f, .046f, .044f, .045f, .047f, .047f, .045f, .041f, .034f };
        static readonly float[] Top = { .097f, .101f, .101f, .099f, .094f, .086f, .074f, .064f, .056f, .050f, .045f };
        static readonly float[] SoleTop = { .027f, .026f, .025f, .024f, .023f, .022f, .022f, .022f, .023f, .027f, .033f };
        static readonly float[] Inward = { 0, 0, 0, 0, .001f, .003f, .005f, .006f, .007f, .007f, .006f };

        // boca da chuteira: do contraforte (S0) até a lingueta (S1); a borda desce nas laterais até RimLow
        const float S0 = .07f, S1 = .43f, RimMid = .24f, RimLow = .071f, Wall = .0035f;
        const int NS = 64, NT = 72, StudSeg = 12;

        /// <summary>Travas: posição ao longo do pé e lado (fração da largura; + = lado de dentro). O meio do pé fica no ar.</summary>
        static readonly Vector2[] Studs =
        {
            new Vector2(.09f, .5f), new Vector2(.09f, -.5f), new Vector2(.21f, .56f), new Vector2(.21f, -.56f),
            new Vector2(.58f, .68f), new Vector2(.58f, -.66f), new Vector2(.70f, .68f), new Vector2(.70f, -.66f),
            new Vector2(.64f, 0), new Vector2(.82f, .55f), new Vector2(.82f, -.52f), new Vector2(.93f, 0),
        };

        static float Curve(float[] k, float s)
        {
            float f = Mathf.Clamp01(s) * (k.Length - 1);
            int i = Mathf.Min((int)f, k.Length - 2);
            float t = f - i;
            float p0 = k[Mathf.Max(i - 1, 0)], p1 = k[i], p2 = k[i + 1], p3 = k[Mathf.Min(i + 2, k.Length - 1)];
            return .5f * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t);
        }

        static float SPow(float v, float e) => Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), e);

        /// <summary>Arredonda as pontas: 1 no meio do pé, 0 no calcanhar e no bico.</summary>
        static float Ends(float s)
        {
            // cúpula (perfil circular) no calcanhar e no bico: sem tampa chata que brilha como espelho
            const float Heel = .1f, Toe = .16f;
            float u = s < Heel ? (Heel - s) / Heel : s > 1 - Toe ? (s - (1 - Toe)) / Toe : 0;
            return Mathf.Sqrt(Mathf.Max(0, 1 - u * u));
        }

        /// <summary>Malha da chuteira em listas (posições, normais, UV) com triângulos virados para fora.</summary>
        public sealed class Geo
        {
            public readonly List<Vector3> V = new List<Vector3>(), N = new List<Vector3>();
            public readonly List<Vector2> UV = new List<Vector2>();
            public readonly List<int> T = new List<int>();
        }

        // regiões da textura (y): solado, cadarço, forro e cabedal
        const float VLace = .18f, VPad = .2f, VLining = .225f, VUpper0 = .25f;

        /// <summary>Seção do cabedal: baixo, cima e meia-largura (sem o arredondamento das pontas aplicado no x).</summary>
        static void Section(float s, out float ybot, out float ytop, out float w, out float e)
        {
            e = Ends(s);
            float eh = Mathf.Sqrt(e);
            float yb = Curve(SoleTop, s), yt = Curve(Top, s);
            float tip = yb + .45f * (yt - yb);
            ybot = Mathf.Lerp(tip, yb, eh); ytop = Mathf.Lerp(tip, yt, eh);
            w = Curve(HalfWidth, s) * e;
        }

        const float TopRound = 2.3f, BottomRound = 4.5f; // expoentes da seção: em cima arredondada, embaixo quase reta

        static float SuperR(float phi, float n) => Mathf.Pow(Mathf.Pow(Mathf.Abs(Mathf.Cos(phi)), n) + Mathf.Pow(Mathf.Abs(Mathf.Sin(phi)), n), -1f / n);

        /// <summary>Ângulo (acima da horizontal) em que a seção chega à altura relativa q (0 = meio, 1 = topo).</summary>
        static float CutAngle(float q)
        {
            if (q <= 0) return 0;
            float lo = 0, hi = Mathf.PI * .5f;
            for (int k = 0; k < 24; k++)
            {
                float m = (lo + hi) * .5f;
                if (SuperR(m, TopRound) * Mathf.Sin(m) < q) lo = m; else hi = m;
            }
            return (lo + hi) * .5f;
        }

        /// <summary>Altura da borda da boca em s (acima do topo = fechado).</summary>
        static float RimY(float s, float ytop)
        {
            if (s <= S0 || s >= S1) return 9f;
            float d = (s - RimMid) / (s < RimMid ? RimMid - S0 : S1 - RimMid);
            return Mathf.Lerp(RimLow, ytop + .002f, d * d);
        }

        /// <summary>
        /// Monta a chuteira esquerda calçada no tornozelo (ankle) com o bico em toeEndZ; mirror = pé direito.
        /// </summary>
        public static Geo Build(Vector3 ankle, float toeEndZ, bool mirror)
        {
            var g = new Geo();
            float zh = ankle.z - .072f, zt = toeEndZ + .01f, x0 = ankle.x;
            float sx = mirror ? -1 : 1;
            // pé esquerdo tem o lado de dentro em +x (o modelo tem o lado esquerdo em x negativo)
            Vector3 X(float x, float y, float z) => new Vector3(sx * x, y, z);
            float Z(float s) => Mathf.Lerp(zh, zt, s);

            float[] sv = new float[NS + 1];
            for (int i = 0; i <= NS; i++) sv[i] = .5f - .5f * Mathf.Cos(Mathf.PI * i / NS); // mais fatias nas pontas

            // cabedal: cada seção dá a volta começando e terminando no topo; na boca o topo fica aberto
            var outer = new Vector3[NS + 1, NT + 1];
            var wallK = new float[NS + 1];
            int open0 = -1, open1 = -1; // fatias onde a boca está aberta // parede mais fina nas pontas (o forro não pode atravessar o bico)
            var uvs = new Vector2[NS + 1, NT + 1];
            for (int i = 0; i <= NS; i++)
            {
                float s = sv[i];
                Section(s, out float ybot, out float ytop, out float w0, out _);
                float mid = (ybot + ytop) * .5f, half = (ytop - ybot) * .5f;
                wallK[i] = Mathf.Clamp01(Mathf.Min(w0, half) / .02f);
                float rim = RimY(s, ytop);
                float a = rim < ytop ? CutAngle((rim - mid) / half) : Mathf.PI * .5f; // a boca vai de a até pi - a
                if (a < Mathf.PI * .5f - .01f) { if (open0 < 0) open0 = i; open1 = i; }
                float th0 = Mathf.PI - a, th1 = 2 * Mathf.PI + a;
                // colunas com ângulo fixo (a textura não entorta); as que caem dentro da boca encostam na borda
                bool collar = s > S0 - .05f && s < S1 + .05f;
                for (int j = 0; j <= NT; j++)
                {
                    float u = j / (float)NT, th = Mathf.Clamp(Mathf.PI * .5f + u * Mathf.PI * 2, th0, th1);
                    float c = Mathf.Cos(th), sn = Mathf.Sin(th);
                    float w = w0 * (1 - (collar ? .08f : .2f) * Mathf.Max(0, sn)); // laterais fecham em cima
                    // superelipse em coordenadas polares: pontos bem distribuídos pela volta (a textura não estica)
                    float r = SuperR(th, sn > 0 ? TopRound : BottomRound);
                    float x = x0 + Curve(Inward, s) + w * r * c;
                    float y = mid + half * r * sn;
                    outer[i, j] = X(x, y, Z(s));
                    // textura: t = 0 no topo, 0,25 lado de fora, 0,5 embaixo, 0,75 lado de dentro, 1 no topo
                    uvs[i, j] = new Vector2(s, VUpper0 + (1 - VUpper0) * ((th - Mathf.PI * .5f) / (Mathf.PI * 2)));
                }
            }
            var on = Sheet(g, outer, uvs, 1f);

            // forro por dentro (aparece pela boca) e a borda acolchoada ligando os dois
            var inner = new Vector3[NS + 1, NT + 1];
            var iuv = new Vector2[NS + 1, NT + 1];
            for (int i = 0; i <= NS; i++)
                for (int j = 0; j <= NT; j++) { inner[i, j] = outer[i, j] - on[i, j] * (Wall * wallK[i]); iuv[i, j] = new Vector2(sv[i], VLining); }
            Sheet(g, inner, iuv, -1f);
            open0 = Mathf.Max(0, open0 - 1); open1 = Mathf.Min(NS, open1 + 1);
            foreach (int j in new[] { 0, NT })
            {
                int start = g.V.Count;
                for (int i = open0; i <= open1; i++)
                {
                    var o = outer[i, j]; var n = on[i, j]; var inn = inner[i, j];
                    var mid = (o + inn) * .5f + Vector3.up * (i == open0 || i == open1 ? 0 : .0028f);
                    g.V.Add(o); g.N.Add(n); g.UV.Add(new Vector2(sv[i], VPad));
                    g.V.Add(mid); g.N.Add(Vector3.up); g.UV.Add(new Vector2(sv[i], VPad));
                    g.V.Add(inn); g.N.Add(-n); g.UV.Add(new Vector2(sv[i], VPad));
                }
                for (int i = 0; i < open1 - open0; i++)
                    for (int k = 0; k < 2; k++)
                    {
                        int q = start + i * 3 + k, r = q + 3;
                        Tri(g, q, r, r + 1); Tri(g, q, r + 1, q + 1);
                    }
            }

            // solado: placa rígida um pouco maior que o cabedal, com o arco do pé mais alto (sem travas no meio)
            var plate = new Vector3[NS + 1, NT + 1];
            var puv = new Vector2[NS + 1, NT + 1];
            for (int i = 0; i <= NS; i++)
            {
                float s = sv[i], e = Ends(s);
                float yb = Curve(SoleTop, s);
                float bottom = PlateBottom(s), topY = yb + .003f;
                float w = (Curve(HalfWidth, s) + .0035f) * e;
                for (int j = 0; j <= NT; j++)
                {
                    float th = j / (float)NT * Mathf.PI * 2;
                    float c = Mathf.Cos(th), sn = Mathf.Sin(th);
                    float r = SuperR(th, 7f); // borda da placa arredondada de forma uniforme (sem faixas de brilho)
                    float x = x0 + Curve(Inward, s) + w * r * c;
                    float y = (bottom + topY) * .5f + (topY - bottom) * .5f * r * sn * Mathf.Sqrt(e);
                    plate[i, j] = X(x, y, Mathf.Lerp(zh - .004f, zt + .003f, s));
                    puv[i, j] = new Vector2(s, .03f + .1f * j / NT);
                }
            }
            Sheet(g, plate, puv, 1f, true);

            // travas cônicas, do fundo do solado até o chão
            foreach (var st in Studs)
            {
                float s = st.x;
                float cx = x0 + Curve(Inward, s) + Curve(HalfWidth, s) * Ends(s) * st.y;
                float pb = PlateBottom(s);
                Stud(g, X(cx, pb + .002f, Z(s)), X(cx, Mathf.Max(0, pb - .014f), Z(s)), .0078f, .0056f, new Vector2(s, .1f)); // na ponta a trava fica no ar
            }

            // lingueta: aba que sai do peito do pé e sobe por trás do cadarço
            {
                const int R = 8, C = 8;
                var tf = new Vector3[R + 1, C + 1]; var tu = new Vector2[R + 1, C + 1];
                Section(.47f, out _, out float yBase, out _, out _);
                for (int r = 0; r <= R; r++)
                {
                    float v = r / (float)R; // 0 na base, 1 na ponta
                    float zc = Mathf.Lerp(Z(.47f), Z(.355f), v), yc = Mathf.Lerp(yBase - .002f, .113f, v) + Mathf.Sin(v * Mathf.PI) * .004f;
                    float hw = Mathf.Lerp(.021f, .017f, v) * (v > .85f ? Mathf.Sqrt(1 - (v - .85f) / .15f * .6f) : 1f);
                    for (int c = 0; c <= C; c++)
                    {
                        float u = c / (float)C * 2 - 1;
                        tf[r, c] = X(x0 + Curve(Inward, .4f) + u * hw, yc - u * u * .006f, zc + v * .004f);
                        tu[r, c] = new Vector2(.5f, VUpper0 + .005f);
                    }
                }
                var tn = Sheet(g, tf, tu, 1f, false, true);
                var back = new Vector3[R + 1, C + 1];
                for (int r = 0; r <= R; r++) for (int c = 0; c <= C; c++) back[r, c] = tf[r, c] - tn[r, c] * .003f;
                Sheet(g, back, tu, -1f, false, true);
            }

            // cadarço cruzado em X por cima do peito do pé
            for (int k = 0; k < 5; k++)
            {
                float s0 = .445f + k * .036f, s1 = s0 + .036f;
                Section(s0, out _, out float y0, out _, out _);
                Section(s1, out _, out float y1, out _, out _);
                float xi = x0 + Curve(Inward, s0);
                foreach (float d in new[] { -1f, 1f })
                    Tube(g, X(xi - d * .017f, y0 - .001f, Z(s0)), X(xi + d * .017f, y1 - .001f, Z(s1)), .0026f, .0045f, new Vector2(.5f, VLace));
            }
            return g;
        }

        /// <summary>Fundo do solado: mais alto no arco do pé, onde não há travas.</summary>
        static float PlateBottom(float s) => Curve(SoleTop, s) - .009f + .006f * Mathf.Exp(-Mathf.Pow((s - .42f) / .11f, 2));

        /// <summary>
        /// Superfície em grade a partir dos pontos. side = 1 normais para fora, -1 para dentro (forro).
        /// closed = a última coluna é a primeira (anel); colunas que coincidem (topo fechado) dividem a normal.
        /// Devolve as normais (para fora) de cada ponto.
        /// </summary>
        static Vector3[,] Sheet(Geo g, Vector3[,] pts, Vector2[,] uv, float side, bool closed = false, bool flat = false)
        {
            int nr = pts.GetLength(0) - 1, nc = pts.GetLength(1) - 1;
            var acc = new Vector3[nr + 1, nc + 1];
            // sentido: o meio da grade deve apontar para fora do centro da seção (ou para cima, na lingueta)
            int mi = nr / 2, mj = nc / 4;
            var center = Vector3.zero; for (int j = 0; j <= nc; j++) center += pts[mi, j]; center /= nc + 1;
            var f0 = Vector3.Cross(pts[mi + 1, mj] - pts[mi, mj], pts[mi, mj + 1] - pts[mi, mj]);
            var outRef = flat ? Vector3.up : pts[mi, mj] - center;
            float sign = Vector3.Dot(f0, outRef) >= 0 ? 1 : -1;
            for (int i = 0; i < nr; i++)
                for (int j = 0; j < nc; j++)
                {
                    Vector3 a = pts[i, j], b = pts[i + 1, j], c = pts[i + 1, j + 1], d = pts[i, j + 1];
                    var n1 = Vector3.Cross(b - a, d - a) * sign; var n2 = Vector3.Cross(c - b, d - b) * sign;
                    acc[i, j] += n1; acc[i + 1, j] += n1 + n2; acc[i, j + 1] += n1 + n2; acc[i + 1, j + 1] += n2;
                }
            for (int i = 0; i <= nr; i++)
                if (closed || (pts[i, 0] - pts[i, nc]).sqrMagnitude < 1e-10f) { var m = acc[i, 0] + acc[i, nc]; acc[i, 0] = m; acc[i, nc] = m; }
            var axis = (pts[nr, 0] - pts[0, 0]).normalized;
            var normals = new Vector3[nr + 1, nc + 1];
            int start = g.V.Count;
            for (int i = 0; i <= nr; i++)
                for (int j = 0; j <= nc; j++)
                {
                    var n = acc[i, j].sqrMagnitude > 1e-20f ? acc[i, j].normalized : (i < mi ? -axis : axis);
                    normals[i, j] = n;
                    g.V.Add(pts[i, j]); g.N.Add(n * side); g.UV.Add(uv[i, j]);
                }
            int W = nc + 1;
            for (int i = 0; i < nr; i++)
                for (int j = 0; j < nc; j++)
                {
                    int a = start + i * W + j, b = a + W, c = b + 1, d = a + 1;
                    Tri(g, a, b, c); Tri(g, a, c, d);
                }
            return normals;
        }

        /// <summary>Tubo curto (cadarço) de a até b, arqueado para cima no meio.</summary>
        static void Tube(Geo g, Vector3 a, Vector3 b, float r, float arch, Vector2 uv)
        {
            const int L = 6, Rr = 6;
            int start = g.V.Count;
            var dir = (b - a).normalized;
            var n0 = Vector3.Cross(dir, Vector3.up).normalized; var b0 = Vector3.Cross(n0, dir);
            for (int i = 0; i <= L; i++)
            {
                float t = i / (float)L;
                var c = Vector3.Lerp(a, b, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arch);
                for (int k = 0; k < Rr; k++)
                {
                    float ang = k / (float)Rr * Mathf.PI * 2;
                    var n = n0 * Mathf.Cos(ang) + b0 * Mathf.Sin(ang);
                    g.V.Add(c + n * r); g.N.Add(n); g.UV.Add(uv);
                }
            }
            for (int i = 0; i < L; i++)
                for (int k = 0; k < Rr; k++)
                {
                    int p = start + i * Rr + k, q = start + i * Rr + (k + 1) % Rr;
                    Tri(g, p, p + Rr, q + Rr); Tri(g, p, q + Rr, q);
                }
        }

        static void Tri(Geo g, int a, int b, int c)
        {
            var fn = Vector3.Cross(g.V[b] - g.V[a], g.V[c] - g.V[a]);
            if (fn.sqrMagnitude < 1e-16f) return; // ponta: triângulo sem área
            if (Vector3.Dot(fn, g.N[a] + g.N[b] + g.N[c]) < 0) { g.T.Add(a); g.T.Add(c); g.T.Add(b); }
            else { g.T.Add(a); g.T.Add(b); g.T.Add(c); }
        }

        static void Stud(Geo g, Vector3 top, Vector3 bottom, float r1, float r2, Vector2 uv)
        {
            int s0 = g.V.Count;
            for (int k = 0; k <= 1; k++)
                for (int i = 0; i < StudSeg; i++)
                {
                    float a = i / (float)StudSeg * Mathf.PI * 2;
                    var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    g.V.Add((k == 0 ? top : bottom) + dir * (k == 0 ? r1 : r2));
                    g.N.Add((dir + Vector3.down * .25f).normalized);
                    g.UV.Add(uv);
                }
            for (int i = 0; i < StudSeg; i++)
            {
                int a = s0 + i, b = s0 + (i + 1) % StudSeg, c = a + StudSeg, d = b + StudSeg;
                Tri(g, a, c, d); Tri(g, a, d, b);
            }
            // ponta da trava (achatada)
            int cen = g.V.Count;
            g.V.Add(bottom); g.N.Add(Vector3.down); g.UV.Add(uv);
            for (int i = 0; i < StudSeg; i++)
            {
                float a = i / (float)StudSeg * Mathf.PI * 2;
                g.V.Add(bottom + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r2); g.N.Add(Vector3.down); g.UV.Add(uv);
            }
            for (int i = 0; i < StudSeg; i++) Tri(g, cen, cen + 1 + i, cen + 1 + (i + 1) % StudSeg);
        }

        // ---------- textura ----------
        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        static string Key(BootStyle b) => (b.c1 + b.c2 + b.sole).ToUpperInvariant();

        /// <summary>Material da chuteira (um por combinação de cores, reaproveitado).</summary>
        public static Material Material(BootStyle b)
        {
            string k = Key(b);
            if (matCache.TryGetValue(k, out var m) && m != null) return m;
            // a sua chuteira em alta resolução; as dos outros (estilos comuns) numa textura menor
            bool common = Array.Exists(Presets, x => Key(x) == k);
            m = Arena.TexMat(Texture(b, common ? 512 : 1024), Vector2.one, .38f);
            m.name = "Chuteira";
            return matCache[k] = m;
        }

        /// <summary>
        /// Textura da chuteira (x = calcanhar → bico). Faixas em y: solado, cadarço, borda acolchoada, forro e,
        /// de VUpper0 para cima, a volta do cabedal começando no topo (0), lado de fora, embaixo, lado de dentro, topo (1).
        /// </summary>
        public static Texture2D Texture(BootStyle b, int W = 1024)
        {
            string k = Key(b) + W;
            if (texCache.TryGetValue(k, out var cached) && cached != null) return cached;
            int H = W / 2;
            Color c1 = Theme.Hex(b.c1), c2 = Theme.Hex(b.c2), sole = Theme.Hex(b.sole);
            if (Mathf.Abs(c1.grayscale - c2.grayscale) < .08f && ((Vector4)(c1 - c2)).magnitude < .15f) c2 = c1.grayscale > .5f ? new Color(.08f, .08f, .09f) : Color.white;
            var lace = Color.Lerp(c1, Color.white, c1.grayscale > .7f ? .3f : .8f);
            var lining = new Color(.11f, .11f, .12f);
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = (y + .5f) / H;
                for (int x = 0; x < W; x++)
                {
                    float s = (x + .5f) / W;
                    Color32 c;
                    if (v < .17f) c = SolePixel(s, Mathf.Min(v, .15f), sole);
                    else if (v < .19f) c = (Color32)(lace * (.92f + .08f * Hash(x, y)));
                    else if (v < .212f) c = (Color32)Color.Lerp(lining, c1 * .55f, .35f); // borda acolchoada
                    else if (v < VUpper0) c = (Color32)(lining * (.9f + .2f * Hash(x / 3, y / 3))); // forro
                    else c = UpperPixel(s, (v - VUpper0) / (1 - VUpper0), c1, c2, sole);
                    px[y * W + x] = c;
                }
            }
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, true) { name = "Chuteira", wrapMode = TextureWrapMode.Clamp, anisoLevel = 2, hideFlags = HideFlags.DontUnloadUnusedAsset };
            tex.SetPixels32(px);
            tex.Apply(true, true); // sem compressão: os blocos de 4x4 serrilhavam as bordas da faixa
            return texCache[k] = tex;
        }

        static float Hash(int x, int y) { unchecked { int h = x * 374761393 + y * 668265263; h = (h ^ (h >> 13)) * 1274126177; return (h & 0xffff) / 65535f; } }

        static Color32 SolePixel(float s, float v, Color sole)
        {
            // borda do solado um pouco mais clara; textura fina de plástico
            float k = .92f + .06f * Hash((int)(s * 400), (int)(v * 400)) + .08f * Mathf.Clamp01(1 - Mathf.Abs(v - .09f) / .09f);
            return (Color32)(sole * k);
        }

        /// <summary>Cabedal: t = 0 no topo, 0,25 lado de fora, 0,5 embaixo, 0,75 lado de dentro.</summary>
        static Color32 UpperPixel(float s, float t, Color c1, Color c2, Color sole)
        {
            t = Mathf.Clamp01(t);
            float th = Mathf.PI * .5f + t * Mathf.PI * 2;
            float h = Mathf.Sin(th);               // 1 em cima, -1 embaixo
            float side = Mathf.Abs(Mathf.Cos(th)); // 1 nas laterais
            float topDist = Mathf.Min(t, 1 - t);
            // couro sintético: granulado fino, luz mais forte em cima
            float grain = (Hash((int)(s * 1024), (int)(t * 1536)) - .5f) * .035f + Mathf.Sin(s * 700f + t * 90f) * .008f;
            var col = c1 * (.84f + .16f * Mathf.Clamp01(h * .5f + .5f) + grain);

            // costuras atravessando o bico (como na chuteira de couro) e biqueira levemente mais escura
            foreach (float ls in new[] { .70f, .765f, .83f })
            {
                float bend = ls + (1 - h) * .02f; // a costura curva para trás nas laterais
                float dd = Mathf.Abs(s - bend);
                if (h > -.35f && dd < .0045f) col *= dd < .0016f ? .72f : .9f;
                else if (h > -.35f && dd < .009f && Mathf.Repeat(t * 260f, 1f) < .5f && dd > .006f) col *= .82f; // pespontos
            }
            if (s > .86f) col *= 1f - .05f * Mathf.Clamp01((s - .86f) / .1f);

            // contraforte do calcanhar na cor de detalhe, com borda costurada
            float heel = .115f + .04f * h;
            if (s < heel) col = Color.Lerp(col, c2 * .92f, Mathf.Clamp01((heel - s) / .006f));
            if (Mathf.Abs(s - heel - .007f) < .0018f && Mathf.Repeat(h * 40f, 1f) < .6f) col *= .7f;

            // respiros perfurados na lateral, atrás
            if (s > .17f && s < .33f && h > .05f && h < .6f && side > .4f)
            {
                float gx = Mathf.Repeat(s * 160f, 1f) - .5f, gy = Mathf.Repeat(h * 22f, 1f) - .5f;
                if (gx * gx + gy * gy < .05f) col *= .45f;
            }

            // faixa lateral (nos dois lados): sai baixa do calcanhar e sobe para o meio do pé, afinando
            if (side > .25f && s > .16f && s < .72f)
            {
                float u = (s - .16f) / .56f;
                float center = Mathf.Lerp(-.4f, .45f, u * u * .4f + u * .6f);
                float thick = .2f * Mathf.Sin(Mathf.Clamp01(u * 1.1f) * Mathf.PI) + .015f;
                float d = Mathf.Abs(h - center) - thick * .5f;
                float a = Mathf.Clamp01(-d / .012f) * Mathf.Clamp01((side - .25f) / .1f);
                col = Color.Lerp(col, c2, a);
                if (a > 0 && a < 1) col *= .85f; // borda da faixa
            }

            // garganta do cadarço: tira de couro mais escura com os ilhoses
            if (topDist < .065f && s > .42f && s < .64f)
            {
                col = Color.Lerp(col, c1 * .7f, Mathf.Clamp01((.065f - topDist) / .008f));
                float eyelet = new Vector2((Mathf.Repeat((s - .445f) / .036f, 1f) - .5f) * 2.2f, (topDist - .052f) / .009f).magnitude;
                if (eyelet < 1f) col = Color.Lerp(col, new Color(.05f, .05f, .05f), .8f);
            }

            // costura junto ao solado e a parte de baixo (escondida pela placa) na cor do solado
            if (h < -.5f) col = Color.Lerp(col, sole, Mathf.Clamp01((-.5f - h) / .03f));
            else if (Mathf.Abs(h + .42f) < .012f && Mathf.Repeat(s * 160f, 1f) < .6f) col *= .7f;
            col.a = 1;
            return (Color32)col;
        }

        // ---------- foto de estúdio para a interface ----------
        static readonly Dictionary<string, Sprite> photoCache = new Dictionary<string, Sprite>();
        const int Layer = 30;

        /// <summary>Foto da chuteira direita de perfil (bico para a direita), com luz de estúdio e fundo transparente.</summary>
        public static Sprite Photo(BootStyle b, int width = 768)
        {
            string k = Key(b) + width;
            if (photoCache.TryGetValue(k, out var sp) && sp != null) return sp;
            try { sp = RenderPhoto(b, width); }
            catch (Exception e) { Debug.LogWarning("[Camisa 10] Foto da chuteira falhou: " + e.Message); sp = null; }
            if (photoCache.Count > 12) photoCache.Clear(); // as cores mudam muito na tela de personalizar
            return photoCache[k] = sp;
        }

        static Sprite RenderPhoto(BootStyle b, int width)
        {
            int height = width * 5 / 8;
            var g = Build(new Vector3(-.082f, .084f, -.022f), .178f, true);
            var mesh = new Mesh { name = "ChuteiraFoto" };
            mesh.SetVertices(g.V); mesh.SetNormals(g.N); mesh.SetUVs(0, g.UV); mesh.SetTriangles(g.T, 0);
            mesh.RecalculateBounds();

            var stage = new GameObject("EstudioChuteira");
            stage.transform.position = new Vector3(0, -900, 0);
            var root = new GameObject("Chuteira") { layer = Layer };
            root.transform.SetParent(stage.transform, false);
            root.transform.localPosition = new Vector3(-.082f, 0, -.051f); // centro da chuteira na origem do estúdio
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mat = new Material(Material(b));
            root.AddComponent<MeshRenderer>().sharedMaterial = mat;
            // gira um pouco para mostrar o peito do pé
            var pivot = new GameObject("Pivo").transform;
            pivot.SetParent(stage.transform, false);
            root.transform.SetParent(pivot, true);
            pivot.localRotation = Quaternion.Euler(0, -32, 0);

            void L(Vector3 euler, float intensity, Color c)
            {
                var lg = new GameObject("Luz").AddComponent<Light>();
                lg.transform.SetParent(stage.transform, false);
                lg.type = LightType.Directional; lg.intensity = intensity; lg.color = c;
                lg.transform.rotation = Quaternion.Euler(euler);
                lg.cullingMask = 1 << Layer;
                lg.shadows = LightShadows.None;
            }
            L(new Vector3(40, -60, 0), 1.15f, new Color(1f, .97f, .92f));
            L(new Vector3(15, 120, 0), .55f, new Color(.85f, .9f, 1f));
            L(new Vector3(-30, -90, 0), .35f, Color.white);

            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.transform.SetParent(stage.transform, false);
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.orthographic = true;
            cam.orthographicSize = .105f;
            cam.aspect = width / (float)height;
            cam.nearClipPlane = .01f; cam.farClipPlane = 5;
            var look = stage.transform.position + new Vector3(0, .05f, 0);
            cam.transform.position = look + Quaternion.Euler(24, -90, 0) * new Vector3(0, 0, -1.2f);
            cam.transform.LookAt(look);
            cam.enabled = false;

            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 4);
            cam.targetTexture = rt;
            var prevMode = RenderSettings.ambientMode; var prevAmb = RenderSettings.ambientLight; var prevFog = RenderSettings.fog;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.36f, .36f, .38f);
            RenderSettings.fog = false;
            cam.Render();
            RenderSettings.ambientMode = prevMode; RenderSettings.ambientLight = prevAmb; RenderSettings.fog = prevFog;

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, true) { hideFlags = HideFlags.DontUnloadUnusedAsset };
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply(true, true);
            RenderTexture.active = prev;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(stage);
            UnityEngine.Object.DestroyImmediate(mesh);
            UnityEngine.Object.DestroyImmediate(mat);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 100);
        }

        // estilos dos outros jogadores: poucos e fixos, para as texturas serem reaproveitadas na partida
        static readonly BootStyle[] Presets =
        {
            new BootStyle { c1 = "#111111", c2 = "#FFFFFF", sole = "#222222" },
            new BootStyle { c1 = "#F5F5F5", c2 = "#111111", sole = "#B0BEC5" },
            new BootStyle { c1 = "#F2C230", c2 = "#111111", sole = "#FFFFFF" },
            new BootStyle { c1 = "#E53935", c2 = "#FFFFFF", sole = "#222222" },
            new BootStyle { c1 = "#00ACC1", c2 = "#0D47A1", sole = "#FFFFFF" },
            new BootStyle { c1 = "#FF4081", c2 = "#FFD54F", sole = "#222222" },
            new BootStyle { c1 = "#ECEFF1", c2 = "#3949AB", sole = "#B0BEC5" },
            new BootStyle { c1 = "#FF7043", c2 = "#111111", sole = "#222222" },
        };

        /// <summary>Chuteira sorteada entre os estilos comuns (para os outros jogadores em campo).</summary>
        public static BootStyle Random() => Presets[Rng.RangeInt(0, Presets.Length - 1)];
    }
}
