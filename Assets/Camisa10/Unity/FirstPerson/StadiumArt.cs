using System.Collections.Generic;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Texturas do estádio. Fotos reais em Resources/Estadio (grama.png, torcida.png) têm prioridade; sem elas,
    /// gera em código uma grama em ladrilho e uma torcida nas cores do clube. As placas usam as marcas fictícias do jogo.
    /// Ficam em cache: só a primeira partida paga o custo de gerar.
    /// </summary>
    public static class StadiumArt
    {
        // ---------- ruído que se repete sem emenda (para ladrilhos) ----------
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 144665);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        /// <summary>Ruído de valor com período inteiro em x e y: o lado direito casa com o esquerdo.</summary>
        static float TileNoise(float x, float y, int px, int py, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int Wx(int v) => ((v % px) + px) % px;
            int Wy(int v) => ((v % py) + py) % py;
            float a = Hash(Wx(x0), Wy(y0), seed), b = Hash(Wx(x0 + 1), Wy(y0), seed);
            float c = Hash(Wx(x0), Wy(y0 + 1), seed), d = Hash(Wx(x0 + 1), Wy(y0 + 1), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static Texture2D Finish(Texture2D t, Color32[] px, TextureWrapMode wrap)
        {
            t.hideFlags = HideFlags.DontUnloadUnusedAsset; // fica em cache entre as partidas
            t.wrapMode = wrap;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 8;
            t.SetPixels32(px);
            t.Apply(true, true); // sem cópia na memória da CPU depois de enviar para a GPU
            return t;
        }

        // ---------- grama ----------
        static Texture2D grass;

        /// <summary>Ladrilho de 512 px que cobre 4 m de gramado: folhas finas no sentido do corte e manchas suaves.</summary>
        public static Texture2D Grass()
        {
            if (grass != null) return grass;
            // foto real de grama (ladrilhável, cobrindo ~4 m) tem prioridade sobre a gerada
            grass = Resources.Load<Texture2D>("Estadio/grama");
            if (grass != null) { grass.wrapMode = TextureWrapMode.Repeat; return grass; }
            const int S = 512;
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float patch = TileNoise(u * 4, v * 4, 4, 4, 1);           // manchas de ~1 m
                    float clump = TileNoise(u * 24, v * 24, 24, 24, 2);       // touceiras
                    float blade = TileNoise(u * 150, v * 96, 150, 96, 3);     // folhas, um pouco alongadas no sentido do corte
                    float fine = Hash(x, y, 4);
                    float k = .86f + (patch - .5f) * .14f + (clump - .5f) * .16f + (blade - .5f) * .16f + (fine - .5f) * .12f;
                    float dry = Mathf.Clamp01((TileNoise(u * 8, v * 8, 8, 8, 5) - .62f) * 3f) * .35f; // pontos mais secos
                    float r = (52 + dry * 40) * k, g = (98 + dry * 10) * k, b = (36 + dry * 6) * k;
                    px[y * S + x] = new Color32((byte)Mathf.Clamp(r, 0, 255), (byte)Mathf.Clamp(g, 0, 255), (byte)Mathf.Clamp(b, 0, 255), 255);
                }
            grass = Finish(new Texture2D(S, S, TextureFormat.RGBA32, true), px, TextureWrapMode.Repeat);
            grass.name = "Grama";
            return grass;
        }

        // ---------- torcida ----------
        public const int CrowdSeats = 32, CrowdRows = 4;   // pessoas por ladrilho
        public const float SeatWidth = .55f;                // metros por pessoa
        static readonly Dictionary<string, Texture2D> crowds = new Dictionary<string, Texture2D>();

        static readonly Color32[] Skins = { new Color32(236, 196, 160, 255), new Color32(205, 150, 108, 255), new Color32(160, 108, 72, 255), new Color32(112, 74, 50, 255), new Color32(80, 52, 36, 255) };
        static readonly Color32[] Hairs = { new Color32(24, 18, 14, 255), new Color32(52, 36, 24, 255), new Color32(96, 70, 44, 255), new Color32(170, 140, 90, 255), new Color32(140, 140, 140, 255) };
        static readonly Color32[] Casual = { new Color32(200, 200, 200, 255), new Color32(30, 30, 34, 255), new Color32(60, 74, 110, 255), new Color32(96, 98, 106, 255), new Color32(40, 40, 46, 255) };

        /// <summary>Ladrilho de torcida: 4 fileiras de 32 pessoas, a maioria com a camisa do time da casa.</summary>
        public static Texture2D Crowd(Color home1, Color home2, int seed)
        {
            string key = ColorUtility.ToHtmlStringRGB(home1) + ColorUtility.ToHtmlStringRGB(home2) + seed;
            if (crowds.TryGetValue(key, out var cached) && cached != null) return cached;
            // foto real de torcida (4 fileiras de 32 pessoas por ladrilho) tem prioridade sobre a gerada
            var photo = Resources.Load<Texture2D>("Estadio/torcida");
            if (photo != null) { photo.wrapMode = TextureWrapMode.Repeat; return crowds[key] = photo; }

            const int CW = 32, CH = 64, W = CW * CrowdSeats, H = CH * CrowdRows;
            var px = new Color32[W * H];
            var rnd = new System.Random(seed);
            float R() => (float)rnd.NextDouble();
            Color32 seat = Color32.Lerp(home1, new Color32(40, 44, 52, 255), .72f);
            var step = new Color32(58, 60, 66, 255);

            void Put(int x, int y, Color32 c, float shade = 1)
            {
                if (x < 0 || x >= W || y < 0 || y >= H) return;
                px[y * W + x] = new Color32((byte)(c.r * shade), (byte)(c.g * shade), (byte)(c.b * shade), 255);
            }

            for (int row = 0; row < CrowdRows; row++)
            {
                int y0 = row * CH;
                // degrau de concreto e cadeiras
                for (int y = 0; y < CH; y++)
                    for (int x = 0; x < W; x++)
                    {
                        bool tread = y < 12;
                        float s = tread ? .8f + y / 60f : .55f + y / (float)CH * .35f;
                        Put(x, y0 + y, tread ? step : seat, s);
                    }
                for (int col = 0; col < CrowdSeats; col++)
                {
                    if (R() < .06f) continue; // cadeira vazia
                    int cx = col * CW + CW / 2 + (int)((R() - .5f) * 6);
                    Color32 shirt = R() < .62f ? (Color32)home1 : R() < .5f ? (Color32)home2 : Casual[rnd.Next(Casual.Length)];
                    var skin = Skins[rnd.Next(Skins.Length)];
                    var hair = Hairs[rnd.Next(Hairs.Length)];
                    bool standing = R() < .5f, armsUp = R() < .07f, cap = R() < .12f;
                    int lift = standing ? 8 : 0;
                    int tw = 9 + rnd.Next(3), bottom = y0 + 10 + lift, top = y0 + 34 + lift;
                    // tronco com ombros arredondados e luz vindo de cima
                    for (int y = bottom; y < top; y++)
                    {
                        float t = (y - bottom) / (float)(top - bottom);
                        int half = t > .8f ? Mathf.RoundToInt(tw * Mathf.Sqrt(1 - (t - .8f) / .2f * .7f)) : tw;
                        for (int x = -half; x <= half; x++) Put(cx + x, y, shirt, .62f + t * .45f - Mathf.Abs(x) / (float)tw * .12f);
                    }
                    // braços para cima comemorando
                    if (armsUp)
                        foreach (int side in new[] { -1, 1 })
                            for (int y = top - 6; y < top + 16; y++)
                                for (int w = 0; w < 3; w++) Put(cx + side * (tw - 1 + (y - top) / 5) + w * side, y, y > top + 13 ? skin : shirt, .8f);
                    // pescoço e cabeça
                    for (int y = top; y < top + 3; y++)
                        for (int x = -2; x <= 2; x++) Put(cx + x, y, skin, .8f);
                    int hcY = top + 9, r = 6;
                    for (int y = -r; y <= r; y++)
                        for (int x = -r; x <= r; x++)
                        {
                            float d = Mathf.Sqrt(x * x + y * y * 1.1f);
                            if (d > r + .3f) continue;
                            bool isHair = cap ? y > 1 : y > 2 || (y > -1 && Mathf.Abs(x) > 4);
                            Put(cx + x, hcY + y, isHair ? (cap ? shirt : hair) : skin, 1.05f - d / r * .25f);
                        }
                }
            }
            var tex = Finish(new Texture2D(W, H, TextureFormat.RGBA32, true), px, TextureWrapMode.Repeat);
            tex.name = "Torcida";
            crowds[key] = tex;
            return tex;
        }

        // ---------- placas de publicidade ----------
        public sealed class Board { public string Text; public Color Bg, Fg; }

        /// <summary>
        /// Desenha as marcas numa textura (uma faixa por placa) usando texto 3D e uma câmera temporária.
        /// Funciona no build porque usa só a fonte do projeto e o shader padrão de texto.
        /// </summary>
        public static Texture2D BoardAtlas(IList<Board> boards)
        {
            int rows = boards.Count;
            const int W = 1024, RowH = 128;
            var rt = new RenderTexture(W, RowH, 16) { name = "Placa", antiAliasing = 4 };
            rt.Create();
            var tex = new Texture2D(W, RowH * rows, TextureFormat.RGBA32, true) { name = "Placas", filterMode = FilterMode.Trilinear, anisoLevel = 8, wrapMode = TextureWrapMode.Clamp };

            const int Layer = 31;
            var root = new GameObject("PlacasTemp");
            var cam = new GameObject("CamPlacas").AddComponent<Camera>();
            cam.transform.SetParent(root.transform, false);
            cam.orthographic = true;
            cam.orthographicSize = .5f;
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.targetTexture = rt;
            cam.aspect = W / (float)RowH;
            cam.transform.position = new Vector3(0, 0, -5);
            cam.enabled = false;

            var tgo = new GameObject("Texto") { layer = Layer };
            tgo.transform.SetParent(root.transform, false);
            var tm = tgo.AddComponent<TextMesh>();
            var font = UIKit.BoldItalic;
            tm.font = font;
            var mr = tgo.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            tm.fontSize = 96;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            float maxW = cam.orthographicSize * 2 * cam.aspect * .8f, maxH = cam.orthographicSize * 2 * .62f;

            var prev = RenderTexture.active;
            for (int i = 0; i < rows; i++)
            {
                var b = boards[i];
                // cada placa é desenhada sozinha (fundo na cor da marca) e copiada para a sua faixa da textura
                cam.backgroundColor = b.Bg;
                tm.text = (b.Text ?? "").ToUpperInvariant();
                tm.color = b.Fg;
                tm.characterSize = .01f;
                var size = mr.bounds.size;
                if (size.x < .001f) size = new Vector3(tm.text.Length * .55f, 1, 0) * .01f * tm.fontSize / 10f; // malha ainda não gerada: estima
                tm.characterSize = .01f * Mathf.Min(maxW / Mathf.Max(.001f, size.x), maxH / Mathf.Max(.001f, size.y));
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, W, RowH), 0, i * RowH, false);
            }
            // vira textura comum: não se perde quando o app vai para segundo plano
            tex.Apply(true, false);
            RenderTexture.active = prev;
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(root);
            return tex;
        }
    }
}
