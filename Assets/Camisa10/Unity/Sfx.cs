using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Sons gerados em código (sem arquivos de áudio): torcida ambiente, explosão no gol, "uuuh" na chance perdida,
    /// chute, trave, rede e apito. Os clipes são sintetizados uma vez e ficam em cache.
    /// Gravações reais em Resources/Sons (crowd, roar, applause, ooh, groan, kick, post, net, whistle) substituem os sintetizados.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public enum Kind { Kick, Post, Net, Whistle, Roar, Ooh, Groan, Applause }

        const int Rate = 22050;
        static Sfx inst;
        AudioSource crowd, oneShot;
        float crowdTarget, crowdBase;
        Coroutine swell;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        public static void Ensure(GameObject host)
        {
            if (inst != null) return;
            inst = host.AddComponent<Sfx>();
            if (FindAnyObjectByType<AudioListener>() == null) host.AddComponent<AudioListener>();
            inst.crowd = host.AddComponent<AudioSource>();
            inst.crowd.loop = true; inst.crowd.playOnAwake = false; inst.crowd.volume = 0; inst.crowd.spatialBlend = 0;
            inst.oneShot = host.AddComponent<AudioSource>();
            inst.oneShot.playOnAwake = false; inst.oneShot.spatialBlend = 0;
        }

        /// <summary>Liga ou desliga a torcida de fundo (com fade).</summary>
        public static void Crowd(bool on, float level = .45f)
        {
            if (inst == null) return;
            if (on && !inst.crowd.isPlaying)
            {
                inst.crowd.clip = Clip("crowd");
                inst.crowd.time = UnityEngine.Random.Range(0f, inst.crowd.clip.length * .9f);
                inst.crowd.Play();
            }
            inst.crowdBase = on ? level : 0;
            inst.crowdTarget = inst.crowdBase;
        }

        /// <summary>A torcida cresce por alguns instantes (bola perto do gol, contra-ataque).</summary>
        public static void Swell(float extra, float seconds)
        {
            if (inst == null || inst.crowdBase <= 0) return;
            if (inst.swell != null) inst.StopCoroutine(inst.swell);
            inst.swell = inst.StartCoroutine(inst.SwellCo(extra, seconds));
        }

        IEnumerator SwellCo(float extra, float seconds)
        {
            crowdTarget = crowdBase + extra;
            yield return new WaitForSeconds(seconds);
            crowdTarget = crowdBase;
        }

        public static void Play(Kind k, float volume = 1f, float pitch = 1f)
        {
            if (inst == null || !GameSettings.Sound) return;
            var c = Clip(k.ToString().ToLowerInvariant());
            inst.oneShot.pitch = pitch;
            inst.oneShot.PlayOneShot(c, volume);
            if (k == Kind.Roar) Swell(.35f, 3.5f);
        }

        void Update()
        {
            if (crowd == null) return;
            float target = GameSettings.Sound ? crowdTarget : 0;
            crowd.volume = Mathf.MoveTowards(crowd.volume, target, Time.unscaledDeltaTime * (target > crowd.volume ? .8f : .35f));
            if (crowd.isPlaying && crowd.volume <= 0 && crowdBase <= 0) crowd.Stop();
        }

        // ---------- síntese ----------
        public static AudioClip Clip(string name)
        {
            if (clips.TryGetValue(name, out var c) && c != null) return c;
            // gravação real em Resources/Sons/<nome> (ex.: crowd.ogg, roar.ogg) tem prioridade sobre a sintetizada
            c = Resources.Load<AudioClip>("Sons/" + name);
            if (c != null) return clips[name] = c;
            var rnd = new System.Random(name.GetHashCode());
            float[] d;
            switch (name)
            {
                case "crowd": d = CrowdLoop(rnd, 8f); break;
                case "roar": d = Roar(rnd, 4f, 1f); break;
                case "applause": d = Roar(rnd, 2.2f, .55f); break;
                case "ooh": d = Ooh(rnd, 1.8f, 1f); break;
                case "groan": d = Ooh(rnd, 1.4f, .7f); break;
                case "kick": d = Kick(rnd); break;
                case "post": d = Post(); break;
                case "net": d = Net(rnd); break;
                case "whistle": d = Whistle(rnd); break;
                default: d = new float[Rate / 10]; break;
            }
            c = AudioClip.Create(name, d.Length, 1, Rate, false);
            c.SetData(d, 0);
            clips[name] = c;
            return c;
        }

        static float Noise(System.Random r) => (float)(r.NextDouble() * 2 - 1);

        /// <summary>Filtro passa-baixa simples de um polo (0 &lt; a &lt; 1; menor = mais grave).</summary>
        static void LowPass(float[] d, float a)
        {
            float y = 0;
            for (int i = 0; i < d.Length; i++) { y += a * (d[i] - y); d[i] = y; }
        }

        static void Normalize(float[] d, float peak)
        {
            float m = 0;
            foreach (var v in d) m = Mathf.Max(m, Mathf.Abs(v));
            if (m <= 0) return;
            for (int i = 0; i < d.Length; i++) d[i] *= peak / m;
        }

        /// <summary>Murmúrio de estádio: milhares de vozes viram um ruído grave que respira; o fim casa com o começo.</summary>
        static float[] CrowdLoop(System.Random r, float seconds)
        {
            int n = (int)(Rate * seconds);
            var a = new float[n]; var b = new float[n];
            for (int i = 0; i < n; i++) { a[i] = Noise(r); b[i] = Noise(r); }
            LowPass(a, .08f); LowPass(b, .17f);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                // ondas lentas de volume (cantos e reações espalhados)
                float swell = .75f + .15f * Mathf.Sin(t * 2 * Mathf.PI / seconds * 2) + .1f * Mathf.Sin(t * 2 * Mathf.PI / seconds * 5 + 1.3f);
                d[i] = (a[i] * .8f + b[i] * .35f) * swell;
            }
            // costura do loop: mistura o fim com o começo
            int fade = Rate / 2;
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                d[n - fade + i] = Mathf.Lerp(d[n - fade + i], d[i], k);
            }
            Normalize(d, .8f);
            return d;
        }

        /// <summary>Explosão da torcida: ataque rápido, mais aguda e cheia, decaindo devagar.</summary>
        static float[] Roar(System.Random r, float seconds, float bright)
        {
            int n = (int)(Rate * seconds);
            var lo = new float[n]; var hi = new float[n];
            for (int i = 0; i < n; i++) { lo[i] = Noise(r); hi[i] = Noise(r); }
            LowPass(lo, .12f); LowPass(hi, .3f);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / .18f) * Mathf.Exp(-Mathf.Max(0, t - .5f) * 1.1f / seconds * 4);
                float flutter = 1 + .12f * Mathf.Sin(t * 37) * Mathf.Sin(t * 5.3f);
                d[i] = (lo[i] + hi[i] * .45f * bright) * env * flutter;
            }
            Normalize(d, .95f * Mathf.Lerp(.6f, 1, bright));
            return d;
        }

        /// <summary>"Uuuh": ruído com uma ressonância de vogal que desce, como a torcida lamentando.</summary>
        static float[] Ooh(System.Random r, float seconds, float intensity)
        {
            int n = (int)(Rate * seconds);
            var d = new float[n];
            // ressonador de 2ª ordem varrendo de ~520 Hz para ~300 Hz
            float y1 = 0, y2 = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float f = Mathf.Lerp(520, 300, t / seconds);
                float w = 2 * Mathf.PI * f / Rate, q = .985f;
                float x = Noise(r);
                float y = x * .06f + 2 * q * Mathf.Cos(w) * y1 - q * q * y2;
                y2 = y1; y1 = y;
                float env = Mathf.Clamp01(t / .25f) * Mathf.Clamp01((seconds - t) / .6f);
                d[i] = y * env;
            }
            var bed = new float[n];
            for (int i = 0; i < n; i++) bed[i] = Noise(r);
            LowPass(bed, .1f);
            for (int i = 0; i < n; i++) d[i] += bed[i] * .25f * Mathf.Clamp01(i / (float)Rate / .2f) * Mathf.Clamp01((n - i) / (float)Rate / .5f);
            Normalize(d, .85f * intensity);
            return d;
        }

        /// <summary>Chute: pancada grave do peito do pé e um estalo curto do couro.</summary>
        static float[] Kick(System.Random r)
        {
            int n = (int)(Rate * .18f);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float thump = Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(140, 70, t / .18f) * t) * Mathf.Exp(-t * 28);
                float click = Noise(r) * Mathf.Exp(-t * 260);
                d[i] = thump * .9f + click * .6f;
            }
            Normalize(d, .95f);
            return d;
        }

        /// <summary>Bola na trave: metal com parciais inarmônicas, decaimento longo.</summary>
        static float[] Post()
        {
            int n = (int)(Rate * 1.2f);
            var d = new float[n];
            float[] f = { 620, 1580, 2870, 4310 }, a = { 1, .6f, .35f, .2f }, k = { 3.5f, 5, 7, 9 };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, v = 0;
                for (int j = 0; j < f.Length; j++) v += Mathf.Sin(2 * Mathf.PI * f[j] * t) * a[j] * Mathf.Exp(-t * k[j]);
                d[i] = v * Mathf.Clamp01(t / .002f);
            }
            Normalize(d, .8f);
            return d;
        }

        /// <summary>Rede balançando: chiado curto e abafado.</summary>
        static float[] Net(System.Random r)
        {
            int n = (int)(Rate * .5f);
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = Noise(r);
            LowPass(d, .35f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                d[i] *= Mathf.Clamp01(t / .02f) * Mathf.Exp(-t * 7) * (1 + .5f * Mathf.Sin(t * 90));
            }
            Normalize(d, .6f);
            return d;
        }

        /// <summary>Apito do árbitro: duas notas próximas batendo, com o trinado da bolinha do apito.</summary>
        static float[] Whistle(System.Random r)
        {
            int n = (int)(Rate * .55f);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float trill = 1 + .35f * Mathf.Sin(2 * Mathf.PI * 38 * t);
                float v = Mathf.Sin(2 * Mathf.PI * 2850 * t) + .8f * Mathf.Sin(2 * Mathf.PI * 2990 * t) + Noise(r) * .15f;
                float env = Mathf.Clamp01(t / .02f) * Mathf.Clamp01((.55f - t) / .06f);
                d[i] = v * trill * env;
            }
            Normalize(d, .5f);
            return d;
        }
    }

    /// <summary>Som da bola batendo na trave (preso na bola do lance).</summary>
    public class BallSounds : MonoBehaviour
    {
        float lastPost;
        void OnCollisionEnter(Collision c)
        {
            if (c.collider == null || !c.collider.name.StartsWith("Trave") || Time.time - lastPost < .3f) return;
            float v = c.relativeVelocity.magnitude;
            if (v < 2f) return;
            lastPost = Time.time;
            Sfx.Play(Sfx.Kind.Post, Mathf.Clamp01(v / 18f));
            Sfx.Play(Sfx.Kind.Ooh, .8f);
            GameSettings.Buzz();
        }
    }
}
