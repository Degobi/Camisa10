using Camisa10.Core;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Jogador articulado (quadril, tronco, braços com cotovelo, pernas com joelho) e animação procedural.
    /// Pés em y = 0, olhando para +z.
    /// </summary>
    public class PersonRig : MonoBehaviour
    {
        public enum Mode { Idle, Run, Ready, Jump, Dive, Stumble, Celebrate }

        public Mode mode = Mode.Idle;
        public float speed = 1f;  // velocidade da corrida em m/s (controla a cadência)
        public float diveSide;    // -1 esquerda, +1 direita (mundo)

        Transform hips, torso, lShoulder, rShoulder, lElbow, rElbow, lHip, rHip, lKnee, rKnee;
        float phase, modeTime, idleSeed;
        Mode lastMode;
        const float HipsY = .95f;

        static readonly Color[] Skins = { Theme.Hex("#F1C9A5"), Theme.Hex("#D9A27A"), Theme.Hex("#B07A52"), Theme.Hex("#8D5A3B"), Theme.Hex("#5C3A24") };
        static readonly Color[] Hairs = { Theme.Hex("#1B1410"), Theme.Hex("#3B2A1E"), Theme.Hex("#6B4A2B"), Theme.Hex("#C9A15A"), Theme.Hex("#111111") };

        public static PersonRig Build(Transform parent, string name, Color shirt, Color shorts, Color socks, Color boots, int number, bool keeper)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var rig = root.gameObject.AddComponent<PersonRig>();
            rig.idleSeed = (float)Rng.RangeF(0, 10);

            var skin = Arena.Mat(Skins[Rng.RangeInt(0, Skins.Length - 1)]);
            var hair = Arena.Mat(Hairs[Rng.RangeInt(0, Hairs.Length - 1)]);
            var shirtM = Arena.Mat(shirt);
            var shortsM = Arena.Mat(shorts);
            var sockM = Arena.Mat(socks);
            var bootM = Arena.Mat(boots);
            var gloveM = Arena.Mat(keeper ? Theme.Hex("#F5F5F5") : skin.color);

            Transform Pivot(string n, Transform p, Vector3 pos)
            {
                var t = new GameObject(n).transform;
                t.SetParent(p, false);
                t.localPosition = pos;
                return t;
            }

            rig.hips = Pivot("Quadril", root, new Vector3(0, HipsY, 0));
            Arena.Prim(PrimitiveType.Capsule, rig.hips, new Vector3(0, -.02f, 0), new Vector3(.42f, .15f, .27f), shortsM);

            rig.torso = Pivot("Tronco", rig.hips, new Vector3(0, .06f, 0));
            Arena.Prim(PrimitiveType.Capsule, rig.torso, new Vector3(0, .26f, 0), new Vector3(.46f, .29f, .27f), shirtM);
            Arena.Prim(PrimitiveType.Capsule, rig.torso, new Vector3(0, .58f, 0), new Vector3(.1f, .06f, .1f), skin);
            Arena.Prim(PrimitiveType.Sphere, rig.torso, new Vector3(0, .74f, .01f), new Vector3(.22f, .25f, .23f), skin);
            Arena.Prim(PrimitiveType.Sphere, rig.torso, new Vector3(0, .8f, -.015f), new Vector3(.235f, .17f, .24f), hair);

            // número nas costas
            var back = Arena.Prim(PrimitiveType.Quad, rig.torso, new Vector3(0, .3f, -.142f), new Vector3(.24f, .24f, 1f), NumberMat(number, shirt), false, Quaternion.Euler(0, 0, 0));
            back.name = "Numero";

            rig.lShoulder = Pivot("OmbroE", rig.torso, new Vector3(-.27f, .45f, 0));
            rig.rShoulder = Pivot("OmbroD", rig.torso, new Vector3(.27f, .45f, 0));
            foreach (var sh in new[] { rig.lShoulder, rig.rShoulder })
            {
                Arena.Prim(PrimitiveType.Capsule, sh, new Vector3(0, -.1f, 0), new Vector3(.13f, .13f, .13f), shirtM);
                var elbow = Pivot("Cotovelo", sh, new Vector3(0, -.27f, 0));
                Arena.Prim(PrimitiveType.Capsule, elbow, new Vector3(0, -.13f, 0), new Vector3(.095f, .14f, .095f), keeper ? shirtM : skin);
                Arena.Prim(PrimitiveType.Sphere, elbow, new Vector3(0, -.29f, 0), new Vector3(.1f, .11f, .1f), gloveM);
                if (sh == rig.lShoulder) rig.lElbow = elbow; else rig.rElbow = elbow;
            }

            rig.lHip = Pivot("QuadrilE", rig.hips, new Vector3(-.11f, -.05f, 0));
            rig.rHip = Pivot("QuadrilD", rig.hips, new Vector3(.11f, -.05f, 0));
            foreach (var hp in new[] { rig.lHip, rig.rHip })
            {
                Arena.Prim(PrimitiveType.Capsule, hp, new Vector3(0, -.2f, 0), new Vector3(.155f, .21f, .155f), skin);
                var knee = Pivot("Joelho", hp, new Vector3(0, -.42f, 0));
                Arena.Prim(PrimitiveType.Capsule, knee, new Vector3(0, -.2f, 0), new Vector3(.125f, .2f, .125f), sockM);
                Arena.Prim(PrimitiveType.Cube, knee, new Vector3(0, -.43f, .05f), new Vector3(.11f, .08f, .27f), bootM);
                if (hp == rig.lHip) rig.lKnee = knee; else rig.rKnee = knee;
            }
            return rig;
        }

        // ---------- número da camisa (fonte bitmap 3x5) ----------
        static readonly string[] Digits =
        {
            "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
            "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111"
        };

        static Material NumberMat(int number, Color shirt)
        {
            int w = 64, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            Color32 bg = shirt, fg = (shirt.r + shirt.g + shirt.b) / 3f > .6f ? new Color32(20, 20, 20, 255) : new Color32(250, 250, 250, 255);
            for (int i = 0; i < px.Length; i++) px[i] = bg;
            string s = Mathf.Clamp(number, 1, 99).ToString();
            int cell = 6, digitW = 3 * cell, gap = 4;
            int total = s.Length * digitW + (s.Length - 1) * gap, x0 = (w - total) / 2, y0 = (h - 5 * cell) / 2;
            for (int k = 0; k < s.Length; k++)
            {
                string pat = Digits[s[k] - '0'];
                for (int row = 0; row < 5; row++)
                    for (int col = 0; col < 3; col++)
                    {
                        if (pat[row * 3 + col] != '1') continue;
                        for (int yy = 0; yy < cell; yy++)
                            for (int xx = 0; xx < cell; xx++)
                            {
                                int x = x0 + k * (digitW + gap) + col * cell + xx;
                                int y = y0 + (4 - row) * cell + yy;
                                if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = fg;
                            }
                    }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Arena.TexMat(tex, Vector2.one);
        }

        // ---------- animação ----------
        public void Set(Mode m, float runSpeed = 1f)
        {
            mode = m;
            speed = runSpeed;
        }

        static Quaternion X(float a) => Quaternion.Euler(a, 0, 0);
        static Quaternion Z(float a) => Quaternion.Euler(0, 0, a);

        void Update()
        {
            if (hips == null) return;
            float dt = Time.deltaTime;
            if (mode != lastMode) { modeTime = 0; lastMode = mode; }
            modeTime += dt;
            float k = 1f - Mathf.Exp(-14f * dt); // suavização entre poses

            Quaternion lh = Quaternion.identity, rh = lh, lk = lh, rk = lh, ls = lh, rs = lh, le = lh, re = lh, tr = lh;
            float hipY = HipsY;

            switch (mode)
            {
                case Mode.Run:
                {
                    phase += dt * Mathf.Clamp(speed, 2f, 9f) * 1.35f;
                    float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
                    lh = X(-s * 38); rh = X(s * 38);
                    lk = X(Mathf.Max(0, c) * 70 + 8); rk = X(Mathf.Max(0, -c) * 70 + 8);
                    ls = X(s * 40); rs = X(-s * 40);
                    le = X(-60); re = X(-60);
                    tr = X(9);
                    hipY = HipsY - .04f + Mathf.Abs(s) * .05f;
                    break;
                }
                case Mode.Ready:
                {
                    float b = Mathf.Sin((Time.time + idleSeed) * 3f) * 2f;
                    lh = X(-28); rh = X(-28); lk = X(52); rk = X(52);
                    ls = Z(-38) * X(-30); rs = Z(38) * X(-30);
                    le = X(-35); re = X(-35);
                    tr = X(18 + b);
                    hipY = HipsY - .14f;
                    break;
                }
                case Mode.Jump:
                {
                    float j = Mathf.Clamp01(modeTime / .45f);
                    float arc = Mathf.Sin(j * Mathf.PI);
                    hipY = HipsY + arc * .38f;
                    lk = X(25 * arc); rk = X(25 * arc);
                    ls = X(-150 * arc); rs = X(-150 * arc);
                    if (j >= 1f) mode = Mode.Idle;
                    break;
                }
                case Mode.Dive:
                {
                    ls = Z(-165); rs = Z(165);
                    le = Quaternion.identity; re = Quaternion.identity;
                    lh = X(-10); rh = X(-25); rk = X(30);
                    break;
                }
                case Mode.Stumble:
                    tr = X(55); lh = X(-40); rk = X(60); ls = X(-70); rs = X(-70);
                    hipY = HipsY - .25f;
                    break;
                case Mode.Celebrate:
                {
                    float b = Mathf.Sin(modeTime * 10f);
                    ls = Z(-150 + b * 10); rs = Z(150 - b * 10);
                    hipY = HipsY + Mathf.Abs(b) * .12f;
                    break;
                }
                default:
                {
                    float b = Mathf.Sin((Time.time + idleSeed) * 1.6f);
                    tr = X(2 + b * 1.2f);
                    ls = Z(-6); rs = Z(6); le = X(-12); re = X(-12);
                    lk = X(6); rk = X(6); lh = X(-3); rh = X(-3);
                    break;
                }
            }

            var hp = hips.localPosition;
            hp.y = Mathf.Lerp(hp.y, hipY, k);
            hips.localPosition = hp;
            torso.localRotation = Quaternion.Slerp(torso.localRotation, tr, k);
            lHip.localRotation = Quaternion.Slerp(lHip.localRotation, lh, k);
            rHip.localRotation = Quaternion.Slerp(rHip.localRotation, rh, k);
            lKnee.localRotation = Quaternion.Slerp(lKnee.localRotation, lk, k);
            rKnee.localRotation = Quaternion.Slerp(rKnee.localRotation, rk, k);
            lShoulder.localRotation = Quaternion.Slerp(lShoulder.localRotation, ls, k);
            rShoulder.localRotation = Quaternion.Slerp(rShoulder.localRotation, rs, k);
            lElbow.localRotation = Quaternion.Slerp(lElbow.localRotation, le, k);
            rElbow.localRotation = Quaternion.Slerp(rElbow.localRotation, re, k);
        }
    }
}
