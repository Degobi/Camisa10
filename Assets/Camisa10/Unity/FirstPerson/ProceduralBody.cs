using System;
using System.Collections.Generic;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Corpo do jogador modelado em código (sem arquivos de modelo nem pacotes): anatomia feita de formas suaves
    /// (elipsoides e cones arredondados fundidos), rosto com nariz, boca, orelhas e olhos, cabelo, mãos com dedos,
    /// chuteiras e o uniforme com volume (camisa e calção mais largos que o corpo, com bainhas de verdade).
    /// A superfície é extraída por "surface nets" de um campo de distâncias e presa ao esqueleto que já tem as
    /// animações de captura de movimento (pose T do HumanModel). Gerado uma vez e compartilhado por todos.
    /// </summary>
    public static class ProceduralBody
    {
        public enum Tag { Body, Hair, Eyes, Boots }

        // ---------- funções de distância ----------
        static float RoundCone(Vector3 p, Vector3 a, Vector3 b, float r1, float r2)
        {
            Vector3 ba = b - a; float l2 = Vector3.Dot(ba, ba); float rr = r1 - r2; float a2 = l2 - rr * rr; float il2 = 1f / l2;
            Vector3 pa = p - a; float y = Vector3.Dot(pa, ba); float z = y - l2;
            Vector3 xv = pa * l2 - ba * y; float x2 = Vector3.Dot(xv, xv); float y2 = y * y * l2; float z2 = z * z * l2;
            float k = Mathf.Sign(rr) * rr * rr * x2;
            if (Mathf.Sign(z) * a2 * z2 > k) return Mathf.Sqrt(x2 + z2) * il2 - r2;
            if (Mathf.Sign(y) * a2 * y2 < k) return Mathf.Sqrt(x2 + y2) * il2 - r1;
            return (Mathf.Sqrt(x2 * a2 * il2) + y * rr) * il2 - r1;
        }

        static float Ellipsoid(Vector3 p, Vector3 c, Vector3 r)
        {
            var q = p - c;
            float k0 = new Vector3(q.x / r.x, q.y / r.y, q.z / r.z).magnitude;
            float k1 = new Vector3(q.x / (r.x * r.x), q.y / (r.y * r.y), q.z / (r.z * r.z)).magnitude;
            return k1 < 1e-6f ? -Mathf.Min(r.x, Mathf.Min(r.y, r.z)) : k0 * (k0 - 1f) / k1;
        }

        static float Smin(float a, float b, float k)
        {
            float h = Mathf.Max(k - Mathf.Abs(a - b), 0) / k;
            return Mathf.Min(a, b) - h * h * k * .25f;
        }

        static float Smax(float a, float b, float k) => -Smin(-a, -b, k);

        // ---------- formas ----------
        static float RoundBox(Vector3 p, Vector3 c, Vector3 b, float r)
        {
            var q = new Vector3(Mathf.Abs(p.x - c.x) - b.x + r, Mathf.Abs(p.y - c.y) - b.y + r, Mathf.Abs(p.z - c.z) - b.z + r);
            return new Vector3(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0), Mathf.Max(q.z, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0) - r;
        }

        struct Shape
        {
            public bool Box;
            public bool Cone;
            public Vector3 A, B, R; // cone: A→B com raios R.x→R.y; elipsoide: centro A, raios R
            public float K;         // suavidade da fusão com o resto
            public Vector3 BC; public float BR; // esfera que envolve a forma (pula o cálculo quando está longe)
            public float Eval(Vector3 p) => Box ? RoundBox(p, A, R, B.x) : Cone ? RoundCone(p, A, B, R.x, R.y) : Ellipsoid(p, A, R);
            public void Bound()
            {
                if (Box) { BC = A; BR = R.magnitude; }
                else if (Cone) { BC = (A + B) * .5f; BR = (B - A).magnitude * .5f + Mathf.Max(R.x, R.y); }
                else { BC = A; BR = Mathf.Max(R.x, Mathf.Max(R.y, R.z)); }
            }
        }

        static Shape E(Vector3 c, Vector3 r, float k = .035f) { var s = new Shape { A = c, R = r, K = k }; s.Bound(); return s; }
        static Shape Bx(Vector3 c, Vector3 half, float round, float k = .035f) { var s = new Shape { Box = true, A = c, R = half, B = new Vector3(round, 0, 0), K = k }; s.Bound(); return s; }
        static Shape C(Vector3 a, Vector3 b, float r1, float r2, float k = .035f) { var s = new Shape { Cone = true, A = a, B = b, R = new Vector3(r1, r2, 0), K = k }; s.Bound(); return s; }

        /// <summary>União suave de uma lista, pulando formas longe demais para mudar o resultado.</summary>
        static float Union(List<Shape> list, Vector3 p, float d)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                if ((p - s.BC).magnitude - s.BR > d + s.K) continue;
                d = Smin(d, s.Eval(p), s.K);
            }
            return d;
        }

        sealed class Rig
        {
            public Vector3 Hips, Spine2, Neck, Head, LArm, LFore, LHand, LUp, LLeg, LFoot, LToe, LToeEnd;
            public Vector3[] LFingers; // 5 dedos x 4 pontos (polegar primeiro)
            public float Hem, ShortsHem, SockTop, BootTop, NeckBase, SleeveEnd, HandStart;
        }

        static readonly List<Shape> body = new List<Shape>(), head = new List<Shape>(), boots = new List<Shape>(), hair = new List<Shape>(), eyes = new List<Shape>();
        static readonly List<(Vector3 c, float r)> sockets = new List<(Vector3, float)>();
        static Rig rg;
        static Vector3 hairC, hairR;
        static float eyeZ, browZ;

        static Vector3 M(Vector3 v) => new Vector3(-v.x, v.y, v.z); // espelha para o lado direito

        static void Both(List<Shape> list, Shape s)
        {
            list.Add(s);
            var m = s; m.A = M(s.A); m.B = M(s.B); m.Bound();
            list.Add(m);
        }

        static void Define(Rig r)
        {
            body.Clear(); head.Clear(); boots.Clear(); hair.Clear(); eyes.Clear(); sockets.Clear();
            float hy = r.Hips.y, sy = r.Spine2.y;
            // tronco: pelve, abdômen, caixa torácica, dorsais, peitoral, glúteos
            body.Add(E(new Vector3(0, hy - .06f, .0f), new Vector3(.14f, .1f, .104f)));
            body.Add(E(new Vector3(0, hy + .1f, .004f), new Vector3(.118f, .12f, .086f)));
            body.Add(Bx(new Vector3(0, sy - .015f, -.004f), new Vector3(.148f, .125f, .086f), .065f, .04f)); // caixa torácica: frente reta
            body.Add(E(new Vector3(0, sy + .01f, -.03f), new Vector3(.172f, .11f, .08f)));
            Both(body, E(new Vector3(-.064f, hy - .095f, -.048f), new Vector3(.072f, .08f, .062f), .03f));
            // pescoço e trapézio
            body.Add(C(new Vector3(0, r.Neck.y - .07f, -.02f), r.Head + new Vector3(0, .015f, -.008f), .061f, .054f, .03f));
            Both(body, C(new Vector3(-.01f, r.Neck.y - .045f, -.035f), r.LArm + new Vector3(.03f, .004f, 0), .044f, .036f, .04f));
            // braço: deltoide, bíceps, tríceps, antebraço
            Both(body, E(r.LArm + new Vector3(-.012f, .0f, 0), new Vector3(.06f, .06f, .06f), .03f));
            Both(body, C(r.LArm, r.LFore, .047f, .039f, .03f));
            Both(body, E(Vector3.Lerp(r.LArm, r.LFore, .45f) + new Vector3(0, -.006f, .017f), new Vector3(.072f, .037f, .035f), .02f));
            Both(body, E(Vector3.Lerp(r.LArm, r.LFore, .4f) + new Vector3(0, .006f, -.02f), new Vector3(.078f, .039f, .036f), .02f));
            Both(body, C(r.LFore, r.LHand, .04f, .027f, .025f));
            Both(body, E(Vector3.Lerp(r.LFore, r.LHand, .25f) + new Vector3(0, .004f, 0), new Vector3(.085f, .041f, .039f), .02f));
            // mão: palma e dedos
            var dirH = (r.LHand - r.LFore).normalized;
            Both(body, E(r.LHand + dirH * .045f + new Vector3(0, -.004f, -.004f), new Vector3(.048f, .016f, .042f), .015f));
            for (int f = 0; f < 5; f++)
            {
                float r1 = f == 0 ? .0115f : .0092f, r2 = f == 0 ? .0085f : .0068f;
                for (int s = 0; s < 3; s++)
                    Both(body, C(r.LFingers[f * 4 + s], r.LFingers[f * 4 + s + 1], Mathf.Lerp(r1, r2, s / 3f), Mathf.Lerp(r1, r2, (s + 1) / 3f), .006f));
            }
            // perna: coxa, quadríceps, posterior, joelho, canela, panturrilha, tornozelo
            Both(body, C(r.LUp + new Vector3(0, .02f, 0), r.LLeg, .087f, .055f, .04f));
            Both(body, E(Vector3.Lerp(r.LUp, r.LLeg, .42f) + new Vector3(.0f, 0, .033f), new Vector3(.07f, .17f, .056f), .03f));
            Both(body, E(Vector3.Lerp(r.LUp, r.LLeg, .36f) + new Vector3(0, 0, -.03f), new Vector3(.068f, .165f, .056f), .03f));
            Both(body, E(Vector3.Lerp(r.LUp, r.LLeg, .25f) + new Vector3(.02f, 0, 0), new Vector3(.058f, .12f, .058f), .03f));
            Both(body, E(r.LLeg + new Vector3(0, 0, .012f), new Vector3(.05f, .05f, .05f), .03f));
            Both(body, C(r.LLeg, r.LFoot + new Vector3(0, .03f, 0), .05f, .033f, .03f));
            Both(body, E(Vector3.Lerp(r.LLeg, r.LFoot, .27f) + new Vector3(0, 0, -.033f), new Vector3(.054f, .12f, .05f), .03f));
            Both(body, E(r.LFoot, new Vector3(.036f, .036f, .036f), .02f));

            // chuteira: corpo achatado do calcanhar ao bico, cano curto, sola reta
            var heel = r.LFoot + new Vector3(0, -.04f, -.07f); var toe = r.LToeEnd + new Vector3(0, .02f, -.005f);
            Both(boots, E(Vector3.Lerp(heel, toe, .5f) + new Vector3(0, .028f, 0), new Vector3(.05f, .045f, (toe.z - heel.z) * .52f), .02f));
            Both(boots, C(r.LFoot + new Vector3(0, -.01f, -.012f), r.LFoot + new Vector3(0, .045f, -.012f), .043f, .039f, .02f));

            // cabeça
            var h = r.Head;
            head.Add(E(h + new Vector3(0, .105f, -.005f), new Vector3(.078f, .093f, .097f), .02f));
            head.Add(E(h + new Vector3(0, .03f, .03f), new Vector3(.069f, .068f, .072f), .03f));
            Both(head, E(h + new Vector3(-.042f, .012f, .045f), new Vector3(.026f, .022f, .03f), .02f)); // ângulo da mandíbula
            head.Add(E(h + new Vector3(0, -.014f, .062f), new Vector3(.032f, .022f, .022f), .02f));
            Both(head, E(h + new Vector3(-.043f, .058f, .062f), new Vector3(.024f, .019f, .018f), .015f));
            head.Add(C(h + new Vector3(0, .078f, .088f), h + new Vector3(0, .044f, .106f), .009f, .015f, .012f));
            head.Add(E(h + new Vector3(0, .038f, .094f), new Vector3(.017f, .008f, .011f), .008f));
            head.Add(E(h + new Vector3(0, .094f, .078f), new Vector3(.058f, .011f, .014f), .015f));
            head.Add(E(h + new Vector3(0, .013f, .083f), new Vector3(.022f, .0075f, .01f), .006f));
            Both(head, E(h + new Vector3(-.079f, .066f, -.006f), new Vector3(.011f, .027f, .017f), .006f));
            sockets.Add((h + new Vector3(-.031f, .068f, .091f), .015f)); sockets.Add((M(h + new Vector3(-.031f, .068f, .091f)), .015f));
            // olhos e sobrancelhas apoiados na superfície do rosto (medida aqui, para não afundar nem flutuar)
            float SurfaceZ(float x, float y)
            {
                for (float z = h.z + .2f; z > h.z - .05f; z -= .001f)
                {
                    float d = 10f;
                    foreach (var sh in head) d = Smin(d, sh.Eval(new Vector3(x, y, z)), sh.K);
                    if (d < 0) return z;
                }
                return h.z + .09f;
            }
            float ez = SurfaceZ(h.x - .031f, h.y + .067f);
            // olhos e sobrancelhas viram peças pequenas em alta resolução (a grade é grossa demais para eles)
            eyeZ = ez;
            browZ = SurfaceZ(h.x - .031f, h.y + .089f);
            // cabelo curto e sobrancelhas
            hairC = h + new Vector3(0, .107f, -.008f); hairR = new Vector3(.09f, .105f, .108f);

        }

        /// <summary>Distância ao corpo (negativa dentro) e qual parte domina ali.</summary>
        static float Field(Vector3 p, out Tag tag)
        {
            float d = Union(body, p, 10f);
            // roupa com volume: camisa e calção mais largos que o corpo (com degrau na bainha)
            d -= Inflate(p);
            float hd = Union(head, p, 10f);
            d = Smin(d, hd, .025f);
            tag = Tag.Body;
            float bd = Union(boots, p, 10f);
            bd = Mathf.Max(bd, -p.y - .002f); // sola reta no chão
            if (bd < d) { d = bd; tag = Tag.Boots; }
            float ed = 10f;
            foreach (var s in eyes) ed = Mathf.Min(ed, s.Eval(p));
            if (ed < d) { d = ed; tag = Tag.Eyes; }
            // cabelo: casca por cima do crânio, só acima da linha do cabelo (testa alta na frente, nuca atrás)
            float hr = Ellipsoid(p, hairC, hairR);
            float front = Mathf.Clamp01((p.z - rg.Head.z + .03f) / .11f);
            float line = rg.Head.y + Mathf.Lerp(.035f, .128f, front) + Mathf.Max(0, Mathf.Abs(p.x) - .06f) * -.4f;
            hr = Mathf.Max(hr, line - p.y);
            hr -= .0025f * (Mathf.Sin(p.x * 220f + p.z * 60f) * Mathf.Sin(p.z * 180f - p.y * 90f)); // relevo de fios
            foreach (var s in hair) hr = Mathf.Min(hr, s.Eval(p));
            if (hr < d) { d = hr; tag = Tag.Hair; }
            return d;
        }

        static float Ss(float e0, float e1, float x) { float t = Mathf.Clamp01((x - e0) / (e1 - e0)); return t * t * (3 - 2 * t); }

        /// <summary>
        /// Quanto a roupa afasta a superfície do corpo em cada ponto. A transição é suave (uns 8 mm), então a bainha
        /// vira uma borda arredondada em vez de um degrau serrilhado.
        /// </summary>
        static float Inflate(Vector3 p)
        {
            float ax = Mathf.Abs(p.x), y = p.y, w = .004f;
            if (y > 1.3f && ax > .2f)
                return .007f * (1 - Ss(rg.SleeveEnd - w, rg.SleeveEnd + w, ax)); // manga um pouco folgada até a barra
            float neck = Ss(rg.NeckBase - w, rg.NeckBase + w, y) * (1 - Ss(.075f, .095f, ax)); // pescoço sem camisa
            float shirt = Ss(rg.Hem - w, rg.Hem + w, y) * (1 - neck);
            // perto da gola a camisa encosta no corpo (sem "ombreira")
            shirt *= 1f - .8f * Ss(rg.NeckBase - .07f, rg.NeckBase, y) * (1f - Ss(.1f, .17f, ax));
            float shorts = Ss(rg.ShortsHem - w, rg.ShortsHem + w, y) * (1 - Ss(rg.Hem - w, rg.Hem + w, y));
            float socks = Ss(rg.BootTop - w, rg.BootTop + w, y) * (1 - Ss(rg.SockTop - w, rg.SockTop + w, y));
            return shirt * (.008f + Mathf.Clamp01((rg.Hem + .1f - y) / .1f) * .004f)
                 + shorts * (.008f + Mathf.Clamp01((rg.ShortsHem + .12f - y) / .12f) * .003f)
                 + socks * .004f;
        }

        /// <summary>Qual peça do uniforme (ou pele) cobre o ponto, pela anatomia da pose T.</summary>
        static HumanModel.Part ZoneOf(Vector3 p)
        {
            float ax = Mathf.Abs(p.x);
            if (p.y > 1.3f && ax > .2f)
            {
                if (ax < rg.SleeveEnd) return HumanModel.Part.Shirt;
                if (ax < rg.HandStart) return HumanModel.Part.Forearms;
                return HumanModel.Part.Hands;
            }
            if (p.y > rg.NeckBase && ax < .085f) return HumanModel.Part.Skin;
            if (p.y > rg.Hem) return p.y > rg.Neck.y + .07f ? HumanModel.Part.Skin : HumanModel.Part.Shirt;
            if (p.y > rg.ShortsHem) return HumanModel.Part.Shorts;
            if (p.y > rg.SockTop) return HumanModel.Part.Skin;
            if (p.y > rg.BootTop) return HumanModel.Part.Socks;
            return HumanModel.Part.Boots;
        }

        // ---------- pesos de pele ----------
        struct Seg { public int Bone, Parent; public Vector3 A, B; public float R; }

        static List<Seg> Segments(HumanModel h, Rig r)
        {
            int B(string n) => h.Bone(n);
            var s = new List<Seg>();
            void Add(string bone, string parent, Vector3 a, Vector3 b, float rad)
            {
                int bi = B(bone);
                if (bi < 0) return;
                s.Add(new Seg { Bone = bi, Parent = parent != null ? B(parent) : -1, A = a, B = b, R = rad });
            }
            Add("Hips", null, new Vector3(0, r.Hips.y - .14f, 0), new Vector3(0, r.Hips.y + .05f, 0), .17f);
            Add("Spine", "Hips", new Vector3(0, r.Hips.y + .05f, 0), new Vector3(0, r.Hips.y + .15f, 0), .15f);
            Add("Spine1", "Spine", new Vector3(0, r.Hips.y + .15f, 0), new Vector3(0, r.Spine2.y - .04f, 0), .16f);
            Add("Spine2", "Spine1", new Vector3(0, r.Spine2.y - .04f, 0), new Vector3(0, r.Neck.y - .04f, 0), .18f);
            Add("Neck", "Spine2", new Vector3(0, r.Neck.y - .04f, -.02f), new Vector3(0, r.Head.y, -.01f), .06f);
            Add("Head", "Neck", r.Head, r.Head + new Vector3(0, .2f, 0), .11f);
            foreach (var sd in new[] { "Left", "Right" })
            {
                float sx = sd == "Left" ? 1 : -1;
                Vector3 F(Vector3 v) => new Vector3(v.x * sx, v.y, v.z);
                Add(sd + "Shoulder", "Spine2", F(new Vector3(-.04f, r.LArm.y + .005f, -.03f)), F(r.LArm), .07f);
                Add(sd + "Arm", sd + "Shoulder", F(r.LArm), F(r.LFore), .052f);
                Add(sd + "ForeArm", sd + "Arm", F(r.LFore), F(r.LHand), .042f);
                Add(sd + "Hand", sd + "ForeArm", F(r.LHand), F(r.LHand + (r.LHand - r.LFore).normalized * .19f), .05f);
                Add(sd + "UpLeg", "Hips", F(r.LUp + new Vector3(0, .03f, 0)), F(r.LLeg), .09f);
                Add(sd + "Leg", sd + "UpLeg", F(r.LLeg), F(r.LFoot), .055f);
                Add(sd + "Foot", sd + "Leg", F(r.LFoot), F(r.LToe), .05f);
                Add(sd + "ToeBase", sd + "Foot", F(r.LToe), F(r.LToeEnd), .045f);
            }
            return s;
        }

        static float SegParam(Vector3 p, Seg s, out float dist)
        {
            var ab = s.B - s.A;
            float t = Mathf.Clamp01(Vector3.Dot(p - s.A, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            dist = Vector3.Distance(p, s.A + ab * t);
            return t;
        }

        static BoneWeight Weigh(Vector3 p, List<Seg> segs, Tag tag, int headBone)
        {
            if ((tag == Tag.Hair || tag == Tag.Eyes) && headBone >= 0) return new BoneWeight { boneIndex0 = headBone, weight0 = 1 };
            int best = 0; float bestD = float.MaxValue, bestT = 0;
            for (int i = 0; i < segs.Count; i++)
            {
                float t = SegParam(p, segs[i], out float d);
                float nd = d / segs[i].R;
                if (nd < bestD) { bestD = nd; best = i; bestT = t; }
            }
            var s = segs[best];
            // perto da junta mistura com o osso pai (início do segmento) ou com o filho (fim)
            int other = -1; float w = 0;
            if (bestT < .22f && s.Parent >= 0) { other = s.Parent; w = .5f * (1 - bestT / .22f); }
            else if (bestT > .78f)
            {
                for (int i = 0; i < segs.Count; i++)
                    if (segs[i].Parent == s.Bone)
                    {
                        float tt = SegParam(p, segs[i], out float dd);
                        if (dd / segs[i].R < 2.2f) { other = segs[i].Bone; w = .5f * ((bestT - .78f) / .22f); break; }
                    }
            }
            if (other < 0 || w < .01f) return new BoneWeight { boneIndex0 = s.Bone, weight0 = 1 };
            return new BoneWeight { boneIndex0 = s.Bone, weight0 = 1 - w, boneIndex1 = other, weight1 = w };
        }

        // ---------- extração da superfície (surface nets) ----------
        static Mesh cached;

        /// <summary>Gera (uma vez) a malha do corpo presa ao esqueleto do HumanModel. Nulo se o esqueleto não servir.</summary>
        public static Mesh Build(HumanModel h, out Matrix4x4[] bindposes, bool bindOnly = false, bool fresh = false)
        {
            int n = h.Names.Length;
            var G = new Matrix4x4[n];
            for (int i = 0; i < n; i++)
            {
                var l = Matrix4x4.TRS(h.RestPos[i], h.RestRot[i], h.RestScale[i]);
                G[i] = h.Parent[i] >= 0 ? G[h.Parent[i]] * l : l;
            }
            bindposes = new Matrix4x4[n];
            for (int i = 0; i < n; i++) bindposes[i] = G[i].inverse;
            if (bindOnly) return null;
            if (cached != null && !fresh) return cached;

            Vector3 J(string name) { int b = h.Bone(name); return b >= 0 ? (Vector3)G[b].GetColumn(3) : Vector3.zero; }
            foreach (var need in new[] { "Hips", "Spine2", "Neck", "Head", "LeftArm", "LeftForeArm", "LeftHand", "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase", "LeftToe_End" })
                if (h.Bone(need) < 0) return null;
            var r = new Rig
            {
                Hips = J("Hips"), Spine2 = J("Spine2"), Neck = J("Neck"), Head = J("Head"),
                LArm = J("LeftArm"), LFore = J("LeftForeArm"), LHand = J("LeftHand"),
                LUp = J("LeftUpLeg"), LLeg = J("LeftLeg"), LFoot = J("LeftFoot"), LToe = J("LeftToeBase"), LToeEnd = J("LeftToe_End"),
            };
            // o modelo original tem o lado esquerdo em x negativo: as formas são definidas nele e espelhadas
            r.LFingers = new Vector3[20];
            string[] fingers = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
            for (int f = 0; f < 5; f++)
                for (int k = 0; k < 4; k++)
                {
                    var pnt = J("LeftHand" + fingers[f] + (k + 1));
                    r.LFingers[f * 4 + k] = pnt == Vector3.zero ? r.LHand + (r.LHand - r.LFore).normalized * (.09f + k * .03f) : pnt;
                }
            r.Hem = r.Hips.y - .105f; r.ShortsHem = r.Hips.y - .38f; r.SockTop = r.LLeg.y - .06f; r.BootTop = r.LFoot.y + .03f;
            r.NeckBase = r.Neck.y - .028f; r.SleeveEnd = Mathf.Abs(r.LArm.x) + .18f; r.HandStart = Mathf.Abs(r.LHand.x) - .008f;
            rg = r;
            Define(r);

            var t0 = Time.realtimeSinceStartup;
            const float H = .011f;
            var lo = new Vector3(Mathf.Min(r.LFingers[12].x, r.LHand.x) - .06f, -.012f, -.18f);
            var hi = new Vector3(-lo.x, r.Head.y + .23f, .2f);
            int nx = Mathf.CeilToInt((hi.x - lo.x) / H) + 1, ny = Mathf.CeilToInt((hi.y - lo.y) / H) + 1, nz = Mathf.CeilToInt((hi.z - lo.z) / H) + 1;
            var val = new float[nx * ny * nz];
            var tagAt = new byte[nx * ny * nz];
            int Id(int x, int y, int z) => (z * ny + y) * nx + x;
            Vector3 Pt(int x, int y, int z) => lo + new Vector3(x * H, y * H, z * H);

            // avaliação esparsa: grade grossa primeiro; a fina só perto da superfície
            const int S = 4;
            int cx = (nx - 1) / S + 2, cy = (ny - 1) / S + 2, cz = (nz - 1) / S + 2;
            var coarse = new float[cx * cy * cz];
            for (int z = 0; z < cz; z++) for (int y = 0; y < cy; y++) for (int x = 0; x < cx; x++)
                coarse[(z * cy + y) * cx + x] = Field(Pt(x * S, y * S, z * S), out _);
            for (int i = 0; i < val.Length; i++) val[i] = float.NaN;
            float near = H * S * 1.9f;
            for (int z = 0; z < cz - 1; z++) for (int y = 0; y < cy - 1; y++) for (int x = 0; x < cx - 1; x++)
            {
                float mn = float.MaxValue, sgn = 0;
                for (int c = 0; c < 8; c++)
                {
                    float v = coarse[((z + (c >> 2 & 1)) * cy + y + (c >> 1 & 1)) * cx + x + (c & 1)];
                    mn = Mathf.Min(mn, Mathf.Abs(v)); sgn += Mathf.Sign(v);
                }
                bool exact = mn < near;
                for (int k = 0; k <= S; k++) for (int j = 0; j <= S; j++) for (int i = 0; i <= S; i++)
                {
                    int gx = x * S + i, gy = y * S + j, gz = z * S + k;
                    if (gx >= nx || gy >= ny || gz >= nz) continue;
                    int id = Id(gx, gy, gz);
                    if (!float.IsNaN(val[id]) && !exact) continue;
                    if (exact) { val[id] = Field(Pt(gx, gy, gz), out var tg); tagAt[id] = (byte)tg; }
                    else if (float.IsNaN(val[id])) val[id] = sgn >= 0 ? near : -near;
                }
            }

            // um vértice por célula que cruza a superfície (média dos cruzamentos nas arestas)
            var cellVert = new int[(nx - 1) * (ny - 1) * (nz - 1)];
            for (int i = 0; i < cellVert.Length; i++) cellVert[i] = -1;
            var verts = new List<Vector3>();
            int Cell(int x, int y, int z) => (z * (ny - 1) + y) * (nx - 1) + x;
            int[,] edges = { { 0, 1 }, { 2, 3 }, { 4, 5 }, { 6, 7 }, { 0, 2 }, { 1, 3 }, { 4, 6 }, { 5, 7 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };
            var cv = new float[8]; var cp = new Vector3[8];
            for (int z = 0; z < nz - 1; z++) for (int y = 0; y < ny - 1; y++) for (int x = 0; x < nx - 1; x++)
            {
                int inside = 0;
                for (int c = 0; c < 8; c++)
                {
                    int ix = x + (c & 1), iy = y + (c >> 1 & 1), iz = z + (c >> 2 & 1);
                    cv[c] = val[Id(ix, iy, iz)]; cp[c] = Pt(ix, iy, iz);
                    if (cv[c] < 0) inside++;
                }
                if (inside == 0 || inside == 8) continue;
                var sum = Vector3.zero; int cnt = 0;
                for (int e = 0; e < 12; e++)
                {
                    float a = cv[edges[e, 0]], b = cv[edges[e, 1]];
                    if ((a < 0) == (b < 0)) continue;
                    sum += Vector3.Lerp(cp[edges[e, 0]], cp[edges[e, 1]], a / (a - b)); cnt++;
                }
                cellVert[Cell(x, y, z)] = verts.Count;
                verts.Add(sum / cnt);
            }

            // uma face (2 triângulos) por aresta da grade que cruza a superfície
            var quads = new List<int>();
            for (int z = 1; z < nz - 1; z++) for (int y = 1; y < ny - 1; y++) for (int x = 1; x < nx - 1; x++)
            {
                float v0 = val[Id(x, y, z)];
                for (int axis = 0; axis < 3; axis++)
                {
                    int x1 = x + (axis == 0 ? 1 : 0), y1 = y + (axis == 1 ? 1 : 0), z1 = z + (axis == 2 ? 1 : 0);
                    if (x1 >= nx || y1 >= ny || z1 >= nz) continue;
                    float v1 = val[Id(x1, y1, z1)];
                    if ((v0 < 0) == (v1 < 0)) continue;
                    int a, b, c, d;
                    if (axis == 0) { a = Cell(x, y - 1, z - 1); b = Cell(x, y, z - 1); c = Cell(x, y, z); d = Cell(x, y - 1, z); }
                    else if (axis == 1) { a = Cell(x - 1, y, z - 1); b = Cell(x - 1, y, z); c = Cell(x, y, z); d = Cell(x, y, z - 1); }
                    else { a = Cell(x - 1, y - 1, z); b = Cell(x, y - 1, z); c = Cell(x, y, z); d = Cell(x - 1, y, z); }
                    int ia = cellVert[a], ib = cellVert[b], ic = cellVert[c], id = cellVert[d];
                    if (ia < 0 || ib < 0 || ic < 0 || id < 0) continue;
                    bool flip = v0 < 0;
                    if (axis == 1) flip = !flip;
                    if (flip) { quads.Add(ia); quads.Add(id); quads.Add(ic); quads.Add(ib); }
                    else { quads.Add(ia); quads.Add(ib); quads.Add(ic); quads.Add(id); }
                }
            }

            // suavização Taubin (não encolhe): tira o aspecto facetado da grade
            {
                var nb = new List<int>[verts.Count];
                for (int i = 0; i < nb.Length; i++) nb[i] = new List<int>(6);
                for (int q = 0; q < quads.Count; q += 4)
                    for (int k = 0; k < 4; k++)
                    {
                        int a = quads[q + k], b = quads[q + (k + 1) % 4];
                        if (!nb[a].Contains(b)) nb[a].Add(b);
                        if (!nb[b].Contains(a)) nb[b].Add(a);
                    }
                var tmp = new Vector3[verts.Count];
                for (int it = 0; it < 6; it++)
                {
                    float lambda = it % 2 == 0 ? .5f : -.53f;
                    for (int i = 0; i < verts.Count; i++)
                    {
                        if (nb[i].Count == 0) { tmp[i] = verts[i]; continue; }
                        var avg = Vector3.zero;
                        foreach (int j in nb[i]) avg += verts[j];
                        avg /= nb[i].Count;
                        tmp[i] = verts[i] + (avg - verts[i]) * lambda;
                    }
                    for (int i = 0; i < verts.Count; i++) verts[i] = tmp[i];
                }
            }

            // vértices perto das divisas vão para a linha exata: bainha, gola, barra da manga e do meião ficam retas
            for (int i = 0; i < verts.Count; i++)
            {
                var p = verts[i]; float ax = Mathf.Abs(p.x);
                bool arm = p.y > 1.3f && ax > .2f;
                if (arm)
                {
                    if (Mathf.Abs(ax - r.SleeveEnd) < H * .55f) p.x = Mathf.Sign(p.x) * r.SleeveEnd;
                    else if (Mathf.Abs(ax - r.HandStart) < H * .55f) p.x = Mathf.Sign(p.x) * r.HandStart;
                }
                else
                {
                    foreach (var line in new[] { r.Hem, r.ShortsHem, r.SockTop, r.BootTop })
                        if (Mathf.Abs(p.y - line) < H * .55f) { p.y = line; break; }
                    if (ax < .1f && Mathf.Abs(p.y - r.NeckBase) < H * .55f) p.y = r.NeckBase;
                }
                verts[i] = p;
            }

            // normais pelo gradiente do campo, peça e osso de cada vértice
            var normals = new Vector3[verts.Count];
            var vtag = new Tag[verts.Count];
            float eps = H * .5f;
            for (int i = 0; i < verts.Count; i++)
            {
                var p = verts[i];
                Field(p, out vtag[i]);
                var g = new Vector3(
                    Field(p + new Vector3(eps, 0, 0), out _) - Field(p - new Vector3(eps, 0, 0), out _),
                    Field(p + new Vector3(0, eps, 0), out _) - Field(p - new Vector3(0, eps, 0), out _),
                    Field(p + new Vector3(0, 0, eps), out _) - Field(p - new Vector3(0, 0, eps), out _));
                normals[i] = g.sqrMagnitude > 1e-12f ? g.normalized : Vector3.up;
            }
            var segs = Segments(h, r);
            int headBone = h.Bone("Head");

            // separa por peça (vértices duplicados na divisa: cada peça tem seu próprio mapa de textura)
            int parts = 10;
            var outV = new List<Vector3>(); var outN = new List<Vector3>(); var outUV = new List<Vector2>(); var outW = new List<BoneWeight>();
            var tris = new List<int>[parts];
            for (int i = 0; i < parts; i++) tris[i] = new List<int>();
            var remap = new Dictionary<long, int>();
            float cz0 = r.Spine2.z;
            int V(int src, int part)
            {
                long key = (long)src * 16 + part;
                if (remap.TryGetValue(key, out int id)) return id;
                id = outV.Count;
                var p = verts[src];
                outV.Add(p); outN.Add(normals[src]);
                outUV.Add(KitUV(p, (HumanModel.Part)part, cz0));
                outW.Add(Weigh(p, segs, vtag[src], headBone));
                remap[key] = id;
                return id;
            }
            for (int q = 0; q < quads.Count; q += 4)
            {
                int a = quads[q], b = quads[q + 1], c = quads[q + 2], d = quads[q + 3];
                // divide pela diagonal mais curta (superfície mais lisa)
                bool diag = (verts[a] - verts[c]).sqrMagnitude <= (verts[b] - verts[d]).sqrMagnitude;
                int[] t = diag ? new[] { a, b, c, a, c, d } : new[] { a, b, d, b, c, d };
                for (int k = 0; k < 6; k += 3)
                {
                    // face sempre virada para fora (confere com a normal do campo)
                    var fn = Vector3.Cross(verts[t[k + 1]] - verts[t[k]], verts[t[k + 2]] - verts[t[k]]);
                    if (Vector3.Dot(fn, normals[t[k]] + normals[t[k + 1]] + normals[t[k + 2]]) < 0) { int tmp = t[k + 1]; t[k + 1] = t[k + 2]; t[k + 2] = tmp; }
                    var cen = (verts[t[k]] + verts[t[k + 1]] + verts[t[k + 2]]) / 3f;
                    int part = (int)PartOf(cen);
                    tris[part].Add(V(t[k], part)); tris[part].Add(V(t[k + 1], part)); tris[part].Add(V(t[k + 2], part));
                }
            }

            // olho (branco + íris) e sobrancelha: elipsoides finos apoiados no rosto, presos à cabeça
            void Blob(Vector3 c, Vector3 rad, int part, float tilt)
            {
                const int Seg = 14, Rings = 8;
                int start = outV.Count;
                var rot = Quaternion.Euler(0, 0, tilt);
                for (int j = 0; j <= Rings; j++)
                {
                    float th = j / (float)Rings * Mathf.PI;
                    for (int i = 0; i <= Seg; i++)
                    {
                        float ph = i / (float)Seg * Mathf.PI * 2;
                        var u = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                        outV.Add(c + rot * Vector3.Scale(u, rad));
                        outN.Add((rot * new Vector3(u.x / rad.x, u.y / rad.y, u.z / rad.z)).normalized);
                        outUV.Add(new Vector2(.5f, .5f));
                        outW.Add(new BoneWeight { boneIndex0 = Mathf.Max(0, headBone), weight0 = 1 });
                    }
                }
                for (int j = 0; j < Rings; j++)
                    for (int i = 0; i < Seg; i++)
                    {
                        int a = start + j * (Seg + 1) + i, b = a + Seg + 1;
                        tris[part].Add(a); tris[part].Add(a + 1); tris[part].Add(b);
                        tris[part].Add(a + 1); tris[part].Add(b + 1); tris[part].Add(b);
                    }
            }
            foreach (float sx in new[] { -1f, 1f })
            {
                var eye = new Vector3(r.Head.x + sx * .031f, r.Head.y + .067f, eyeZ - .0012f);
                Blob(eye, new Vector3(.0125f, .0056f, .0038f), (int)HumanModel.Part.Sclera, 0);
                Blob(eye + new Vector3(0, 0, .0021f), new Vector3(.0056f, .0056f, .0024f), (int)HumanModel.Part.Eyes, 0);
                Blob(new Vector3(r.Head.x + sx * .031f, r.Head.y + .089f, browZ + .0006f), new Vector3(.0185f, .0033f, .0035f), (int)HumanModel.Part.Hair, -sx * 7f);
            }

            var mesh = new Mesh { name = "CorpoProcedural", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, hideFlags = HideFlags.DontUnloadUnusedAsset };
            mesh.SetVertices(outV); mesh.SetNormals(outN); mesh.SetUVs(0, outUV);
            mesh.boneWeights = outW.ToArray();
            mesh.bindposes = bindposes;
            mesh.subMeshCount = parts;
            for (int i = 0; i < parts; i++) mesh.SetTriangles(tris[i], i, false);
            mesh.RecalculateBounds();
            Debug.Log($"[Camisa 10] Corpo gerado por código: {outV.Count} vértices em {(Time.realtimeSinceStartup - t0) * 1000:0} ms.");
            cached = mesh;
            return mesh;
        }

        // ---------- arquivo pré-gerado (Resources/Modelos/corpo.bytes) ----------
        /// <summary>Suba quando mudar a modelagem: o editor gera o arquivo de novo e o jogo ignora o antigo.</summary>
        public const int ModelVersion = 1;

        /// <summary>Salva a malha gerada para o jogo só carregar (gerar leva alguns segundos no celular).</summary>
        public static byte[] Serialize(Mesh m, int boneCount)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var w = new System.IO.BinaryWriter(ms))
            {
                w.Write(System.Text.Encoding.ASCII.GetBytes("C10B")); w.Write(ModelVersion); w.Write(boneCount);
                var v = m.vertices; var n = m.normals; var uv = m.uv; var bw = m.boneWeights;
                w.Write(v.Length);
                foreach (var x in v) { w.Write(x.x); w.Write(x.y); w.Write(x.z); }
                foreach (var x in n) { w.Write(x.x); w.Write(x.y); w.Write(x.z); }
                foreach (var x in uv) { w.Write(x.x); w.Write(x.y); }
                foreach (var b in bw)
                {
                    w.Write((short)b.boneIndex0); w.Write((short)b.boneIndex1); w.Write((short)b.boneIndex2); w.Write((short)b.boneIndex3);
                    w.Write(b.weight0); w.Write(b.weight1); w.Write(b.weight2); w.Write(b.weight3);
                }
                w.Write(m.subMeshCount);
                for (int i = 0; i < m.subMeshCount; i++) { var t = m.GetTriangles(i); w.Write(t.Length); foreach (int k in t) w.Write(k); }
                return ms.ToArray();
            }
        }

        /// <summary>Carrega o corpo pré-gerado; nulo se não existir ou for de outra versão/esqueleto.</summary>
        public static Mesh Load(HumanModel h)
        {
            var ta = Resources.Load<TextAsset>("Modelos/corpo");
            if (ta == null) return null;
            using (var r = new System.IO.BinaryReader(new System.IO.MemoryStream(ta.bytes)))
            {
                if (System.Text.Encoding.ASCII.GetString(r.ReadBytes(4)) != "C10B" || r.ReadInt32() != ModelVersion || r.ReadInt32() != h.Names.Length) return null;
                int nv = r.ReadInt32();
                var v = new Vector3[nv]; var n = new Vector3[nv]; var uv = new Vector2[nv]; var bw = new BoneWeight[nv];
                for (int i = 0; i < nv; i++) v[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                for (int i = 0; i < nv; i++) n[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                for (int i = 0; i < nv; i++) uv[i] = new Vector2(r.ReadSingle(), r.ReadSingle());
                for (int i = 0; i < nv; i++)
                    bw[i] = new BoneWeight
                    {
                        boneIndex0 = r.ReadInt16(), boneIndex1 = r.ReadInt16(), boneIndex2 = r.ReadInt16(), boneIndex3 = r.ReadInt16(),
                        weight0 = r.ReadSingle(), weight1 = r.ReadSingle(), weight2 = r.ReadSingle(), weight3 = r.ReadSingle(),
                    };
                var mesh = new Mesh { name = "CorpoProcedural", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, hideFlags = HideFlags.DontUnloadUnusedAsset };
                mesh.vertices = v; mesh.normals = n; mesh.uv = uv; mesh.boneWeights = bw;
                Build(h, out var bind, bindOnly: true);
                mesh.bindposes = bind;
                int parts = r.ReadInt32();
                mesh.subMeshCount = parts;
                for (int i = 0; i < parts; i++) { int c = r.ReadInt32(); var t = new int[c]; for (int k = 0; k < c; k++) t[k] = r.ReadInt32(); mesh.SetTriangles(t, i, false); }
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        static HumanModel.Part PartOf(Vector3 p)
        {
            Field(p, out var tag);
            if (tag == Tag.Hair) return HumanModel.Part.Hair;
            if (tag == Tag.Eyes) return HumanModel.Part.Eyes;
            if (tag == Tag.Boots) return HumanModel.Part.Boots;
            var z = ZoneOf(p);
            return z == HumanModel.Part.Boots ? HumanModel.Part.Socks : z;
        }

        /// <summary>Coordenadas de textura do uniforme (mesma convenção do KitArt).</summary>
        static Vector2 KitUV(Vector3 p, HumanModel.Part part, float cz)
        {
            float Around(Vector3 q) => Mathf.Repeat(-Mathf.Atan2(q.x, q.z - cz) / (2 * Mathf.PI) + .25f, 1f);
            float ax = Mathf.Abs(p.x);
            switch (part)
            {
                case HumanModel.Part.Shirt:
                    if (ax > .2f && p.y > 1.3f)
                    {
                        float around = Mathf.Repeat(Mathf.Atan2(p.z - rg.LArm.z, p.y - rg.LArm.y) / (2 * Mathf.PI), 1f);
                        return new Vector2(Mathf.InverseLerp(.17f, rg.SleeveEnd, ax), Mathf.Lerp(HumanModel.SleeveV0, 1f, around));
                    }
                {
                    // a gola (faixa de cima da textura) só aparece em volta do pescoço; ombros ficam na cor da camisa
                    float tv = Mathf.InverseLerp(rg.Hem, rg.NeckBase, p.y);
                    if (ax > .1f) tv = Mathf.Min(tv, .9f);
                    return new Vector2(Around(p), tv * HumanModel.TorsoV);
                }
                case HumanModel.Part.Forearms:
                {
                    float around = Mathf.Repeat(Mathf.Atan2(p.z - rg.LArm.z, p.y - rg.LArm.y) / (2 * Mathf.PI), 1f);
                    return new Vector2(Mathf.Lerp(.2f, .95f, Mathf.InverseLerp(rg.SleeveEnd, rg.HandStart, ax)), Mathf.Lerp(HumanModel.SleeveV0, 1f, around));
                }
                case HumanModel.Part.Shorts: return new Vector2(Around(p), Mathf.InverseLerp(rg.ShortsHem, rg.Hips.y + .02f, p.y));
                case HumanModel.Part.Socks: return new Vector2(.5f, Mathf.InverseLerp(rg.BootTop, rg.SockTop, p.y));
                default: return new Vector2(.5f, .5f);
            }
        }
    }
}
