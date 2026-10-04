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

                var mesh = new Mesh { name = "Jogador" };
                mesh.vertices = pos;
                mesh.normals = nrm;
                mesh.boneWeights = bw;
                mesh.bindposes = bind;
                int parts = r.ReadInt32();
                mesh.subMeshCount = parts;
                int spine2 = m.Bone("Spine2");
                for (int p = 0; p < parts; p++)
                {
                    int n = r.ReadInt32();
                    var tri = new int[n];
                    for (int i = 0; i < n; i++) tri[i] = r.ReadInt32();
                    mesh.SetTriangles(tri, p, false);
                }
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
