using System.Collections.Generic;
using Camisa10.Core;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Gera as imagens do jogo em tempo de execução (cantos arredondados, escudos,
    /// listras da figurinha e a chuteira personalizável). Assim o projeto não precisa de assets.
    /// </summary>
    public static partial class Procedural
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            return t;
        }

        public static Sprite RoundedSprite(int size, int radius)
        {
            var tex = NewTex(size, size);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + .5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + .5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(cx, cy));
                    byte a = (byte)(Mathf.Clamp01(radius - d + .5f) * 255);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }

        /// <summary>
        /// Fundo dos menus: degradê azul-noite com faixas diagonais de luz, no clima das telas de modo carreira.
        /// </summary>
        public static Sprite BackdropSprite()
        {
            if (cache.TryGetValue("backdrop", out var s)) return s;
            int w = 480, h = 270;
            var tex = NewTex(w, h);
            var px = new Color32[w * h];
            Color top = Theme.BgTop, bottom = Theme.Bg, glow = Theme.Turf, glow2 = Theme.Cyan;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)w, ny = y / (float)h;
                    Color c = Color.Lerp(bottom, top, Mathf.SmoothStep(0, 1, ny * .9f + nx * .25f));
                    // faixas diagonais suaves
                    float d1 = Mathf.Abs((nx * 1.2f - ny) - .55f), d2 = Mathf.Abs((nx * 1.2f - ny) - .78f);
                    c = Color.Lerp(c, glow, Mathf.Clamp01(1 - d1 / .05f) * .07f);
                    c = Color.Lerp(c, glow2, Mathf.Clamp01(1 - d2 / .12f) * .06f);
                    // vinheta
                    float vx = nx - .5f, vy = ny - .5f;
                    c = Color.Lerp(c, Color.black, Mathf.Clamp01((vx * vx + vy * vy) * 1.1f) * .55f);
                    c.a = 1;
                    px[y * w + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100);
            cache["backdrop"] = s;
            return s;
        }

        /// <summary>Degradê horizontal ou vertical de branco opaco para transparente (tingido pela cor da Image).</summary>
        public static Sprite FadeSprite(bool vertical)
        {
            string key = vertical ? "fadeV" : "fadeH";
            if (cache.TryGetValue(key, out var s)) return s;
            int w = vertical ? 4 : 128, h = vertical ? 128 : 4;
            var tex = NewTex(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float t = vertical ? y / (float)(h - 1) : 1 - x / (float)(w - 1);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.SmoothStep(0, 1, t) * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100);
            cache[key] = s;
            return s;
        }

        /// <summary>Faixa listrada da figurinha com as cores do clube.</summary>
        public static Sprite StripesSprite(string c1, string c2)
        {
            string key = "stripes" + c1 + c2;
            if (cache.TryGetValue(key, out var s)) return s;
            int w = 540, h = 120;
            var tex = NewTex(w, h);
            var px = new Color32[w * h];
            Color32 a = Theme.Hex(c1), b = Theme.Hex(c2);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = ((x + y * 0.45f) % 56) < 28 ? a : b;
            tex.SetPixels32(px);
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100);
            cache[key] = s;
            return s;
        }

        // ---------- chuteira ----------
        class Path
        {
            public readonly List<Vector2> Pts = new List<Vector2>();
            Vector2 cur;
            public Path(float x, float y) { cur = new Vector2(x, y); Pts.Add(cur); }
            public Path L(float x, float y) { cur = new Vector2(x, y); Pts.Add(cur); return this; }
            public Path C(float x1, float y1, float x2, float y2, float x, float y)
            {
                Vector2 p0 = cur, p1 = new Vector2(x1, y1), p2 = new Vector2(x2, y2), p3 = new Vector2(x, y);
                for (int i = 1; i <= 14; i++)
                {
                    float t = i / 14f, u = 1 - t;
                    Pts.Add(u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3);
                }
                cur = p3;
                return this;
            }
        }

        static Path RectPath(float x, float y, float w, float h) => new Path(x, y).L(x + w, y).L(x + w, y + h).L(x, y + h);

        static Path Line(float x1, float y1, float x2, float y2, float width)
        {
            var d = new Vector2(x2 - x1, y2 - y1).normalized;
            var n = new Vector2(-d.y, d.x) * (width / 2);
            return new Path(x1 + n.x, y1 + n.y).L(x2 + n.x, y2 + n.y).L(x2 - n.x, y2 - n.y).L(x1 - n.x, y1 - n.y);
        }

        static bool Inside(List<Vector2> poly, float x, float y)
        {
            bool c = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                Vector2 a = poly[i], b = poly[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) c = !c;
            }
            return c;
        }

        // Coordenadas no espaço 250x120 (y para baixo), rasterizadas em escala 2x.
        static void Fill(Color32[] buf, int w, int h, float scale, List<Vector2> poly, Color32 col)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in poly) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            int x0 = Mathf.Max(0, (int)(minX * scale)), x1 = Mathf.Min(w - 1, (int)(maxX * scale) + 1);
            int y0 = Mathf.Max(0, (int)(minY * scale)), y1 = Mathf.Min(h - 1, (int)(maxY * scale) + 1);
            for (int py = y0; py <= y1; py++)
                for (int px = x0; px <= x1; px++)
                    if (Inside(poly, (px + .5f) / scale, (py + .5f) / scale))
                        buf[(h - 1 - py) * w + px] = col; // inverte y: textura cresce para cima
        }

        /// <summary>Silhueta de camisa de futebol (tela de criação e de aposentadoria).</summary>
        public static Sprite ShirtSprite()
        {
            if (cache.TryGetValue("shirt", out var cached)) return cached;
            const float scale = 2f;
            int w = 400, h = 400;
            var tex = NewTex(w, h);
            var buf = new Color32[w * h];
            var shirt = new Path(62, 18).L(86, 10).C(92, 20, 108, 20, 114, 10).L(138, 18).L(182, 50).L(162, 82).L(146, 72)
                .L(146, 188).L(54, 188).L(54, 72).L(38, 82).L(18, 50);
            var border = new List<Vector2>();
            foreach (var p in shirt.Pts) border.Add(new Vector2(100 + (p.x - 100) * 1.035f, 100 + (p.y - 100) * 1.03f));
            Fill(buf, w, h, scale, border, Theme.Gold);
            Fill(buf, w, h, scale, shirt.Pts, Theme.ShirtGreen);
            tex.SetPixels32(buf);
            tex.Apply();
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100);
            cache["shirt"] = s;
            return s;
        }
    }
}
