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
        bool diving, down;
        float diveStart, diveDur, side, rollMax, shiftMax, riseMax, reactAt;
        Vector3 origin;
        Vector3 aim; // ponto previsto (no plano do goleiro)
        public bool Committed => diving;
        public float LineZ => T.position.z;

        const float Reach = 2.25f;   // dos pés às mãos com os braços esticados
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
            var toBall = new Vector3(ball.x, 0, ball.z);
            var dir = toBall.sqrMagnitude > .01f ? toBall.normalized : Vector3.back;
            var spot = dir * depth;            // sobre a linha que liga o centro do gol à bola
            spot.x = Mathf.Clamp(spot.x, -2.2f, 2.2f);
            spot.z = Mathf.Min(spot.z, -.3f);
            T.position = Vector3.MoveTowards(T.position, spot, 2.2f * dt);
            var face = new Vector3(ball.x - T.position.x, 0, ball.z - T.position.z);
            if (face.sqrMagnitude > .01f) T.rotation = Quaternion.RotateTowards(T.rotation, Quaternion.LookRotation(face), 360f * dt);
            rig?.Set(PersonRig.Mode.Ready);
        }

        /// <summary>
        /// Chamado no chute. curve = aceleração lateral do efeito; screened = a bola passou pela barreira/defensores
        /// (ele vê tarde); guess = pênalti: o goleiro escolhe o canto antes (-1, 0 ou 1) e sai junto com a batida.
        /// </summary>
        public void OnShot(Vector3 ballPos, Vector3 ballVel, float curve, bool screened, int? guess = null, Vector3? truth = null)
        {
            diving = false; down = false;
            origin = T.position;
            float plane = origin.z;
            float t = (plane - ballPos.z) / Mathf.Max(1f, ballVel.z);
            // previsão: trajetória sem efeito + só parte do efeito (é aí que a curva engana)
            var p = ballPos + ballVel * t + .5f * Physics.gravity * t * t;
            p.x += .5f * curve * t * t * Mathf.Lerp(.75f, .92f, skill);
            float err = Mathf.Lerp(.55f, .2f, skill) * (.6f + Mathf.Abs(curve) * .12f) * Mathf.Clamp(ballVel.magnitude / 20f, .6f, 1.5f);
            p.x += (float)Rng.Gauss() * err;
            p.y += (float)Rng.Gauss() * err * .6f;

            float react = Mathf.Lerp(.23f, .2f, skill) * R(.9f, 1.12f) + (screened ? .08f : 0f);
            if (guess.HasValue)
            {
                // pênalti: não dá tempo de reagir, ele adivinha o canto e salta junto com a batida
                react = R(-.02f, .08f);
                var b = truth ?? p;
                if (guess.Value == 0) p = new Vector3(origin.x, R(.4f, 1.4f), plane);
                else if (Mathf.Sign(b.x) == guess.Value)
                    // acertou o lado: ajusta pela altura e pela distância que viu no corpo do batedor
                    p = new Vector3(origin.x + guess.Value * Mathf.Clamp(Mathf.Abs(b.x) * .85f + (float)Rng.Gauss() * .8f, 1.4f, 3f), b.y * .8f + (float)Rng.Gauss() * .3f, plane);
                else p = new Vector3(origin.x + guess.Value * R(1.8f, 2.8f), R(.3f, 1.6f), plane);
            }
            aim = new Vector3(p.x, Mathf.Clamp(p.y, 0, 2.6f), plane);
            reactAt = Time.time + react;

            float dx = aim.x - origin.x;
            side = Mathf.Sign(dx); if (side == 0) side = 1;
            float adx = Mathf.Abs(dx);
            // escolhe o corpo: deslocar os pés, subir o quadril e girar até as mãos apontarem para a bola
            shiftMax = Mathf.Min(adx * .45f, Mathf.Lerp(1.1f, 1.5f, skill));
            riseMax = Mathf.Clamp(aim.y - .9f, 0, .55f) + (adx > 1.2f ? .15f : 0);
            float lateral = Mathf.Max(0, adx - shiftMax), up = Mathf.Max(.2f, aim.y - riseMax);
            rollMax = Mathf.Clamp(Mathf.Atan2(lateral, up) * Mathf.Rad2Deg, 0, 92);
            // tempo para esticar: ~0,7 s até o canto (medido em goleiros profissionais), menos para bolas perto do corpo
            diveDur = Mathf.Lerp(.7f, .65f, skill) * Mathf.Lerp(.55f, 1f, Mathf.Clamp01(adx / 3f));
        }

        /// <summary>Progresso do mergulho (0 a 1). Calibrado por simulação: goleiro médio defende ~50% do canto baixo com força a 18 m.</summary>
        float Progress() => Mathf.Clamp01((Time.time - diveStart) / diveDur);
        static float Ease(float p) => p * p * (3f - 2f * p); // o impulso das pernas leva um instante para ganhar velocidade

        public void Tick(float dt)
        {
            if (down)
            {
                // já caiu: fica no chão
                var pd = T.position; pd.y = Mathf.MoveTowards(pd.y, 0, 3f * dt); T.position = pd;
                return;
            }
            if (!diving)
            {
                if (reactAt > 0 && Time.time >= reactAt) { diving = true; diveStart = Time.time; rig?.Set(rollMax > 25 ? PersonRig.Mode.Dive : PersonRig.Mode.Jump); }
                return;
            }
            float p = Progress(), e = Ease(p);
            var pos = origin + new Vector3(side * shiftMax * e, riseMax * Mathf.Sin(Mathf.Min(1f, p * 1.15f) * Mathf.PI * .5f), 0);
            T.position = pos;
            var yaw = Quaternion.LookRotation(Vector3.back);
            T.rotation = yaw * Quaternion.Euler(0, 0, side * rollMax * e); // de frente para o campo, girando para o lado
            if (p >= 1f && Time.time - diveStart > diveDur + .25f) Land();
        }

        /// <summary>Termina o mergulho deitado no gramado.</summary>
        public void Land()
        {
            if (down) return;
            down = true; diving = false;
            T.rotation = Quaternion.LookRotation(Vector3.back) * Quaternion.Euler(0, 0, side * Mathf.Max(rollMax, 80f));
        }

        /// <summary>Segmento do corpo agora: dos pés às mãos (com braços esticados no mergulho).</summary>
        void Body(out Vector3 a, out Vector3 b)
        {
            float ext = diving ? Mathf.Lerp(1.95f, Reach, Ease(Progress())) : 1.95f;
            a = T.position + T.up * .25f;
            b = T.position + T.up * ext;
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
