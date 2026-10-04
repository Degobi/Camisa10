using System;
using System.Collections.Generic;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Troféus em 3D de metal (ouro, prata, malaquita), cada um no formato da taça de verdade, fotografados com luz
    /// de estúdio e reflexo e entregues como imagem para a interface. Ficam em cache.
    /// </summary>
    public static class TrophyStudio
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        const int Layer = 30, Size = 512;

        /// <summary>Qual troféu corresponde ao título/prêmio (pelo texto que o jogo grava).</summary>
        public static string KindFor(string title)
        {
            string t = title ?? "";
            if (t.Contains("Copa do Mundo")) return "copa_mundo";
            if (t.Contains("Bola de Ouro")) return "bola_ouro";
            if (t.StartsWith("Artilheiro")) return "chuteira";
            if (t.StartsWith("Craque") || t.StartsWith("Seleção") || t.StartsWith("Revelação")) return "estrela";
            if (t.Contains("Brasileirão")) return "brasileirao";
            if (t.Contains("Premier League")) return "premier";
            if (t.Contains("La Liga")) return "laliga";
            if (t.Contains("Serie A")) return "seriea";
            if (t.Contains("Ligue 1")) return "ligue1";
            if (t.Contains("MLS")) return "mls";
            if (t.Contains("Saudi") || t.Contains("Süper Lig")) return "ouro_alcas";
            if (t.Contains("Copa") || t.Contains("Cup") || t.Contains("Coppa") || t.Contains("Coupe")) return "copa";
            return "taca";
        }

        public static Sprite Get(string kind)
        {
            if (cache.TryGetValue(kind, out var s) && s != null) return s;
            Sprite sp = null;
            try { sp = Render(kind); }
            catch (Exception e) { Debug.LogWarning("[Camisa 10] Troféu 3D falhou: " + e.Message); }
            cache[kind] = sp;
            return sp;
        }

        // ---------- materiais ----------
        static Material Metal(Color c, float smooth)
        {
            var m = Arena.Mat(c, smooth);
            m = new Material(m);
            m.SetFloat("_Metallic", 1f);
            m.SetFloat("_Glossiness", smooth);
            m.SetFloat("_Smoothness", smooth);
            return m;
        }
        static readonly Color GoldC = new Color(1f, .78f, .34f), SilverC = new Color(.9f, .91f, .93f);

        // ---------- geometria ----------
        /// <summary>Superfície de revolução a partir de um perfil (raio, altura). twist deforma o raio (espirais, gomos).</summary>
        static Mesh Lathe(IList<Vector2> profile, int seg = 64, Func<float, float, float> twist = null)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int j = 0; j < profile.Count; j++)
                for (int i = 0; i <= seg; i++)
                {
                    float a = i / (float)seg * Mathf.PI * 2;
                    float r = profile[j].x + (twist != null ? twist(a, profile[j].y) : 0);
                    v.Add(new Vector3(Mathf.Cos(a) * r, profile[j].y, Mathf.Sin(a) * r));
                }
            for (int j = 0; j < profile.Count - 1; j++)
                for (int i = 0; i < seg; i++)
                {
                    int a = j * (seg + 1) + i, b = a + seg + 1;
                    t.Add(a); t.Add(b); t.Add(a + 1); t.Add(a + 1); t.Add(b); t.Add(b + 1); // horário visto de fora
                }
            var m = new Mesh();
            m.SetVertices(v); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Alça: tubo em arco (meia-volta) do lado indicado.</summary>
        static Mesh Handle(float side, float y0, float y1, float x0, float reach, float thick)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            const int N = 24, R = 12;
            float cy = (y0 + y1) / 2, ry = (y1 - y0) / 2;
            for (int i = 0; i <= N; i++)
            {
                float u = i / (float)N * Mathf.PI; // 0..pi
                var c = new Vector3(side * (x0 + Mathf.Sin(u) * reach), cy + Mathf.Cos(u) * ry, 0);
                var tan = new Vector3(side * Mathf.Cos(u) * reach, -Mathf.Sin(u) * ry, 0).normalized;
                var nrm = Vector3.Cross(tan, Vector3.forward).normalized;
                for (int k = 0; k <= R; k++)
                {
                    float a = k / (float)R * Mathf.PI * 2;
                    v.Add(c + (nrm * Mathf.Cos(a) + Vector3.forward * Mathf.Sin(a)) * thick);
                }
            }
            for (int i = 0; i < N; i++)
                for (int k = 0; k < R; k++)
                {
                    int a = i * (R + 1) + k, b = a + R + 1;
                    t.Add(a); t.Add(b); t.Add(a + 1); t.Add(a + 1); t.Add(b); t.Add(b + 1);
                }
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static GameObject Part(Transform root, Mesh mesh, Material mat, List<UnityEngine.Object> trash)
        {
            var go = new GameObject("Peca") { layer = Layer };
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            trash.Add(mesh);
            return go;
        }

        static GameObject Prim(Transform root, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, Quaternion? rot = null)
        {
            var go = Arena.Prim(type, root, pos, scale, mat, false, rot);
            go.layer = Layer;
            return go;
        }

        static List<Vector2> P(params float[] rv) { var l = new List<Vector2>(); for (int i = 0; i < rv.Length; i += 2) l.Add(new Vector2(rv[i], rv[i + 1])); return l; }

        /// <summary>Monta o troféu (altura ~1) na raiz. Devolve a altura total para enquadrar.</summary>
        static float Build(string kind, Transform root, List<UnityEngine.Object> trash)
        {
            var gold = Metal(GoldC, .86f); var silver = Metal(SilverC, .9f);
            var black = Arena.Mat(new Color(.04f, .04f, .05f), .75f);
            var green = Arena.Mat(new Color(.03f, .38f, .2f), .85f); // malaquita
            trash.Add(gold); trash.Add(silver);
            // base preta padrão
            void Base(float r, float h) => Part(root, Lathe(P(0, 0, r, 0, r, h * .15f, r * .92f, h * .2f, r * .92f, h * .8f, r, h * .85f, r, h, 0, h)), black, trash);

            switch (kind)
            {
                case "copa_mundo":
                {
                    // base com dois anéis verdes e figuras em espiral subindo até segurar o globo
                    Part(root, Lathe(P(0, 0, .2f, 0, .2f, .035f, .185f, .04f)), gold, trash);
                    Part(root, Lathe(P(.185f, .04f, .19f, .07f, .18f, .075f)), green, trash);
                    Part(root, Lathe(P(.18f, .075f, .175f, .1f, .165f, .105f)), gold, trash);
                    Part(root, Lathe(P(.165f, .105f, .168f, .135f, .155f, .14f)), green, trash);
                    var body = P(.155f, .14f, .15f, .2f, .12f, .32f, .085f, .45f, .075f, .55f, .1f, .64f, .14f, .7f, .155f, .74f, .12f, .78f, 0, .8f);
                    Part(root, Lathe(body, 96, (a, y) => Mathf.Sin(a * 3 + y * 14f) * .018f * Mathf.Clamp01((y - .14f) * 4)), gold, trash);
                    var globe = Prim(root, PrimitiveType.Sphere, new Vector3(0, .86f, 0), Vector3.one * .26f, gold);
                    return 1f;
                }
                case "bola_ouro":
                {
                    Base(.16f, .14f);
                    Part(root, Lathe(P(.13f, .14f, .1f, .18f, .05f, .24f, .05f, .3f, .08f, .33f, 0, .34f)), gold, trash);
                    Prim(root, PrimitiveType.Sphere, new Vector3(0, .5f, 0), Vector3.one * .34f, gold);
                    return .7f;
                }
                case "chuteira":
                {
                    Base(.2f, .12f);
                    // chuteira estilizada (bico para a frente): bico baixo, peito do pé, calcanhar alto e solado com travas
                    Prim(root, PrimitiveType.Sphere, new Vector3(0, .19f, .13f), new Vector3(.14f, .09f, .26f), gold);
                    Prim(root, PrimitiveType.Sphere, new Vector3(0, .22f, -.02f), new Vector3(.15f, .13f, .26f), gold);
                    Prim(root, PrimitiveType.Sphere, new Vector3(0, .28f, -.12f), new Vector3(.13f, .2f, .14f), gold);
                    Prim(root, PrimitiveType.Cube, new Vector3(0, .155f, 0), new Vector3(.13f, .02f, .44f), gold);
                    for (int i = 0; i < 4; i++) Prim(root, PrimitiveType.Cylinder, new Vector3(0, .14f, -.15f + i * .1f), new Vector3(.03f, .012f, .03f), gold);
                    root.localRotation = Quaternion.Euler(0, 60, 0);
                    return .55f;
                }
                case "estrela":
                {
                    Base(.13f, .14f);
                    var star = new MeshBuilder();
                    for (int i = 0; i < 10; i++)
                    {
                        float a0 = i / 10f * Mathf.PI * 2, a1 = (i + 1) / 10f * Mathf.PI * 2;
                        float r0 = i % 2 == 0 ? .22f : .09f, r1 = i % 2 == 0 ? .09f : .22f;
                        Vector3 p0 = new Vector3(Mathf.Sin(a0) * r0, Mathf.Cos(a0) * r0 + .42f, 0), p1 = new Vector3(Mathf.Sin(a1) * r1, Mathf.Cos(a1) * r1 + .42f, 0);
                        var c = new Vector3(0, .42f, .05f); var cb = new Vector3(0, .42f, -.05f);
                        star.Quad(p0, c, c, p1); star.Quad(p1, cb, cb, p0);
                    }
                    var sm = star.ToMesh("Estrela"); sm.RecalculateNormals();
                    Part(root, sm, gold, trash);
                    Prim(root, PrimitiveType.Cylinder, new Vector3(0, .24f, 0), new Vector3(.04f, .1f, .04f), gold); // haste até a estrela
                    return .7f;
                }
                case "premier":
                {
                    Base(.17f, .16f);
                    Part(root, Lathe(P(.13f, .16f, .06f, .2f, .05f, .3f, .1f, .36f, .2f, .5f, .23f, .62f, .21f, .7f, .0f, .7f)), silver, trash);
                    // coroa dourada no alto, com pontas
                    Part(root, Lathe(P(.2f, .7f, .21f, .76f, .19f, .78f)), gold, trash);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i / 8f * Mathf.PI * 2;
                        Prim(root, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * .19f, .82f, Mathf.Sin(a) * .19f), Vector3.one * .05f, gold);
                    }
                    Prim(root, PrimitiveType.Sphere, new Vector3(0, .84f, 0), Vector3.one * .1f, gold);
                    Part(root, Handle(1, .38f, .62f, .2f, .1f, .018f), silver, trash);
                    Part(root, Handle(-1, .38f, .62f, .2f, .1f, .018f), silver, trash);
                    return .95f;
                }
                case "ligue1":
                {
                    // "Hexagoal": peça sextavada que afina e abre de novo
                    Base(.16f, .12f);
                    Part(root, Lathe(P(.14f, .12f, .09f, .3f, .07f, .45f, .12f, .65f, .18f, .82f, 0, .84f), 6, (a, y) => 0), silver, trash);
                    return .9f;
                }
                case "seriea":
                {
                    Base(.16f, .16f);
                    Part(root, Lathe(P(.12f, .16f, .05f, .22f, .05f, .34f, .09f, .4f, .16f, .55f, .17f, .72f, .14f, .78f, .15f, .8f, .0f, .8f)), silver, trash);
                    Part(root, Lathe(P(.15f, .8f, .1f, .84f, .03f, .9f, .05f, .95f, 0, .96f)), silver, trash);
                    return 1f;
                }
                case "mls":
                {
                    Base(.15f, .14f);
                    Part(root, Lathe(P(.12f, .14f, .07f, .3f, .08f, .5f, .0f, .5f)), silver, trash);
                    Part(root, Lathe(P(.08f, .5f, .17f, .62f, .18f, .74f, .0f, .74f)), gold, trash);
                    return .85f;
                }
                case "laliga":
                {
                    Base(.17f, .14f);
                    Part(root, Lathe(P(.13f, .14f, .07f, .2f, .06f, .3f, .14f, .45f, .19f, .6f, .16f, .72f, .17f, .74f, 0, .74f)), silver, trash);
                    Part(root, Lathe(P(.17f, .74f, .12f, .8f, .04f, .86f, .06f, .9f, 0, .92f)), silver, trash);
                    Part(root, Handle(1, .45f, .7f, .17f, .08f, .016f), silver, trash);
                    Part(root, Handle(-1, .45f, .7f, .17f, .08f, .016f), silver, trash);
                    return .95f;
                }
                case "copa":
                {
                    // taça de copa nacional: prata, alças grandes e tampa
                    Base(.16f, .14f);
                    Part(root, Lathe(P(.12f, .14f, .05f, .2f, .05f, .32f, .1f, .38f, .2f, .5f, .2f, .66f, .17f, .7f, 0, .7f)), silver, trash);
                    Part(root, Lathe(P(.17f, .7f, .1f, .76f, .04f, .82f, .07f, .88f, 0, .9f)), silver, trash);
                    Part(root, Handle(1, .36f, .7f, .19f, .16f, .022f), silver, trash);
                    Part(root, Handle(-1, .36f, .7f, .19f, .16f, .022f), silver, trash);
                    return .95f;
                }
                case "brasileirao":
                {
                    // taça dourada larga com alças, sobre base preta com anel
                    Base(.18f, .18f);
                    Part(root, Lathe(P(.15f, .18f, .16f, .2f, .07f, .24f, .05f, .34f, .09f, .4f, .2f, .5f, .24f, .62f, .22f, .7f, 0, .7f)), gold, trash);
                    Part(root, Handle(1, .38f, .66f, .22f, .13f, .022f), gold, trash);
                    Part(root, Handle(-1, .38f, .66f, .22f, .13f, .022f), gold, trash);
                    return .9f;
                }
                case "ouro_alcas":
                {
                    Base(.17f, .15f);
                    Part(root, Lathe(P(.13f, .15f, .06f, .22f, .05f, .32f, .12f, .42f, .22f, .58f, .2f, .7f, 0, .7f)), gold, trash);
                    Part(root, Handle(1, .4f, .66f, .2f, .12f, .02f), gold, trash);
                    Part(root, Handle(-1, .4f, .66f, .2f, .12f, .02f), gold, trash);
                    return .9f;
                }
                default:
                {
                    Base(.15f, .14f);
                    Part(root, Lathe(P(.12f, .14f, .05f, .2f, .05f, .3f, .1f, .36f, .18f, .5f, .19f, .62f, 0, .62f)), gold, trash);
                    Part(root, Handle(1, .36f, .58f, .18f, .1f, .018f), gold, trash);
                    Part(root, Handle(-1, .36f, .58f, .18f, .1f, .018f), gold, trash);
                    return .8f;
                }
            }
        }

        static Cubemap studioEnv;

        /// <summary>Ambiente de estúdio para o reflexo do metal: softboxes claras em cima e dos lados, chão escuro.</summary>
        static Cubemap StudioEnv()
        {
            if (studioEnv != null) return studioEnv;
            const int S = 64;
            studioEnv = new Cubemap(S, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontUnloadUnusedAsset };
            var faces = new[] { CubemapFace.PositiveX, CubemapFace.NegativeX, CubemapFace.PositiveY, CubemapFace.NegativeY, CubemapFace.PositiveZ, CubemapFace.NegativeZ };
            foreach (var f in faces)
            {
                var px = new Color[S * S];
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float u = (x + .5f) / S * 2 - 1, v = (y + .5f) / S * 2 - 1;
                        Vector3 d;
                        switch (f)
                        {
                            case CubemapFace.PositiveX: d = new Vector3(1, -v, -u); break;
                            case CubemapFace.NegativeX: d = new Vector3(-1, -v, u); break;
                            case CubemapFace.PositiveY: d = new Vector3(u, 1, v); break;
                            case CubemapFace.NegativeY: d = new Vector3(u, -1, -v); break;
                            case CubemapFace.PositiveZ: d = new Vector3(u, -v, 1); break;
                            default: d = new Vector3(-u, -v, -1); break;
                        }
                        d.Normalize();
                        float top = Mathf.Clamp01(d.y);
                        float c = .06f + .25f * Mathf.Clamp01(d.y + .2f) + .9f * Mathf.Pow(top, 6);
                        // duas softboxes verticais dos lados
                        float box = Mathf.Pow(Mathf.Clamp01(Mathf.Abs(d.x) - .55f) / .45f, 2) * Mathf.Clamp01(1 - Mathf.Abs(d.y) * 1.3f);
                        c += box * 1.1f;
                        px[y * S + x] = new Color(c, c * .98f, c * .95f, 1);
                    }
                studioEnv.SetPixels(px, f);
            }
            studioEnv.Apply();
            return studioEnv;
        }

        static Sprite Render(string kind)
        {
            var trash = new List<UnityEngine.Object>();
            var stage = new GameObject("EstudioTrofeu");
            stage.transform.position = new Vector3(0, -800, 0);
            var root = new GameObject("Trofeu").transform;
            root.SetParent(stage.transform, false);
            float h = Build(kind, root, trash);

            // luz de estúdio só nesta camada
            Light L(Vector3 euler, float intensity, Color c)
            {
                var lg = new GameObject("Luz").AddComponent<Light>();
                lg.transform.SetParent(stage.transform, false);
                lg.type = LightType.Directional; lg.intensity = intensity; lg.color = c;
                lg.transform.rotation = Quaternion.Euler(euler);
                lg.cullingMask = 1 << Layer;
                lg.shadows = LightShadows.None;
                return lg;
            }
            L(new Vector3(35, -40, 0), 1.25f, new Color(1f, .97f, .92f));
            L(new Vector3(10, 140, 0), .7f, new Color(.85f, .9f, 1f));
            L(new Vector3(-20, 180, 0), .5f, Color.white);

            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.transform.SetParent(stage.transform, false);
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 24;
            cam.nearClipPlane = .05f; cam.farClipPlane = 20;
            float dist = h * 2.55f + .5f;
            cam.transform.localPosition = new Vector3(0, h * .62f, -dist);
            cam.transform.LookAt(stage.transform.position + new Vector3(0, h * .48f, 0));
            cam.enabled = false;

            var rt = RenderTexture.GetTemporary(Size, Size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 4);
            cam.targetTexture = rt;

            // ambiente e reflexo de estúdio só durante a foto
            var prevMode = RenderSettings.ambientMode; var prevAmb = RenderSettings.ambientLight;
            var prevRefl = RenderSettings.defaultReflectionMode; var prevCube = RenderSettings.customReflectionTexture;
            var prevFog = RenderSettings.fog;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.22f, .22f, .24f);
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = StudioEnv();
            RenderSettings.fog = false;
            cam.Render();
            RenderSettings.ambientMode = prevMode; RenderSettings.ambientLight = prevAmb;
            RenderSettings.defaultReflectionMode = prevRefl; RenderSettings.customReflectionTexture = prevCube;
            RenderSettings.fog = prevFog;

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { hideFlags = HideFlags.DontUnloadUnusedAsset };
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply(true, true);
            RenderTexture.active = prev;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(stage);
            foreach (var o in trash) if (o != null) UnityEngine.Object.DestroyImmediate(o);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), 100);
        }
    }
}
