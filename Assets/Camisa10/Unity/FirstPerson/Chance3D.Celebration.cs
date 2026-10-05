using System.Collections;
using System.Collections.Generic;
using Camisa10.Core;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Comemoração do gol, como na transmissão: corte para a câmera de fora, o jogador (com a camisa 10 e a chuteira
    /// escolhida) corre para a torcida e faz a comemoração escolhida no Perfil (ou uma diferente a cada gol), e os
    /// companheiros chegam para abraçar. O botão PULAR encerra na hora.
    /// </summary>
    public partial class Chance3D
    {
        bool celebrating, skipCelebration;
        Vector3 celebCamVel;

        /// <summary>Teste automático: encerra o lance como gol seu (para exercitar a comemoração).</summary>
        public void TestGoal() { if (phase != Phase.Done) Finish(LiveOutcome.Goal); }

        static string PickCelebration(string chosen)
        {
            if (!string.IsNullOrEmpty(chosen))
                foreach (var c in GameData.Celebrations) if (c[0] == chosen) return chosen;
            return GameData.Celebrations[Rng.RangeInt(1, GameData.Celebrations.Length - 1)][0];
        }

        IEnumerator Celebration()
        {
            yield return new WaitForSeconds(1.0f); // a bola na rede e o grito de gol primeiro
            celebrating = true;
            skipCelebration = false;
            hud.EndBanner();
            hud.ShowSkip(true, () => skipCelebration = true);
            string gesture = PickCelebration(P.celebration);
            var myKit = Kit.For(m.My.name, m.My.c1, m.My.c2);

            // corre na diagonal para a lateral, em direção à torcida
            var start = Flat(me);
            float side = Mathf.Abs(start.x) > 3f ? Mathf.Sign(start.x) : (Rng.Chance(.5) ? 1 : -1);
            var target = start + new Vector3(side, 0, -.5f).normalized * 12f;
            target.x = Mathf.Clamp(target.x, -Arena.HalfWidth + 2.5f, Arena.HalfWidth - 2.5f);
            target.z = Mathf.Min(target.z, -2f);
            var heading = Flat(target - start).normalized;
            if (heading.sqrMagnitude < .01f) heading = new Vector3(side, 0, 0);

            var meT = A.Person(P.name, myKit, start, Quaternion.LookRotation(heading).eulerAngles.y, P.boot, 10);
            var rig = meT.GetComponent<PersonRig>();
            var mates = new List<Transform>();
            if (mate != null) mates.Add(mate);
            var perp = new Vector3(heading.z, 0, -heading.x);
            while (mates.Count < 3)
                mates.Add(A.Person("Companheiro", myKit, target - heading * R(9f, 14f) + perp * R(-7f, 7f), 0));
            foreach (var mt in mates) { var r = Rig(mt); r.LookAt = meT; r.Set(PersonRig.Mode.Run, 7f); }
            A.Crowd?.Excite(1f, 8f);

            // corte para a câmera de fora: na frente do jogador, um pouco de lado
            var cam = A.Cam.transform;
            cam.position = meT.position + heading * 5f + perp * 1.8f + Vector3.up * 1.6f;
            cam.rotation = Quaternion.LookRotation(meT.position + Vector3.up * 1.1f - cam.position);
            celebCamVel = Vector3.zero;

            // 1) a corrida
            float t = 0, v = 2f;
            while (!skipCelebration && t < 2.8f)
            {
                float dt = Time.deltaTime; t += dt;
                var to = Flat(target - meT.position);
                if (to.magnitude < .5f) break;
                var dir = to.normalized;
                if (gesture == "aviao") dir = Quaternion.Euler(0, Mathf.Sin(t * 2.4f) * 28f, 0) * dir; // aviãozinho faz curvas
                v = Mathf.MoveTowards(v, 7.8f, 8f * dt);
                meT.position += dir * v * dt;
                meT.rotation = Quaternion.Slerp(meT.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-8f * dt));
                rig.Set(PersonRig.Mode.Run, v);
                rig.Gesture = gesture == "aviao" && t > .4f ? "aviao" : null;
                MatesStep(mates, meT, dt);
                CelebCam(meT, false, gesture, dt);
                yield return null;
            }

            // 2) a comemoração (a joelhada desliza com o embalo da corrida)
            if (!skipCelebration)
            {
                rig.Set(PersonRig.Mode.Pose);
                rig.Gesture = gesture;
                sfx?.Cheer(1f);
                A.Crowd?.Excite(1f, 5f);
                var fwd = Flat(meT.forward).normalized;
                var face = gesture == "silencio" || gesture == "soco" ? new Vector3(side, 0, 0) : fwd; // vira para a arquibancada
                float slide = gesture == "joelhada" ? v : 0, decel = slide * slide / (2f * 3.2f);
                float pt = 0;
                while (!skipCelebration && pt < 3.3f)
                {
                    float dt = Time.deltaTime; pt += dt;
                    if (slide > 0) { slide = Mathf.Max(0, slide - decel * dt); meT.position += fwd * slide * dt; }
                    else meT.rotation = Quaternion.Slerp(meT.rotation, Quaternion.LookRotation(face), 1f - Mathf.Exp(-4f * dt));
                    MatesStep(mates, meT, dt);
                    CelebCam(meT, true, gesture, dt);
                    yield return null;
                }
            }
            hud.ShowSkip(false);
        }

        /// <summary>Companheiros correm até o artilheiro e chegam de braços abertos, em volta dele (atrás e dos lados).</summary>
        void MatesStep(List<Transform> mates, Transform hero, float dt)
        {
            for (int i = 0; i < mates.Count; i++)
            {
                var mt = mates[i];
                if (mt == null) continue;
                var spot = hero.position + Quaternion.Euler(0, 125f + i * 55f, 0) * Flat(hero.forward).normalized * 1.05f;
                float d = HorizDist(mt.position, spot);
                Steer(mt, Arrive(mt.position, spot, 7.5f, 1.8f), 9f, dt, hero.position);
                var r = Rig(mt);
                r.Set(PersonRig.Mode.Run, 7f);
                r.Gesture = d < 2.6f ? "abraco" : null;
            }
        }

        /// <summary>Câmera da comemoração: acompanha a corrida pela frente e depois gira devagar em volta.</summary>
        void CelebCam(Transform hero, bool orbit, string gesture, float dt)
        {
            var cam = A.Cam.transform;
            var p = hero.position;
            bool low = gesture == "joelhada" && orbit;
            var look = p + Vector3.up * (low ? .75f : 1.15f);
            Vector3 want;
            if (!orbit)
            {
                var f = Flat(hero.forward).normalized; var side = new Vector3(f.z, 0, -f.x);
                want = p + f * 4.4f + side * 1.6f + Vector3.up * 1.55f;
            }
            else
            {
                var off = Flat(cam.position - p);
                if (off.sqrMagnitude < .01f) off = Flat(hero.forward);
                off = Quaternion.Euler(0, 13f * dt, 0) * off.normalized * 3.6f;
                want = p + off + Vector3.up * (low ? 1.1f : 1.5f);
            }
            cam.position = Vector3.SmoothDamp(cam.position, want, ref celebCamVel, orbit ? .5f : .3f);
            cam.rotation = Quaternion.Slerp(cam.rotation, Quaternion.LookRotation(look - cam.position), 1f - Mathf.Exp(-8f * dt));
        }
    }
}
