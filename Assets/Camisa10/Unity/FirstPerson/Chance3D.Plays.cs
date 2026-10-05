using System.Collections;
using Camisa10.Core;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>Lances especiais: corte de cabeça na defesa, cruzamento pela ponta e goleiro caído no rebote.</summary>
    public partial class Chance3D
    {
        bool crossPass; // o passe em voo é um cruzamento (o companheiro cabeceia)

        /// <summary>Corte de cabeça: salto no tempo certo e disputa com o atacante.</summary>
        void CorteButton() => HeaderButton();

        void CorteContact(float q)
        {
            float duel = Mathf.Clamp(.5f + ((Stat(Attr.Def) + Stat(Attr.Fis)) / 2f - 60f) * .012f - opp01 * .15f + q * .25f + (Has("muralha") ? .08f : 0f), .15f, .95f);
            if (!Rng.Chance(duel)) { AttackerHeads("ELE GANHOU NO ALTO"); return; }
            float side = headPoint.x >= 0 ? 1 : -1;
            var away = new Vector3(Mathf.Clamp(headPoint.x + side * R(6, 16), -32, 32), 0, headPoint.z - R(16, 30));
            Launch(A.Ball.transform.position, away, R(15, 19));
            sfx?.Kick(.55f, true);
            Rig(attacker)?.Set(PersonRig.Mode.Jump);
            Finish(LiveOutcome.Cleared, q > .75f ? "CORTOU NA MEDIDA!" : "CORTOU!");
        }

        /// <summary>O atacante ganha a disputa e cabeceia para o seu gol.</summary>
        void AttackerHeads(string why)
        {
            if (phase == Phase.Done) return;
            Rig(attacker)?.Set(PersonRig.Mode.Jump);
            var shot = new Vector3(R(-3f, 3f), R(.4f, 2f), 0);
            curveAccel = 0;
            var from = crossLaunched ? A.Ball.transform.position : headPoint;
            A.Ball.isKinematic = true;
            A.Ball.transform.position = new Vector3(from.x, Mathf.Max(1.6f, from.y), from.z);
            Launch(A.Ball.transform.position, shot, R(12, 16));
            sfx?.Kick(.45f, true);
            Finish(LiveOutcome.Beaten, why);
        }

        /// <summary>Câmera do corte: olha para a bola vindo, com o seu gol às costas.</summary>
        void CorteCamera()
        {
            var look = Vector3.Lerp(headPoint + new Vector3(0, -.3f, -8f), A.Ball.transform.position, .65f);
            A.Cam.transform.rotation = Quaternion.Slerp(A.Cam.transform.rotation, Quaternion.LookRotation(look - A.Cam.transform.position), 5f * Time.deltaTime);
        }

        /// <summary>Cruzamento: bola levantada até a cabeça do centroavante, na corrida dele.</summary>
        void Cross()
        {
            var start = A.Ball.transform.position;
            var run = Flat(mateTarget - mate.position);
            var lead = Flat(mate.position) + (run.sqrMagnitude > .01f ? run.normalized * Mathf.Min(2.5f, run.magnitude) : Vector3.zero);
            float sigma = Mathf.Max(.25f, 2.0f - Stat(Attr.Pas) * .016f) * (Has("maestro") ? .7f : 1f);
            var target = new Vector3(lead.x + (float)Rng.Gauss() * sigma, 1.85f, lead.z + (float)Rng.Gauss() * sigma);
            float speed = R(16, 19);
            curveAccel = 0;
            passFlight = true; crossPass = true;
            mateTarget = Flat(target);
            passArrive = Time.time + Mathf.Max(.25f, HorizDist(start, target) / speed);
            carry = false;
            hud.HideControls();
            kickT = 0;
            sfx?.Kick(.55f);
            Launch(start, target, speed);
            shotTime = Time.time;
            phase = Phase.Flight;
            hud.SetHint("");
        }

        /// <summary>Goleiro caído (rebote): fica no chão e depois levanta.</summary>
        IEnumerator KeeperDownFor(float seconds)
        {
            gk.SetDown(Rng.Chance(.5) ? 1 : -1);
            yield return new WaitForSeconds(Time.time < introUntil ? seconds + (introUntil - Time.time) : seconds);
            if (phase == Phase.Aim) gk.GetUp();
        }
    }
}
