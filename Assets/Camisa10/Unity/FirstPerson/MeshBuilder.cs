using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Camisa10.UI
{
    /// <summary>
    /// Junta muitas peças (faixas, fios da rede, degraus) numa malha só. No celular cada objeto custa uma chamada de desenho;
    /// a rede do gol, por exemplo, era feita de centenas de cubos e agora vira uma malha.
    /// </summary>
    public sealed class MeshBuilder
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> t = new List<int>();

        public int Count => v.Count;

        /// <summary>Quadrilátero a-b-c-d (horário visto de frente, como a Unity desenha).</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            var nrm = Vector3.Cross(b - a, d - a).normalized;
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            n.Add(nrm); n.Add(nrm); n.Add(nrm); n.Add(nrm);
            uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) =>
            Quad(a, b, c, d, new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));

        /// <summary>Retângulo deitado no chão (y fixo), com UV em metros vezes uvScale.</summary>
        public void Ground(float x0, float z0, float x1, float z1, float y, float uvScale = 1, Vector2 uvOffset = default)
        {
            Vector2 U(float x, float z) => new Vector2(x * uvScale, z * uvScale) + uvOffset;
            Quad(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0),
                U(x0, z0), U(x0, z1), U(x1, z1), U(x1, z0));
        }

        /// <summary>Faixa fina deitada no chão entre dois pontos (linhas do campo).</summary>
        public void GroundStrip(Vector3 p0, Vector3 p1, float width, float y)
        {
            var d = p1 - p0; d.y = 0;
            if (d.sqrMagnitude < 1e-6f) return;
            var side = new Vector3(d.z, 0, -d.x).normalized * (width / 2);
            p0.y = p1.y = y;
            Quad(p0 - side, p1 - side, p1 + side, p0 + side);
        }

        /// <summary>Caixa orientada (centro, tamanho, rotação); as faces de baixo podem ser omitidas.</summary>
        public void Box(Vector3 c, Vector3 size, Quaternion rot, bool bottom = true)
        {
            Vector3 hx = rot * new Vector3(size.x / 2, 0, 0), hy = rot * new Vector3(0, size.y / 2, 0), hz = rot * new Vector3(0, 0, size.z / 2);
            Vector3 P(int sx, int sy, int sz) => c + hx * sx + hy * sy + hz * sz;
            Quad(P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1)); // frente (-z)
            Quad(P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), P(-1, -1, 1));     // trás (+z)
            Quad(P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1)); // esquerda
            Quad(P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1));     // direita
            Quad(P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1));     // topo
            if (bottom) Quad(P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1));
        }

        /// <summary>Barra fina entre dois pontos (fios da rede, estruturas).</summary>
        public void Beam(Vector3 a, Vector3 b, float thickness)
        {
            var d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            Box((a + b) / 2, new Vector3(thickness, thickness, d.magnitude), Quaternion.LookRotation(d), false);
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        public GameObject Build(Transform parent, string name, Material mat, bool castShadows = true, bool receiveShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = ToMesh(name);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = receiveShadows;
            return go;
        }
    }
}
