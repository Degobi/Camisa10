using System.Collections.Generic;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Torcida de verdade: um recorte de torcedor por assento, virado para o campo, montado numa malha só.
    /// As fileiras entram de trás para a frente, então as pessoas se sobrepõem certo sem custo extra.
    /// No gol a torcida pula e levanta os braços; numa bola perto ela se levanta. Bandeiras tremulam.
    /// </summary>
    public class CrowdMotion : MonoBehaviour
    {
        // ---------- montagem ----------
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<Color32> col = new List<Color32>();
        readonly List<int> tri = new List<int>();
        readonly List<float> phase = new List<float>(), jump = new List<float>(), cheerChance = new List<float>();
        readonly List<int> cell = new List<int>(); // célula do atlas (coluna + linha do grupo)
        readonly List<bool> lower = new List<bool>();

        Mesh mesh;
        Vector3[] baseV, curV;
        Vector2[] curUV;
        float excite, exciteUntil, exciteLevel;
        int frame;

        // bandeiras
        sealed class Flag { public Mesh Mesh; public Vector3[] Base, Cur; public Vector3 Normal; public float Phase, Amp; public int W, H; }
        readonly List<Flag> flags = new List<Flag>();

        static float Rf() => (float)Camisa10.Core.Rng.Value;

        /// <summary>Um torcedor: pé no assento (mundo), largura/altura, lado "along" (esquerda→direita vista do campo), grupo 0 casa / 1 fora.</summary>
        public void AddPerson(Vector3 seat, Vector3 along, float h, int group, Color32 tint, bool lowerTier)
        {
            int c = Random.Range(0, StadiumArt.PeopleCols);
            float w = h * .52f;
            var r = along * (w / 2); var up = Vector3.up * h;
            int i = v.Count;
            v.Add(seat - r); v.Add(seat - r + up); v.Add(seat + r + up); v.Add(seat + r);
            int row = group * 2;
            AddUV(c, row);
            for (int k = 0; k < 4; k++) col.Add(tint);
            tri.Add(i); tri.Add(i + 1); tri.Add(i + 2); tri.Add(i); tri.Add(i + 2); tri.Add(i + 3);
            phase.Add(Rf() * 6.28f); jump.Add(.12f + Rf() * .22f); cheerChance.Add(Rf());
            cell.Add(c + row * StadiumArt.PeopleCols);
            lower.Add(lowerTier);
        }

        void AddUV(int c, int row)
        {
            float u0 = c / (float)StadiumArt.PeopleCols, u1 = (c + 1) / (float)StadiumArt.PeopleCols;
            float v0 = row / (float)StadiumArt.PeopleRows, v1 = (row + 1) / (float)StadiumArt.PeopleRows;
            uv.Add(new Vector2(u0, v0)); uv.Add(new Vector2(u0, v1)); uv.Add(new Vector2(u1, v1)); uv.Add(new Vector2(u1, v0));
        }

        public int Count => phase.Count;

        /// <summary>Fecha a malha dos torcedores com o material (sprites: sem luz, com transparência).</summary>
        public void Finish(Material mat)
        {
            mesh = new Mesh { name = "Torcedores", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetColors(col); mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            mesh.MarkDynamic();
            baseV = v.ToArray(); curV = v.ToArray(); curUV = uv.ToArray();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            v.Clear(); col.Clear(); tri.Clear(); // a lista de UV continua para trocar a célula
        }

        /// <summary>Bandeira tremulando: canto inferior esquerdo, direção "along", tamanho, textura.</summary>
        public void AddFlag(Transform parent, Vector3 corner, Vector3 along, float width, float height, Material mat, float amp)
        {
            const int W = 10, H = 6;
            var verts = new Vector3[(W + 1) * (H + 1)]; var uvs = new Vector2[verts.Length]; var t = new List<int>();
            for (int y = 0; y <= H; y++)
                for (int x = 0; x <= W; x++)
                {
                    verts[y * (W + 1) + x] = corner + along * (width * x / W) + Vector3.up * (height * y / H);
                    uvs[y * (W + 1) + x] = new Vector2(x / (float)W, y / (float)H);
                }
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int a = y * (W + 1) + x, b = a + W + 1;
                    t.Add(a); t.Add(b); t.Add(a + 1); t.Add(a + 1); t.Add(b); t.Add(b + 1);
                }
            var m = new Mesh { name = "Bandeira" };
            m.vertices = verts; m.uv = uvs; m.SetTriangles(t, 0); m.RecalculateBounds(); m.MarkDynamic();
            var go = new GameObject("Bandeira");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            flags.Add(new Flag { Mesh = m, Base = verts, Cur = (Vector3[])verts.Clone(), Normal = Vector3.Cross(along, Vector3.up).normalized, Phase = Rf() * 6.28f, Amp = amp, W = W, H = H });
        }

        // ---------- reação ----------
        /// <summary>Anima a torcida: 1 = gol (pula e levanta os braços), ~0,5 = de pé (bola perto), por alguns segundos.</summary>
        public void Excite(float level, float seconds)
        {
            if (level >= exciteLevel || Time.time > exciteUntil) { exciteLevel = level; exciteUntil = Time.time + seconds; }
        }

        void Update()
        {
            if (mesh == null) return;
            frame++;
            float target = Time.time < exciteUntil ? exciteLevel : .06f; // sempre um balanço leve
            excite = Mathf.MoveTowards(excite, target, Time.deltaTime * (target > excite ? 4f : .8f));
            // parada: atualiza a cada 4 quadros (economiza bateria); comemorando: todo quadro
            bool calm = excite < .1f;
            if (!(calm && frame % 4 != 0))
            {
                float t = Time.time;
                for (int q = 0; q < phase.Count; q++)
                {
                    float amp = calm ? (q % 5 == 0 ? .03f : 0f) : excite * jump[q] * (lower[q] ? 1f : .7f);
                    float freq = calm ? 2f : 7f + (q % 7);
                    float off = amp * Mathf.Abs(Mathf.Sin(t * freq + phase[q]));
                    int i = q * 4;
                    var o = new Vector3(0, off, 0);
                    curV[i] = baseV[i] + o; curV[i + 1] = baseV[i + 1] + o; curV[i + 2] = baseV[i + 2] + o; curV[i + 3] = baseV[i + 3] + o;
                    // braços para cima: troca para a mesma pessoa comemorando
                    bool cheer = excite > .35f && cheerChance[q] < excite;
                    int c = cell[q] % StadiumArt.PeopleCols, row = cell[q] / StadiumArt.PeopleCols + (cheer ? 1 : 0);
                    float u0 = c / (float)StadiumArt.PeopleCols, u1 = (c + 1) / (float)StadiumArt.PeopleCols;
                    float v0 = row / (float)StadiumArt.PeopleRows, v1 = (row + 1) / (float)StadiumArt.PeopleRows;
                    curUV[i] = new Vector2(u0, v0); curUV[i + 1] = new Vector2(u0, v1); curUV[i + 2] = new Vector2(u1, v1); curUV[i + 3] = new Vector2(u1, v0);
                }
                mesh.vertices = curV;
                mesh.uv = curUV;
            }
            // bandeiras: onda que corre do mastro para a ponta, mais forte na comemoração
            foreach (var f in flags)
            {
                float a = f.Amp * (1f + excite * 1.5f);
                for (int y = 0; y <= f.H; y++)
                    for (int x = 0; x <= f.W; x++)
                    {
                        int k = y * (f.W + 1) + x;
                        float u = x / (float)f.W;
                        float wave = Mathf.Sin(Time.time * 4f - u * 5f + f.Phase + y * .25f) * a * u;
                        f.Cur[k] = f.Base[k] + f.Normal * wave + Vector3.up * (wave * .2f);
                    }
                f.Mesh.vertices = f.Cur;
            }
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            foreach (var f in flags) if (f.Mesh != null) Destroy(f.Mesh);
        }
    }
}
