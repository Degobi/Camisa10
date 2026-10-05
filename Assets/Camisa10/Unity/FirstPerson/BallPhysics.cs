using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Física de bola de futebol: gravidade, resistência do ar (a bola perde velocidade no caminho) e efeito Magnus
    /// (o giro curva a bola: efeito lateral faz a "folha seca"/colocado, efeito por cima faz a bola cair).
    /// A mira usa o mesmo modelo para resolver a trajetória, então a bola termina onde o jogador mirou.
    /// </summary>
    public static class BallPhysics
    {
        public const float Drag = .0125f;    // ½ρ·Cd·A/m de uma bola oficial (≈ 0,0125 1/m)
        public const float Magnus = .0042f;  // força lateral por (rad/s · m/s)
        const float G = 9.81f;

        /// <summary>Aceleração além da gravidade (a Unity aplica a gravidade): arrasto + Magnus (só no ar).</summary>
        public static Vector3 ExtraAccel(Vector3 v, Vector3 spin, float height)
        {
            var a = -Drag * v.magnitude * v;
            if (height > .14f) a += Magnus * Vector3.Cross(spin, v);
            return a;
        }

        /// <summary>
        /// Velocidade inicial para sair de start com a velocidade dada e o giro dado e passar pelo alvo.
        /// Resolve por tiro: simula, mede o erro no plano do alvo e corrige (4 a 6 iterações bastam).
        /// </summary>
        public static Vector3 Solve(Vector3 start, Vector3 target, float speed, Vector3 spin, out float time)
        {
            var flat = new Vector3(target.x - start.x, 0, target.z - start.z);
            float dist = Mathf.Max(.5f, flat.magnitude);
            var dir = flat / dist;
            float T0 = Mathf.Max(.2f, dist / Mathf.Max(2f, speed));
            var v = (target - start) / T0 + new Vector3(0, .5f * G * T0, 0);
            time = T0;
            for (int it = 0; it < 6; it++)
            {
                var hit = Simulate(start, v, spin, dir, dist, out time);
                var miss = target - hit;
                miss -= dir * Vector3.Dot(miss, dir); // só o erro lateral e vertical (o plano já foi alcançado)
                if (miss.sqrMagnitude < .0004f) break;
                v += miss / Mathf.Max(.15f, time);
            }
            return v;
        }

        /// <summary>Integra como a física da Unity (Euler semi-implícito, passo fixo) até cruzar a distância.</summary>
        static Vector3 Simulate(Vector3 p, Vector3 v, Vector3 spin, Vector3 dir, float dist, out float time)
        {
            float dt = Time.fixedDeltaTime > 0 ? Time.fixedDeltaTime : .02f;
            var start = p;
            time = 0;
            for (int i = 0; i < 400; i++)
            {
                var prev = p;
                var a = ExtraAccel(v, spin, p.y) + new Vector3(0, -G, 0);
                v += a * dt;
                p += v * dt;
                time += dt;
                float along = Vector3.Dot(new Vector3(p.x - start.x, 0, p.z - start.z), dir);
                if (along >= dist)
                {
                    float prevAlong = Vector3.Dot(new Vector3(prev.x - start.x, 0, prev.z - start.z), dir);
                    float k = Mathf.Clamp01((dist - prevAlong) / Mathf.Max(1e-4f, along - prevAlong));
                    time -= dt * (1 - k);
                    return Vector3.Lerp(prev, p, k);
                }
                if (p.y < -.5f) break;
            }
            return p;
        }

        /// <summary>
        /// Rolando na grama: resistência ao rolamento (a bola desacelera e para) e giro coerente com o rolar.
        /// Devolve a aceleração extra e corrige o giro.
        /// </summary>
        public static Vector3 Rolling(Vector3 v, float height, ref Vector3 angular)
        {
            if (height > BallRadiusPlus || Mathf.Abs(v.y) > .6f) return Vector3.zero;
            var flat = new Vector3(v.x, 0, v.z);
            float sp = flat.magnitude;
            if (sp < .02f) return -flat * 4f;
            // rolando, a bola gira sem escorregar: ω = (up × v) / r
            angular = Vector3.Lerp(angular, Vector3.Cross(Vector3.up, flat) / .11f, .2f);
            return -flat / sp * RollFriction * 9.81f * (sp < 1.2f ? 1.6f : 1f);
        }
        const float BallRadiusPlus = .14f, RollFriction = .07f; // grama cortada: ~0,06-0,08

        /// <summary>Curva lateral média (m/s²) que um giro produz, para o goleiro estimar.</summary>
        public static float LateralAccel(Vector3 spin, float speed) => Magnus * spin.y * speed * .8f;
    }
}
