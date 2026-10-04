using System.Collections.Generic;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>Uniforme de um clube: padrão da camisa, cores, calção e meião.</summary>
    public sealed class Kit
    {
        public enum Pattern { Solid, Stripes, Hoops, Sash, Band, Sleeves }
        public Pattern P;
        public Color[] Body;            // cores do padrão (listras, aros...); Body[0] é a principal
        public Color Trim, Sleeve, Shorts, ShortsTrim, Socks, SocksTrim, Number;
        public string Sponsor;          // marca fictícia no peito
        public bool Keeper;

        static Color H(string hex) => Theme.Hex(hex);

        static Kit K(Pattern p, string[] body, string trim, string shorts, string socks, string number, string sleeve = null, string shortsTrim = null, string socksTrim = null)
        {
            var b = new Color[body.Length];
            for (int i = 0; i < body.Length; i++) b[i] = H(body[i]);
            return new Kit
            {
                P = p, Body = b, Trim = H(trim), Shorts = H(shorts), Socks = H(socks), Number = H(number),
                Sleeve = sleeve != null ? H(sleeve) : b[0],
                ShortsTrim = H(shortsTrim ?? trim), SocksTrim = H(socksTrim ?? trim),
            };
        }

        // uniformes 1 (aproximados) dos clubes do jogo
        static readonly Dictionary<string, Kit> Table = new Dictionary<string, Kit>
        {
            ["Flamengo"] = K(Pattern.Hoops, new[] { "#C8102E", "#111111" }, "#111111", "#FFFFFF", "#C8102E", "#FFFFFF", null, "#C8102E", "#111111"),
            ["Palmeiras"] = K(Pattern.Solid, new[] { "#006437" }, "#FFFFFF", "#FFFFFF", "#006437", "#FFFFFF"),
            ["Botafogo"] = K(Pattern.Stripes, new[] { "#111111", "#FFFFFF" }, "#111111", "#111111", "#111111", "#FFFFFF", "#111111"),
            ["Cruzeiro"] = K(Pattern.Solid, new[] { "#003DA5" }, "#FFFFFF", "#FFFFFF", "#FFFFFF", "#FFFFFF", null, "#003DA5", "#003DA5"),
            ["Fluminense"] = K(Pattern.Stripes, new[] { "#7A0026", "#FFFFFF", "#00613C", "#FFFFFF" }, "#FFFFFF", "#FFFFFF", "#FFFFFF", "#FFFFFF", "#7A0026", "#7A0026", "#7A0026"),
            ["São Paulo"] = K(Pattern.Band, new[] { "#FFFFFF", "#C8102E", "#111111" }, "#C8102E", "#FFFFFF", "#FFFFFF", "#111111", null, "#C8102E", "#C8102E"),
            ["Corinthians"] = K(Pattern.Solid, new[] { "#FFFFFF" }, "#111111", "#111111", "#FFFFFF", "#111111", null, "#FFFFFF", "#111111"),
            ["Bahia"] = K(Pattern.Band, new[] { "#FFFFFF", "#0057B8", "#E30613" }, "#0057B8", "#0057B8", "#FFFFFF", "#0057B8", null, "#E30613", "#E30613"),
            ["Vasco da Gama"] = K(Pattern.Sash, new[] { "#111111", "#FFFFFF" }, "#FFFFFF", "#111111", "#111111", "#FFFFFF"),
            ["Grêmio"] = K(Pattern.Stripes, new[] { "#0D80BF", "#FFFFFF", "#111111", "#FFFFFF" }, "#111111", "#111111", "#FFFFFF", "#FFFFFF", "#0D80BF", "#0D80BF", "#0D80BF"),
            ["Real Madrid"] = K(Pattern.Solid, new[] { "#F7F7F5" }, "#C9A227", "#F7F7F5", "#F7F7F5", "#1B2A4A"),
            ["Barcelona"] = K(Pattern.Stripes, new[] { "#A50044", "#004D98" }, "#FEBE10", "#004D98", "#004D98", "#FEBE10", "#A50044"),
            ["Atlético de Madrid"] = K(Pattern.Stripes, new[] { "#CB3524", "#FFFFFF" }, "#272E61", "#272E61", "#CB3524", "#272E61", "#CB3524"),
            ["Athletic Bilbao"] = K(Pattern.Stripes, new[] { "#EE2523", "#FFFFFF" }, "#111111", "#111111", "#111111", "#111111", "#EE2523"),
            ["Villarreal"] = K(Pattern.Solid, new[] { "#FFE667" }, "#005187", "#FFE667", "#FFE667", "#005187"),
            ["Real Betis"] = K(Pattern.Stripes, new[] { "#00954C", "#FFFFFF" }, "#00954C", "#FFFFFF", "#00954C", "#00954C", "#00954C"),
            ["Real Sociedad"] = K(Pattern.Stripes, new[] { "#0067B1", "#FFFFFF" }, "#0067B1", "#FFFFFF", "#0067B1", "#0067B1", "#0067B1"),
            ["Sevilla"] = K(Pattern.Solid, new[] { "#FFFFFF" }, "#D71920", "#FFFFFF", "#FFFFFF", "#D71920"),
            ["Valencia"] = K(Pattern.Solid, new[] { "#FFFFFF" }, "#111111", "#111111", "#111111", "#111111"),
            ["Celta de Vigo"] = K(Pattern.Solid, new[] { "#8AC3EE" }, "#C8102E", "#FFFFFF", "#8AC3EE", "#C8102E"),
            ["Liverpool"] = K(Pattern.Solid, new[] { "#C8102E" }, "#FFFFFF", "#C8102E", "#C8102E", "#FFFFFF"),
            ["Manchester City"] = K(Pattern.Solid, new[] { "#6CABDD" }, "#1C2C5B", "#FFFFFF", "#6CABDD", "#FFFFFF"),
            ["Arsenal"] = K(Pattern.Sleeves, new[] { "#EF0107" }, "#FFFFFF", "#FFFFFF", "#FFFFFF", "#FFFFFF", "#FFFFFF", "#EF0107", "#EF0107"),
            ["Chelsea"] = K(Pattern.Solid, new[] { "#034694" }, "#FFFFFF", "#034694", "#FFFFFF", "#FFFFFF", null, "#FFFFFF", "#034694"),
            ["Manchester United"] = K(Pattern.Solid, new[] { "#DA291C" }, "#111111", "#FFFFFF", "#111111", "#FFFFFF"),
            ["Newcastle"] = K(Pattern.Stripes, new[] { "#111111", "#FFFFFF" }, "#111111", "#111111", "#111111", "#FFFFFF", "#111111"),
            ["Tottenham"] = K(Pattern.Solid, new[] { "#FFFFFF" }, "#132257", "#132257", "#FFFFFF", "#132257"),
            ["Aston Villa"] = K(Pattern.Sleeves, new[] { "#670E36" }, "#95BFE5", "#FFFFFF", "#95BFE5", "#FFFFFF", "#95BFE5"),
            ["Brighton"] = K(Pattern.Stripes, new[] { "#0057B8", "#FFFFFF" }, "#0057B8", "#0057B8", "#FFFFFF", "#0057B8", "#0057B8"),
            ["West Ham"] = K(Pattern.Sleeves, new[] { "#7A263A" }, "#1BB1E7", "#FFFFFF", "#FFFFFF", "#FFFFFF", "#1BB1E7"),
            ["Inter de Milão"] = K(Pattern.Stripes, new[] { "#0068A8", "#111111" }, "#111111", "#111111", "#111111", "#FFFFFF", "#0068A8"),
            ["Milan"] = K(Pattern.Stripes, new[] { "#FB090B", "#111111" }, "#111111", "#FFFFFF", "#111111", "#FFFFFF", "#FB090B"),
            ["Juventus"] = K(Pattern.Stripes, new[] { "#111111", "#FFFFFF" }, "#111111", "#111111", "#111111", "#FFFFFF", "#111111"),
            ["Atalanta"] = K(Pattern.Stripes, new[] { "#1E71B8", "#111111" }, "#111111", "#111111", "#111111", "#FFFFFF", "#1E71B8"),
            ["Napoli"] = K(Pattern.Solid, new[] { "#12A0D7" }, "#FFFFFF", "#FFFFFF", "#12A0D7", "#FFFFFF"),
            ["Roma"] = K(Pattern.Solid, new[] { "#8E1F2F" }, "#F0BC42", "#8E1F2F", "#8E1F2F", "#F0BC42"),
            ["Bologna"] = K(Pattern.Stripes, new[] { "#A21C26", "#1A2F48" }, "#FFFFFF", "#FFFFFF", "#1A2F48", "#FFFFFF", "#A21C26"),
            ["Paris Saint-Germain"] = K(Pattern.Band, new[] { "#004170", "#DA291C", "#FFFFFF" }, "#DA291C", "#004170", "#004170", "#FFFFFF"),
            ["Olympique de Marseille"] = K(Pattern.Solid, new[] { "#FFFFFF" }, "#2FAEE0", "#FFFFFF", "#FFFFFF", "#2FAEE0"),
            ["Lens"] = K(Pattern.Solid, new[] { "#FFD100" }, "#E30613", "#111111", "#FFD100", "#E30613"),
            ["Galatasaray"] = K(Pattern.Sash, new[] { "#FDB912", "#A90432" }, "#A90432", "#A90432", "#A90432", "#A90432"),
            ["Fenerbahçe"] = K(Pattern.Stripes, new[] { "#FFED00", "#002D72" }, "#002D72", "#FFFFFF", "#002D72", "#002D72", "#002D72"),
            ["Beşiktaş"] = K(Pattern.Stripes, new[] { "#111111", "#FFFFFF" }, "#111111", "#111111", "#111111", "#FFFFFF", "#111111"),
            ["Al-Hilal"] = K(Pattern.Solid, new[] { "#005BAC" }, "#FFFFFF", "#FFFFFF", "#005BAC", "#FFFFFF"),
            ["Al-Nassr"] = K(Pattern.Solid, new[] { "#FFE600" }, "#0033A0", "#0033A0", "#FFE600", "#0033A0"),
            ["Al-Ittihad"] = K(Pattern.Stripes, new[] { "#FFE500", "#111111" }, "#111111", "#111111", "#111111", "#111111", "#FFE500"),
            ["Inter Miami"] = K(Pattern.Solid, new[] { "#F7B5CD" }, "#231F20", "#F7B5CD", "#F7B5CD", "#231F20"),
            ["LAFC"] = K(Pattern.Solid, new[] { "#111111" }, "#C39E6D", "#111111", "#111111", "#C39E6D"),
            // seleções
            ["Brasil"] = K(Pattern.Solid, new[] { "#FEDD00" }, "#009C3B", "#0033A0", "#FFFFFF", "#0033A0", null, "#FFFFFF", "#009C3B"),
            ["Argentina"] = K(Pattern.Stripes, new[] { "#75AADB", "#FFFFFF" }, "#111111", "#111111", "#FFFFFF", "#111111", "#75AADB"),
            ["França"] = K(Pattern.Solid, new[] { "#1F2D5C" }, "#C8102E", "#FFFFFF", "#C8102E", "#FFFFFF"),
            ["Alemanha"] = K(Pattern.Solid, new[] { "#FFFFFF" }, "#111111", "#111111", "#FFFFFF", "#111111"),
            ["Inglaterra"] = K(Pattern.Solid, new[] { "#FFFFFF" }, "#1F2D5C", "#1F2D5C", "#FFFFFF", "#1F2D5C"),
            ["Espanha"] = K(Pattern.Solid, new[] { "#C60B1E" }, "#FFC400", "#1F2D5C", "#1F2D5C", "#FFC400"),
            ["Portugal"] = K(Pattern.Solid, new[] { "#C8102E" }, "#046A38", "#046A38", "#C8102E", "#FFD700"),
            ["Holanda"] = K(Pattern.Solid, new[] { "#F36C21" }, "#111111", "#111111", "#F36C21", "#111111"),
        };

        static readonly string[] Sponsors = { "BANCO NUVEM", "PIXELFORGE", "RAIO DRINK", "CRONOS", "PAGO+", "TURBO MAX", "ARENA PLAY", "MERIDIAN", "CONTA ÁGIL", "NEXTLEVEL" };

        /// <summary>Uniforme do clube (tabela acima; para os demais, camisa lisa nas cores do clube).</summary>
        public static Kit For(string club, string c1, string c2)
        {
            Kit k;
            if (!Table.TryGetValue(club ?? "", out k))
                k = K(Pattern.Solid, new[] { c1 }, c2, c2, c1, Luma(H(c1)) > .6f ? "#111111" : "#FFFFFF");
            k.Sponsor = Sponsors[Mathf.Abs((club ?? "").GetHashCode()) % Sponsors.Length];
            return k;
        }

        /// <summary>Goleiro: manga comprida numa cor que não confunde com nenhum dos times.</summary>
        public static Kit Goalkeeper(string hex = "#C6E03A")
        {
            var k = K(Pattern.Solid, new[] { hex }, "#222222", "#222222", hex, "#111111");
            k.Keeper = true;
            k.Sponsor = "";
            return k;
        }

        static float Luma(Color c) => .299f * c.r + .587f * c.g + .114f * c.b;
        public Color Main => Body[0];
    }

    /// <summary>Texturas do uniforme (camisa com número e patrocinador, calção, meião), em cache por clube e número.</summary>
    public static class KitArt
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        static string Key(Kit k, string what, int n) => what + ColorUtility.ToHtmlStringRGB(k.Main) + (int)k.P + k.Body.Length + ColorUtility.ToHtmlStringRGB(k.Trim) + ColorUtility.ToHtmlStringRGB(k.Shorts) + k.Sponsor + n;

        static Texture2D Keep(string key, Texture2D t)
        {
            t.hideFlags = HideFlags.DontUnloadUnusedAsset;
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            cache[key] = t;
            return t;
        }

        /// <summary>Camisa: tronco em v 0..0,8 (frente em u 0,25, costas em 0,75) e mangas na faixa de cima.</summary>
        public static Texture2D Shirt(Kit k, int number)
        {
            string key = Key(k, "camisa", number);
            if (cache.TryGetValue(key, out var hit) && hit != null) return hit;
            const int W = 512, Hh = 512;
            var px = new Color32[W * Hh];
            for (int y = 0; y < Hh; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + .5f) / W, v = (y + .5f) / Hh;
                    Color c;
                    if (v >= HumanModel.SleeveV0 - .02f)
                    {
                        // manga: cor da manga, punho na ponta (manga comprida do goleiro sem punho)
                        c = k.P == Kit.Pattern.Sleeves ? k.Sleeve : k.Sleeve;
                        if (!k.Keeper && u > .86f) c = k.Trim;
                    }
                    else
                    {
                        float tv = v / HumanModel.TorsoV; // 0 = barra, 1 = gola
                        c = Body(k, u, tv);
                        if (tv > .978f) c = k.Trim;                                       // gola fina
                        else if (tv > .955f && Mathf.Abs(Mathf.Repeat(u - .25f + .5f, 1f) - .5f) < .03f) c = k.Trim; // decote em V na frente
                    }
                    // trama do tecido: leve variação
                    float n = 1f + (((x * 7 + y * 13) & 3) - 1.5f) * .012f;
                    px[y * W + x] = new Color(c.r * n, c.g * n, c.b * n, 1);
                }
            var baseTex = new Texture2D(W, Hh, TextureFormat.RGBA32, false);
            baseTex.SetPixels32(px);
            baseTex.Apply();

            // número nas costas (com contorno) e patrocinador no peito
            var labels = new List<TextPaint.Label>();
            var numCol = k.Number;
            var outline = Luma(numCol) > .5f ? new Color(0, 0, 0, .85f) : new Color(1, 1, 1, .85f);
            float aspect = .72f; // u vale mais metros que v: achata o texto na horizontal
            labels.Add(new TextPaint.Label { Text = number.ToString(), Center = new Vector2(.75f, .47f * HumanModel.TorsoV / .8f), Height = .27f, MaxWidth = .2f, Color = numCol, Outline = outline, XScale = aspect });
            if (!string.IsNullOrEmpty(k.Sponsor))
                labels.Add(new TextPaint.Label { Text = k.Sponsor, Center = new Vector2(.25f, .43f), Height = .075f, MaxWidth = .23f, Color = SponsorColor(k), XScale = aspect });
            labels.Add(new TextPaint.Label { Text = number.ToString(), Center = new Vector2(.31f, .66f), Height = .07f, MaxWidth = .05f, Color = numCol, XScale = aspect });
            var tex = TextPaint.Paint(baseTex, labels);
            Object.DestroyImmediate(baseTex);
            return Keep(key, tex);
        }

        static float Luma(Color c) => .299f * c.r + .587f * c.g + .114f * c.b;

        static Color SponsorColor(Kit k)
        {
            var c = k.Number;
            return Mathf.Abs(Luma(c) - Luma(k.Main)) < .25f ? (Luma(k.Main) > .5f ? new Color(.08f, .08f, .1f) : Color.white) : c;
        }

        /// <summary>Cor do padrão no tronco (u em volta do corpo, tv da barra até a gola).</summary>
        static Color Body(Kit k, float u, float tv)
        {
            var b = k.Body;
            switch (k.P)
            {
                case Kit.Pattern.Stripes:
                {
                    // listras verticais (~14 em volta do corpo); a lista pode ter listra fina branca entre as cores
                    int n = b.Length;
                    float stripes = 14f;
                    float s = u * stripes;
                    int i = Mathf.FloorToInt(s);
                    if (n == 4)
                    {
                        // [cor A, fina, cor B, fina]: as finas ocupam 20% de cada par
                        float f = s - i;
                        bool thin = f > .82f;
                        return thin ? b[1] : (i % 2 == 0 ? b[0] : b[2]);
                    }
                    return b[i % n];
                }
                case Kit.Pattern.Hoops:
                    return b[Mathf.FloorToInt(tv * 7.5f) % b.Length];
                case Kit.Pattern.Sash:
                {
                    // faixa diagonal na frente e nas costas (ombro direito até o quadril esquerdo)
                    float f = Mathf.Repeat(u, .5f) * 4f - 1f; // -1..1 em cada metade
                    float line = .9f - (f + 1f) * .42f;
                    return Mathf.Abs(tv - line) < .11f ? b[1] : b[0];
                }
                case Kit.Pattern.Band:
                {
                    // faixas horizontais no peito (São Paulo, Bahia)
                    if (tv > .56f && tv < .63f) return b[1];
                    if (b.Length > 2 && tv > .64f && tv < .71f) return b[2];
                    return b[0];
                }
                default:
                    return b[0];
            }
        }

        /// <summary>Calção: cor do calção com filete lateral.</summary>
        public static Texture2D Shorts(Kit k)
        {
            string key = Key(k, "calcao", 0);
            if (cache.TryGetValue(key, out var hit) && hit != null) return hit;
            const int W = 256, Hh = 128;
            var px = new Color32[W * Hh];
            for (int y = 0; y < Hh; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + .5f) / W, v = (y + .5f) / Hh;
                    float side = Mathf.Min(Mathf.Abs(u - .5f), Mathf.Min(u, 1 - u)); // laterais em u = 0/1 e 0,5
                    Color c = side < .025f ? k.ShortsTrim : k.Shorts;
                    if (v > .9f) c = Color.Lerp(k.Shorts, Color.black, .12f); // cós
                    px[y * W + x] = c;
                }
            var t = new Texture2D(W, Hh, TextureFormat.RGBA32, true);
            t.SetPixels32(px);
            t.Apply(true, true);
            return Keep(key, t);
        }

        /// <summary>Meião: cor do meião com faixas no alto e a canela mais justa.</summary>
        public static Texture2D Socks(Kit k)
        {
            string key = Key(k, "meiao", 0);
            if (cache.TryGetValue(key, out var hit) && hit != null) return hit;
            const int W = 16, Hh = 128;
            var px = new Color32[W * Hh];
            for (int y = 0; y < Hh; y++)
            {
                float v = (y + .5f) / Hh;
                Color c = k.Socks;
                if (v > .8f && v < .86f) c = k.SocksTrim;
                if (v > .9f && v < .94f) c = k.SocksTrim;
                if (v < .12f) c = Color.Lerp(k.Socks, Color.black, .1f); // parte que entra na chuteira
                for (int x = 0; x < W; x++) px[y * W + x] = c;
            }
            var t = new Texture2D(W, Hh, TextureFormat.RGBA32, true);
            t.SetPixels32(px);
            t.Apply(true, true);
            return Keep(key, t);
        }
    }

    /// <summary>Escreve textos (números, marcas) por cima de uma textura usando a fonte do jogo e uma câmera temporária.</summary>
    public static class TextPaint
    {
        public struct Label
        {
            public string Text;
            public Vector2 Center;   // em coordenadas de textura (0..1)
            public float Height, MaxWidth, XScale;
            public Color Color;
            public Color? Outline;
        }

        public static Texture2D Paint(Texture2D src, IList<Label> labels)
        {
            int W = src.width, H = src.height;
            var rt = RenderTexture.GetTemporary(W, H, 16, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);

            const int Layer = 31;
            var root = new GameObject("PinturaTemp");
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.transform.SetParent(root.transform, false);
            cam.orthographic = true;
            cam.orthographicSize = .5f;
            cam.aspect = W / (float)H;
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.Nothing;
            cam.targetTexture = rt;
            cam.transform.position = new Vector3(.5f * W / H, .5f, -5);
            cam.enabled = false;

            var font = UIKit.Bold;
            foreach (var l in labels)
            {
                if (string.IsNullOrEmpty(l.Text)) continue;
                var offsets = l.Outline.HasValue
                    ? new[] { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1), new Vector2(0, 1), new Vector2(-.7f, -.7f), new Vector2(.7f, .7f), new Vector2(-.7f, .7f), new Vector2(.7f, -.7f), Vector2.zero }
                    : new[] { Vector2.zero };
                foreach (var o in offsets)
                {
                    var go = new GameObject("Texto") { layer = Layer };
                    go.transform.SetParent(root.transform, false);
                    var tm = go.AddComponent<TextMesh>();
                    tm.font = font;
                    var mr = go.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = font.material;
                    tm.fontSize = 128;
                    tm.anchor = TextAnchor.MiddleCenter;
                    tm.alignment = TextAlignment.Center;
                    tm.text = l.Text;
                    tm.characterSize = .01f;
                    var size = mr.bounds.size;
                    if (size.y < 1e-4f) size = new Vector3(l.Text.Length * .7f, 1.3f, 0);
                    float k = Mathf.Min(l.Height / size.y, l.MaxWidth / Mathf.Max(1e-4f, size.x * l.XScale));
                    tm.characterSize = .01f * k;
                    bool isOutline = o != Vector2.zero;
                    tm.color = isOutline ? l.Outline.Value : l.Color;
                    float px = l.Height * .045f;
                    go.transform.position = new Vector3(l.Center.x * W / H + o.x * px, l.Center.y + o.y * px, isOutline ? .01f : 0);
                    go.transform.localScale = new Vector3(Mathf.Max(.05f, l.XScale), 1, 1);
                    cam.Render();
                    Object.DestroyImmediate(go);
                }
            }

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, true);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply(true, true);
            RenderTexture.active = prev;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(root);
            return tex;
        }
    }
}
