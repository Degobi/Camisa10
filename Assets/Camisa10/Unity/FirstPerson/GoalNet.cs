using System.Collections.Generic;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Rede do gol em fios finos (linhas de 1 pixel, como na transmissão), no formato "caixote": teto que desce um pouco
    /// até o fundo, fundo reto e laterais. Os painéis cedem no meio. Quando a bola entra, a rede estufa onde ela bateu
    /// e balança até parar.
    /// </summary>
    public class GoalNet : MonoBehaviour
    {
        Mesh mesh;
        Vector3[] rest, cur, normalOut;
        float hitTime = -99, amp;
        Vector3 hitPoint;
        bool settled = true;

        const float Depth = 2.0f, BackTop = 2.15f, Step = .12f;

        /// <summary>Monta a rede de um gol com linha em z = goalZ e fundo para o lado de back (+1 ou -1).</summary>
        public static GoalNet Build(Transform parent, float goalZ, float back, Material mat)
        {
            var go = new GameObject("Rede");
            go.transform.SetParent(parent, false);
            var net = go.AddComponent<GoalNet>();
            net.Make(goalZ, back);
            go.AddComponent<MeshFilter>().sharedMesh = net.mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return net;
        }

        void Make(float goalZ, float back)
        {
            var verts = new List<Vector3>(); var outs = new List<Vector3>(); var idx = new List<int>();
            float hw = Arena.GoalHalfWidth, H = Arena.GoalHeight;
            Vector3 P(float x, float y, float d) => new Vector3(x, y, goalZ + back * d);

            // painel em grade: f(u, v) devolve o ponto; sag = quanto o meio cede (na direção "out")
            void Panel(int nu, int nv, System.Func<float, float, Vector3> f, Vector3 outDir, float sag)
            {
                int start = verts.Count;
                for (int j = 0; j <= nv; j++)
                    for (int i = 0; i <= nu; i++)
                    {
                        float u = i / (float)nu, v = j / (float)nv;
                        float bulge = Mathf.Sin(u * Mathf.PI) * Mathf.Sin(v * Mathf.PI) * sag;
                        verts.Add(f(u, v) + outDir * bulge);
                        outs.Add(outDir);
                    }
                for (int j = 0; j <= nv; j++)
                    for (int i = 0; i <= nu; i++)
                    {
                        int k = start + j * (nu + 1) + i;
                        if (i < nu) { idx.Add(k); idx.Add(k + 1); }
                        if (j < nv) { idx.Add(k); idx.Add(k + nu + 1); }
                    }
            }

            int nx = Mathf.RoundToInt(hw * 2 / Step), ny = Mathf.RoundToInt(BackTop / Step), nd = Mathf.RoundToInt(Depth / Step);
            var outBack = new Vector3(0, 0, back);
            // fundo
            Panel(nx, ny, (u, v) => P(Mathf.Lerp(-hw, hw, u), v * BackTop, Depth), outBack, .14f);
            // teto: do travessão até o alto do fundo
            Panel(nx, nd, (u, v) => P(Mathf.Lerp(-hw, hw, u), Mathf.Lerp(H, BackTop, v), Mathf.Lerp(0, Depth, v)), Vector3.down, .1f);
            // laterais (trapézio: altura vai do travessão até o alto do fundo)
            foreach (var sx in new[] { -hw, hw })
                Panel(nd, ny, (u, v) => P(sx, v * Mathf.Lerp(H, BackTop, u), Mathf.Lerp(0, Depth, u)), new Vector3(Mathf.Sign(sx), 0, 0), .06f);

            mesh = new Mesh { name = "Rede" };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetIndices(idx.ToArray(), MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            mesh.MarkDynamic();
            rest = verts.ToArray();
            cur = verts.ToArray();
            normalOut = outs.ToArray();
        }

        /// <summary>A bola entrou: estufa a rede no ponto (mundo) com força proporcional à velocidade.</summary>
        public void Hit(Vector3 worldPoint, float speed)
        {
            hitPoint = transform.InverseTransformPoint(worldPoint);
            amp = Mathf.Clamp(speed * .022f, .15f, .6f);
            hitTime = Time.time;
            settled = false;
        }

        void Update()
        {
            if (settled || mesh == null) return;
            float t = Time.time - hitTime;
            // empurrão para fora que volta balançando (mola amortecida)
            float a = amp * Mathf.Exp(-t * 3.2f) * Mathf.Cos(t * 11f) * Mathf.Clamp01(t / .06f);
            for (int i = 0; i < rest.Length; i++)
            {
                float d2 = (rest[i] - hitPoint).sqrMagnitude;
                float w = Mathf.Exp(-d2 / .55f);
                cur[i] = rest[i] + (normalOut[i] + (rest[i] - hitPoint).normalized * .3f) * (a * w);
            }
            mesh.vertices = cur;
            if (t > 2.2f) { settled = true; mesh.vertices = rest; }
        }

        void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
