using System.Collections;
using System.Collections.Generic;
using Camisa10.Core;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Replay do gol, como na TV: o lance (bola e goleiro) é gravado do chute até a rede e, antes da comemoração,
    /// passa de novo em câmera lenta numa câmera alta de lado, com o selo REPLAY. O botão PULAR encerra na hora.
    /// </summary>
    public partial class Chance3D
    {
        struct RFrame { public float t; public Vector3 ball; public Quaternion ballRot; public Vector3 kp; public Quaternion kr; public PersonRig.Mode km; }

        const float ReplaySpeed = .4f;   // câmera lenta
        const int ReplayMaxFrames = 600;
        readonly List<RFrame> rec = new List<RFrame>(256);
        float recEnd = float.MaxValue;
        bool replaying, replayed, skipReplay;

        /// <summary>Teste automático: o replay está passando (para fotografar).</summary>
        public bool Replaying => replaying;
        public Texture ViewTexture => A != null ? A.View : null;

        /// <summary>Grava um quadro do chute em voo (só os seus chutes ao gol; passes e lances de defesa não têm replay).</summary>
        void RecordFrame()
        {
            if (phase != Phase.Flight && phase != Phase.Done) return;
            if (passFlight || mateShotFlight || defending || keeper == null) return;
            if (Time.time > recEnd || rec.Count >= ReplayMaxFrames) return;
            var b = A.Ball.transform;
            rec.Add(new RFrame { t = Time.time, ball = b.position, ballRot = b.rotation, kp = keeper.position, kr = keeper.rotation, km = Rig(keeper).mode });
        }

        /// <summary>Teste automático: grava um chute sintético no ângulo e encerra como gol (exercita replay e comemoração).</summary>
        public void TestReplay()
        {
            if (phase == Phase.Done || keeper == null) return;
            var start = A.Ball.transform.position; var end = new Vector3(1.6f, 1.2f, 2.2f);
            float t0 = Time.time - 1.2f;
            for (int i = 0; i <= 40; i++)
            {
                float u = i / 40f;
                var p = Vector3.Lerp(start, end, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 1.2f;
                rec.Add(new RFrame { t = t0 + u * 1.1f, ball = p, ballRot = Quaternion.identity, kp = keeper.position, kr = keeper.rotation, km = i > 20 ? PersonRig.Mode.Dive : PersonRig.Mode.Ready });
            }
            Finish(LiveOutcome.Goal);
            recEnd = 0; // nada mais é gravado
        }

        bool CanReplay => rec.Count >= 8 && rec[rec.Count - 1].t - rec[0].t > .3f;

        IEnumerator Replay()
        {
            replaying = true; replayed = true; skipReplay = false;
            celebrating = true; // o replay conduz a cena (o Update para de mexer em câmera e goleiro)
            hud.EndBanner();
            hud.SetHint("");
            hud.ReplayTag(true);
            hud.ShowSkip(true, () => skipReplay = true);
            Time.timeScale = ReplaySpeed;

            var first = rec[0]; var last = rec[rec.Count - 1];
            var toGoal = Flat(new Vector3(0, 0, 0) - first.ball);
            toGoal = toGoal.sqrMagnitude < .01f ? Vector3.forward : toGoal.normalized;
            bool air = first.ball.y > 1.1f; // cabeceio: o batedor está no ar
            var myKit = Kit.For(m.My.name, m.My.c1, m.My.c2);
            var kickSpot = Flat(first.ball) - toGoal * .45f;
            var shooter = A.Person(P.name, myKit, kickSpot - toGoal * 1.6f, Quaternion.LookRotation(toGoal).eulerAngles.y, P.boot, 10);
            var sRig = shooter.GetComponent<PersonRig>();
            sRig.Set(PersonRig.Mode.Run, 4f);

            // câmera alta de transmissão, do lado de onde veio o chute
            float side = first.ball.x >= 0 ? 1 : -1;
            var cam = A.Cam.transform;
            var camPos = new Vector3(side * 15f, 6.5f, Mathf.Min(-7f, first.ball.z * .55f));
            cam.position = camPos;
            var look = Vector3.Lerp(new Vector3(0, 1f, 0), first.ball, .6f);
            cam.rotation = Quaternion.LookRotation(look - cam.position);

            // volta tudo para o momento do chute
            A.Ball.isKinematic = true;
            A.Ball.transform.position = first.ball;
            keeper.position = first.kp; keeper.rotation = first.kr;
            var kRig = Rig(keeper);
            kRig.Set(PersonRig.Mode.Ready);
            A.NearNet?.Release();

            // 1) a corrida até a bola
            float t0 = Time.time, runUp = .55f;
            while (!skipReplay && Time.time - t0 < runUp)
            {
                float u = (Time.time - t0) / runUp;
                shooter.position = Vector3.Lerp(kickSpot - toGoal * 1.6f, kickSpot, u) + (air ? Vector3.up * Mathf.Sin(u * Mathf.PI * .5f) * .3f : Vector3.zero);
                ReplayCam(cam, A.Ball.transform.position);
                yield return null;
            }
            sRig.Set(air ? PersonRig.Mode.Jump : PersonRig.Mode.Idle);
            sfx?.Kick(.7f);

            // 2) a bola em câmera lenta até o fundo da rede (a rede estica de novo)
            t0 = Time.time;
            int i = 0; var lastMode = first.km; bool netHeld = false;
            float span = last.t - first.t;
            while (!skipReplay && Time.time - t0 <= span)
            {
                float tt = first.t + (Time.time - t0);
                while (i < rec.Count - 2 && rec[i + 1].t < tt) i++;
                var a = rec[i]; var b = rec[Mathf.Min(i + 1, rec.Count - 1)];
                float k = b.t > a.t ? Mathf.Clamp01((tt - a.t) / (b.t - a.t)) : 0;
                var bp = Vector3.Lerp(a.ball, b.ball, k);
                A.Ball.transform.position = bp;
                A.Ball.transform.rotation = Quaternion.Slerp(a.ballRot, b.ballRot, k);
                keeper.position = Vector3.Lerp(a.kp, b.kp, k);
                keeper.rotation = Quaternion.Slerp(a.kr, b.kr, k);
                if (a.km != lastMode) { kRig.Set(a.km); lastMode = a.km; }
                if (bp.z > 1.75f) { A.NearNet?.Hold(new Vector3(bp.x, bp.y, 2f), bp.z - 1.75f); netHeld = true; }
                else if (netHeld) { A.NearNet?.Release(); netHeld = false; }
                ReplayCam(cam, bp);
                yield return null;
            }
            if (netHeld) A.NearNet?.Release();
            // um respiro olhando a rede balançar
            t0 = Time.time;
            while (!skipReplay && Time.time - t0 < .5f) { ReplayCam(cam, A.Ball.transform.position); yield return null; }

            // fim: tudo como estava no fim do lance
            Time.timeScale = 1f;
            A.Ball.transform.position = last.ball;
            keeper.position = last.kp; keeper.rotation = last.kr;
            if (kRig.mode != last.km) kRig.Set(last.km);
            if (shooter != null) Destroy(shooter.gameObject);
            hud.ReplayTag(false);
            hud.ShowSkip(false);
            replaying = false;
        }

        /// <summary>Câmera do replay: parada no alto, gira devagar acompanhando a bola (com o gol sempre no quadro).</summary>
        void ReplayCam(Transform cam, Vector3 ball)
        {
            var look = Vector3.Lerp(new Vector3(0, 1f, 0), ball, .6f);
            cam.rotation = Quaternion.Slerp(cam.rotation, Quaternion.LookRotation(look - cam.position), 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
        }
    }
}
