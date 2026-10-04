using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Corpo humano com esqueleto e animações de captura de movimento (Resources/Modelos/jogador.bytes).
    /// O arquivo vem de Tools/Modelos/converter.py: malha já no sistema de coordenadas da Unity, triângulos
    /// separados por parte do uniforme (camisa, pele, calção, meião, chuteira, mãos, cabelo) e clipes amostrados a 30 qps.
    /// </summary>
    public sealed class HumanModel
    {
        public enum Part { Shirt, Skin, Shorts, Socks, Boots, Hands, Hair }

        public sealed class Clip
        {
            public string Name;
            public int Frames, Fps;
            public Quaternion[] Rot; // [osso * Frames + quadro]
            public Vector3[] Hips;   // posição local do quadril por quadro
            public float Length => (Frames - 1) / (float)Fps;
        }

        public string[] Names;
        public int[] Parent;
        public Vector3[] RestPos, RestScale;
        public Quaternion[] RestRot;
        public Mesh Mesh;
        public readonly Dictionary<string, Clip> Clips = new Dictionary<string, Clip>();
        public int Hips;
        public Vector3 BackNumber; // ponto nas costas (espaço do modelo) para o número da camisa

        static HumanModel cached;
        static bool tried;

        /// <summary>Modelo compartilhado por todos os jogadores; nulo se o arquivo não existir (cai no boneco simples).</summary>
        public static HumanModel Get()
        {
            if (tried) return cached;
            tried = true;
            var ta = Resources.Load<TextAsset>("Modelos/jogador");
            if (ta == null) return null;
            try { cached = Parse(ta.bytes); }
            catch (Exception e) { Debug.LogWarning("[Camisa 10] Não foi possível ler o modelo do jogador: " + e.Message); }
            return cached;
        }

        public int Bone(string name) => Array.IndexOf(Names, name);

        static string Str(BinaryReader r) => Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));
        static Vector3 V3(BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        static Quaternion Q(BinaryReader r) => new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        static Vector4 V4(BinaryReader r) => new Vector4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

        static HumanModel Parse(byte[] data)
        {
            var m = new HumanModel();
            using (var r = new BinaryReader(new MemoryStream(data)))
            {
                if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "C10R") throw new Exception("formato desconhecido");
                r.ReadInt32(); // versão

                int nb = r.ReadInt32();
                m.Names = new string[nb]; m.Parent = new int[nb];
                m.RestPos = new Vector3[nb]; m.RestRot = new Quaternion[nb]; m.RestScale = new Vector3[nb];
                var bind = new Matrix4x4[nb];
                for (int i = 0; i < nb; i++)
                {
                    m.Names[i] = Str(r);
                    m.Parent[i] = r.ReadInt32();
                    m.RestPos[i] = V3(r); m.RestRot[i] = Q(r); m.RestScale[i] = V3(r);
                    bind[i] = new Matrix4x4(V4(r), V4(r), V4(r), V4(r));
                }
                m.Hips = m.Bone("Hips");

                int nv = r.ReadInt32();
                var pos = new Vector3[nv]; var nrm = new Vector3[nv];
                for (int i = 0; i < nv; i++) pos[i] = V3(r);
                for (int i = 0; i < nv; i++) nrm[i] = V3(r);
                var idx = r.ReadBytes(nv * 4);
                var bw = new BoneWeight[nv];
                var w = new float[4];
                var ord = new int[4];
                for (int i = 0; i < nv; i++)
                {
                    for (int k = 0; k < 4; k++) { w[k] = r.ReadSingle(); ord[k] = k; }
                    Array.Sort(ord, (a, b) => w[b].CompareTo(w[a])); // maior peso primeiro
                    bw[i] = new BoneWeight
                    {
                        boneIndex0 = idx[i * 4 + ord[0]], weight0 = w[ord[0]],
                        boneIndex1 = idx[i * 4 + ord[1]], weight1 = w[ord[1]],
                        boneIndex2 = idx[i * 4 + ord[2]], weight2 = w[ord[2]],
                        boneIndex3 = idx[i * 4 + ord[3]], weight3 = w[ord[3]],
                    };
                }

                var mesh = new Mesh { name = "Jogador", hideFlags = HideFlags.DontUnloadUnusedAsset }; // fica em cache
                mesh.vertices = pos;
                mesh.normals = nrm;
                mesh.boneWeights = bw;
                mesh.bindposes = bind;
                int parts = r.ReadInt32();
                mesh.subMeshCount = parts;
                int spine2 = m.Bone("Spine2");
                var tris = new int[parts][];
                for (int p = 0; p < parts; p++)
                {
                    int n = r.ReadInt32();
                    var tri = new int[n];
                    for (int i = 0; i < n; i++) tri[i] = r.ReadInt32();
                    mesh.SetTriangles(tri, p, false);
                    tris[p] = tri;
                }
                SlimShorts(pos, tris);
                mesh.vertices = pos;
                mesh.uv = KitUVs(m, pos, bw, tris);
                mesh.RecalculateBounds();
                m.Mesh = mesh;

                // ponto das costas na altura do peito, para o número
                float chestY = 0;
                {
                    // altura do Spine2 em repouso (soma das posições na cadeia)
                    var g = Matrix4x4.identity;
                    var chain = new List<int>();
                    for (int b = spine2; b >= 0; b = m.Parent[b]) chain.Add(b);
                    for (int c = chain.Count - 1; c >= 0; c--)
                    {
                        int b = chain[c];
                        g = g * Matrix4x4.TRS(m.RestPos[b], m.RestRot[b], m.RestScale[b]);
                    }
                    chestY = g.MultiplyPoint3x4(Vector3.zero).y - .05f;
                }
                float backZ = 0;
                for (int i = 0; i < nv; i++)
                    if (Mathf.Abs(pos[i].x) < .06f && Mathf.Abs(pos[i].y - chestY) < .04f && pos[i].z < backZ) backZ = pos[i].z;
                m.BackNumber = new Vector3(0, chestY, backZ - .006f);

                int nc = r.ReadInt32();
                for (int c = 0; c < nc; c++)
                {
                    var clip = new Clip { Name = Str(r), Frames = r.ReadInt32(), Fps = r.ReadInt32() };
                    clip.Rot = new Quaternion[nb * clip.Frames];
                    for (int i = 0; i < clip.Rot.Length; i++) clip.Rot[i] = Q(r);
                    clip.Hips = new Vector3[clip.Frames];
                    for (int i = 0; i < clip.Frames; i++) clip.Hips[i] = V3(r);
                    m.Clips[clip.Name] = clip;
                }
            }
            return m;
        }

        /// <summary>
        /// O manequim original tem o quadril bufante; um calção de futebol é mais justo. Puxa os vértices do calção
        /// (só os de dentro: as bordas são compartilhadas com a pele e ficam onde estão) para perto de cada perna.
        /// </summary>
        static void SlimShorts(Vector3[] pos, int[][] tris)
        {
            if (tris.Length <= (int)Part.Shorts) return;
            var owner = new int[pos.Length];
            for (int i = 0; i < owner.Length; i++) owner[i] = -1;
            for (int p = 0; p < tris.Length; p++) foreach (var v in tris[p]) if (owner[v] < 0) owner[v] = p;
            float cz = 0, legX = 0; int n = 0;
            foreach (var v in tris[(int)Part.Shorts]) { cz += pos[v].z; legX += Mathf.Abs(pos[v].x); n++; }
            if (n == 0) return;
            cz /= n; legX = legX / n * .8f;
            var done = new bool[pos.Length];
            foreach (var v in tris[(int)Part.Shorts])
            {
                if (done[v] || owner[v] != (int)Part.Shorts) continue;
                done[v] = true;
                var q = pos[v];
                float cx = Mathf.Sign(q.x) * legX;
                q.x = cx + (q.x - cx) * .86f;
                q.z = cz + (q.z - cz) * (q.z < cz ? .74f : .9f); // o "bumbum" do manequim é o que mais estufa
                pos[v] = q;
            }
        }

        // Regiões da textura da camisa (ver KitArt): tronco em v 0..TorsoV, mangas na faixa de cima.
        public const float TorsoV = .8f, SleeveV0 = .84f;

        /// <summary>
        /// Coordenadas de textura para o uniforme (o modelo original não tem): a camisa é desenrolada em volta do tronco
        /// (frente em u = 0,25 e costas em u = 0,75), as mangas vão para a faixa de cima ao longo do braço,
        /// calção e meião em volta do corpo/da perna, com v na altura.
        /// </summary>
        static Vector2[] KitUVs(HumanModel m, Vector3[] pos, BoneWeight[] bw, int[][] tris)
        {
            int nv = pos.Length;
            var uv = new Vector2[nv];
            var part = new int[nv];
            for (int i = 0; i < nv; i++) part[i] = -1;
            for (int p = 0; p < tris.Length; p++)
                foreach (var v in tris[p]) if (part[v] < 0) part[v] = p;

            bool IsArm(int bone)
            {
                if (bone < 0 || bone >= m.Names.Length) return false;
                var n = m.Names[bone];
                return n.Contains("Arm") || n.Contains("Hand");
            }

            // limites de cada região na pose de repouso
            float tMin = 99, tMax = -99, tz = 0; int tc = 0;
            float aMin = 99, aMax = -99, sMin = 99, sMax = -99, kMin = 99, kMax = -99;
            for (int i = 0; i < nv; i++)
            {
                var q = pos[i];
                switch ((Part)Mathf.Max(0, part[i]))
                {
                    case Part.Shirt:
                        if (IsArm(bw[i].boneIndex0)) { aMin = Mathf.Min(aMin, Mathf.Abs(q.x)); aMax = Mathf.Max(aMax, Mathf.Abs(q.x)); }
                        else { tMin = Mathf.Min(tMin, q.y); tMax = Mathf.Max(tMax, q.y); tz += q.z; tc++; }
                        break;
                    case Part.Shorts: sMin = Mathf.Min(sMin, q.y); sMax = Mathf.Max(sMax, q.y); break;
                    case Part.Socks: kMin = Mathf.Min(kMin, q.y); kMax = Mathf.Max(kMax, q.y); break;
                }
            }
            tz = tc > 0 ? tz / tc : 0;
            // sentido horário visto de cima: texto e números ficam legíveis (não espelhados)
            float Around(Vector3 q, float cz) => Mathf.Repeat(-Mathf.Atan2(q.x, q.z - cz) / (2 * Mathf.PI) + .25f, 1f);
            for (int i = 0; i < nv; i++)
            {
                var q = pos[i];
                switch ((Part)Mathf.Max(0, part[i]))
                {
                    case Part.Shirt:
                        if (IsArm(bw[i].boneIndex0))
                            uv[i] = new Vector2(Mathf.InverseLerp(aMin, aMax, Mathf.Abs(q.x)), Mathf.Lerp(SleeveV0, 1f, Around(new Vector3(q.y, 0, q.z), tz)));
                        else
                            uv[i] = new Vector2(Around(q, tz), Mathf.InverseLerp(tMin, tMax, q.y) * TorsoV);
                        break;
                    case Part.Shorts: uv[i] = new Vector2(Around(q, tz), Mathf.InverseLerp(sMin, sMax, q.y)); break;
                    case Part.Socks: uv[i] = new Vector2(.5f, Mathf.InverseLerp(kMin, kMax, q.y)); break;
                    default: uv[i] = new Vector2(.5f, .5f); break;
                }
            }
            return uv;
        }

        /// <summary>Pose do clipe no tempo t (em laço), em rotações locais por osso e posição do quadril.</summary>
        public void Sample(Clip c, float t, Quaternion[] rot, out Vector3 hips)
        {
            float len = Mathf.Max(.0001f, c.Length);
            float u = Mathf.Repeat(t, len) / len * (c.Frames - 1);
            int f0 = Mathf.Min((int)u, c.Frames - 2), f1 = f0 + 1;
            float k = u - f0;
            for (int b = 0; b < rot.Length; b++)
                rot[b] = Quaternion.Slerp(c.Rot[b * c.Frames + f0], c.Rot[b * c.Frames + f1], k);
            hips = Vector3.Lerp(c.Hips[f0], c.Hips[f1], k);
        }
    }
}
