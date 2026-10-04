using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Sons do lance gerados por código (não há arquivos de áudio no projeto): murmúrio da torcida em laço,
    /// grito de gol, lamento, toque na bola, chute e apito. Troque por gravações reais em Resources/Sons quando houver.
    /// </summary>
    public class MatchAudio : MonoBehaviour
    {
        const int Rate = 22050;
        static AudioClip crowd, cheer, groan, kick, whistle;

        AudioSource ambience, fx;
        float ambTarget = .32f;

        public static MatchAudio Create(Transform parent, Camera cam)
        {
            var go = new GameObject("Som");
            go.transform.SetParent(parent, false);
            // a câmera do lance pode ser a única ativa; sem ouvinte ligado não sai som
            var listener = FindFirstObjectByType<AudioListener>();
            if (listener == null || !listener.isActiveAndEnabled) cam.gameObject.AddComponent<AudioListener>();

            var a = go.AddComponent<MatchAudio>();
            Build();
            a.ambience = go.AddComponent<AudioSource>();
            a.ambience.clip = Load("torcida") ?? crowd;
            a.ambience.loop = true;
            a.ambience.volume = 0;
            a.ambience.spatialBlend = 0;
            a.ambience.Play();
            a.fx = go.AddComponent<AudioSource>();
            a.fx.spatialBlend = 0;
            return a;
        }

        static AudioClip Load(string name) => Resources.Load<AudioClip>("Sons/" + name);

        void Update()
        {
            if (ambience == null) return;
            ambience.volume = Mathf.MoveTowards(ambience.volume, ambTarget, Time.deltaTime * .5f);
            ambTarget = Mathf.MoveTowards(ambTarget, .32f, Time.deltaTime * .08f);
        }

        public void Touch(float strength) => Play(kick, .25f + strength * .35f, 1.25f + Random.Range(-.08f, .08f));
        public void Kick(float strength) => Play(Load("chute") ?? kick, .55f + strength * .45f, .9f + Random.Range(-.05f, .05f));
        public void Whistle() => Play(Load("apito") ?? whistle, .5f, 1f);

        public void Cheer(float amount)
        {
            ambTarget = .32f + amount * .3f;
            Play(Load("gol") ?? cheer, .35f + amount * .55f, 1f);
        }

        public void Groan() => Play(Load("lamento") ?? groan, .6f, 1f);

        void Play(AudioClip c, float vol, float pitch)
        {
            if (c == null || fx == null) return;
            // pitch por toque: um AudioSource extra para não alterar os outros sons
            if (Mathf.Abs(pitch - 1f) < .01f) { fx.PlayOneShot(c, vol); return; }
            var s = gameObject.AddComponent<AudioSource>();
            s.spatialBlend = 0;
            s.pitch = pitch;
            s.PlayOneShot(c, vol);
            Destroy(s, c.length / pitch + .1f);
        }

        // ---------- síntese ----------
        static void Build()
        {
            if (crowd != null) return;
            var rng = new System.Random(10);
            crowd = Make("Torcida", Crowd(rng, 6f, .5f, 1f));
            cheer = Make("Grito", Shaped(rng, 3.2f, .25f, 2.6f, 260f, 2400f, 1f));
            groan = Make("Lamento", Groan(rng, 1.6f));
            kick = Make("Chute", Thump(rng, .16f));
            whistle = Make("Apito", Whistle(rng, .55f));
        }

        static AudioClip Make(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        static float Noise(System.Random r) => (float)(r.NextDouble() * 2 - 1);

        /// <summary>Ruído filtrado em faixa (passa-alta e passa-baixa de um polo).</summary>
        static float[] Band(System.Random r, int n, float lo, float hi)
        {
            var o = new float[n];
            float aLo = Mathf.Exp(-2f * Mathf.PI * lo / Rate), aHi = Mathf.Exp(-2f * Mathf.PI * hi / Rate);
            float lpHi = 0, lpLo = 0;
            for (int i = 0; i < n; i++)
            {
                float x = Noise(r);
                lpHi = (1 - aHi) * x + aHi * lpHi;
                lpLo = (1 - aLo) * lpHi + aLo * lpLo;
                o[i] = lpHi - lpLo;
            }
            return o;
        }

        static void Normalize(float[] o, float peak)
        {
            float m = 1e-5f;
            foreach (var v in o) m = Mathf.Max(m, Mathf.Abs(v));
            for (int i = 0; i < o.Length; i++) o[i] *= peak / m;
        }

        /// <summary>Murmúrio de estádio: várias faixas com volume oscilando devagar, emendado para tocar em laço.</summary>
        static float[] Crowd(System.Random r, float seconds, float lowAmt, float midAmt)
        {
            int n = (int)(seconds * Rate);
            var low = Band(r, n, 90, 420);
            var mid = Band(r, n, 350, 1800);
            var o = new float[n];
            float p1 = (float)r.NextDouble() * 6, p2 = (float)r.NextDouble() * 6;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float swell = .75f + .15f * Mathf.Sin(t * 1.1f + p1) + .1f * Mathf.Sin(t * 2.7f + p2);
                o[i] = (low[i] * lowAmt + mid[i] * midAmt) * swell;
            }
            // emenda: o fim se funde no começo
            int fade = Rate / 2;
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                o[i] = o[i] * k + o[n - fade + i] * (1 - k);
            }
            System.Array.Resize(ref o, n - fade);
            Normalize(o, .6f);
            return o;
        }

        static float[] Shaped(System.Random r, float seconds, float attack, float release, float lo, float hi, float peak)
        {
            int n = (int)(seconds * Rate);
            var o = Band(r, n, lo, hi);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = t < attack ? t / attack : Mathf.Exp(-(t - attack) / release * 2.2f);
                o[i] *= env * (1f + .2f * Mathf.Sin(t * 9f));
            }
            Normalize(o, peak);
            return o;
        }

        /// <summary>"Uhhh" da torcida: faixa grave que sobe e cai de tom.</summary>
        static float[] Groan(System.Random r, float seconds)
        {
            int n = (int)(seconds * Rate);
            var a = Band(r, n, 160, 520);
            var b = Band(r, n, 300, 900);
            var o = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float env = Mathf.Sin(Mathf.Clamp01(t * 1.6f) * Mathf.PI * .5f) * (1 - t);
                o[i] = Mathf.Lerp(b[i], a[i], t) * env;
            }
            Normalize(o, .8f);
            return o;
        }

        /// <summary>Batida na bola: tom grave que cai rápido mais um estalo curto.</summary>
        static float[] Thump(System.Random r, float seconds)
        {
            int n = (int)(seconds * Rate);
            var o = new float[n];
            float ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(150f, 70f, t / seconds);
                ph += 2 * Mathf.PI * f / Rate;
                o[i] = Mathf.Sin(ph) * Mathf.Exp(-t * 32f) + Noise(r) * Mathf.Exp(-t * 400f) * .5f;
            }
            Normalize(o, .9f);
            return o;
        }

        /// <summary>Apito do árbitro: tom agudo com o trinado da bolinha.</summary>
        static float[] Whistle(System.Random r, float seconds)
        {
            int n = (int)(seconds * Rate);
            var o = new float[n];
            float ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float trill = 1f + .5f * Mathf.Sin(t * 2 * Mathf.PI * 28f);
                ph += 2 * Mathf.PI * (2950f + 40f * Mathf.Sin(t * 2 * Mathf.PI * 28f)) / Rate;
                float env = Mathf.Clamp01(t / .02f) * Mathf.Clamp01((seconds - t) / .05f);
                o[i] = (Mathf.Sin(ph) * .8f + Noise(r) * .08f) * trill * env;
            }
            Normalize(o, .7f);
            return o;
        }
    }
}
