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

            var human = HumanModel.Get();
            if (human != null)
            {
                rig.BuildHuman(human, shirt, shorts, socks, boots, number, keeper);
                return rig;
            }

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

        // ---------- corpo com captura de movimento ----------
        HumanModel model;
        Transform[] bones;
        Transform modelRoot;
        Quaternion[] poseA, poseB;
        string clip = "idle", prevClip;
        float clipTime, prevTime, blend = 1;
        float wArms, wCrouch, wLean, wReady;

        /// <summary>Para onde a cabeça olha (normalmente a bola).</summary>
        public Transform LookAt;
        // velocidade medida pelo deslocamento real: escolhe andar/correr e a cadência sem o pé "patinar"
        Vector3 lastPos, moveVel, leanEuler;
        bool posInit;
        float lookYaw, bodyYaw;
        int bNeck, bHead;
        int bSpine, bSpine1, bLArm, bLFore, bLHand, bRArm, bRFore, bRHand, bLUp, bLLeg, bLFoot, bRUp, bRLeg, bRFoot;

        void BuildHuman(HumanModel h, Color shirt, Color shorts, Color socks, Color boots, int number, bool keeper)
        {
            model = h;
            modelRoot = new GameObject("Corpo").transform;
            modelRoot.SetParent(transform, false);
            int n = h.Names.Length;
            bones = new Transform[n];
            for (int i = 0; i < n; i++)
            {
                var t = new GameObject(h.Names[i]).transform;
                t.SetParent(h.Parent[i] >= 0 ? bones[h.Parent[i]] : modelRoot, false);
                t.localPosition = h.RestPos[i]; t.localRotation = h.RestRot[i]; t.localScale = h.RestScale[i];
                bones[i] = t;
            }
            poseA = new Quaternion[n]; poseB = new Quaternion[n];

            Color skinC = Skins[Rng.RangeInt(0, Skins.Length - 1)];
            var skin = Arena.Mat(skinC, .3f);
            var mats = new Material[h.Mesh.subMeshCount];
            Material M(HumanModel.Part p, Material m) { if ((int)p < mats.Length) mats[(int)p] = m; return m; }
            M(HumanModel.Part.Shirt, Arena.Mat(shirt, .22f));
            M(HumanModel.Part.Skin, skin);
            M(HumanModel.Part.Shorts, Arena.Mat(shorts, .22f));
            M(HumanModel.Part.Socks, Arena.Mat(socks, .1f));
            M(HumanModel.Part.Boots, Arena.Mat(boots, .55f));
            M(HumanModel.Part.Hands, keeper ? Arena.Mat(Theme.Hex("#F5F5F5"), .2f) : skin);
            M(HumanModel.Part.Hair, Arena.Mat(Hairs[Rng.RangeInt(0, Hairs.Length - 1)], .35f));

            var smrGo = new GameObject("Malha");
            smrGo.transform.SetParent(modelRoot, false);
            var smr = smrGo.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = h.Mesh;
            smr.bones = bones;
            smr.rootBone = bones[h.Hips];
            smr.sharedMaterials = mats;
            smr.updateWhenOffscreen = true;
            smr.quality = SkinQuality.Bone4;

            // número nas costas, preso ao peito
            int chest = h.Bone("Spine2");
            if (chest >= 0)
            {
                var back = Arena.Prim(PrimitiveType.Quad, modelRoot, h.BackNumber, new Vector3(.22f, .22f, 1f), NumberMat(number, shirt));
                back.name = "Numero";
                back.transform.SetParent(bones[chest], true);
            }

            bSpine = h.Bone("Spine"); bSpine1 = h.Bone("Spine1");
            bLArm = h.Bone("LeftArm"); bLFore = h.Bone("LeftForeArm"); bLHand = h.Bone("LeftHand");
            bRArm = h.Bone("RightArm"); bRFore = h.Bone("RightForeArm"); bRHand = h.Bone("RightHand");
            bLUp = h.Bone("LeftUpLeg"); bLLeg = h.Bone("LeftLeg"); bLFoot = h.Bone("LeftFoot");
            bRUp = h.Bone("RightUpLeg"); bRLeg = h.Bone("RightLeg"); bRFoot = h.Bone("RightFoot");
            bNeck = h.Bone("Neck"); bHead = h.Bone("Head");
            clipTime = idleSeed;
        }

        /// <summary>Gira o osso (no mundo) para que o segmento até o filho aponte para dir.</summary>
        void Aim(int bone, int child, Vector3 dir, float w)
        {
            if (w <= .001f || bone < 0 || child < 0) return;
            var b = bones[bone];
            var cur = bones[child].position - b.position;
            if (cur.sqrMagnitude < 1e-6f) return;
            var rot = Quaternion.FromToRotation(cur, dir);
            b.rotation = Quaternion.Slerp(Quaternion.identity, rot, w) * b.rotation;
        }

        /// <summary>Velocidade horizontal real do jogador, em m/s.</summary>
        public float MeasuredSpeed => moveVel.magnitude;

        void UpdateHuman(float dt)
        {
            if (mode != lastMode) { modeTime = 0; lastMode = mode; }
            modeTime += dt;
            float k = 1f - Mathf.Exp(-10f * dt);

            // mede o movimento de verdade (quem move o jogador é o lance, não a animação)
            var pos = transform.position;
            if (!posInit) { lastPos = pos; posInit = true; }
            var inst = dt > 1e-4f ? (pos - lastPos) / dt : Vector3.zero;
            inst.y = 0;
            if (inst.sqrMagnitude > 15f * 15f) inst = moveVel; // teletransporte: ignora
            lastPos = pos;
            var prevVel = moveVel;
            moveVel = Vector3.Lerp(moveVel, inst, 1f - Mathf.Exp(-12f * dt));
            var accel = dt > 1e-4f ? (moveVel - prevVel) / dt : Vector3.zero;
            float spd = moveVel.magnitude;

            // parado, andando ou correndo conforme a velocidade, com folga para não ficar trocando
            bool locomote = mode == Mode.Idle || mode == Mode.Run || mode == Mode.Ready || mode == Mode.Stumble;
            float s = locomote ? spd : 0;
            bool hasWalk = model.Clips.ContainsKey("walk");
            string want;
            if (clip == "run") want = s > 1.9f ? "run" : s > .3f && hasWalk ? "walk" : s > .3f ? "run" : "idle";
            else if (clip == "walk") want = s > 2.5f ? "run" : s > .2f ? "walk" : "idle";
            else want = s > 2.5f ? "run" : s > .45f ? (hasWalk ? "walk" : "run") : "idle";
            if (want != clip && model.Clips.ContainsKey(want))
            {
                float frac = 0;
                if (model.Clips.TryGetValue(clip, out var oldC) && clip != "idle") frac = Mathf.Repeat(clipTime, oldC.Length) / Mathf.Max(.01f, oldC.Length);
                prevClip = clip; prevTime = clipTime;
                clip = want;
                // mantém a fase da passada entre andar e correr; do parado, começa em um ponto qualquer
                clipTime = want == "idle" ? idleSeed : (prevClip == "idle" ? UnityEngine.Random.value : frac) * model.Clips[want].Length;
                blend = 0;
            }
            // anda de costas (marcador acompanhando, goleiro recuando): toca o ciclo ao contrário
            float fwdDot = spd > .3f ? Vector3.Dot(moveVel / spd, transform.forward) : 1f;
            float cycleDir = fwdDot < -.35f ? -1f : 1f;
            float rate = clip == "run" ? Mathf.Clamp(s / 3.8f, .7f, 1.75f) : clip == "walk" ? Mathf.Clamp(s / 1.4f, .6f, 1.7f) : 1f;
            clipTime += dt * rate * (clip == "idle" ? 1f : cycleDir);
            prevTime += dt;
            blend = Mathf.Min(1, blend + dt / .25f);

            if (!model.Clips.TryGetValue(clip, out var c)) return;
            model.Sample(c, clipTime, poseA, out var hipsPos);
            if (blend < 1 && prevClip != null && model.Clips.TryGetValue(prevClip, out var pc))
            {
                model.Sample(pc, prevTime, poseB, out var hipsB);
                for (int i = 0; i < poseA.Length; i++) poseA[i] = Quaternion.Slerp(poseB[i], poseA[i], blend);
                hipsPos = Vector3.Lerp(hipsB, hipsPos, blend);
            }
            for (int i = 0; i < bones.Length; i++) bones[i].localRotation = poseA[i];

            // camadas por cima da captura: agachar, braços para cima, inclinar
            bool arms = mode == Mode.Jump || mode == Mode.Dive || mode == Mode.Celebrate;
            wArms = Mathf.Lerp(wArms, arms ? 1 : 0, k);
            wCrouch = Mathf.Lerp(wCrouch, mode == Mode.Ready ? 1 : mode == Mode.Stumble ? .7f : 0, k);
            wReady = Mathf.Lerp(wReady, mode == Mode.Ready ? 1 : 0, k);
            wLean = Mathf.Lerp(wLean, mode == Mode.Stumble ? 1 : 0, k);

            var hipsT = bones[model.Hips];
            var hp = hipsPos;
            if (hipsT.parent != null) hp += hipsT.parent.InverseTransformVector(Vector3.down * .16f * wCrouch);
            hipsT.localPosition = hp;

            Vector3 up = transform.up, fwd = transform.forward, right = transform.right;
            if (wCrouch > .001f)
            {
                Aim(bLUp, bLLeg, (fwd * .75f - up).normalized, wCrouch);
                Aim(bRUp, bRLeg, (fwd * .75f - up).normalized, wCrouch);
                Aim(bLLeg, bLFoot, (-fwd * .3f - up).normalized, wCrouch);
                Aim(bRLeg, bRFoot, (-fwd * .3f - up).normalized, wCrouch);
                if (bSpine >= 0) bones[bSpine].rotation = Quaternion.AngleAxis(22f * wCrouch, right) * bones[bSpine].rotation;
            }
            if (wLean > .001f && bSpine1 >= 0)
                bones[bSpine1].rotation = Quaternion.AngleAxis(40f * wLean, right) * bones[bSpine1].rotation;
            foreach (var (arm, fore, hand) in new[] { (bLArm, bLFore, bLHand), (bRArm, bRFore, bRHand) })
            {
                if (arm < 0) continue;
                float side = Mathf.Sign(Vector3.Dot(bones[arm].position - transform.position, right));
                if (wReady > .001f)
                {
                    Aim(arm, fore, (right * side * .7f - up * .5f + fwd * .45f).normalized, wReady);
                    Aim(fore, hand, (fwd * .8f + right * side * .3f).normalized, wReady);
                }
                if (wArms > .001f)
                {
                    var dir = (up + right * side * (mode == Mode.Celebrate ? .5f : .2f)).normalized;
                    Aim(arm, fore, dir, wArms);
                    Aim(fore, hand, dir, wArms);
                }
            }

            // saltos (barreira, comemoração)
            float lift = 0;
            if (mode == Mode.Jump)
            {
                float j = Mathf.Clamp01(modeTime / .45f);
                lift = Mathf.Sin(j * Mathf.PI) * .38f;
                if (j >= 1f) mode = Mode.Idle;
            }
            else if (mode == Mode.Celebrate) lift = Mathf.Abs(Mathf.Sin(modeTime * 10f)) * .12f;
            var mp = modelRoot.localPosition;
            mp.y = Mathf.Lerp(mp.y, lift, mode == Mode.Jump ? 1 : k);
            modelRoot.localPosition = mp;

            // corpo inclina como um pêndulo: para a frente ao arrancar e correr, para dentro nas curvas,
            // para trás ao frear; de lado, os quadris giram um pouco na direção do passo
            var right2 = transform.right; var fwd2 = transform.forward;
            float aF = Vector3.Dot(accel, fwd2), aR = Vector3.Dot(accel, right2);
            var leanT = locomote && mode != Mode.Stumble
                ? new Vector3(Mathf.Clamp(spd * 1.6f + aF * 1.4f, -10f, 16f), 0, Mathf.Clamp(-aR * 2.2f, -16f, 16f))
                : Vector3.zero;
            leanEuler = Vector3.Lerp(leanEuler, leanT, 1f - Mathf.Exp(-6f * dt));
            float sideMove = spd > .5f ? Vector3.Dot(moveVel / spd, right2) * cycleDir : 0;
            bodyYaw = Mathf.Lerp(bodyYaw, locomote ? Mathf.Clamp(sideMove * (mode == Mode.Ready ? 20f : 55f), -55f, 55f) : 0, 1f - Mathf.Exp(-6f * dt));
            modelRoot.localRotation = Quaternion.Euler(leanEuler.x, bodyYaw, leanEuler.z);

            // cabeça acompanha a bola
            if (LookAt != null && bHead >= 0)
            {
                var to = LookAt.position - bones[bHead].position; to.y = 0;
                var f = modelRoot.forward; f.y = 0;
                float ang = to.sqrMagnitude > .01f && f.sqrMagnitude > .01f ? Mathf.Clamp(Vector3.SignedAngle(f, to, Vector3.up), -60f, 60f) : 0;
                lookYaw = Mathf.Lerp(lookYaw, ang, 1f - Mathf.Exp(-5f * dt));
                if (bNeck >= 0) bones[bNeck].rotation = Quaternion.AngleAxis(lookYaw * .45f, Vector3.up) * bones[bNeck].rotation;
                bones[bHead].rotation = Quaternion.AngleAxis(lookYaw * .45f, Vector3.up) * bones[bHead].rotation;
            }
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
            if (model != null) { UpdateHuman(Time.deltaTime); return; }
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
