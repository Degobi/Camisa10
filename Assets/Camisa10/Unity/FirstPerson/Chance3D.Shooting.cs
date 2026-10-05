using Camisa10.Core;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Finalização: segure CHUTAR para carregar a força e solte para chutar.
    /// Na falta e no pênalti o dedo arrasta a mira no gol (e a falta tem efeito); no cabeceio a mira é arrastada
    /// enquanto a bola vem e o anel mostra a hora certa. Em jogada corrida a mira sai do joystick e da posição do goleiro.
    /// </summary>
    public partial class Chance3D
    {
        Goalkeeper gk;
        bool setPiece;           // falta ou pênalti: mira arrastável
        Vector3 aimPoint;        // alvo no plano do gol (z = 0)
        int curveSteps;          // efeito escolhido na falta (-3 a 3)
        bool charging, finesse; // finesse = chute colocado (efeito, mais preciso, menos força)
        float chargeStart;
        bool keeperHolds;
        float headArrive = -1;

        const float ChargeTime = 1.05f;   // tempo para encher a barra
        const float SweetMin = .55f, SweetMax = .82f;

        // entrada em corrida (efeito de velocidade na apresentação do lance)
        Vector3 introEye, introFwd;
        bool introRun;

        void SetupShooting()
        {
            gk = new Goalkeeper(keeper, Mathf.Lerp(.3f, .8f, opp01));
            setPiece = type == "falta" || type == "penalti";
            hud.Pad.AimMode = setPiece || type == "cabeceio" || type == "corte";
            hud.Pad.OnAimDrag = AimDrag;
            hud.Shoot.OnPress = () => ShootPress(false);
            hud.Shoot.OnRelease = ShootRelease;
            hud.Finesse.OnPress = () => ShootPress(true);
            hud.Finesse.OnRelease = ShootRelease;
            // o colocado existe em jogada corrida e no pênalti (cavadinha/colocado no canto)
            hud.Finesse.gameObject.SetActive(carry || type == "penalti" || type == "rebote");
            hud.CurveL.OnPress = () => { curveSteps = Mathf.Max(-3, curveSteps - 1); hud.Curve(true, curveSteps); };
            hud.CurveR.OnPress = () => { curveSteps = Mathf.Min(3, curveSteps + 1); hud.Curve(true, curveSteps); };
            hud.Curve(type == "falta", curveSteps);
            if (type == "cabeceio") aimPoint = new Vector3(headPoint.x > 0 ? -2.3f : 2.3f, .7f, 0);

            // a câmera chega correndo até a posição do lance
            introRun = type != "defesa";
            introEye = A.Cam.transform.position;
            introFwd = Flat(A.Cam.transform.forward).normalized;
            if (introFwd.sqrMagnitude < .01f) introFwd = Vector3.forward;
        }

        /// <summary>Apresentação: corre até a bola com borrão de velocidade (como no I Am Playr).</summary>
        void IntroRun()
        {
            if (!introRun) return;
            float t = 1f - Mathf.Clamp01((introUntil - Time.time) / 1.4f);
            float e = 1f - (1f - t) * (1f - t);
            var pos = introEye - introFwd * (11f * (1f - e)) + Vector3.up * Mathf.Abs(Mathf.Sin(t * 14f)) * .05f * (1f - t);
            A.Cam.transform.position = pos;
            hud.SetSpeed(1f - e);
            A.FovBoost = 8f * (1f - e);
        }

        // ---------- mira ----------
        void AimDrag(Vector2 delta)
        {
            if (phase != Phase.Aim || Time.time < introUntil) return;
            // arrastar a tela inteira cobre o gol todo, com folga para fora (dá para errar)
            float k = 11f / Mathf.Max(1, Screen.width);
            aimPoint.x = Mathf.Clamp(aimPoint.x + delta.x * k * (A.Cam.transform.right.x >= 0 ? 1 : -1), -5.5f, 5.5f);
            aimPoint.y = Mathf.Clamp(aimPoint.y + delta.y * k, .1f, 3.6f);
        }

        /// <summary>Alvo de uma finalização em jogada corrida: canto oposto ao goleiro, ajustado pelo joystick.</summary>
        Vector3 OpenPlayTarget(float power)
        {
            var b = A.Ball.transform.position;
            if (finesse)
            {
                // colocado: no ângulo do segundo pau, a não ser que o joystick escolha o outro lado
                float far = b.x > .5f ? -1 : b.x < -.5f ? 1 : (keeper.position.x > 0 ? -1 : 1);
                var sk = hud.Stick != null ? hud.Stick.Value : Vector2.zero;
                if (Mathf.Abs(sk.x) > .35f) far = Mathf.Sign(sk.x) * (A.Cam.transform.right.x >= 0 ? 1 : -1);
                return new Vector3(far * Mathf.Lerp(2.7f, 3.2f, power), Mathf.Lerp(.9f, 1.9f, power), 0);
            }
            float kx = keeper.position.x;
            float side = Mathf.Abs(kx - b.x * .3f) < .3f ? (b.x > 0 ? -1 : 1) : (kx > b.x * .3f ? -1 : 1);
            var st = hud.Stick != null ? hud.Stick.Value : Vector2.zero;
            if (Mathf.Abs(st.x) > .35f) side = Mathf.Sign(st.x) * (A.Cam.transform.right.x >= 0 ? 1 : -1);
            float x = side * Mathf.Lerp(2.0f, 3.0f, Mathf.Abs(st.x));
            return new Vector3(x, Mathf.Lerp(.3f, 1.7f, power), 0);
        }

        Vector2 ToHud(Vector3 world)
        {
            var vp = A.Cam.WorldToViewportPoint(world);
            var r = A.Cam.rect;
            return new Vector2(r.x + vp.x * r.width, r.y + vp.y * r.height);
        }

        float Charge01 => charging ? Mathf.Clamp01((Time.time - chargeStart) / ChargeTime) : 0f;

        /// <summary>Atualiza mira, barra de força, anel do cabeceio e o goleiro antes do chute.</summary>
        void ShootingStep(float dt)
        {
            var ball = A.Ball.transform.position;
            if (setPiece)
            {
                // o goleiro arma na bissetriz; na falta ele cobre o lado sem barreira
                float bias = type == "falta" ? -Mathf.Sign(ball.x) * .5f : 0f;
                gk.Position(ball + new Vector3(bias * 6f, 0, 0), dt, type == "penalti" ? .15f : .7f);
                hud.Reticle(true, ToHud(aimPoint), charging ? Theme.FeedGold : Color.white);
            }
            else if (type == "cabeceio" || type == "corte")
            {
                if (type == "cabeceio") gk.Position(headPoint, dt, .6f);
                hud.Reticle(crossLaunched && type == "cabeceio", ToHud(aimPoint));
                if (crossLaunched && headArrive > 0)
                {
                    float left = headArrive - Time.time;
                    hud.Timing(left > -.15f, ToHud(headPoint), 1f + Mathf.Max(0, left) * 2.2f, Mathf.Abs(left) < .09f);
                }
            }
            else if (charging) hud.Reticle(true, ToHud(OpenPlayTarget(Charge01)), Theme.FeedGold);
            else hud.Reticle(false);

            if (charging)
            {
                float p = Charge01;
                hud.Power(true, p, SweetMin, SweetMax);
                if (Time.time - chargeStart > ChargeTime + .35f) ShootRelease(); // segurou demais: sai no máximo
            }
        }

        // ---------- chute ----------
        void ShootPress(bool placed)
        {
            if (!CanAct() || charging) return;
            finesse = placed && type != "cabeceio" && type != "corte";
            if (type == "cabeceio") { HeaderButton(); return; }
            if (type == "corte") { CorteButton(); return; }
            if (type == "defesa") return;
            charging = true;
            chargeStart = Time.time;
            hud.Power(true, 0, SweetMin, SweetMax);
        }

        void ShootRelease()
        {
            if (!charging) return;
            charging = false;
            float power = Mathf.Clamp01((Time.time - chargeStart) / ChargeTime);
            hud.Power(false);
            if (phase != Phase.Aim) return;
            if (power < .08f) power = .08f; // toque rápido: chute colocado e fraco

            Vector3 target = setPiece ? aimPoint : OpenPlayTarget(power);
            // força demais levanta a bola (isola); de menos, ela vai rasteira e fraca
            if (power > SweetMax) target.y += (power - SweetMax) * (finesse ? 6f : 10f);
            var b = A.Ball.transform.position;
            float dri = Stat(Attr.Dri);
            Vector3 spin;
            float acc = type == "penalti" ? Stat(Attr.Fin) + 12 + (Has("frieza") ? 10 : 0) : Stat(Attr.Fin) + (Has("finalizador") ? 8 : 0);
            float scale = 1f;
            if (type == "falta")
            {
                // efeito escolhido nos botões: lateral para contornar a barreira, por cima para cair atrás dela
                spin = new Vector3(8f + power * 6f, curveSteps * (11f + dri * .08f) * (Has("cobrador") ? 1.25f : 1f), 0);
                if (Has("cobrador")) acc += 10;
            }
            else if (finesse)
            {
                // colocado: efeito lateral que abre e fecha no canto; mais preciso, menos forte
                float inward = -Mathf.Sign(target.x == 0 ? 1 : target.x);
                spin = new Vector3(4f, inward * (24f + dri * .22f), 0);
                acc += 18 + dri * .1f;
                scale = .8f;
            }
            else spin = new Vector3(5f + power * 8f, R(-3, 3), 0); // chute forte: seco, cai um pouco no fim
            FireShot(target, power, spin, acc, scale);
            finesse = false;
        }

        /// <summary>Chute com física real: o giro curva/derruba a bola e a trajetória é resolvida para terminar no alvo.</summary>
        void FireShot(Vector3 target, float power, Vector3 spin, float accuracyAttr, float powerScale)
        {
            var start = A.Ball.transform.position;
            float speed = Mathf.Lerp(13f, 27f + Stat(Attr.Fis) * .1f, power) * powerScale;
            float dist = HorizDist(start, target);
            // erro de execução: cresce com a distância, com a força e com a finalização fraca
            float sigma = Mathf.Max(.04f, (1f - accuracyAttr * .0085f) * (.45f + power * .75f) * (dist / 16f));
            target.x += (float)Rng.Gauss() * sigma;
            target.y += (float)Rng.Gauss() * sigma * .7f;
            carry = false;
            hud.HideControls();
            hud.Reticle(false); hud.Timing(false); hud.Curve(false); hud.Power(false);
            kickT = 0;
            sfx?.Kick(.5f + power * .5f);
            foreach (var bl in blockers) if (wallJump) Rig(bl).Set(PersonRig.Mode.Jump);
            Launch(start, target, speed, spin);
            float curve = curveAccel;
            shotTime = Time.time;
            phase = Phase.Flight;
            hud.SetHint("");
            hud.ShowNow(false);

            int? guess = null;
            if (type == "penalti")
            {
                // o goleiro adivinha o canto: às vezes lê o batedor, às vezes fica no meio
                float sideOf = Mathf.Abs(target.x) < .7f ? 0 : Mathf.Sign(target.x);
                double read = .33 + opp01 * .15;
                guess = Rng.Chance(.14) ? 0 : Rng.Chance(read) ? (int)sideOf : (Rng.Chance(.5) ? 1 : -1);
                if (guess == 0 && sideOf == 0 && Rng.Chance(.5)) guess = Rng.Chance(.5) ? 1 : -1;
            }
            gk.OnShot(start, A.Ball.linearVelocity, curve, type == "falta" || blockers.Count > 0, guess, target);
        }

        /// <summary>Cabeceio/corte: aperte para saltar; o contato acontece quando a bola chega (ver CrossStep).</summary>
        void HeaderButton()
        {
            if (!crossLaunched || jumpQueued) return;
            jumpQueued = true;
            jumpAt = Time.time;
            StartCoroutine(JumpCamera());
        }

        /// <summary>Salto: a câmera (seus olhos) sobe e desce em ~0,56 s.</summary>
        System.Collections.IEnumerator JumpCamera()
        {
            var basePos = A.Cam.transform.position;
            float t0 = Time.time;
            while (Time.time - t0 < .56f && A != null)
            {
                float u = (Time.time - t0) / .56f;
                var p = A.Cam.transform.position;
                A.Cam.transform.position = new Vector3(p.x, basePos.y + Mathf.Sin(u * Mathf.PI) * .42f, p.z);
                yield return null;
            }
        }

        /// <summary>Cabeçada no tempo: mira arrastada antes, força e precisão pela qualidade do salto.</summary>
        void HeaderContact(float q)
        {
            float acc = ((Stat(Attr.Fin) + Stat(Attr.Fis)) / 2f + (Has("cabeceador") ? 8 : 0)) * Mathf.Lerp(.55f, 1.12f, q);
            if (q > .75f) { hud.Banner("NA MEDIDA!", Theme.FeedGold); StartCoroutine(HideBanner(.5f)); }
            sfx?.Kick(.5f, true);
            FireShot(aimPoint, Mathf.Lerp(.35f, .78f, q), new Vector3(Mathf.Lerp(2f, 6f, q), 0, 0), acc, .72f);
        }

        // ---------- goleiro na hora do cruzamento da linha ----------
        /// <summary>Checa o goleiro quando a bola passa pela linha dele. true = a jogada acabou ali.</summary>
        bool KeeperCheck(Vector3 prev, Vector3 b)
        {
            float z = gk.LineZ;
            if (!(prev.z < z && b.z >= z)) return false;
            var c = Cross(prev, b, z);
            int r = gk.Contact(c, A.Ball.linearVelocity.magnitude);
            if (r == 0) return false;
            if (r == 2)
            {
                keeperHolds = true;
                A.Ball.isKinematic = true;
                sfx?.Touch(.5f);
                Finish(LiveOutcome.Saved, "SEGUROU!");
            }
            else
            {
                // espalma para o lado de onde veio o mergulho
                var v = A.Ball.linearVelocity;
                A.Ball.linearVelocity = new Vector3(Mathf.Sign(c.x - keeper.position.x + .01f) * R(3, 6), Mathf.Abs(v.y) * .4f + R(1, 3), -v.z * R(.15f, .3f));
                sfx?.Touch(.8f);
                Finish(LiveOutcome.Saved, Mathf.Abs(c.y) > 1.8f ? "ESPALMOU PARA ESCANTEIO!" : "QUE DEFESA!");
            }
            return true;
        }

        /// <summary>Depois do lance: o goleiro termina o movimento e, se segurou, a bola fica nas mãos dele.</summary>
        void KeeperAfter(float dt)
        {
            gk.Tick(dt);
            if (keeperHolds) A.Ball.transform.position = Vector3.Lerp(A.Ball.transform.position, gk.Hands, 1f - Mathf.Exp(-20f * dt));
        }
    }
}
