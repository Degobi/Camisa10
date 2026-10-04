using System.Collections;
using Camisa10.Core;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Firulas com a bola nos pés. O botão FIRULA faz o movimento escolhido pela direção do joystick:
    /// solto = pedalada, para o lado = elástico, para a frente com marcador na frente = chapéu,
    /// marcador dando o bote ou colado = caneta. Cada uma tem dificuldade, pontos de nota e um jeito de dar errado.
    /// </summary>
    public partial class Chance3D
    {
        enum SkillMove { Pedalada, Elastico, Chapeu, Caneta }

        bool skillActive;
        float skillCd;
        bool showOffScored;    // firula sem marcador só vale ponto uma vez por lance
        float footCircleUntil; // pedalada: o pé gira por cima da bola

        static string SkillLabel(SkillMove k) =>
            k == SkillMove.Elastico ? "ELÁSTICO" : k == SkillMove.Chapeu ? "CHAPÉU" : k == SkillMove.Caneta ? "CANETA" : "PEDALADA";

        /// <summary>Marcador mais perto (que não esteja caído) e a distância até ele.</summary>
        Transform NearestPresser(Vector3 b, out float dist)
        {
            Transform near = null; dist = 99;
            foreach (var d in pressers)
            {
                if (d == null || (stunUntil.TryGetValue(d, out var u) && Time.time < u)) continue;
                float dd = HorizDist(d.position, b);
                if (dd < dist) { dist = dd; near = d; }
            }
            return near;
        }

        SkillMove PickSkill(Transform near, float dist)
        {
            var st = hud.Stick != null ? hud.Stick.Value : Vector2.zero;
            if (near != null && (Mv(near).lunging || dist < 1.25f)) return SkillMove.Caneta;
            if (Mathf.Abs(st.x) > .5f) return SkillMove.Elastico;
            if (st.y > .55f && near != null && dist < 3.4f) return SkillMove.Chapeu;
            return SkillMove.Pedalada;
        }

        /// <summary>Atualiza o nome da firula embaixo do botão.</summary>
        void SkillHintStep()
        {
            if (!carry) return;
            var near = NearestPresser(Flat(A.Ball.transform.position), out float d);
            hud.SkillName(SkillLabel(PickSkill(near, d)));
        }

        void SkillButton()
        {
            if (!CanAct() || !carry || skillActive || Time.time < skillCd) return;
            var b = Flat(A.Ball.transform.position);
            var near = NearestPresser(b, out float dist);
            if (near != null && dist > 3.6f) near = null;
            var move = PickSkill(near, dist);
            bool timing = near != null && Mv(near).lunging; // na hora do bote é o momento certo
            float difficulty = move == SkillMove.Pedalada ? 0 : move == SkillMove.Elastico ? .08f : .13f;
            float p = near == null ? 1f : Mathf.Clamp(.56f + (Stat(Attr.Dri) - 60) * .012f - opp01 * .15f - difficulty + (timing ? .22f : 0f), .15f, .93f);
            StartCoroutine(DoSkill(move, near, Rng.Chance(p)));
        }

        IEnumerator DoSkill(SkillMove move, Transform near, bool ok)
        {
            skillActive = true;
            skillCd = Time.time + 1.1f;
            actCd = Time.time + .3f;
            var goalDir = Flat(Vector3.zero - me).normalized;
            if (goalDir.sqrMagnitude < .01f) goalDir = Vector3.forward;
            var side = new Vector3(goalDir.z, 0, -goalDir.x);
            float s = hud.Stick != null && Mathf.Abs(hud.Stick.Value.x) > .3f ? Mathf.Sign(hud.Stick.Value.x) * (A.Cam.transform.right.x >= 0 ? 1 : -1) : (Rng.Chance(.5) ? 1 : -1);
            var start = Flat(A.Ball.transform.position);
            float dur = move == SkillMove.Chapeu ? .75f : move == SkillMove.Pedalada ? .6f : move == SkillMove.Caneta ? .45f : .5f;

            // o marcador enganado vai para o lado errado e cai/escorrega
            if (ok && near != null)
            {
                stunUntil[near] = Time.time + dur + 1.1f;
                var nmv = Mv(near);
                nmv.lunging = false;
                nmv.vel = (move == SkillMove.Elastico ? side * s : -side * s) * 3.2f + goalDir * -1f;
                Rig(near).Set(PersonRig.Mode.Stumble);
            }
            if (move == SkillMove.Pedalada) footCircleUntil = Time.time + dur;
            sfx?.Touch(.4f);

            float t0 = Time.time;
            while (Time.time - t0 < dur && phase == Phase.Aim)
            {
                float u = (Time.time - t0) / dur;
                Vector3 pos; float h = 0;
                switch (move)
                {
                    case SkillMove.Elastico:
                        // sai para um lado e volta cortando para o outro
                        float lat = u < .4f ? Mathf.Sin(u / .4f * Mathf.PI * .5f) * .35f : Mathf.Lerp(.35f, -.75f, (u - .4f) / .6f);
                        pos = me + goalDir * .5f + side * s * lat;
                        break;
                    case SkillMove.Chapeu:
                        // cavadinha por cima da cabeça do marcador, caindo atrás dele
                        var land = start + goalDir * 3.6f;
                        pos = Vector3.Lerp(start, land, u);
                        h = Mathf.Sin(u * Mathf.PI) * 2.1f;
                        break;
                    case SkillMove.Caneta:
                        pos = Vector3.Lerp(start, start + goalDir * 2.6f, u);
                        break;
                    default:
                        // pedalada: a bola fica parada na frente enquanto o pé passa por cima
                        pos = me + goalDir * .5f;
                        break;
                }
                A.Ball.isKinematic = true;
                A.Ball.transform.position = new Vector3(pos.x, Arena.BallRadius + h, pos.z);
                if (!ok && move != SkillMove.Pedalada && u > .55f) break; // deu errado no meio
                yield return null;
            }

            if (phase != Phase.Aim) { skillActive = false; yield break; }
            var b = Flat(A.Ball.transform.position);
            if (!ok)
            {
                skillActive = false;
                if (near == null || move == SkillMove.Pedalada)
                {
                    hud.Popup("NÃO ENGANOU", Color.white); // perdeu o tempo, mas a bola continua com você
                    yield break;
                }
                string why = move == SkillMove.Chapeu ? "ELE CORTOU DE CABEÇA" : move == SkillMove.Caneta ? "ELE FECHOU AS PERNAS" : "PERDEU NO " + SkillLabel(move);
                if (near != null) { ballVel = Flat(near.position - b).normalized * -3f; Rig(near).Set(PersonRig.Mode.Ready); }
                sfx?.Boo();
                Finish(LiveOutcome.LostBall, why);
                yield break;
            }

            // deu certo: bola e corpo saem juntos para o espaço
            SetBall(b);
            switch (move)
            {
                case SkillMove.Elastico: ballVel = (-side * s * 2.6f + goalDir * 2.2f); carryVel += -side * s * 3.2f + goalDir * 1.2f; break;
                case SkillMove.Chapeu: ballVel = goalDir * 2.2f; carryVel = goalDir * Mathf.Max(carryVel.magnitude, 5.5f); break;
                case SkillMove.Caneta: ballVel = goalDir * 2.6f; carryVel += side * s * 2.5f + goalDir * 2f; break;
                default: ballVel = goalDir * 1.6f; burstUntil = Time.time + .6f; break;
            }
            lastTouch = touchAt = Time.time;
            skillActive = false;

            string label = SkillLabel(move);
            int pts = move == SkillMove.Pedalada ? MatchEngine.Pts.Skill : MatchEngine.Pts.SkillHard;
            if (near == null)
            {
                // sem marcador é só para a torcida: vale um ponto, uma vez
                if (!showOffScored) { showOffScored = true; AddPoints("Firula", 1); }
                else hud.Popup(label + "!", Theme.FeedGold);
                yield break;
            }
            AddPoints(label, pts);
            sfx?.Cheer(.4f);
            A.Crowd?.Excite(.5f, 1.4f);
            hud.Banner(label + "!", Theme.FeedGold);
            StartCoroutine(HideBanner(.7f));
        }

        /// <summary>Soma pontos na nota da partida e mostra na tela.</summary>
        void AddPoints(string label, int pts)
        {
            m.Score(label, pts);
            hud.Popup((pts > 0 ? "+" : "") + pts + " " + label.ToUpperInvariant(), pts > 0 ? Theme.Turf : Theme.Red);
        }
    }
}
