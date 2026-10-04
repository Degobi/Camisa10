using System.Collections.Generic;
using Camisa10.Core;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Estádio 3D montado por código. Convenção: linha do gol em z = 0, centro do gol em x = 0,
    /// o campo de ataque fica em z negativo e quem ataca olha para +z.
    /// </summary>
    public class Arena
    {
        public const float GoalHalfWidth = 3.66f, GoalHeight = 2.44f, BallRadius = 0.11f;

        public Transform Root;
        public Camera Cam;
        public Rigidbody Ball;
        Light sceneLight;
        LightShadows sceneShadows;
        float sceneIntensity;
        Quaternion sceneLightRot;
        bool prevFog; Color prevFogColor; float prevFogStart, prevFogEnd; FogMode prevFogMode;
        Material prevSky; int prevAA;
        static Material skyMat;

        static Material baseMat;
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        // ---------- materiais (herdam o shader do material padrão: funciona no Built-in e na URP) ----------
        static Material Base()
        {
            if (baseMat == null)
            {
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

        public static Material TexMat(Texture2D tex, Vector2 tiling, float smooth = .05f)
        {
            var m = new Material(Base()) { color = Color.white, mainTexture = tex };
            m.mainTextureScale = tiling;
            Matte(m, smooth);
            return m;
        }

        static Material Glow(Color c)
        {
            var m = new Material(Base()) { color = c };
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 1.6f);
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

        // ---------- texturas ----------
        static Texture2D grassTex, crowdTex, ballTex;

        static Texture2D Grass()
        {
            if (grassTex != null) return grassTex;
            int w = 512, h = 2048;
            grassTex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 16 };
            var px = new Color32[w * h];
            float ox = Rng.RangeInt(0, 1000), oy = Rng.RangeInt(0, 1000);
            for (int y = 0; y < h; y++)
            {
                // faixas de corte (~5 m) com transição suave, como grama cortada em sentidos opostos
                float band = Mathf.Sin(y / (float)h * Mathf.PI * 2 * 9.5f);
                float stripe = Mathf.SmoothStep(-1, 1, band * 3f) * 2 - 1;
                for (int x = 0; x < w; x++)
                {
                    float big = Mathf.PerlinNoise(ox + x * .008f, oy + y * .008f);
                    float mid = Mathf.PerlinNoise(ox + x * .05f, oy + y * .05f);
                    float fine = Mathf.PerlinNoise(ox + x * .45f, oy + y * .12f);
                    float g = 118 + stripe * 11 + (big - .5f) * 22 + (mid - .5f) * 12 + (fine - .5f) * 26;
                    // desgaste perto da área (parte de cima da textura fica na linha do gol)
                    float wear = Mathf.Clamp01((y / (float)h - .78f) * 4f) * Mathf.Clamp01(1 - Mathf.Abs(x / (float)w - .5f) * 3f);
                    float dirt = wear * Mathf.Clamp01(mid * 1.6f - .5f) * .5f;
                    float r = g * .34f, gg = g, bl = g * .3f;
                    r = Mathf.Lerp(r, 112, dirt); gg = Mathf.Lerp(gg, 96, dirt); bl = Mathf.Lerp(bl, 60, dirt);
                    px[y * w + x] = new Color32((byte)Mathf.Clamp(r, 0, 255), (byte)Mathf.Clamp(gg, 0, 255), (byte)Mathf.Clamp(bl, 0, 255), 255);
                }
            }
            grassTex.SetPixels32(px);
            grassTex.Apply(true);
            return grassTex;
        }

        static Texture2D Crowd()
        {
            if (crowdTex != null) return crowdTex;
            int w = 512, h = 128;
            crowdTex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            var seat = new Color32(48, 52, 62, 255);
            for (int i = 0; i < px.Length; i++) px[i] = seat;
            Color32[] shirts = { new Color32(214, 46, 46, 255), new Color32(240, 240, 240, 255), new Color32(36, 72, 170, 255), new Color32(250, 204, 48, 255), new Color32(28, 28, 28, 255), new Color32(30, 130, 70, 255) };
            Color32[] skins = { new Color32(241, 201, 165, 255), new Color32(198, 138, 94, 255), new Color32(120, 80, 52, 255) };
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 64; col++)
                {
                    if (Rng.Chance(.12)) continue; // cadeiras vazias
                    int cx = col * 8 + 4 + Rng.RangeInt(-1, 1), cy = row * 16;
                    var sh = shirts[Rng.RangeInt(0, shirts.Length - 1)];
                    var sk = skins[Rng.RangeInt(0, skins.Length - 1)];
                    for (int y = 0; y < 9; y++)
                        for (int x = -3; x <= 3; x++) Put(px, w, h, cx + x, cy + 1 + y, sh);
                    for (int y = 0; y < 4; y++)
                        for (int x = -2; x <= 1; x++) Put(px, w, h, cx + x, cy + 10 + y, sk);
                }
            crowdTex.SetPixels32(px);
            crowdTex.Apply(true);
            return crowdTex;
        }

        static void Put(Color32[] px, int w, int h, int x, int y, Color32 c)
        {
            if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c;
        }

        static Texture2D BallTexture()
        {
            if (ballTex != null) return ballTex;
            int w = 256, h = 128;
            ballTex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int row = y / 32;
                    float cx = ((x + (row % 2) * 32) % 64) - 32, cy = (y % 32) - 16;
                    float d = Mathf.Abs(cx) * .87f + Mathf.Abs(cy) * .5f;
                    bool panel = Mathf.Max(d, Mathf.Abs(cy)) < 11;
                    bool seam = Mathf.Abs(d - 22) < 1.2f;
                    px[y * w + x] = panel ? new Color32(30, 30, 34, 255) : seam ? new Color32(200, 200, 205, 255) : new Color32(248, 248, 248, 255);
                }
            ballTex.SetPixels32(px);
            ballTex.Apply(true);
            return ballTex;
        }

        // ---------- montagem ----------
        public static Arena Build(Transform parent, string[] boardColors)
        {
            var a = new Arena();
            a.Root = new GameObject("Arena").transform;
            a.Root.SetParent(parent, false);
            var R = a.Root;

            // luz: usa o sol da cena se existir, senão cria um; sombras suaves
            a.sceneLight = Object.FindFirstObjectByType<Light>();
            Light sun = a.sceneLight;
            if (sun == null)
            {
                var lg = new GameObject("Sol");
                lg.transform.SetParent(R, false);
                sun = lg.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            else
            {
                a.sceneShadows = sun.shadows; a.sceneIntensity = sun.intensity; a.sceneLightRot = sun.transform.rotation;
            }
            sun.intensity = 1.2f;
            sun.color = new Color(1f, .96f, .9f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42, -35, 0);
            QualitySettings.shadowDistance = 90;
            a.prevAA = QualitySettings.antiAliasing;
            QualitySettings.antiAliasing = 4; // linhas do campo e traves sem serrilhado
            a.prevSky = RenderSettings.skybox;
            a.prevFog = RenderSettings.fog; a.prevFogColor = RenderSettings.fogColor; a.prevFogMode = RenderSettings.fogMode;
            a.prevFogStart = RenderSettings.fogStartDistance; a.prevFogEnd = RenderSettings.fogEndDistance;
            if (skyMat == null)
            {
                var sh = Shader.Find("Skybox/Procedural"); // pode não existir no build; aí fica a cor sólida
                if (sh != null)
                {
                    skyMat = new Material(sh);
                    skyMat.SetFloat("_SunSize", .03f);
                    skyMat.SetFloat("_AtmosphereThickness", .9f);
                    skyMat.SetColor("_SkyTint", new Color(.45f, .6f, .85f));
                    skyMat.SetColor("_GroundColor", new Color(.35f, .38f, .4f));
                    skyMat.SetFloat("_Exposure", 1.25f);
                }
            }
            if (skyMat != null) RenderSettings.skybox = skyMat;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.62f, .7f, .8f);
            RenderSettings.fogStartDistance = 70;
            RenderSettings.fogEndDistance = 260;
            RenderSettings.ambientLight = new Color(.55f, .6f, .68f);

            // câmera em primeira pessoa (usa o céu da cena quando existir)
            var cg = new GameObject("CameraJogador");
            cg.transform.SetParent(R, false);
            a.Cam = cg.AddComponent<Camera>();
            a.Cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            a.Cam.backgroundColor = new Color(.55f, .72f, .9f);
            a.Cam.nearClipPlane = .05f;
            a.Cam.farClipPlane = 500;
            a.Cam.depth = 50; // sempre por cima da câmera da interface
            a.FitCamera();

            // gramado (com colisor, a bola quica nele)
            var pitch = Prim(PrimitiveType.Plane, R, new Vector3(0, 0, -30), new Vector3(9, 1, 9), TexMat(Grass(), Vector2.one), true);
            pitch.name = "Gramado";
            Prim(PrimitiveType.Plane, R, new Vector3(0, -.02f, -30), new Vector3(30, 1, 30), Mat(Theme.Hex("#2F5E2E"), .05f)).name = "Entorno";

            // marcações
            var white = Mat(new Color(.96f, .96f, .96f), .1f);
            void LineX(float x1, float x2, float z) => Prim(PrimitiveType.Cube, R, new Vector3((x1 + x2) / 2, .006f, z), new Vector3(Mathf.Abs(x2 - x1), .012f, .1f), white);
            void LineZ(float x, float z1, float z2) => Prim(PrimitiveType.Cube, R, new Vector3(x, .006f, (z1 + z2) / 2), new Vector3(.1f, .012f, Mathf.Abs(z2 - z1)), white);
            void Arc(Vector3 c, float r, float a0, float a1, int seg)
            {
                for (int i = 0; i < seg; i++)
                {
                    float t0 = Mathf.Lerp(a0, a1, i / (float)seg) * Mathf.Deg2Rad, t1 = Mathf.Lerp(a0, a1, (i + 1) / (float)seg) * Mathf.Deg2Rad;
                    var p0 = c + new Vector3(Mathf.Sin(t0), 0, Mathf.Cos(t0)) * r;
                    var p1 = c + new Vector3(Mathf.Sin(t1), 0, Mathf.Cos(t1)) * r;
                    var mid = (p0 + p1) / 2; var d = p1 - p0;
                    Prim(PrimitiveType.Cube, R, new Vector3(mid.x, .006f, mid.z), new Vector3(.1f, .012f, d.magnitude + .02f), white, false, Quaternion.LookRotation(d));
                }
            }
            LineX(-34, 34, 0);
            LineX(-20.16f, 20.16f, -16.5f); LineZ(-20.16f, 0, -16.5f); LineZ(20.16f, 0, -16.5f);
            LineX(-9.16f, 9.16f, -5.5f); LineZ(-9.16f, 0, -5.5f); LineZ(9.16f, 0, -5.5f);
            LineZ(-34, 0, -52.5f); LineZ(34, 0, -52.5f); LineX(-34, 34, -52.5f);
            Arc(new Vector3(0, 0, -11), 9.15f, 127, 233, 16);        // meia-lua da grande área
            Arc(new Vector3(0, 0, -52.5f), 9.15f, -180, 180, 40);  // círculo central
            Prim(PrimitiveType.Cylinder, R, new Vector3(0, .006f, -11), new Vector3(.24f, .006f, .24f), white);

            // gol: traves com colisor (a bola bate na trave de verdade)
            var postMat = Mat(Color.white, .6f);
            float px = GoalHalfWidth + .06f;
            Prim(PrimitiveType.Cylinder, R, new Vector3(-px, 1.25f, 0), new Vector3(.12f, 1.25f, .12f), postMat, true);
            Prim(PrimitiveType.Cylinder, R, new Vector3(px, 1.25f, 0), new Vector3(.12f, 1.25f, .12f), postMat, true);
            Prim(PrimitiveType.Cylinder, R, new Vector3(0, GoalHeight + .06f, 0), new Vector3(.12f, px, .12f), postMat, true, Quaternion.Euler(0, 0, 90));

            // rede com teto inclinado (só visual) e um colisor invisível que segura a bola
            var netMat = Mat(new Color(.92f, .93f, .95f), .1f);
            const float depthTop = 1.1f, depthBottom = 2.2f;
            for (float x = -GoalHalfWidth; x <= GoalHalfWidth + .01f; x += .22f)
            {
                var top = new Vector3(x, GoalHeight, depthTop);
                NetLine(R, top, new Vector3(x, 0, depthBottom), netMat);
                NetLine(R, new Vector3(x, GoalHeight, 0), top, netMat);
            }
            for (float t = 0; t <= 1.001f; t += .1f)
            {
                float y = Mathf.Lerp(GoalHeight, 0, t), z = Mathf.Lerp(depthTop, depthBottom, t);
                NetLine(R, new Vector3(-GoalHalfWidth, y, z), new Vector3(GoalHalfWidth, y, z), netMat);
                NetLine(R, new Vector3(-px, y, 0), new Vector3(-px, y, z), netMat);
                NetLine(R, new Vector3(px, y, 0), new Vector3(px, y, z), netMat);
            }
            for (float z = .22f; z < depthTop; z += .22f)
                NetLine(R, new Vector3(-GoalHalfWidth, GoalHeight, z), new Vector3(GoalHalfWidth, GoalHeight, z), netMat);
            var wall = new GameObject("RedeColisor");
            wall.transform.SetParent(R, false);
            wall.transform.localPosition = new Vector3(0, 1.3f, depthBottom - .3f);
            wall.AddComponent<BoxCollider>().size = new Vector3(9, 2.8f, .3f);

            // placas de publicidade
            for (int i = 0; i < 9; i++)
            {
                var board = Prim(PrimitiveType.Cube, R, new Vector3(-36 + i * 9, .5f, 5.5f), new Vector3(8.8f, 1f, .15f), Mat(Theme.Hex(boardColors[i % boardColors.Length]), .4f));
                Prim(PrimitiveType.Cube, board.transform, new Vector3(0, 0, -.6f), new Vector3(.55f, .25f, .2f), Mat(Color.white, .4f));
            }
            for (int i = 0; i < 6; i++)
            {
                var m = Mat(Theme.Hex(boardColors[(i + 3) % boardColors.Length]), .4f);
                Prim(PrimitiveType.Cube, R, new Vector3(-38, .5f, -4 - i * 9), new Vector3(.15f, 1f, 8.8f), m);
                Prim(PrimitiveType.Cube, R, new Vector3(38, .5f, -4 - i * 9), new Vector3(.15f, 1f, 8.8f), m);
            }

            // arquibancadas em degraus, com torcida
            var crowdBack = TexMat(Crowd(), new Vector2(26, 1));
            var crowdSide = TexMat(Crowd(), new Vector2(24, 1));
            for (int i = 0; i < 9; i++)
            {
                float y = 1.2f + i * 1.15f, zb = 9 + i * 1.6f, xs = 42 + i * 1.6f;
                Prim(PrimitiveType.Cube, R, new Vector3(0, y, zb), new Vector3(110, 1.15f, 1.6f), crowdBack);
                Prim(PrimitiveType.Cube, R, new Vector3(-xs, y, -30), new Vector3(1.6f, 1.15f, 100), crowdSide);
                Prim(PrimitiveType.Cube, R, new Vector3(xs, y, -30), new Vector3(1.6f, 1.15f, 100), crowdSide);
            }
            var concrete = Mat(Theme.Hex("#5E6672"), .1f);
            Prim(PrimitiveType.Cube, R, new Vector3(0, 6, 25), new Vector3(116, 12, 1), concrete);
            Prim(PrimitiveType.Cube, R, new Vector3(-57, 6, -30), new Vector3(1, 12, 104), concrete);
            Prim(PrimitiveType.Cube, R, new Vector3(57, 6, -30), new Vector3(1, 12, 104), concrete);

            // cobertura e refletores
            var roof = Mat(Theme.Hex("#D9DDE3"), .5f);
            var steel = Mat(Theme.Hex("#8A939E"), .6f);
            Prim(PrimitiveType.Cube, R, new Vector3(0, 15.5f, 18), new Vector3(118, .5f, 16), roof, false, Quaternion.Euler(-8, 0, 0));
            Prim(PrimitiveType.Cube, R, new Vector3(-50, 15.5f, -30), new Vector3(14, .5f, 104), roof, false, Quaternion.Euler(0, 0, 8));
            Prim(PrimitiveType.Cube, R, new Vector3(50, 15.5f, -30), new Vector3(14, .5f, 104), roof, false, Quaternion.Euler(0, 0, -8));
            for (int i = 0; i < 6; i++)
                Prim(PrimitiveType.Cylinder, R, new Vector3(-50 + i * 20, 8, 25), new Vector3(.5f, 8, .5f), steel);
            var lamp = Glow(new Color(1f, .97f, .85f));
            foreach (var x in new[] { -40f, -20f, 0f, 20f, 40f })
                Prim(PrimitiveType.Cube, R, new Vector3(x, 15.1f, 10.5f), new Vector3(4, .3f, .8f), lamp, false, Quaternion.Euler(-30, 0, 0));

            // bola
            var ball = Prim(PrimitiveType.Sphere, R, new Vector3(0, BallRadius, -11), Vector3.one * BallRadius * 2, TexMat(BallTexture(), Vector2.one, .35f), true);
            ball.name = "Bola";
            a.Ball = ball.AddComponent<Rigidbody>();
            a.Ball.mass = .43f;
            a.Ball.linearDamping = .05f;
            a.Ball.angularDamping = .3f;
            a.Ball.interpolation = RigidbodyInterpolation.Interpolate;
            a.Ball.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            a.Ball.isKinematic = true;
            return a;
        }

        static void NetLine(Transform R, Vector3 a, Vector3 b, Material m)
        {
            var d = b - a;
            if (d.magnitude < .01f) return;
            Prim(PrimitiveType.Cube, R, (a + b) / 2, new Vector3(.012f, .012f, d.magnitude), m, false, Quaternion.LookRotation(d));
        }

        /// <summary>
        /// Imagem do lance. A câmera desenha numa textura que a própria interface mostra (ChanceHud),
        /// assim nenhum painel da interface consegue cobrir o campo.
        /// </summary>
        public RenderTexture View;
        public System.Action<Texture> OnView;

        void EnsureView()
        {
            int w = Mathf.Max(16, Screen.width), h = Mathf.Max(16, Screen.height);
            if (View != null && View.width == w && View.height == h) return;
            if (View != null) { Cam.targetTexture = null; View.Release(); Object.Destroy(View); }
            View = new RenderTexture(w, h, 24) { name = "Lance3D", antiAliasing = Mathf.Clamp(QualitySettings.antiAliasing, 1, 8) };
            View.Create();
            Cam.targetTexture = View;
            OnView?.Invoke(View);
        }

        /// <summary>Ajusta o campo de visão e a área de desenho para a tela deitada.</summary>
        public void FitCamera()
        {
            EnsureView();
            // paisagem: campo de visão horizontal amplo, como a câmera de um jogo de futebol
            Cam.rect = Landscape.Viewport01;
            float hFov = 80f * Mathf.Deg2Rad;
            Cam.fieldOfView = Mathf.Clamp(2f * Mathf.Atan(Mathf.Tan(hFov / 2f) / Landscape.GameAspect) * Mathf.Rad2Deg, 50f, 60f);
        }

        public Transform Person(string name, Color shirt, Color shorts, Vector3 pos, float yaw, bool keeper = false)
        {
            var rig = PersonRig.Build(Root, name, shirt, shorts, keeper ? shorts : shirt, Theme.Hex("#151515"), Rng.RangeInt(2, 30), keeper);
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
            RenderSettings.skybox = prevSky;
            RenderSettings.fog = prevFog; RenderSettings.fogColor = prevFogColor; RenderSettings.fogMode = prevFogMode;
            RenderSettings.fogStartDistance = prevFogStart; RenderSettings.fogEndDistance = prevFogEnd;
            if (Cam != null) Cam.targetTexture = null;
            if (View != null) { View.Release(); Object.Destroy(View); View = null; }
            if (Root != null) Object.Destroy(Root.gameObject);
        }
    }
}
