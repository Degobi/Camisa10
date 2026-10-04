using System;
using System.Collections.Generic;

namespace Camisa10.UI.Art
{
    // Rasterizador 2D com antisserrilhado, sem dependência da Unity (dá para testar fora do editor).
    // Coordenadas de desenho em "unidades de design" com y para baixo; Scale converte para pixels.

    public struct V2
    {
        public float x, y;
        public V2(float x, float y) { this.x = x; this.y = y; }
    }

    public struct Rgba
    {
        public float r, g, b, a;
        public Rgba(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }

        public static Rgba Hex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return new Rgba(1, 1, 1);
            hex = hex.TrimStart('#');
            int v = Convert.ToInt32(hex.Substring(0, 6), 16);
            return new Rgba(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f);
        }

        public Rgba Mul(float k) => new Rgba(Clamp(r * k), Clamp(g * k), Clamp(b * k), a);
        public Rgba Lerp(Rgba o, float t) => new Rgba(r + (o.r - r) * t, g + (o.g - g) * t, b + (o.b - b) * t, a + (o.a - a) * t);
        public Rgba WithA(float na) => new Rgba(r, g, b, na);
        public float Luma => .299f * r + .587f * g + .114f * b;
        static float Clamp(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
    }

    public sealed class Shape
    {
        public readonly List<V2> Pts = new List<V2>();
        V2 cur;

        public static Shape M(float x, float y) { var s = new Shape(); s.cur = new V2(x, y); s.Pts.Add(s.cur); return s; }
        public Shape L(float x, float y) { cur = new V2(x, y); Pts.Add(cur); return this; }

        public Shape C(float x1, float y1, float x2, float y2, float x, float y)
        {
            V2 p0 = cur;
            for (int i = 1; i <= 16; i++)
            {
                float t = i / 16f, u = 1 - t;
                float a = u * u * u, b = 3 * u * u * t, c = 3 * u * t * t, d = t * t * t;
                Pts.Add(new V2(a * p0.x + b * x1 + c * x2 + d * x, a * p0.y + b * y1 + c * y2 + d * y));
            }
            cur = new V2(x, y);
            return this;
        }

        public static Shape Ellipse(float cx, float cy, float rx, float ry, int n = 72)
        {
            var s = new Shape();
            for (int i = 0; i < n; i++)
            {
                double t = i * Math.PI * 2 / n;
                s.Pts.Add(new V2(cx + rx * (float)Math.Cos(t), cy + ry * (float)Math.Sin(t)));
            }
            return s;
        }

        public static Shape Star(float cx, float cy, float r, int points = 5, float inner = .42f, float rotDeg = -90)
        {
            var s = new Shape();
            for (int i = 0; i < points * 2; i++)
            {
                double t = (rotDeg + i * 180.0 / points) * Math.PI / 180;
                float rr = i % 2 == 0 ? r : r * inner;
                s.Pts.Add(new V2(cx + rr * (float)Math.Cos(t), cy + rr * (float)Math.Sin(t)));
            }
            return s;
        }

        /// <summary>Retângulo girado em torno do próprio centro.</summary>
        public static Shape Box(float cx, float cy, float w, float h, float rotDeg = 0)
        {
            var s = new Shape();
            double t = rotDeg * Math.PI / 180;
            float c = (float)Math.Cos(t), sn = (float)Math.Sin(t);
            foreach (var (dx, dy) in new[] { (-w / 2, -h / 2), (w / 2, -h / 2), (w / 2, h / 2), (-w / 2, h / 2) })
                s.Pts.Add(new V2(cx + dx * c - dy * sn, cy + dx * sn + dy * c));
            return s;
        }

        public Shape Scaled(float cx, float cy, float kx, float ky)
        {
            var s = new Shape();
            foreach (var p in Pts) s.Pts.Add(new V2(cx + (p.x - cx) * kx, cy + (p.y - cy) * ky));
            return s;
        }

        public Shape Moved(float dx, float dy)
        {
            var s = new Shape();
            foreach (var p in Pts) s.Pts.Add(new V2(p.x + dx, p.y + dy));
            return s;
        }

        public void Bounds(out float x0, out float y0, out float x1, out float y1)
        {
            x0 = y0 = float.MaxValue; x1 = y1 = float.MinValue;
            foreach (var p in Pts)
            {
                if (p.x < x0) x0 = p.x; if (p.x > x1) x1 = p.x;
                if (p.y < y0) y0 = p.y; if (p.y > y1) y1 = p.y;
            }
        }
    }

    public sealed class Raster
    {
        public readonly int W, H;
        public readonly float Scale;
        readonly float[] px; // RGBA não pré-multiplicado, linha 0 = topo
        const int Sub = 4;   // sublinhas por pixel (antisserrilhado vertical); o horizontal é analítico

        public Raster(int w, int h, float scale)
        {
            W = w; H = h; Scale = scale;
            px = new float[w * h * 4];
        }

        /// <summary>Preenche o polígono; shade recebe (x, y) em unidades de design e devolve a cor do ponto.</summary>
        public void Fill(Shape s, Func<float, float, Rgba> shade)
        {
            var pts = s.Pts;
            int n = pts.Count;
            if (n < 3) return;
            s.Bounds(out float bx0, out float by0, out float bx1, out float by1);
            int py0 = Math.Max(0, (int)Math.Floor(by0 * Scale)), py1 = Math.Min(H - 1, (int)Math.Ceiling(by1 * Scale));
            var cov = new float[W];
            var xs = new List<float>(16);
            for (int py = py0; py <= py1; py++)
            {
                int lo = W, hi = -1;
                for (int k = 0; k < Sub; k++)
                {
                    float y = (py + (k + .5f) / Sub) / Scale;
                    xs.Clear();
                    for (int i = 0, j = n - 1; i < n; j = i++)
                    {
                        V2 a = pts[i], b = pts[j];
                        if ((a.y > y) != (b.y > y))
                            xs.Add((a.x + (y - a.y) / (b.y - a.y) * (b.x - a.x)) * Scale);
                    }
                    xs.Sort();
                    for (int q = 0; q + 1 < xs.Count; q += 2)
                    {
                        float x0 = Math.Max(0, xs[q]), x1 = Math.Min(W, xs[q + 1]);
                        if (x1 <= x0) continue;
                        int i0 = (int)x0, i1 = Math.Min(W - 1, (int)x1);
                        for (int ix = i0; ix <= i1; ix++)
                        {
                            float o = Math.Min(x1, ix + 1) - Math.Max(x0, ix);
                            if (o > 0) cov[ix] += o / Sub;
                        }
                        if (i0 < lo) lo = i0;
                        if (i1 > hi) hi = i1;
                    }
                }
                for (int ix = lo; ix <= hi; ix++)
                {
                    float c = cov[ix];
                    cov[ix] = 0;
                    if (c <= .001f) continue;
                    var col = shade((ix + .5f) / Scale, (py + .5f) / Scale);
                    Blend(py * W + ix, col, Math.Min(1, c) * col.a);
                }
            }
        }

        public void Fill(Shape s, Rgba col) => Fill(s, (x, y) => col);

        void Blend(int i, Rgba c, float a)
        {
            int o = i * 4;
            float da = px[o + 3], na = a + da * (1 - a);
            if (na <= 0) return;
            float k = da * (1 - a);
            px[o] = (c.r * a + px[o] * k) / na;
            px[o + 1] = (c.g * a + px[o + 1] * k) / na;
            px[o + 2] = (c.b * a + px[o + 2] * k) / na;
            px[o + 3] = na;
        }

        /// <summary>Bytes RGBA32 com a linha 0 embaixo (formato das texturas da Unity).</summary>
        public byte[] ToRgba32BottomUp()
        {
            var bytes = new byte[W * H * 4];
            for (int y = 0; y < H; y++)
            {
                int src = y * W * 4, dst = (H - 1 - y) * W * 4;
                for (int i = 0; i < W * 4; i++)
                    bytes[dst + i] = (byte)(Math.Max(0, Math.Min(1, px[src + i])) * 255 + .5f);
            }
            return bytes;
        }

        /// <summary>Ruído de valor suave (0..1), para textura de tecido e de metal.</summary>
        public static float Noise(float x, float y)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), d = Hash(xi + 1, yi + 1);
            return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        public static float Smooth(float e0, float e1, float v)
        {
            float t = Math.Max(0, Math.Min(1, (v - e0) / (e1 - e0)));
            return t * t * (3 - 2 * t);
        }
    }
}
