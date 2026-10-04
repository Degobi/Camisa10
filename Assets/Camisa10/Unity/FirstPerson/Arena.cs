using System.Collections.Generic;
using Camisa10.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Camisa10.UI
{
    /// <summary>Como o estádio deve parecer nesta partida: cores das torcidas, placas e horário.</summary>
    public sealed class StadiumStyle
    {
        public Color Home1 = Theme.Hex("#C8102E"), Home2 = Color.white, Away1 = Theme.Hex("#0B1F4B"), Away2 = Color.white;
        public bool Night;
        public int Seed = 7;
        public List<StadiumArt.Board> Boards = new List<StadiumArt.Board>();

        /// <summary>Placas com as marcas fictícias do jogo; o patrocinador do jogador (se houver) aparece primeiro.</summary>
        public static List<StadiumArt.Board> DefaultBoards(IEnumerable<string> extraBrands = null)
        {
            var list = new List<StadiumArt.Board>();
            void Add(string text, string bg, string fg) => list.Add(new StadiumArt.Board { Text = text, Bg = Theme.Hex(bg), Fg = Theme.Hex(fg) });
            if (extraBrands != null) foreach (var b in extraBrands) Add(b, "#0A0F1E", "#19E68C");
            Add("Volt", "#111111", "#F5F5F5");
            Add("Banco Nuvem", "#1565C0", "#FFFFFF");
            Add("Raio Drink", "#F2C230", "#111111");
            Add("Bola TV", "#C62828", "#FFFFFF");
            Add("PixelForge", "#21123D", "#2BD9FE");
            Add("Cronos", "#0E0E0E", "#D4AF37");
            Add("Strika", "#F5F5F5", "#C62828");
            Add("Pago+", "#00A86B", "#FFFFFF");
            return list;
        }
    }

    /// <summary>
    /// Estádio 3D montado por código. Convenção: linha do gol em z = 0, centro do gol em x = 0,
    /// o campo de ataque fica em z negativo e quem ataca olha para +z. O outro gol fica em z = -105.
    /// Peças repetidas (linhas, rede, arquibancadas, placas) são juntadas em poucas malhas para rodar leve no celular.
    /// </summary>
    public class Arena
    {
        public const float GoalHalfWidth = 3.66f, GoalHeight = 2.44f, BallRadius = 0.11f;
        public const float HalfWidth = 34f, Length = 105f;

        public Transform Root;
        public Camera Cam;
        public Rigidbody Ball;
        public bool Night { get; private set; }

        Light sceneLight;
        LightShadows sceneShadows;
        float sceneIntensity, prevShadowDistance;
        Quaternion sceneLightRot;
        bool prevFog; Color prevFogColor; float prevFogStart, prevFogEnd; FogMode prevFogMode;
        Material prevSky; int prevAA;
        AmbientMode prevAmbientMode; Color prevAmbient, prevAmbientSky, prevAmbientEq, prevAmbientGround;
        readonly List<Object> owned = new List<Object>(); // malhas e texturas desta partida, destruídas no fim

        static Material baseMat, spriteBase;
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        // ---------- materiais (herdam o shader do material padrão: funciona no Built-in e na URP) ----------
        static Material Base()
        {
            if (baseMat == null)
            {
                // material em Resources (criado pelo editor) garante que o shader entre no build do celular
                baseMat = Resources.Load<Material>("Materiais/Padrao");
                if (baseMat != null) return baseMat;
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseMat = tmp.GetComponent<Renderer>().sharedMaterial;
                Object.DestroyImmediate(tmp);
            }
            return baseMat;
        }

        static void Matte(Material m, float smooth)
        {
            m.SetFloat("_Glossiness", smooth); // Built-in Standard
            m.SetFloat("_Smoothness", smooth); // URP Lit
            m.SetFloat("_Metallic", 0f);
        }

        public static Material Mat(Color c, float smooth = .15f)
        {
            string key = ColorUtility.ToHtmlStringRGBA(c) + smooth;
            if (mats.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Base()) { color = c };
            Matte(m, smooth);
            mats[key] = m;
            return m;
        }

        public static Material TexMat(Texture tex, Vector2 tiling, float smooth = .05f, Color? tint = null)
        {
            var m = new Material(Base()) { color = tint ?? Color.white, mainTexture = tex };
            m.mainTextureScale = tiling;
            Matte(m, smooth);
            return m;
        }

        /// <summary>
        /// Material sem iluminação (refletores acesos, placas de LED, céu). Usa o shader de sprites,
        /// que a Unity sempre inclui no build, em vez de depender de variações do Standard que podem ser removidas.
        /// </summary>
        public static Material Unlit(Color c, Texture tex = null)
        {
            if (spriteBase == null)
            {
                var sh = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default"); // os dois vêm sempre no build
                spriteBase = sh != null ? new Material(sh) : new Material(Base());
            }
            var m = new Material(spriteBase) { color = c };
            if (tex != null) m.mainTexture = tex;
            return m;
        }

        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool keepCollider = false, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (rot.HasValue) go.transform.localRotation = rot.Value;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        T Own<T>(T o) where T : Object { owned.Add(o); return o; }

        GameObject Build(MeshBuilder mb, string name, Material mat, bool cast = true, bool receive = true)
        {
            var go = mb.Build(Root, name, mat, cast, receive);
            Own(go.GetComponent<MeshFilter>().sharedMesh);
            return go;
        }

        // ---------- texturas pequenas ----------
        static Texture2D ballTex, glowTex;

        static Texture2D BallTexture()
        {
            if (ballTex != null) return ballTex;
            int w = 512, h = 256;
            ballTex = new Texture2D(w, h, TextureFormat.RGBA32, true) { hideFlags = HideFlags.DontUnloadUnusedAsset, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int row = y / 64;
                    float cx = ((x + (row % 2) * 64) % 128) - 64, cy = (y % 64) - 32;
                    float d = Mathf.Abs(cx) * .87f + Mathf.Abs(cy) * .5f;
                    float panel = Mathf.Clamp01(22.5f - Mathf.Max(d, Mathf.Abs(cy)));
                    float seam = Mathf.Clamp01(1.6f - Mathf.Abs(d - 44));
                    float v = y / (float)h, shade = .94f + Mathf.Sin(v * Mathf.PI) * .06f;
                    var c = Color.Lerp(new Color(.97f, .97f, .97f), new Color(.1f, .11f, .14f), panel);
                    c = Color.Lerp(c, new Color(.72f, .74f, .78f), seam * (1 - panel)) * shade;
                    px[y * w + x] = c;
                }
            ballTex.SetPixels32(px);
            ballTex.Apply(true, true);
            return ballTex;
        }

        /// <summary>Brilho redondo e suave (halo dos refletores à noite).</summary>
        static Texture2D Glow()
        {
            if (glowTex != null) return glowTex;
            const int S = 128;
            glowTex = new Texture2D(S, S, TextureFormat.RGBA32, true) { hideFlags = HideFlags.DontUnloadUnusedAsset, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = new Vector2(x - S / 2 + .5f, y - S / 2 + .5f).magnitude / (S / 2);
                    float a = Mathf.Pow(Mathf.Clamp01(1 - d), 2.2f);
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            glowTex.SetPixels32(px);
            glowTex.Apply(true, true);
            return glowTex;
        }

        Texture2D SkyTexture(bool night)
        {
            const int H = 256;
            var t = Own(new Texture2D(4, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear });
            Color horizon = night ? new Color(.16f, .19f, .28f) : new Color(.80f, .86f, .93f);
            Color mid = night ? new Color(.05f, .07f, .13f) : new Color(.52f, .68f, .9f);
            Color zenith = night ? new Color(.01f, .015f, .04f) : new Color(.25f, .45f, .8f);
            var px = new Color[4 * H];
            for (int y = 0; y < H; y++)
            {
                float v = y / (H - 1f); // 0 = horizonte, 1 = topo
                var c = v < .25f ? Color.Lerp(horizon, mid, Mathf.SmoothStep(0, 1, v / .25f)) : Color.Lerp(mid, zenith, Mathf.SmoothStep(0, 1, (v - .25f) / .75f));
                for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
            }
            t.SetPixels(px);
            t.Apply(false, true);
            return t;
        }

        // ---------- montagem ----------
        public static Arena Build(Transform parent, StadiumStyle style)
        {
            var a = new Arena { Night = style.Night };
            a.Root = new GameObject("Arena").transform;
            a.Root.SetParent(parent, false);
            a.SetupLighting(style.Night);
            a.SetupCamera();
            a.BuildSky(style.Night);
            a.BuildPitch();
            a.BuildGoal(0, 1, true);
            a.BuildGoal(-Length, -1, false);
            a.BuildBoards(style);
            a.BuildStands(style);
            a.BuildBall();
            return a;
        }

        void SetupLighting(bool night)
        {
            // usa o sol da cena se existir, senão cria um
            sceneLight = Object.FindAnyObjectByType<Light>();
            Light sun = sceneLight;
            if (sun == null)
            {
                var lg = new GameObject("Sol");
                lg.transform.SetParent(Root, false);
                sun = lg.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            else
            {
                sceneShadows = sun.shadows; sceneIntensity = sun.intensity; sceneLightRot = sun.transform.rotation;
            }
            sun.shadows = GameSettings.Shadows ? LightShadows.Soft : LightShadows.None;
            if (night)
            {
                // refletores altos: luz branca, quase de cima
                sun.intensity = 1.05f;
                sun.color = new Color(.96f, .98f, 1f);
                sun.shadowStrength = .7f;
                sun.transform.rotation = Quaternion.Euler(64, 28, 0);
            }
            else
            {
                // fim de tarde: sol baixo e quente, sombra da arquibancada entrando no gramado
                sun.intensity = 1.08f;
                sun.color = new Color(1f, .94f, .84f);
                sun.shadowStrength = .82f;
                sun.transform.rotation = Quaternion.Euler(38, -32, 0);
            }

            prevShadowDistance = QualitySettings.shadowDistance;
            QualitySettings.shadowDistance = 80;
            prevAA = QualitySettings.antiAliasing;
            QualitySettings.antiAliasing = GameSettings.AntiAliasing; // linhas do campo e traves sem serrilhado

            prevSky = RenderSettings.skybox;
            prevFog = RenderSettings.fog; prevFogColor = RenderSettings.fogColor; prevFogMode = RenderSettings.fogMode;
            prevFogStart = RenderSettings.fogStartDistance; prevFogEnd = RenderSettings.fogEndDistance;
            prevAmbientMode = RenderSettings.ambientMode; prevAmbient = RenderSettings.ambientLight;
            prevAmbientSky = RenderSettings.ambientSkyColor; prevAmbientEq = RenderSettings.ambientEquatorColor; prevAmbientGround = RenderSettings.ambientGroundColor;

            RenderSettings.skybox = null; // o céu é uma cúpula própria (BuildSky), igual no editor e no build
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = night ? new Color(.07f, .09f, .14f) : new Color(.74f, .8f, .88f);
            RenderSettings.fogStartDistance = night ? 50 : 70;
            RenderSettings.fogEndDistance = night ? 320 : 420;
            // luz ambiente em três tons (céu, horizonte, chão): sombras azuladas e grama refletindo verde
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = night ? new Color(.2f, .23f, .32f) : new Color(.56f, .64f, .78f);
            RenderSettings.ambientEquatorColor = night ? new Color(.18f, .2f, .22f) : new Color(.48f, .5f, .48f);
            RenderSettings.ambientGroundColor = night ? new Color(.08f, .11f, .07f) : new Color(.2f, .27f, .16f);
        }

        void SetupCamera()
        {
            var cg = new GameObject("CameraJogador");
            cg.transform.SetParent(Root, false);
            Cam = cg.AddComponent<Camera>();
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = RenderSettings.fogColor;
            Cam.nearClipPlane = .05f;
            Cam.farClipPlane = 600;
            Cam.depth = 50; // sempre por cima da câmera da interface
            FitCamera();
        }

        void BuildSky(bool night)
        {
            // cilindro gigante com degradê; o shader de sprites desenha os dois lados e ignora a neblina
            var mb = new MeshBuilder();
            const int Seg = 32; const float Rad = 420, Top = 260, Bottom = -20;
            for (int i = 0; i < Seg; i++)
            {
                float a0 = i / (float)Seg * Mathf.PI * 2, a1 = (i + 1) / (float)Seg * Mathf.PI * 2;
                var p0 = new Vector3(Mathf.Cos(a0) * Rad, 0, Mathf.Sin(a0) * Rad - 50);
                var p1 = new Vector3(Mathf.Cos(a1) * Rad, 0, Mathf.Sin(a1) * Rad - 50);
                mb.Quad(p0 + Vector3.up * Bottom, p0 + Vector3.up * Top, p1 + Vector3.up * Top, p1 + Vector3.up * Bottom,
                    new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));
            }
            // tampa
            var c = new Vector3(0, Top, -50);
            for (int i = 0; i < Seg; i++)
            {
                float a0 = i / (float)Seg * Mathf.PI * 2, a1 = (i + 1) / (float)Seg * Mathf.PI * 2;
                var p0 = new Vector3(Mathf.Cos(a0) * Rad, Top, Mathf.Sin(a0) * Rad - 50);
                var p1 = new Vector3(Mathf.Cos(a1) * Rad, Top, Mathf.Sin(a1) * Rad - 50);
                mb.Quad(c, p1, p0, c, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1));
            }
            var sky = Build(mb, "Ceu", Own(Unlit(Color.white, SkyTexture(night))), false, false);
            sky.GetComponent<Renderer>().sharedMaterial.renderQueue = 1000; // desenha antes de tudo
        }

        // ---------- gramado e linhas ----------
        void BuildPitch()
        {
            // faixas de corte (20 no comprimento do campo), cada uma com tom levemente diferente
            var light = new MeshBuilder(); var dark = new MeshBuilder();
            const float Band = Length / 20f, X0 = -41, X1 = 41, ZMin = -113, ZMax = 8;
            const float Uv = 1f / 4f; // um ladrilho a cada 4 m
            int first = Mathf.FloorToInt(ZMin / Band), last = Mathf.CeilToInt(ZMax / Band);
            for (int i = first; i < last; i++)
            {
                float z0 = Mathf.Max(ZMin, i * Band), z1 = Mathf.Min(ZMax, (i + 1) * Band);
                (i % 2 == 0 ? light : dark).Ground(X0, z0, X1, z1, 0, Uv);
            }
            var grass = StadiumArt.Grass();
            Build(light, "GramadoClaro", Own(TexMat(grass, Vector2.one, .12f, new Color(1.08f, 1.1f, 1.04f))), false);
            Build(dark, "GramadoEscuro", Own(TexMat(grass, Vector2.one, .12f, new Color(.84f, .9f, .84f))), false);

            // colisor do chão (a bola quica nele)
            var floor = new GameObject("Chao");
            floor.transform.SetParent(Root, false);
            var bc = floor.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, -.5f, -50); bc.size = new Vector3(120, 1, 140);

            // linhas: tudo numa malha só
            var lines = new MeshBuilder();
            const float LW = .12f, LY = .006f;
            void L(float x1, float z1, float x2, float z2) => lines.GroundStrip(new Vector3(x1, 0, z1), new Vector3(x2, 0, z2), LW, LY);
            void Arc(Vector3 c, float r, float a0, float a1, int seg)
            {
                for (int i = 0; i < seg; i++)
                {
                    float t0 = Mathf.Lerp(a0, a1, i / (float)seg) * Mathf.Deg2Rad, t1 = Mathf.Lerp(a0, a1, (i + 1) / (float)seg) * Mathf.Deg2Rad;
                    // um pouco mais longo para não abrir fresta entre os pedaços
                    var p0 = c + new Vector3(Mathf.Sin(t0), 0, Mathf.Cos(t0)) * r;
                    var p1 = c + new Vector3(Mathf.Sin(t1), 0, Mathf.Cos(t1)) * r;
                    var d = (p1 - p0).normalized * .01f;
                    lines.GroundStrip(p0 - d, p1 + d, LW, LY);
                }
            }
            void Spot(Vector3 c, float r) { for (int i = 0; i < 8; i++) Arc(c, r * .5f, i * 45, i * 45 + 45, 1); lines.Ground(c.x - r * .6f, c.z - r * .6f, c.x + r * .6f, c.z + r * .6f, LY); }

            float H = HalfWidth;
            L(-H, 0, H, 0); L(-H, -Length, H, -Length);       // linhas de fundo
            L(-H, 0, -H, -Length); L(H, 0, H, -Length);       // laterais
            L(-H, -Length / 2, H, -Length / 2);               // meio-campo
            Arc(new Vector3(0, 0, -Length / 2), 9.15f, -180, 180, 48);
            Spot(new Vector3(0, 0, -Length / 2), .22f);
            foreach (var (gz, s) in new[] { (0f, -1f), (-Length, 1f) })
            {
                float bz = gz + s * 16.5f, sz = gz + s * 5.5f;
                L(-20.16f, gz, -20.16f, bz); L(20.16f, gz, 20.16f, bz); L(-20.16f, bz, 20.16f, bz);
                L(-9.16f, gz, -9.16f, sz); L(9.16f, gz, 9.16f, sz); L(-9.16f, sz, 9.16f, sz);
                var pen = new Vector3(0, 0, gz + s * 11);
                Spot(pen, .22f);
                // meia-lua: só a parte fora da grande área
                float half = Mathf.Acos(5.5f / 9.15f) * Mathf.Rad2Deg;
                if (s < 0) Arc(pen, 9.15f, 180 - half, 180 + half, 16); else Arc(pen, 9.15f, -half, half, 16);
                // escanteios
                Arc(new Vector3(-H, 0, gz), 1, s < 0 ? 90 : 0, s < 0 ? 180 : 90, 6);
                Arc(new Vector3(H, 0, gz), 1, s < 0 ? 180 : 270, s < 0 ? 270 : 360, 6);
            }
            Build(lines, "Linhas", Mat(new Color(.93f, .94f, .92f), .1f), false);

            // bandeirinhas de escanteio
            var pole = Mat(new Color(.95f, .95f, .95f), .4f);
            var flag = Mat(Theme.Hex("#F2C230"), .2f);
            foreach (var x in new[] { -H, H })
                foreach (var z in new[] { 0f, -Length })
                {
                    Prim(PrimitiveType.Cylinder, Root, new Vector3(x, .75f, z), new Vector3(.03f, .75f, .03f), pole);
                    Prim(PrimitiveType.Cube, Root, new Vector3(x - Mathf.Sign(x) * .2f, 1.35f, z), new Vector3(.38f, .28f, .01f), flag);
                }
        }

        // ---------- gols ----------
        void BuildGoal(float z, float back, bool colliders)
        {
            // traves com colisor no gol do lance (a bola bate na trave de verdade)
            var postMat = Mat(new Color(.97f, .97f, .97f), .7f);
            float px = GoalHalfWidth + .06f;
            Prim(PrimitiveType.Cylinder, Root, new Vector3(-px, 1.25f, z), new Vector3(.12f, 1.25f, .12f), postMat, colliders).name = "TraveE";
            Prim(PrimitiveType.Cylinder, Root, new Vector3(px, 1.25f, z), new Vector3(.12f, 1.25f, .12f), postMat, colliders).name = "TraveD";
            Prim(PrimitiveType.Cylinder, Root, new Vector3(0, GoalHeight + .06f, z), new Vector3(.12f, px, .12f), postMat, colliders, Quaternion.Euler(0, 0, 90)).name = "TraveTravessao";

            // rede: teto inclinado até o fundo, malha de 15 cm, tudo numa malha só
            var net = new MeshBuilder();
            const float depthTop = 1.2f, depthBottom = 2.3f, step = .15f, thick = .013f;
            Vector3 P(float x, float y, float d) => new Vector3(x, y, z + back * d);
            for (float x = -GoalHalfWidth; x <= GoalHalfWidth + .01f; x += step)
            {
                net.Beam(P(x, GoalHeight, depthTop), P(x, 0, depthBottom), thick);   // fundo
                net.Beam(P(x, GoalHeight, 0), P(x, GoalHeight, depthTop), thick);    // teto
            }
            for (float t = 0; t <= 1.001f; t += step / GoalHeight)
            {
                float y = Mathf.Lerp(GoalHeight, 0, t), d = Mathf.Lerp(depthTop, depthBottom, t);
                net.Beam(P(-GoalHalfWidth, y, d), P(GoalHalfWidth, y, d), thick);
                net.Beam(P(-px, y, 0), P(-px, y, d), thick);
                net.Beam(P(px, y, 0), P(px, y, d), thick);
            }
            for (float d = step; d < depthTop; d += step)
                net.Beam(P(-GoalHalfWidth, GoalHeight, d), P(GoalHalfWidth, GoalHeight, d), thick);
            // verticais das laterais
            for (float d = step; d < depthBottom; d += step)
                foreach (var sx in new[] { -px, px })
                {
                    float topY = d <= depthTop ? GoalHeight : Mathf.Lerp(GoalHeight, 0, (d - depthTop) / (depthBottom - depthTop));
                    net.Beam(P(sx, 0, d), P(sx, topY, d), thick);
                }
            Build(net, "Rede", Mat(new Color(.9f, .91f, .93f), .1f), true, false);

            // suportes de trás
            var frame = new MeshBuilder();
            foreach (var sx in new[] { -px, px })
            {
                frame.Beam(P(sx, GoalHeight, .05f), P(sx, GoalHeight, depthTop), .04f);
                frame.Beam(P(sx, GoalHeight, depthTop), P(sx, 0, depthBottom), .04f);
                frame.Beam(P(sx, .02f, 0), P(sx, .02f, depthBottom), .04f);
            }
            frame.Beam(P(-px, .02f, depthBottom), P(px, .02f, depthBottom), .04f);
            Build(frame, "SuporteRede", Mat(new Color(.85f, .86f, .88f), .5f));

            if (colliders)
            {
                // colisor invisível que segura a bola dentro da rede
                var wall = new GameObject("RedeColisor");
                wall.transform.SetParent(Root, false);
                wall.transform.localPosition = new Vector3(0, 1.3f, z + back * (depthBottom - .3f));
                wall.AddComponent<BoxCollider>().size = new Vector3(9, 2.8f, .3f);
            }
        }

        // ---------- placas de publicidade (LED) ----------
        Material ledMat;
        int ledRows = 1;

        void BuildBoards(StadiumStyle style)
        {
            var boards = style.Boards != null && style.Boards.Count > 0 ? style.Boards : StadiumStyle.DefaultBoards();
            int rows = boards.Count;
            var atlas = Own(StadiumArt.BoardAtlas(boards));
            var faces = new MeshBuilder(); var bodies = new MeshBuilder();
            int k = 0;
            const float Hb = .9f, Seg = 9f;

            void Board(Vector3 left, Vector3 right, Vector3 facing)
            {
                int row = k++ % rows;
                float v0 = row / (float)rows, v1 = (row + 1f) / rows;
                var up = Vector3.up * Hb;
                var lean = -facing * .08f; // levemente inclinada para trás
                faces.Quad(left, left + up + lean, right + up + lean, right, new Vector2(0, v0), new Vector2(0, v1), new Vector2(1, v1), new Vector2(1, v0));
                var c = (left + right) / 2 - facing * .24f + Vector3.up * Hb / 2; // estrutura atrás da face inclinada
                bodies.Box(c, new Vector3((right - left).magnitude, Hb, .2f), Quaternion.LookRotation(-facing), false);
            }

            // atrás do gol (de frente para o campo, olhando para -z)
            for (float x = -36; x < 36 - .1f; x += Seg)
                Board(new Vector3(x, 0, 4.5f), new Vector3(x + Seg - .1f, 0, 4.5f), Vector3.back);
            for (float x = -36; x < 36 - .1f; x += Seg)
                Board(new Vector3(x + Seg - .1f, 0, -Length - 4.5f), new Vector3(x, 0, -Length - 4.5f), Vector3.forward);
            // laterais
            for (float z = -4; z > -Length + 4; z -= Seg)
            {
                Board(new Vector3(-38, 0, z - Seg + .1f), new Vector3(-38, 0, z), Vector3.right);
                Board(new Vector3(38, 0, z), new Vector3(38, 0, z - Seg + .1f), Vector3.left);
            }
            // placas de LED brilham por conta própria: sem iluminação, mais vivas à noite
            ledMat = Own(Unlit(Night ? Color.white : new Color(.92f, .92f, .92f), atlas));
            ledRows = rows;
            Build(faces, "PlacasLED", ledMat, false, false);
            Build(bodies, "PlacasEstrutura", Mat(Theme.Hex("#1A1D24"), .3f));
        }

        // ---------- arquibancadas, cobertura e refletores ----------
        struct Tier { public float Offset, BaseY, Rows, Depth, Rise; public float D => Rows * Depth; public float TopY => BaseY + Rows * Rise; }

        static readonly Tier Lower = new Tier { Offset = 0, BaseY = 1.3f, Rows = 16, Depth = .8f, Rise = .38f };
        static readonly Tier Upper = new Tier { Offset = 13.6f, BaseY = 8.9f, Rows = 15, Depth = .8f, Rise = .5f };

        void BuildStands(StadiumStyle style)
        {
            var home = new MeshBuilder(); var away = new MeshBuilder();
            var concrete = new MeshBuilder(); var ribbon = new MeshBuilder();
            var roofTop = new MeshBuilder(); var roofUnder = new MeshBuilder(); var lamps = new MeshBuilder(); var trusses = new MeshBuilder();
            var rnd = new System.Random(style.Seed);
            float tileW = StadiumArt.CrowdSeats * StadiumArt.SeatWidth;
            const float RoofY = 20.5f, RoofFront = 3f, BackOff = 13.6f + 15 * .8f + 1f;

            // estruturas vistas dos dois lados (cobertura, paredes): face nos dois sentidos
            void Both(MeshBuilder mb, Vector3 a, Vector3 b, Vector3 c, Vector3 d) { mb.Quad(a, b, c, d); mb.Quad(d, c, b, a); }

            // Um lado do estádio: front0→front1 é a borda da frente (da esquerda para a direita, vista do campo);
            // inward aponta para fora do campo. O anel de cima e a cobertura se estendem nas pontas até encontrar os cantos.
            void Side(Vector3 f0, Vector3 f1, Vector3 inward, bool awaySection)
            {
                var along = (f1 - f0).normalized;
                float len = (f1 - f0).magnitude;
                foreach (var t in new[] { Lower, Upper })
                {
                    float ext = t.Offset;
                    // seções de 1 ladrilho com deslocamento aleatório: a torcida não se repete igual
                    for (float s = -ext; s < len + ext - .01f; s += tileW)
                    {
                        float e = Mathf.Min(len + ext, s + tileW);
                        bool isAway = awaySection && t.Offset == 0 && s > len * .72f;
                        var mb = isAway ? away : home;
                        float u0 = rnd.Next(StadiumArt.CrowdSeats) / (float)StadiumArt.CrowdSeats;
                        float uLen = (e - s) / tileW;
                        Vector3 a = f0 + along * s + inward * t.Offset + Vector3.up * t.BaseY;
                        Vector3 b = f0 + along * e + inward * t.Offset + Vector3.up * t.BaseY;
                        Vector3 back = inward * t.D + Vector3.up * (t.TopY - t.BaseY);
                        float vRows = t.Rows / StadiumArt.CrowdRows;
                        mb.Quad(a, a + back, b + back, b, new Vector2(u0, 0), new Vector2(u0, vRows), new Vector2(u0 + uLen, vRows), new Vector2(u0 + uLen, 0));
                    }
                    var fa = f0 + inward * t.Offset - along * ext; var fb = f1 + inward * t.Offset + along * ext;
                    if (t.Offset == 0)
                    {
                        // muro da frente do anel de baixo
                        concrete.Quad(fa, fa + Vector3.up * t.BaseY, fb + Vector3.up * t.BaseY, fb);
                        continue;
                    }
                    // anel de LED entre as arquibancadas, com as mesmas marcas das placas
                    float fl = (fb - fa).magnitude, seg = 16f;
                    for (float s = 0; s < fl - .01f; s += seg)
                    {
                        float e = Mathf.Min(fl, s + seg);
                        int row = rnd.Next(ledRows);
                        float v0 = row / (float)ledRows, v1 = (row + 1f) / ledRows;
                        Vector3 p0 = fa + along * s, p1 = fa + along * e;
                        Vector3 lo = Vector3.up * (Lower.TopY + .15f), hi = Vector3.up * (t.BaseY - .1f);
                        ribbon.Quad(p0 + lo, p0 + hi, p1 + hi, p1 + lo, new Vector2(0, v0), new Vector2(0, v1), new Vector2((e - s) / seg, v1), new Vector2((e - s) / seg, v0));
                    }
                    concrete.Quad(fa + Vector3.up * Lower.TopY, fa + Vector3.up * (Lower.TopY + .15f), fb + Vector3.up * (Lower.TopY + .15f), fb + Vector3.up * Lower.TopY);
                }
                // passarela entre os anéis
                Both(concrete, f0 + inward * Lower.D + Vector3.up * Lower.TopY, f0 + inward * Upper.Offset + Vector3.up * Lower.TopY,
                    f1 + inward * Upper.Offset + Vector3.up * Lower.TopY, f1 + inward * Lower.D + Vector3.up * Lower.TopY);
                // fundo e cobertura
                var ba = f0 + inward * BackOff; var bb = f1 + inward * BackOff;
                Both(concrete, ba, ba + Vector3.up * (RoofY + 1), bb + Vector3.up * (RoofY + 1), bb);
                var ra = f0 - along * RoofFront + inward * RoofFront + Vector3.up * (RoofY - 1.2f);
                var rb = f1 + along * RoofFront + inward * RoofFront + Vector3.up * (RoofY - 1.2f);
                var rba = ba - along * RoofFront + Vector3.up * (RoofY + 1); var rbb = bb + along * RoofFront + Vector3.up * (RoofY + 1);
                Both(roofUnder, ra, rba, rbb, rb);
                var lift = Vector3.up * .5f;
                Both(roofTop, ra + lift, rba + lift, rbb + lift, rb + lift);
                Both(roofTop, ra, ra + lift, rb + lift, rb); // borda da frente
                // vigas da cobertura (dão leitura de estrutura metálica vista de baixo)
                for (float s = -RoofFront; s <= len + RoofFront + .01f; s += 8f)
                {
                    var front = f0 + along * s + inward * RoofFront + Vector3.up * (RoofY - 1.3f);
                    var rear = f0 + along * s + inward * BackOff + Vector3.up * (RoofY + .9f);
                    trusses.Beam(front, rear, .35f);
                }
                // faixa de refletores sob a borda da cobertura
                var la = f0 + inward * (RoofFront + .6f) + Vector3.up * (RoofY - 1.3f); var lb = f1 + inward * (RoofFront + .6f) + Vector3.up * (RoofY - 1.3f);
                Both(lamps, la, la + inward * .9f, lb + inward * .9f, lb);
            }

            // Canto entre dois lados: completa os anéis para não abrir buraco.
            void Corner(Vector3 front, Vector3 inA, Vector3 inB)
            {
                foreach (var t in new[] { Lower, Upper })
                {
                    var f = front + (inA + inB) * t.Offset + Vector3.up * t.BaseY;
                    var top = Vector3.up * (t.TopY - t.BaseY);
                    home.Quad(f, f + inA * t.D + top, f + (inA + inB) * t.D + top, f + inB * t.D + top,
                        new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));
                    home.Quad(f, f + inB * t.D + top, f + (inA + inB) * t.D + top, f + inA * t.D + top,
                        new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));
                }
                // cobertura do canto: baixa na quina de dentro, alta no fundo (casa com as dos dois lados)
                var r0 = front + (inA + inB) * RoofFront + Vector3.up * (RoofY - 1.2f);
                var rise = Vector3.up * 2.2f; float span = BackOff - RoofFront;
                Both(roofUnder, r0, r0 + inA * span + rise, r0 + (inA + inB) * span + rise, r0 + inB * span + rise);
                Both(roofTop, r0 + Vector3.up * .5f, r0 + inA * span + rise + Vector3.up * .5f, r0 + (inA + inB) * span + rise + Vector3.up * .5f, r0 + inB * span + rise + Vector3.up * .5f);
                var w0 = front + (inA + inB) * BackOff; var hgt = Vector3.up * (RoofY + 1);
                Both(concrete, w0 - inB * BackOff, w0 - inB * BackOff + hgt, w0 + hgt, w0);
                Both(concrete, w0, w0 + hgt, w0 - inA * BackOff + hgt, w0 - inA * BackOff);
            }

            const float SX = 41, SZ = 8, FZ = -Length - 8;
            Side(new Vector3(-SX, 0, SZ), new Vector3(SX, 0, SZ), Vector3.forward, false);   // atrás do gol do lance
            Side(new Vector3(SX, 0, FZ), new Vector3(-SX, 0, FZ), Vector3.back, false);      // atrás do outro gol
            Side(new Vector3(-SX, 0, FZ), new Vector3(-SX, 0, SZ), Vector3.left, true);      // lateral esquerda (visitantes no canto)
            Side(new Vector3(SX, 0, SZ), new Vector3(SX, 0, FZ), Vector3.right, false);      // lateral direita
            Corner(new Vector3(-SX, 0, SZ), Vector3.forward, Vector3.left);
            Corner(new Vector3(SX, 0, SZ), Vector3.forward, Vector3.right);
            Corner(new Vector3(-SX, 0, FZ), Vector3.back, Vector3.left);
            Corner(new Vector3(SX, 0, FZ), Vector3.back, Vector3.right);

            Build(home, "TorcidaCasa", Own(TexMat(StadiumArt.Crowd(style.Home1, style.Home2, style.Seed), Vector2.one, 0)), false);
            Build(away, "TorcidaVisitante", Own(TexMat(StadiumArt.Crowd(style.Away1, style.Away2, style.Seed + 1), Vector2.one, 0)), false);
            Build(concrete, "Concreto", Mat(Theme.Hex(Night ? "#30343C" : "#5B6068"), .05f), false);
            if (ribbon.Count > 0) Build(ribbon, "AnelLED", ledMat ?? Mat(Color.Lerp(style.Home1, Color.black, .35f), .3f), false, false);
            Build(roofTop, "Cobertura", Mat(Theme.Hex("#D5D9DF"), .4f));
            Build(roofUnder, "CoberturaBaixo", Mat(Theme.Hex(Night ? "#23272E" : "#7C838D"), .2f));
            Build(trusses, "Vigas", Mat(Theme.Hex(Night ? "#1B1E24" : "#4A5059"), .4f), false);
            Build(lamps, "Refletores", Own(Unlit(Night ? Color.white : new Color(.9f, .92f, .95f))), false, false);

            // à noite, halos suaves em volta das baterias de refletores
            if (Night)
            {
                var halo = Own(Unlit(new Color(1f, .98f, .92f, .55f), Glow()));
                var halos = new MeshBuilder();
                void Halo(Vector3 c, float size)
                {
                    // cartaz voltado para o centro do campo
                    var toPitch = (new Vector3(0, 6, -Length / 2) - c).normalized;
                    var right = Vector3.Cross(Vector3.up, toPitch).normalized * size; var up = Vector3.Cross(toPitch, right).normalized * size;
                    halos.Quad(c - right - up, c - right + up, c + right + up, c + right - up);
                }
                for (float x = -30; x <= 30; x += 15)
                {
                    Halo(new Vector3(x, RoofY - 1.4f, SZ + RoofFront + 1), 4.5f);
                    Halo(new Vector3(x, RoofY - 1.4f, FZ - RoofFront - 1), 4.5f);
                }
                for (float z = -95; z <= -5; z += 15)
                {
                    Halo(new Vector3(-SX - RoofFront - 1, RoofY - 1.4f, z), 4.5f);
                    Halo(new Vector3(SX + RoofFront + 1, RoofY - 1.4f, z), 4.5f);
                }
                Build(halos, "HalosRefletores", halo, false, false);
            }
        }

        // ---------- bola ----------
        void BuildBall()
        {
            var ball = Prim(PrimitiveType.Sphere, Root, new Vector3(0, BallRadius, -11), Vector3.one * BallRadius * 2, TexMat(BallTexture(), Vector2.one, .45f), true);
            ball.name = "Bola";
            Ball = ball.AddComponent<Rigidbody>();
            Ball.mass = .43f;
            Ball.linearDamping = .05f;
            Ball.angularDamping = .3f;
            Ball.interpolation = RigidbodyInterpolation.Interpolate;
            Ball.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Ball.isKinematic = true;
            ball.AddComponent<BallSounds>();
        }

        /// <summary>
        /// Imagem do lance. A câmera desenha numa textura que a própria interface mostra (ChanceHud),
        /// assim nenhum painel da interface consegue cobrir o campo.
        /// </summary>
        public RenderTexture View;
        public System.Action<Texture> OnView;

        void EnsureView()
        {
            float scale = GameSettings.RenderScale;
            int w = Mathf.Max(16, Mathf.RoundToInt(Screen.width * scale)), h = Mathf.Max(16, Mathf.RoundToInt(Screen.height * scale));
            if (View != null && View.width == w && View.height == h) return;
            if (View != null) { Cam.targetTexture = null; View.Release(); Object.Destroy(View); }
            View = new RenderTexture(w, h, 24) { name = "Lance3D", antiAliasing = Mathf.Clamp(QualitySettings.antiAliasing, 1, 8) };
            View.Create();
            Cam.targetTexture = View;
            OnView?.Invoke(View);
        }

        /// <summary>Ajusta o campo de visão e a área de desenho para a tela deitada.</summary>
        /// <summary>Graus extras no campo de visão (arrancada).</summary>
        public float FovBoost;

        public void FitCamera()
        {
            EnsureView();
            // paisagem: campo de visão horizontal amplo, como a câmera de um jogo de futebol
            Cam.rect = Landscape.Viewport01;
            float hFov = 80f * Mathf.Deg2Rad;
            Cam.fieldOfView = Mathf.Clamp(2f * Mathf.Atan(Mathf.Tan(hFov / 2f) / Landscape.GameAspect) * Mathf.Rad2Deg, 50f, 60f) + FovBoost;
        }

        public Transform Person(string name, Color shirt, Color shorts, Vector3 pos, float yaw, bool keeper = false)
        {
            var rig = PersonRig.Build(Root, name, shirt, shorts, keeper ? shorts : shirt, Theme.Hex("#151515"), Rng.RangeInt(2, 30), keeper);
            rig.transform.localPosition = pos;
            rig.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            return rig.transform;
        }

        /// <summary>Jogador com o uniforme completo do clube (ou de goleiro, se kit.Keeper).</summary>
        public Transform Person(string name, Kit kit, Vector3 pos, float yaw)
        {
            var boots = Theme.Hex(Rng.Chance(.5) ? "#151515" : Rng.Chance(.5) ? "#F5F5F5" : "#E8542B");
            var rig = PersonRig.Build(Root, name, kit.Main, kit.Shorts, kit.Socks, boots, kit.Keeper ? 1 : Rng.RangeInt(2, 30), kit.Keeper, kit);
            rig.transform.localPosition = pos;
            rig.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            return rig.transform;
        }

        public void PlaceCamera(Vector3 eye, Vector3 lookAt)
        {
            Cam.transform.position = eye;
            // deitado o campo vertical é menor: inclina a câmera para a bola nos pés continuar na tela
            Cam.transform.rotation = Quaternion.LookRotation(lookAt - eye) * Quaternion.Euler(PitchBias, 0, 0);
        }

        public const float PitchBias = 12f;

        public void Destroy()
        {
            if (sceneLight != null)
            {
                sceneLight.shadows = sceneShadows;
                sceneLight.intensity = sceneIntensity;
                sceneLight.transform.rotation = sceneLightRot;
            }
            QualitySettings.antiAliasing = prevAA;
            QualitySettings.shadowDistance = prevShadowDistance;
            RenderSettings.skybox = prevSky;
            RenderSettings.fog = prevFog; RenderSettings.fogColor = prevFogColor; RenderSettings.fogMode = prevFogMode;
            RenderSettings.fogStartDistance = prevFogStart; RenderSettings.fogEndDistance = prevFogEnd;
            RenderSettings.ambientMode = prevAmbientMode; RenderSettings.ambientLight = prevAmbient;
            RenderSettings.ambientSkyColor = prevAmbientSky; RenderSettings.ambientEquatorColor = prevAmbientEq; RenderSettings.ambientGroundColor = prevAmbientGround;
            if (Cam != null) Cam.targetTexture = null;
            if (View != null) { View.Release(); Object.Destroy(View); View = null; }
            if (Root != null) Object.Destroy(Root.gameObject);
            foreach (var o in owned) if (o != null) Object.Destroy(o);
            owned.Clear();
        }
    }
}
