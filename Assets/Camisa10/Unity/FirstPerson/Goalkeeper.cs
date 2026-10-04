using Camisa10.Core;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Goleiro com física de mergulho. Depois do chute ele leva um tempo para reagir, prevê onde a bola vai cruzar
    /// a linha dele (com erro: o efeito e a barreira enganam) e mergulha. O corpo desloca, sobe e gira; a defesa
    /// só acontece se a bola passar perto do corpo esticado naquele instante. Ângulo bem colocado e com força é gol.
    /// </summary>
    public sealed class Goalkeeper
    {
        public readonly Transform T;
        readonly PersonRig rig;
        readonly float skill; // 0 = time fraco, 1 = time de elite

        // mergulho
        bool diving, down, rising;
        float riseStart, downSide;
        Vector3 posVel; // posicionamento suave (acelera e freia, sem deslizar)
        float diveStart, diveDur, side, rollMax, shiftMax, riseMax, reactAt;
        Vector3 origin;
        Vector3 aim; // ponto previsto (no plano do goleiro)
        public bool Committed => diving;
        public float LineZ => T.position.z;

        const float BodyR = .3f;     // "grossura" do corpo e das luvas

        public Goalkeeper(Transform t, float skill01)
        {
            T = t;
            rig = t.GetComponent<PersonRig>();
            skill = Mathf.Clamp01(skill01);
        }

        static float R(float a, float b) => (float)Rng.RangeF(a, b);

        /// <summary>Antes do chute: fica na bissetriz do ângulo, um passo à frente da linha, com pequenos ajustes.</summary>
        public void Position(Vector3 ball, float dt, float depth = .8f)
        {
            if (diving || down) return;
            if (rising)
            {
                // levantando do chão: gira de volta para de pé em ~0,6 s
                float u = Mathf.Clamp01((Time.time - riseStart) / .6f);
                T.rotation = Quaternion.LookRotation(Vector3.back) * Quaternion.Euler(0, 0, downSide * 85f * (1 - u * u));
                if (u >= 1) { rising = false; rig?.Set(PersonRig.Mode.Ready); }
                return;
            }
            var toBall = new Vector3(ball.x, 0, ball.z);
            var dir = toBall.sqrMagnitude > .01f ? toBall.normalized : Vector3.back;
            var spot = dir * depth;            // sobre a linha que liga o centro do gol à bola
            spot.x = Mathf.Clamp(spot.x, -2.2f, 2.2f);
            spot.z = Mathf.Min(spot.z, -.3f);
            T.position = Vector3.SmoothDamp(T.position, spot, ref posVel, .28f, 4.5f, dt);
            var face = new Vector3(ball.x - T.position.x, 0, ball.z - T.position.z);
            if (face.sqrMagnitude > .01f) T.rotation = Quaternion.RotateTowards(T.rotation, Quaternion.LookRotation(face), 360f * dt);
            rig?.Set(PersonRig.Mode.Ready);
        }

        // mergulho pelo quadril: o quadril voa de lado num arco e o corpo gira em torno dele (não "tomba" pelos pés)
        const float HipH = .95f;
        Vector3 originHip;
        float hipShift, hipEndY, extMax, landStart;
        Quaternion landFrom;

        /// <summary>
        /// Chamado no chute. curve = aceleração lateral do efeito; screened = a bola passou pela barreira/defensores
        /// (ele vê tarde); guess = pênalti: o goleiro escolhe o canto antes (-1, 0 ou 1) e sai junto com a batida.
        /// Calibrado por simulação (goleiro médio): canto baixo forte a 18 m ~43% de gol, ângulo ~57%, colocado no
        /// segundo pau ~47%, pênalti com o canto certo ~45%, bola no meio quase nunca.
        /// </summary>
        public void OnShot(Vector3 ballPos, Vector3 ballVel, float curve, bool screened, int? guess = null, Vector3? truth = null)
        {
            // caído ou levantando (rebote): reage bem mais tarde e alcança menos
            bool late = down || rising;
            if (late) { rising = false; T.rotation = Quaternion.LookRotation(Vector3.back); }
            diving = false; down = false;
            origin = T.position;
            float plane = origin.z;
            float t = (plane - ballPos.z) / Mathf.Max(1f, ballVel.z);
            // previsão: trajetória sem efeito + parte do efeito (é aí que a curva engana; efeito exagerado ele percebe)
            var p = ballPos + ballVel * t + .5f * Physics.gravity * t * t;
            float fool = Mathf.Clamp(curve, -2.5f, 2.5f);
            p.x += .5f * t * t * (curve - fool * (1f - Mathf.Lerp(.62f, .82f, skill)));
            float err = Mathf.Lerp(.55f, .2f, skill) * (.6f + Mathf.Abs(curve) * .12f) * Mathf.Clamp(ballVel.magnitude / 20f, .6f, 1.5f);
            p.x += (float)Rng.Gauss() * err;
            p.y += (float)Rng.Gauss() * err * .6f;
            t *= 1.12f; // o ar freia a bola: ela chega um pouco depois do que a conta sem arrasto diz

            float react = Mathf.Lerp(.225f, .205f, skill) * R(.9f, 1.12f) + (screened ? .08f : 0f);
            if (guess.HasValue)
            {
                // pênalti: não dá tempo de reagir, ele adivinha o canto e salta junto com a batida
                react = R(-.02f, .08f);
                var b = truth ?? p;
                if (guess.Value == 0) p = new Vector3(origin.x, R(.4f, 1.4f), plane);
                else if (Mathf.Sign(b.x) == guess.Value)
                    // acertou o lado: ajusta pela altura e pela distância que viu no corpo do batedor
                    p = new Vector3(origin.x + guess.Value * Mathf.Clamp(Mathf.Abs(b.x) * .95f + (float)Rng.Gauss() * .7f, 1.4f, 3f), b.y * .8f + (float)Rng.Gauss() * .3f, plane);
                else p = new Vector3(origin.x + guess.Value * R(1.8f, 2.8f), R(.3f, 1.6f), plane);
            }
            aim = new Vector3(p.x, Mathf.Clamp(p.y, 0, 2.6f), plane);
            if (late) react += .32f;
            reactAt = Time.time + react;
            if (rig != null) rig.Reach = aim; // os braços vão na direção de onde ele acha que a bola vai

            float dx = aim.x - origin.x;
            side = Mathf.Sign(dx); if (side == 0) side = 1;
            float adx = Mathf.Abs(dx);
            float baseDur = Mathf.Lerp(.7f, .66f, skill);
            diveDur = baseDur * Mathf.Lerp(.55f, 1f, Mathf.Clamp01(adx / 3f)) * (guess.HasValue ? .84f : 1f) * (late ? 1.25f : 1f);
            // quanto o quadril voa de lado: impulso + um passo antes, se sobrar tempo (menos se a bola tem efeito)
            float spare = Mathf.Max(0, t - react - baseDur - .1f);
            hipShift = Mathf.Min(adx * .62f, Mathf.Lerp(1.42f, 1.6f, skill) + Mathf.Min(.75f, spare * (Mathf.Abs(curve) > .5f ? 1.1f : 2.2f)));
            // quadril termina baixo em bola rasteira e alto em bola no ângulo; o corpo gira até as mãos apontarem para ela
            hipEndY = Mathf.Clamp(aim.y * .55f + .35f, .45f, 1.5f);
            rollMax = Mathf.Clamp(Mathf.Atan2(Mathf.Max(0, adx - hipShift), aim.y - hipEndY) * Mathf.Rad2Deg, 0, 112);
            extMax = Mathf.Lerp(1.22f, 1.34f, skill);
            originHip = origin + Vector3.up * HipH;
        }

        /// <summary>Progresso do mergulho (0 a 1).</summary>
        float Progress() => Mathf.Clamp01((Time.time - diveStart) / diveDur);
        static float Ease(float p) => p * p * (3f - 2f * p); // o impulso das pernas leva um instante para ganhar velocidade

        public void Tick(float dt)
        {
            if (down)
            {
                // caindo e deitando de lado no gramado (quadril vai para perto do chão)
                float u = Mathf.Clamp01((Time.time - landStart) / .35f);
                var lyRot = Quaternion.LookRotation(Vector3.back) * Quaternion.Euler(0, 0, side * Mathf.Clamp(Mathf.Max(rollMax, 82f), 82f, 100f));
                T.rotation = Quaternion.Slerp(landFrom, lyRot, u * u);
                var hip = T.position + T.up * HipH;
                hip.y = Mathf.MoveTowards(hip.y, .22f, 4f * dt);
                T.position = hip - T.up * HipH;
                return;
            }
            if (!diving)
            {
                if (reactAt > 0 && Time.time >= reactAt) { diving = true; diveStart = Time.time; rig?.Set(rollMax > 25 ? PersonRig.Mode.Dive : PersonRig.Mode.Jump); }
                return;
            }
            float p = Progress(), e = Ease(p);
            // quadril: sai agachado, voa de lado num arco (corpo fora do chão) até a altura da defesa
            var hipPos = originHip + new Vector3(side * hipShift * e, Mathf.Lerp(.85f, hipEndY, e) - HipH + Mathf.Sin(p * Mathf.PI) * .12f, 0);
            T.rotation = Quaternion.LookRotation(Vector3.back) * Quaternion.Euler(0, 0, side * rollMax * e);
            T.position = hipPos - T.up * HipH; // o corpo gira em volta do quadril
            if (p >= 1f && Time.time - diveStart > diveDur + .2f) Land();
        }

        /// <summary>Começa o lance caído (rebote de uma defesa anterior).</summary>
        public void SetDown(float side)
        {
            down = true; diving = false; rising = false; downSide = side; this.side = side;
            rollMax = 88f; landStart = Time.time - 1f; landFrom = T.rotation;
            var p = T.position; p.y = 0; T.position = p;
            T.rotation = Quaternion.LookRotation(Vector3.back) * Quaternion.Euler(0, 0, side * 88f);
            T.position = new Vector3(p.x, .22f, p.z) - T.up * HipH;
            rig?.Set(PersonRig.Mode.Dive);
        }

        /// <summary>Levanta do chão (continua em Position).</summary>
        public void GetUp()
        {
            if (!down) return;
            down = false; rising = true; riseStart = Time.time;
            var p = T.position; T.rotation = Quaternion.LookRotation(Vector3.back); p.y = 0; T.position = p;
            rig?.Set(PersonRig.Mode.Stumble);
        }

        /// <summary>Termina o mergulho caindo de lado no gramado.</summary>
        public void Land()
        {
            if (down) return;
            down = true; diving = false;
            landStart = Time.time; landFrom = T.rotation;
        }

        /// <summary>Segmento do corpo agora: dos pés às mãos (braços esticados acima da cabeça no mergulho).</summary>
        void Body(out Vector3 a, out Vector3 b)
        {
            float ext = diving ? extMax * Mathf.Lerp(.85f, 1f, Ease(Progress())) : 1.0f;
            a = T.position + T.up * .1f;
            b = T.position + T.up * (HipH + ext);
        }

        static float SegDist(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude));
            return Vector3.Distance(p, a + ab * t);
        }

        /// <summary>
        /// A bola cruzou a linha do goleiro em <paramref name="ball"/>. Devolve 0 = passou, 1 = rebateu, 2 = segurou.
        /// Na ponta dos dedos às vezes ela passa mesmo assim.
        /// </summary>
        public int Contact(Vector3 ball, float speed)
        {
            Body(out var a, out var b);
            float d = SegDist(ball, a, b) - Arena.BallRadius;
            if (d > BodyR) return 0;
            bool fingertips = Vector3.Distance(ball, b) < .45f && d > BodyR * .45f;
            if (fingertips && Rng.Chance(Mathf.Lerp(.5f, .35f, skill) * Mathf.Clamp(speed / 22f, .6f, 1.3f))) return 0;
            bool catchIt = !fingertips && speed < Mathf.Lerp(17f, 22f, skill) && Vector3.Distance(ball, b) < 1.1f;
            return catchIt ? 2 : 1;
        }

        /// <summary>Mãos agora (para segurar a bola na defesa).</summary>
        public Vector3 Hands { get { Body(out _, out var b); return b; } }
    }
}
