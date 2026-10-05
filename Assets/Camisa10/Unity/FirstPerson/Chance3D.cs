using System;
using System.Collections;
using System.Collections.Generic;
using Camisa10.Core;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Um lance decisivo jogado em primeira pessoa. O jogador desliza o dedo:
    /// a direção define o alvo, a velocidade define a força e a curva do traço define o efeito.
    /// Os atributos mudam precisão (finalização), força (físico), efeito (drible), passe e desarme.
    /// </summary>
    public partial class Chance3D : MonoBehaviour
    {
        const float Gravity = 9.81f;
        enum Phase { Aim, Flight, Watch, Done }

        Arena A;
        Game g;
        MatchEngine m;
        ChanceHud hud;
        Action<LiveOutcome> onDone;
        string type;
        float opp01;
        Player P => g.S.player;
        float Stat(Attr a) => P.Get(a);

        Phase phase = Phase.Aim;
        float startTime, shotTime, aimTimeout = 9f;
        Vector3 prevBall;
        float curveAccel;

        // goleiro (o mergulho fica em Goalkeeper)
        Transform keeper;
        float keeperZ = -.6f;

        // defensores que podem bloquear
        readonly List<Transform> blockers = new List<Transform>();
        bool wallJump;

        // companheiro (passe)
        Transform mate, marker;
        Vector3 mateTarget;
        float mateSpeed, passArrive;
        bool mateHasBall, passFlight, mateShotFlight;

        // condução da bola com o joystick (contra-ataque, cara a cara, chance e meio-campo)
        bool carry, autoRun;
        float carrySpeed, chaserSpeed, presserSpeed, stamina = 1, tackleCd, burstUntil, actCd;
        bool sprintTired; // fôlego zerado: só volta a correr depois de recuperar um pouco
        const float SprintMult = 1.5f;
        Vector3 carryVel;
        Transform chaser;
        readonly List<Transform> pressers = new List<Transform>();
        readonly Dictionary<Transform, float> stunUntil = new Dictionary<Transform, float>();

        // corpo e bola separados: a bola rola à frente e o jogador a toca de novo quando alcança
        Vector3 me, ballVel, camVel, camLook;
        bool camInit;
        float lastTouch = -9, touchAt = -9, stepPhase, fovBoost;
        Vector3 ballSpin; // giro atual da bola (rad/s), usado no efeito Magnus

        /// <summary>Estado de movimento de quem corre em campo: velocidade com aceleração, reação atrasada e bote.</summary>
        sealed class Mover
        {
            public Vector3 vel, seenBall, seenVel, lungeDir;
            public float react, aggr, lungeUntil, nextLunge;
            public bool lunging;
        }
        readonly Dictionary<Transform, Mover> movers = new Dictionary<Transform, Mover>();
        MatchAudio sfx;

        // cabeceio (salto: o contato acontece quando a bola chega)
        bool jumpQueued, headDone;
        float jumpAt;
        Vector3 headPoint;
        bool crossLaunched;
        float crossLaunchAt, windowOpen = -1, windowClose = -1;

        // pé do jogador em primeira pessoa (mostra a chuteira personalizada)
        Transform foot;
        float kickT = -1;
        float introUntil;

        // desarme
        Vector3 defSpot;
        bool defending, crossMode;
        float keeperDown;
        Transform attacker;
        float meZ, attackerSpeed, cutStart = -1;
        int cutSide;
        bool attackerRunsOn;

        public static Chance3D Play(Transform parent, Game game, MatchEngine match, ChanceHud hud, Action<LiveOutcome> done)
        {
            var go = new GameObject("Lance3D");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<Chance3D>();
            c.Init(game, match, hud, done);
            return c;
        }

        void Init(Game game, MatchEngine match, ChanceHud h, Action<LiveOutcome> done)
        {
            g = game; m = match; hud = h; onDone = done;
            type = m.Current.Type;
            opp01 = Mathf.Clamp01((m.Opp.str - 50) / 40f);

            A = Arena.Build(transform, Style(g, m));
            A.OnView = t => hud.SetView(t);
            hud.SetView(A.View);
            Debug.Log($"[Camisa 10] Lance 3D: câmera {(A.Cam.enabled ? "ligada" : "desligada")}, imagem {A.View.width}x{A.View.height}, " +
                $"{UnityEngine.Object.FindObjectsByType<Renderer>().Length} objetos visíveis na cena.");

            var oppKit = Kit.For(m.Opp.name, m.Opp.c1, m.Opp.c2);
            var myKit = Kit.For(m.My.name, m.My.c1, m.My.c2);

            keeper = A.Person("Goleiro", Kit.Goalkeeper(KeeperColor(oppKit, myKit)), new Vector3(0, 0, keeperZ), 180);
            Rig(keeper).Set(PersonRig.Mode.Ready);

            if (m.Home) hud.SetScore(m.Minute, m.My.name, Theme.Hex(m.My.c1), m.Gf, m.Ga, m.Opp.name, Theme.Hex(m.Opp.c1));
            else hud.SetScore(m.Minute, m.Opp.name, Theme.Hex(m.Opp.c1), m.Ga, m.Gf, m.My.name, Theme.Hex(m.My.c1));
            string hint;
            switch (type)
            {
                case "cara":
                {
                    // de frente, pela diagonal ou quase sem ângulo; às vezes um zagueiro vem na cobertura
                    float ang = R(-55, 55) * Mathf.Deg2Rad, dist = R(9, 16);
                    var b = new Vector3(Mathf.Sin(ang) * dist, 0, -Mathf.Cos(ang) * dist);
                    PlaceBallAndCamera(b);
                    keeperZ = -R(1.6f, 3.4f);
                    keeper.position = new Vector3(b.x * .25f, 0, keeperZ);
                    carry = true; carrySpeed = 4.4f + Stat(Attr.Vel) * .022f; aimTimeout = 12;
                    if (Rng.Chance(.55))
                    {
                        chaser = A.Person("Zagueiro", oppKit, b + new Vector3(R(-6, 6), 0, -R(3, 7)), 0);
                        chaserSpeed = carrySpeed * SprintMult * (.82f + opp01 * .12f);
                        pressers.Add(chaser);
                    }
                    hint = "Cara a cara! Conduza e SEGURE CHUTAR para a força; o joystick escolhe o canto.";
                    break;
                }
                case "falta":
                {
                    // distância e ângulo variados; barreira maior quanto mais perto
                    var b = new Vector3(R(-14, 14), 0, -R(18, 32));
                    PlaceBallAndCamera(b);
                    Vector3 dir = (Vector3.zero - b).normalized, perp = new Vector3(dir.z, 0, -dir.x), wallC = b + dir * 9.15f;
                    float nearSide = b.x >= 0 ? 1 : -1;
                    int wall = b.z > -22 ? 5 : b.z > -27 ? 4 : 3;
                    if (Mathf.Abs(b.x) > 9) wall = Mathf.Max(2, wall - 2);
                    for (int i = 0; i < wall; i++)
                        blockers.Add(A.Person("Barreira", oppKit, wallC + perp * ((i - (wall - 1) / 2f) * .55f) + new Vector3(nearSide * .5f, 0, 0), 180));
                    wallJump = true;
                    keeper.position = new Vector3(-nearSide * .7f, 0, keeperZ);
                    aimPoint = new Vector3(-nearSide * 2.6f, 1.9f, 0);
                    aimTimeout = 25;
                    hint = "Falta! Arraste o dedo para mirar, escolha o EFEITO e SEGURE CHUTAR para a força. Solte para bater.";
                    break;
                }
                case "penalti":
                {
                    var b = new Vector3(0, 0, -11);
                    SetBall(b);
                    A.PlaceCamera(b + new Vector3(Rng.Chance(.5) ? -.5f : .5f, 1.6f, -2.6f), new Vector3(0, 1f, 0));
                    keeperZ = -.25f;
                    keeper.position = new Vector3(0, 0, keeperZ);
                    aimPoint = new Vector3(R(-1.5f, 1.5f), 1f, 0);
                    aimTimeout = 25;
                    hint = "Pênalti! Arraste o dedo para mirar no canto e SEGURE CHUTAR. Solte na faixa verde: força demais isola.";
                    break;
                }
                case "contra":
                {
                    // sai do campo de defesa ou do meio, pela ponta ou pelo centro
                    var b = new Vector3(R(-22, 22), 0, -R(36, 62));
                    PlaceBallAndCamera(b);
                    carry = true; autoRun = true;
                    carrySpeed = 4.8f + Stat(Attr.Vel) * .026f;
                    float side = Rng.Chance(.5) ? 1 : -1;
                    chaser = A.Person("Zagueiro", oppKit, b + new Vector3(side * R(4, 8), 0, -R(1, 4)), 0);
                    chaserSpeed = carrySpeed * SprintMult * (.8f + opp01 * .13f); // corre atrás em arrancada, mas perde para a sua
                    pressers.Add(chaser);
                    if (Rng.Chance(.45))
                    {
                        // outro defensor vem de frente fechar o caminho
                        var front = A.Person("Zagueiro", oppKit, new Vector3(Mathf.Clamp(b.x * .4f + R(-6, 6), -20, 20), 0, b.z + R(14, 22)), 180);
                        pressers.Add(front);
                    }
                    aimTimeout = 22;
                    hint = "Contra-ataque! Empurre o joystick até a borda (ou segure CORRER) para arrancar. DRIBLE ou FIRULA quando chegarem perto.";
                    break;
                }
                case "meio":
                {
                    var b = new Vector3(R(-16, 16), 0, -R(30, 44));
                    PlaceBallAndCamera(b);
                    float side = Rng.Chance(.5) ? 1 : -1;
                    mate = A.Person("Companheiro", myKit, new Vector3(side * R(6, 16), 0, b.z + R(6, 12)), 0);
                    mateTarget = new Vector3(side * R(1, 8), 0, -R(8, 13));
                    mateSpeed = R(5f, 6.2f);
                    marker = A.Person("Zagueiro", oppKit, mate.position + new Vector3(-side * 2, 0, 2), 180);
                    blockers.Add(A.Person("Volante", oppKit, new Vector3(b.x * .6f + R(-3, 3), 0, b.z + R(7, 11)), 180));
                    pressers.Add(blockers[blockers.Count - 1]);
                    if (Rng.Chance(.4)) { var z2 = A.Person("Zagueiro", oppKit, new Vector3(R(-8, 8), 0, -R(16, 20)), 180); blockers.Add(z2); pressers.Add(z2); }
                    carry = true; carrySpeed = 4.5f + Stat(Attr.Vel) * .022f; aimTimeout = 16;
                    hint = "Toque em PASSE para lançar o companheiro, ou conduza e arrisque o chute.";
                    break;
                }
                case "cruzamento":
                {
                    // pela ponta, perto da linha de fundo: cruze para o companheiro cabecear ou arrisque
                    float side = Rng.Chance(.5) ? 1 : -1;
                    var b = new Vector3(side * R(22, 30), 0, -R(7, 20));
                    PlaceBallAndCamera(b);
                    mate = A.Person("Centroavante", myKit, new Vector3(R(-4, 4) - side * 3, 0, -R(16, 20)), 0);
                    mateTarget = new Vector3(R(-2.5f, 2.5f), 0, -R(6, 9));
                    mateSpeed = R(5f, 6f);
                    marker = A.Person("Zagueiro", oppKit, mate.position + new Vector3(side * 1.2f, 0, 1.4f), 180);
                    var fb = A.Person("Lateral", oppKit, b + new Vector3(-side * R(1, 3), 0, R(4, 7)), 180);
                    blockers.Add(fb); pressers.Add(fb);
                    carry = true; carrySpeed = 4.5f + Stat(Attr.Vel) * .022f; aimTimeout = 14;
                    crossMode = true;
                    hint = "Pela ponta! Passe pelo lateral e toque em CRUZAR para o centroavante cabecear (ou arrisque o chute).";
                    break;
                }
                case "rebote":
                {
                    // o goleiro espalmou e a bola ficou viva na área: decida rápido
                    var b = new Vector3(R(-5, 5), 0, -R(6, 11));
                    PlaceBallAndCamera(b);
                    A.Cam.transform.position = b + new Vector3(R(-1, 1), 1.6f, -R(2.5f, 4f));
                    float ks = Rng.Chance(.5) ? 1 : -1;
                    keeper.position = new Vector3(ks * R(1.2f, 2.4f), 0, -R(.6f, 1.6f));
                    keeperDown = R(.9f, 1.6f);
                    var z = A.Person("Zagueiro", oppKit, b + new Vector3(R(-4, 4), 0, R(2.5f, 4f)), 180);
                    pressers.Add(z);
                    carry = true; carrySpeed = 4.4f + Stat(Attr.Vel) * .022f; aimTimeout = 5;
                    hint = "Rebote! O goleiro está caído: chute rápido antes que ele levante.";
                    break;
                }
                case "corte":
                {
                    // defendendo a sua área: cruzamento do adversário, tire de cabeça antes do atacante
                    defending = true;
                    headPoint = new Vector3(R(-3.5f, 3.5f), R(1.8f, 1.95f), -R(5, 10));
                    float side = Rng.Chance(.5) ? 1 : -1;
                    bool corner = Rng.Chance(.55);
                    SetBall(corner ? new Vector3(side * 33.5f, 0, -.5f) : new Vector3(side * R(18, 32), 0, -R(14, 30)));
                    A.PlaceCamera(new Vector3(headPoint.x, 1.75f, headPoint.z + 1.1f), A.Ball.transform.position + Vector3.up * 2f);
                    crossLaunchAt = Time.time + .9f;
                    attacker = A.Person("Atacante", oppKit, new Vector3(headPoint.x + side * .9f, 0, headPoint.z - .7f), 0);
                    keeper.position = new Vector3(headPoint.x * .2f, 0, -.6f);
                    aimTimeout = 99;
                    hint = "Bola na sua área! Toque em CORTAR quando o anel fechar na bola, antes do atacante.";
                    break;
                }
                case "cabeceio":
                {
                    headPoint = new Vector3(R(-4, 4), R(1.8f, 1.95f), -R(5.5f, 12));
                    A.PlaceCamera(new Vector3(headPoint.x, 1.75f, headPoint.z - 1.2f), new Vector3(0, 1.3f, 0));
                    float side = Rng.Chance(.5) ? 1 : -1;
                    // escanteio ou falta lateral levantada na área
                    SetBall(Rng.Chance(.6) ? new Vector3(side * 33.5f, 0, -.5f) : new Vector3(side * R(18, 30), 0, -R(14, 28)));
                    crossLaunchAt = Time.time + .8f;
                    blockers.Add(A.Person("Zagueiro", oppKit, new Vector3(headPoint.x + side * R(.6f, 1.1f), 0, headPoint.z + R(.4f, 1f)), 180));
                    if (Rng.Chance(.5)) blockers.Add(A.Person("Zagueiro", oppKit, new Vector3(headPoint.x - side * 1.4f, 0, headPoint.z + 1.6f), 180));
                    aimTimeout = 99;
                    hint = "Bola levantada na área! Arraste para mirar e toque em CABECEAR quando o anel fechar na bola.";
                    break;
                }
                case "defesa":
                {
                    var me = new Vector3(R(-10, 10), 0, -R(12, 24));
                    meZ = me.z;
                    defSpot = me;
                    A.PlaceCamera(me + new Vector3(0, 1.7f, 0), me + new Vector3(0, 1f, -10));
                    attacker = A.Person("Atacante", oppKit, me + new Vector3(R(-9, 9), 0, -R(13, 18)), 0);
                    SetBall(attacker.position + new Vector3(0, 0, .7f));
                    attackerSpeed = 5.5f + opp01 * 1.5f;
                    cutSide = Rng.Chance(.5) ? 1 : -1;
                    aimTimeout = 99;
                    hint = "Ele vem para cima de você. Leia o corpo dele e toque em DESARME do lado em que ele cortar.";
                    break;
                }
                default: // "chance" e qualquer outro tipo de finalização
                {
                    var b = new Vector3(R(-15, 15), 0, -R(12, 24));
                    PlaceBallAndCamera(b);
                    blockers.Add(A.Person("Zagueiro", oppKit,
                        Vector3.Lerp(b, new Vector3(0, 0, -.5f), R(.2f, .4f)) + new Vector3(R(-2f, 2f), 0, 0), 180));
                    pressers.Add(blockers[0]);
                    if (Rng.Chance(.35)) { var v2 = A.Person("Volante", oppKit, b + new Vector3(R(-5, 5), 0, -R(2, 5)), 0); pressers.Add(v2); }
                    if (Rng.Chance(.75))
                    {
                        float side = b.x > 0 ? -1 : 1;
                        mate = A.Person("Companheiro", myKit, new Vector3(Mathf.Clamp(b.x + side * R(6, 12), -25, 25), 0, b.z + R(-1, 4)), 0);
                        mateTarget = new Vector3(side * R(1, 6), 0, -R(6, 10));
                        mateSpeed = R(4.3f, 5.5f);
                        marker = A.Person("Zagueiro", oppKit, mate.position + new Vector3(-side * 1.5f, 0, 1.5f), 180);
                    }
                    carry = true; carrySpeed = 4.4f + Stat(Attr.Vel) * .022f; aimTimeout = 14;
                    hint = "Conduza e drible o zagueiro. SEGURE CHUTAR para a força (o joystick escolhe o canto) ou toque em PASSE.";
                    break;
                }
            }
            presserSpeed = 3.6f + opp01 * 1.5f;
            hud.SetHint(hint);
            hud.Pad.OnSwipe = OnSwipe;
            hud.PassBtn.OnPress = PassButton;
            hud.Dribble.OnPress = DribbleButton;
            hud.Skill.OnPress = SkillButton;
            hud.TackleL.OnPress = () => TackleButton(-1);
            hud.TackleR.OnPress = () => TackleButton(1);
            hud.Controls(carry, type != "defesa", mate != null, pressers.Count > 0, carry, type == "defesa");
            if (type == "cabeceio") hud.Shoot.GetComponentInChildren<UnityEngine.UI.Text>().text = "CABECEAR";
            if (type == "corte") hud.Shoot.GetComponentInChildren<UnityEngine.UI.Text>().text = "CORTAR";
            if (crossMode) hud.PassBtn.GetComponentInChildren<UnityEngine.UI.Text>().text = "CRUZAR";
            hud.SetHint(hint);
            SetupShooting();
            if (keeperDown > 0) StartCoroutine(KeeperDownFor(keeperDown)); // depois do goleiro existir
            foreach (var bl in blockers) Rig(bl).Set(wallJump ? PersonRig.Mode.Idle : PersonRig.Mode.Ready);
            // sem pé de primeira pessoa: em jogo de futebol a câmera é os olhos do jogador (nada flutuando na tela)

            foreach (var r in A.Root.GetComponentsInChildren<PersonRig>()) r.LookAt = A.Ball.transform;
            me = Flat(A.Ball.transform.position) - Vector3.forward * .45f;
            sfx = MatchAudio.Create(A.Root, A.Cam);

            // apresentação do lance antes de liberar o controle
            introUntil = Time.time + 1.4f;
            if (type == "falta") sfx?.Whistle();
            hud.Intro($"{m.Minute}'  {m.Current.Text}");
            startTime = introUntil;
            prevBall = A.Ball.transform.position;
        }

        /// <summary>
        /// Estádio do mandante: torcida com as cores dele, um canto com a torcida visitante, placas com as marcas do jogo
        /// (os patrocinadores do jogador aparecem primeiro) e jogo à noite em parte das rodadas.
        /// </summary>
        static StadiumStyle Style(Game g, MatchEngine m)
        {
            var home = m.Home ? m.My : m.Opp; var away = m.Home ? m.Opp : m.My;
            var se = g.S.season;
            var brands = new List<string>();
            foreach (var d in g.S.activeSponsors) if (!brands.Contains(d.brand)) brands.Add(d.brand);
            if (m.My.league == "wc")
            {
                // Copa do Mundo: estádio neutro à noite, placas do torneio
                var boards = new List<StadiumArt.Board>();
                void B(string t, string bg, string fg) => boards.Add(new StadiumArt.Board { Text = t, Bg = Theme.Hex(bg), Fg = Theme.Hex(fg) });
                B($"COPA DO MUNDO {g.S.wc.year}", "#0A2342", "#F2C230");
                B(m.My.name + " x " + m.Opp.name, "#111111", "#FFFFFF");
                B("COPA DO MUNDO", "#7A0E2A", "#FFFFFF");
                foreach (var bb in StadiumStyle.DefaultBoards(brands)) boards.Add(bb);
                return new StadiumStyle
                {
                    Home1 = Theme.Hex(home.c1), Home2 = Theme.Hex(home.c2), Away1 = Theme.Hex(away.c1), Away2 = Theme.Hex(away.c2),
                    Night = true, Seed = 2026, Boards = boards,
                };
            }
            return new StadiumStyle
            {
                Home1 = Theme.Hex(home.c1), Home2 = Theme.Hex(home.c2),
                Away1 = Theme.Hex(away.c1), Away2 = Theme.Hex(away.c2),
                Night = (se.year * 31 + se.week * 7) % 5 < 2,
                Seed = Mathf.Abs((home.id ?? home.name ?? "").GetHashCode()) % 1000,
                Boards = StadiumStyle.DefaultBoards(brands),
            };
        }

        /// <summary>Cor da camisa do goleiro que não se confunde com nenhum dos dois times.</summary>
        static string KeeperColor(Kit a, Kit b)
        {
            foreach (var hex in new[] { "#C6E03A", "#FF7A1A", "#2BD9FE", "#9B6BFF", "#222222" })
            {
                var c = Theme.Hex(hex);
                bool clash = false;
                foreach (var k in new[] { a, b })
                    foreach (var col in k.Body)
                        if (Mathf.Abs(c.r - col.r) + Mathf.Abs(c.g - col.g) + Mathf.Abs(c.b - col.b) < .6f) clash = true;
                if (!clash) return hex;
            }
            return "#C6E03A";
        }

        // ---------- utilidades ----------
        static float R(float a, float b) => (float)Rng.RangeF(a, b);
        static PersonRig Rig(Transform t) => t != null ? t.GetComponent<PersonRig>() : null;

        Mover Mv(Transform t)
        {
            if (!movers.TryGetValue(t, out var mv))
            {
                var b = Flat(A.Ball.transform.position);
                mv = new Mover
                {
                    react = Mathf.Lerp(.34f, .17f, opp01) * R(.85f, 1.2f),
                    aggr = R(.7f, 1.25f) * Mathf.Lerp(.85f, 1.15f, opp01),
                    seenBall = b,
                    nextLunge = Time.time + R(.5f, 1.1f),
                };
                movers[t] = mv;
            }
            return mv;
        }

        /// <summary>
        /// Move com aceleração limitada (ninguém sai do zero ao máximo num quadro) e gira o corpo aos poucos.
        /// </summary>
        void Steer(Transform t, Vector3 desiredVel, float accel, float dt, Vector3? faceAt = null, float turnDeg = 480f)
        {
            var mv = Mv(t);
            mv.vel = Vector3.MoveTowards(mv.vel, Flat(desiredVel), accel * dt);
            t.position += mv.vel * dt;
            var look = faceAt.HasValue ? Flat(faceAt.Value - t.position) : mv.vel;
            if (look.sqrMagnitude > .04f) t.rotation = Quaternion.RotateTowards(t.rotation, Quaternion.LookRotation(look), turnDeg * dt);
        }

        /// <summary>Velocidade desejada para chegar a um ponto, freando perto dele.</summary>
        static Vector3 Arrive(Vector3 from, Vector3 to, float maxSpeed, float slowRadius = 1.6f)
        {
            var d = Flat(to - from);
            float dist = d.magnitude;
            if (dist < .05f) return Vector3.zero;
            return d / dist * Mathf.Min(maxSpeed, maxSpeed * dist / slowRadius);
        }

        void CreateFoot()
        {
            var boot = P.boot;
            foot = new GameObject("MeuPe").transform;
            foot.SetParent(A.Root, false);
            var shin = Arena.Prim(PrimitiveType.Capsule, foot, new Vector3(0, -.25f, 0), new Vector3(.13f, .26f, .13f), Arena.Mat(Theme.Hex(m.My.c1)));
            var shoe = Arena.Prim(PrimitiveType.Cube, foot, new Vector3(0, -.52f, .07f), new Vector3(.12f, .09f, .29f), Arena.Mat(Theme.Hex(boot.c1), .5f));
            Arena.Prim(PrimitiveType.Cube, shoe.transform, new Vector3(.51f, .1f, 0), new Vector3(.05f, .35f, .7f), Arena.Mat(Theme.Hex(boot.c2), .5f));
            Arena.Prim(PrimitiveType.Cube, shoe.transform, new Vector3(0, -.55f, 0), new Vector3(1.02f, .15f, 1.02f), Arena.Mat(Theme.Hex(boot.sole), .3f));
            PlaceFoot();
        }

        void PlaceFoot()
        {
            if (foot == null) return;
            var b = A.Ball.transform.position;
            float touch = Time.time - touchAt < .18f ? Mathf.Sin((Time.time - touchAt) / .18f * Mathf.PI) : 0;
            if (Time.time < footCircleUntil)
            {
                // pedalada: o pé dá a volta por cima da bola
                float a = (footCircleUntil - Time.time) / .3f * Mathf.PI * 2f;
                foot.position = b + new Vector3(Mathf.Cos(a) * .22f, .5f + Mathf.Abs(Mathf.Sin(a)) * .08f, -.1f + Mathf.Sin(a) * .12f);
                foot.rotation = Quaternion.Euler(10, Mathf.Cos(a) * 40f, 0);
                return;
            }
            if (carry && kickT < 0)
            {
                // o pé acompanha o corpo e encosta na bola a cada toque
                var toBall = Flat(b - me);
                var fwd = toBall.sqrMagnitude > .01f ? toBall.normalized : Vector3.forward;
                var reach = Mathf.Lerp(.2f, Mathf.Min(toBall.magnitude - .12f, .8f), touch);
                foot.position = me + fwd * reach + new Vector3(.18f, .58f + Mathf.Sin(stepPhase * Mathf.PI * 2f) * .02f, 0);
                foot.rotation = Quaternion.LookRotation(fwd) * Quaternion.Euler(-40f * touch + 12f, 0, 0);
                return;
            }
            foot.position = new Vector3(b.x + .22f, .58f, b.z - .45f);
            float swing = kickT >= 0 ? Mathf.Sin(Mathf.Clamp01(kickT / .22f) * Mathf.PI) : 0;
            foot.rotation = Quaternion.Euler(-70f * swing + 12f, 0, 0);
        }
        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);
        static float HorizDist(Vector3 a, Vector3 b) => Flat(b - a).magnitude;

        void SetBall(Vector3 p)
        {
            A.Ball.isKinematic = true;
            A.Ball.transform.position = new Vector3(p.x, Arena.BallRadius, p.z);
        }

        void PlaceBallAndCamera(Vector3 ball)
        {
            SetBall(ball);
            A.PlaceCamera(ball + new Vector3(0, 1.6f, -2.4f), new Vector3(0, .9f, 0));
        }

        /// <summary>
        /// Lança a bola com física real (arrasto + giro) para passar pelo alvo. spin em rad/s: y = efeito lateral
        /// (positivo curva para +x), x = efeito por cima (positivo faz a bola cair). Devolve o tempo até o alvo.
        /// </summary>
        float Launch(Vector3 start, Vector3 target, float speed, Vector3 spin = default)
        {
            var v = BallPhysics.Solve(start, target, speed, spin, out float time);
            A.Ball.isKinematic = false;
            A.Ball.position = start;
            A.Ball.transform.position = start;
            A.Ball.linearVelocity = v;
            A.Ball.maxAngularVelocity = 120f;
            A.Ball.angularVelocity = spin + new Vector3(R(-2, 2), 0, R(-2, 2));
            ballSpin = spin;
            curveAccel = BallPhysics.LateralAccel(spin, speed);
            prevBall = start;
            return time;
        }

        Vector3 GoalTarget(Ray ray)
        {
            if (ray.direction.z < .01f) return new Vector3(0, 1, 0);
            float t = -ray.origin.z / ray.direction.z;
            var p = ray.origin + ray.direction * t;
            return new Vector3(Mathf.Clamp(p.x, -8, 8), Mathf.Clamp(p.y, .12f, 4.5f), 0);
        }

        // ---------- entrada ----------
        void OnSwipe(SwipeData s)
        {
            if (phase != Phase.Aim || Time.time < introUntil) return;
            if (type == "defesa") { DefenseSwipe(s); return; }
            if (type == "cabeceio") { HeaderSwipe(s); return; }

            var ray = A.Cam.ScreenPointToRay(s.End);
            float power01 = Mathf.Clamp01((s.Length / Mathf.Max(1, Mathf.Max(Screen.width, Screen.height)) / s.Duration - .8f) / 3.2f);

            // o dedo terminou no gramado antes da área do gol e há companheiro: é passe
            if (mate != null && ray.direction.y < 0)
            {
                var gp = ray.origin + ray.direction * (-ray.origin.y / ray.direction.y);
                if (gp.z < -2f) { Pass(gp, power01); return; }
            }
            Shot(GoalTarget(ray), power01, s.Bend, Stat(Attr.Fin), 1f);
        }

        void Shot(Vector3 target, float power01, float bend, float accuracyAttr, float powerScale)
        {
            var start = A.Ball.transform.position;
            float dist = HorizDist(start, target);
            float sigma = Mathf.Max(.05f, (1f - accuracyAttr * .0085f) * (.6f + power01 * .8f) * (dist / 16f));
            target.x += (float)Rng.Gauss() * sigma;
            target.y += (float)Rng.Gauss() * sigma * .7f;
            float speed = Mathf.Lerp(13f, 21f + Stat(Attr.Fis) * .12f, power01) * powerScale;
            float bendAccel = Mathf.Clamp(bend / (Mathf.Min(Screen.width, Screen.height) * .12f), -1f, 1f) * (3f + Stat(Attr.Dri) * .05f);
            carry = false;
            hud.HideControls();
            kickT = 0;
            sfx?.Kick(.6f + power01 * .4f);
            foreach (var bl in blockers) if (wallJump) Rig(bl).Set(PersonRig.Mode.Jump);
            // curva do traço vira efeito lateral; força alta dá um pouco de efeito por cima
            Launch(start, target, speed, new Vector3(power01 * 6f, bendAccel / (BallPhysics.Magnus * Mathf.Max(8f, speed) * .8f), 0));
            shotTime = Time.time;
            phase = Phase.Flight;
            hud.SetHint("");
            hud.ShowNow(false);
            hud.Reticle(false); hud.Timing(false); hud.Power(false);
            gk.OnShot(start, A.Ball.linearVelocity, curveAccel, blockers.Count > 0, null);
        }

        void Pass(Vector3 groundPoint, float power01)
        {
            var start = A.Ball.transform.position;
            float sigma = Mathf.Max(.2f, 1.8f - Stat(Attr.Pas) * .014f);
            var target = new Vector3(groundPoint.x + (float)Rng.Gauss() * sigma, Arena.BallRadius, groundPoint.z + (float)Rng.Gauss() * sigma);
            float dist = HorizDist(start, target);
            float speed = Mathf.Clamp(dist * .9f + 4f, 9f, 20f) * Mathf.Lerp(.9f, 1.15f, power01);
            curveAccel = 0;
            passFlight = true;
            mateTarget = Flat(target);
            carry = false;
            hud.HideControls();
            kickT = 0;
            sfx?.Kick(.45f);
            // passe rasteiro: a bola rola no chão (freia pela grama), não sai pelo alto
            A.Ball.isKinematic = false;
            A.Ball.position = new Vector3(start.x, Arena.BallRadius, start.z);
            A.Ball.linearVelocity = Flat(target - start).normalized * speed;
            ballSpin = Vector3.zero;
            prevBall = start;
            passArrive = Time.time + Mathf.Max(.25f, dist / (speed * .85f));
            shotTime = Time.time;
            phase = Phase.Flight;
            hud.SetHint("");
        }

        void HeaderSwipe(SwipeData s)
        {
            if (!crossLaunched || Time.time < windowOpen - .05f) { Finish(LiveOutcome.Missed, "FORA DO TEMPO"); return; }
            if (Time.time > windowClose) return;
            A.Ball.isKinematic = true;
            A.Ball.transform.position = headPoint;
            var ray = A.Cam.ScreenPointToRay(s.End);
            float accuracy = (Stat(Attr.Fin) + Stat(Attr.Fis)) / 2f;
            Shot(GoalTarget(ray), .5f, 0, accuracy, .72f);
        }

        void DefenseSwipe(SwipeData s)
        {
            float dx = s.End.x - s.Start.x;
            DefenseTry(Mathf.Abs(dx) < 40f ? 0 : (dx > 0 ? 1 : -1) * (A.Cam.transform.right.x >= 0 ? 1 : -1));
        }

        void DefenseTry(int side)
        {
            float d = meZ - attacker.position.z, bonus = Stat(Attr.Def) * .004f;
            bool win;
            if (cutStart < 0) win = d <= 6f && side == cutSide && Rng.Chance(.55 + bonus);
            else win = Time.time - cutStart <= .32f + bonus && side == cutSide && Rng.Chance(.78 + bonus * .5);

            if (win)
            {
                A.Ball.isKinematic = false;
                A.Ball.linearVelocity = new Vector3(-cutSide * 2f, 2.5f, 7f);
                Rig(attacker).Set(PersonRig.Mode.Stumble);
                Finish(LiveOutcome.TackleWon);
            }
            else
            {
                if (cutStart < 0) { cutSide = side == 0 ? cutSide : -side; cutStart = Time.time; }
                attackerSpeed += 1.5f;
                attackerRunsOn = true;
                Finish(LiveOutcome.Beaten);
            }
        }

        // ---------- loop ----------
        void Update()
        {
            if (A == null) return;
            float dt = Time.deltaTime;
            A.FitCamera();
            if (celebrating) return; // a comemoração conduz a cena
            if (kickT >= 0) kickT += dt;
            if (phase == Phase.Aim && Time.time < introUntil) { PlaceFoot(); IntroRun(); return; }
            hud.EndIntro();
            if (!carry) hud.SetSpeed(0);
            UpdateRigs();

            if (mate != null && !mateHasBall)
            {
                // o companheiro arranca, faz a curva da corrida e diminui ao chegar no espaço
                Steer(mate, Arrive(mate.position, mateTarget, mateSpeed), 7f, dt, phase == Phase.Aim ? (Vector3?)null : A.Ball.transform.position);
            }
            else if (mate != null) Steer(mate, Vector3.zero, 9f, dt, A.Ball.transform.position);
            if (marker != null && mate != null)
            {
                // o marcador lê a corrida com atraso e acompanha de lado, entre o atacante e o gol
                var mv = Mv(marker);
                mv.seenBall = Vector3.Lerp(mv.seenBall, mate.position, 1f - Mathf.Exp(-dt / mv.react));
                var spot = mv.seenBall + Flat(-mv.seenBall).normalized * 1.1f;
                Steer(marker, Arrive(marker.position, spot, mateSpeed * .95f, 1.2f), 6.5f, dt, mate.position);
            }

            // fora da condução, quem estava correndo freia com o embalo em vez de congelar
            if (phase != Phase.Aim || !carry)
                foreach (var d in pressers)
                    if (d != null && movers.ContainsKey(d)) Steer(d, Vector3.zero, 6f, dt, A.Ball.transform.position, 240f);

            switch (phase)
            {
                case Phase.Aim:
                    if (carry) CarryStep(dt);
                    if (phase == Phase.Aim) ShootingStep(dt);
                    if (phase == Phase.Aim) SkillHintStep();
                    if (type == "cabeceio" || type == "corte") CrossStep();
                    if (type == "defesa") DefenseStep(dt);
                    if (phase == Phase.Aim && Time.time - startTime > aimTimeout) Finish(LiveOutcome.LostBall, "DEMOROU DEMAIS");
                    break;
                case Phase.Flight:
                    if (foot != null && kickT > .5f) { Destroy(foot.gameObject); foot = null; }
                    else PlaceFootKick();
                    if (passFlight) PassStep();
                    else { gk.Tick(dt); CheckCrossings(); }
                    FollowBall(dt);
                    break;
                case Phase.Watch:
                    FollowBall(dt);
                    break;
                case Phase.Done:
                    KeeperAfter(dt);
                    if (attackerRunsOn) DefenseStep(dt);
                    if (!A.Ball.isKinematic && A.Ball.transform.position.z > 1.7f) A.Ball.linearVelocity *= .8f; // a rede segura a bola
                    if (type != "defesa") FollowBall(dt);
                    break;
            }
        }

        Vector3 footAnchor;
        bool footAnchored;

        void PlaceFootKick()
        {
            if (foot == null) return;
            if (!footAnchored) { footAnchor = foot.position; footAnchored = true; }
            foot.position = footAnchor;
            float swing = Mathf.Sin(Mathf.Clamp01(kickT / .22f) * Mathf.PI);
            foot.rotation = Quaternion.Euler(-70f * swing + 12f, 0, 0);
        }

        void UpdateRigs()
        {
            // a animação (parado, andando, correndo) sai da velocidade real de cada um
            if (mate != null && Rig(mate).mode != PersonRig.Mode.Celebrate) Rig(mate).Set(PersonRig.Mode.Run, mateSpeed);
            if (marker != null) Rig(marker).Set(PersonRig.Mode.Run, 4.2f);
            foreach (var d in pressers)
            {
                var mv = Mv(d);
                bool stunned = stunUntil.TryGetValue(d, out var until) && Time.time < until;
                bool falling = stunned && until - Time.time > .45f;
                bool close = HorizDist(d.position, A.Ball.transform.position) < 4f;
                var mode = mv.lunging || falling ? PersonRig.Mode.Stumble
                    : phase == Phase.Aim && carry && close && !stunned ? PersonRig.Mode.Ready
                    : PersonRig.Mode.Run;
                Rig(d).Set(mode, d == chaser ? chaserSpeed : presserSpeed);
            }
            if (attacker != null && (phase == Phase.Aim || attackerRunsOn)) Rig(attacker).Set(PersonRig.Mode.Run, attackerSpeed);
            if (phase == Phase.Aim) PlaceFoot();
        }

        /// <summary>Arrasto do ar e efeito Magnus enquanto a bola está solta (o mesmo modelo usado para mirar).</summary>
        void FixedUpdate()
        {
            if (A == null || A.Ball.isKinematic) return;
            var v = A.Ball.linearVelocity;
            if (v.sqrMagnitude < .04f) return;
            A.Ball.AddForce(BallPhysics.ExtraAccel(v, ballSpin, A.Ball.position.y), ForceMode.Acceleration);
            var ang = A.Ball.angularVelocity;
            var roll = BallPhysics.Rolling(v, A.Ball.position.y, ref ang);
            if (roll != Vector3.zero) { A.Ball.AddForce(roll, ForceMode.Acceleration); A.Ball.angularVelocity = ang; ballSpin *= .9f; }
            ballSpin *= .996f; // o giro diminui aos poucos
        }

        void FollowBall(float dt)
        {
            var b = A.Ball.transform.position;
            var look = Vector3.Lerp(new Vector3(0, 1.1f, 0), b, .55f);
            var rot = Quaternion.LookRotation(look - A.Cam.transform.position);
            A.Cam.transform.rotation = Quaternion.Slerp(A.Cam.transform.rotation, rot, 3f * dt);
        }

        // ---------- condução com o joystick ----------
        bool CanAct() => phase == Phase.Aim && Time.time >= introUntil && Time.time >= actCd;

        void CarryStep(float dt)
        {
            if (Time.time < introUntil) return;
            Vector2 st = hud.Stick.Value;
            // corre segurando CORRER ou empurrando o joystick até a borda; o fôlego dura uns 6 s de arrancada
            Vector2 stk = hud.Stick.Value;
            bool wantSprint = hud.Sprint.Held || stk.magnitude > .92f || autoRun && stk.y > .7f;
            if (stamina < .02f) sprintTired = true;
            if (sprintTired && stamina > .25f) sprintTired = false;
            bool sprint = wantSprint && !sprintTired;
            stamina = Mathf.Clamp01(stamina + (sprint ? -.16f : stk.sqrMagnitude > .04f ? .09f : .14f) * dt);
            hud.SetStamina(stamina);
            hud.SprintOn(sprint);
            float dri01 = Mathf.Clamp01((Stat(Attr.Dri) - 40f) / 55f);

            // o joystick é relativo à câmera, que sempre olha para o gol: para cima = em direção ao gol
            var want = new Vector3(st.x, 0, st.y);
            if (autoRun) want.z = Mathf.Max(want.z, .45f);
            if (want.sqrMagnitude > 1f) want.Normalize();
            if (sprint)
            {
                // arrancada é sempre no máximo: na direção do joystick ou, sem joystick, reto para o gol
                var toGoalDir = Flat(Vector3.zero - me).normalized;
                want = want.sqrMagnitude > .02f ? want.normalized : toGoalDir;
            }

            var b = Flat(A.Ball.transform.position);
            var toBall = b - me;
            float db = toBall.magnitude;

            // o corpo segue o joystick, mas puxa para a bola quando ela se afasta (ninguém larga a bola de propósito)
            var moveDir = want;
            if (db > .3f && want.sqrMagnitude > .01f)
                moveDir = Vector3.Lerp(want, toBall / db * Mathf.Max(want.magnitude, .7f), Mathf.Clamp01((db - .3f) / .3f));

            float top = carrySpeed * (sprint ? SprintMult : 1f) * (Time.time < burstUntil ? 1.25f : 1f);
            var target = moveDir * top;
            // arranca em meio segundo, freia mais rápido e perde velocidade em curva fechada
            if (carryVel.sqrMagnitude > 1f && target.sqrMagnitude > .01f)
            {
                float ang = Vector3.Angle(carryVel, target);
                if (ang > 50f) target *= Mathf.Lerp(1f, .5f, (ang - 50f) / 130f);
            }
            float acc = target.sqrMagnitude < carryVel.sqrMagnitude ? 12f : (sprint ? 10f : 8.5f) + dri01 * 2f + Stat(Attr.Vel) * .02f;
            carryVel = Vector3.MoveTowards(carryVel, target, acc * dt);
            me += carryVel * dt;
            me.x = Mathf.Clamp(me.x, -26f, 26f);
            me.z = Mathf.Clamp(me.z, -72f, -5.5f);

            // bola: rola na grama e perde velocidade (durante a firula quem move a bola é a animação dela)
            if (skillActive) { b = Flat(A.Ball.transform.position); goto afterBall; }
            ballVel *= Mathf.Exp(-.85f * dt);
            b += ballVel * dt;
            toBall = b - me; db = toBall.magnitude;
            float spd = carryVel.magnitude;
            if (db < .62f && Time.time - lastTouch > .2f && (spd > .7f || want.sqrMagnitude > .04f))
            {
                // toque: empurra a bola para onde o joystick aponta; em velocidade o toque é mais longo
                var dirT = want.sqrMagnitude > .01f ? want.normalized : carryVel.normalized;
                // mudando de direção o toque é curto: só a velocidade que já vai para o novo lado empurra a bola
                // toque na corrida: a bola vai um pouco à frente (na arrancada um pouco mais), sem fugir do pé
                float push = Mathf.Max(Vector3.Dot(carryVel, dirT), 1.8f) * (sprint ? 1.2f : 1.15f) * R(.97f, 1.05f);
                var perp = new Vector3(dirT.z, 0, -dirT.x);
                ballVel = dirT * push + perp * (float)Rng.Gauss() * (sprint ? .45f : .2f) * (1.2f - dri01);
                lastTouch = touchAt = Time.time;
                sfx?.Touch(sprint ? .55f : .35f);
            }
            else if (db < 1.1f && want.sqrMagnitude < .04f && spd < 1.2f)
            {
                // sem joystick: domina a bola nos pés
                ballVel = Vector3.Lerp(ballVel, Vector3.zero, 1f - Mathf.Exp(-8f * dt));
                var hold = me + (toBall.sqrMagnitude > .01f ? toBall.normalized : Vector3.forward) * .5f;
                b = Vector3.Lerp(b, hold, 1f - Mathf.Exp(-5f * dt));
            }
            if (Mathf.Abs(b.x) > 33.5f || b.z < -75f) { Finish(LiveOutcome.LostBall, "BOLA PARA FORA"); return; }
            b.z = Mathf.Min(b.z, -4.5f);
            RollBall(b, dt);
            afterBall:

            CarryCamera(b, sprint, dt);

            // marcadores: leem com atraso, correm com aceleração, acompanham de frente e dão o bote na hora certa
            tackleCd -= dt;
            float nearest = 99;
            bool anyLunge = false;
            var goal = Vector3.zero;
            foreach (var d in pressers)
            {
                if (d == null) continue;
                var mv = Mv(d);
                bool stunned = stunUntil.TryGetValue(d, out var until) && Time.time < until;
                float dist = HorizDist(d.position, b);
                float sp = d == chaser ? chaserSpeed : presserSpeed;
                mv.seenBall = Vector3.Lerp(mv.seenBall, b, 1f - Mathf.Exp(-dt / mv.react));
                mv.seenVel = Vector3.Lerp(mv.seenVel, carryVel, 1f - Mathf.Exp(-dt / mv.react));

                if (stunned)
                {
                    // passou do lance: escorrega com o embalo e só depois se recompõe
                    Steer(d, Vector3.zero, 5f, dt, null, 120f);
                    continue;
                }
                nearest = Mathf.Min(nearest, dist);

                if (mv.lunging)
                {
                    anyLunge = true;
                    Steer(d, mv.lungeDir * (sp * 1.3f + 1.2f), 28f, dt, null, 200f);
                    if (dist < .78f)
                    {
                        mv.lunging = false;
                        float p = Mathf.Clamp(.42f + opp01 * .2f - (Stat(Attr.Dri) - 50) * .006f + (db > 1.2f ? .25f : 0f) - (sprint ? 0 : .05f), .12f, .8f);
                        if (Rng.Chance(p))
                        {
                            // bote dentro da área: às vezes ele chega atrasado e derruba você
                            if (b.z > -16.5f && Mathf.Abs(b.x) < 20.16f && Rng.Chance(.3f + (sprint ? .1f : 0f)))
                            {
                                Rig(d).Set(PersonRig.Mode.Stumble);
                                sfx?.Whistle();
                                Finish(LiveOutcome.PenaltyWon, "PÊNALTI!");
                                return;
                            }
                            ballVel = mv.vel * .8f + new Vector3(R(-1.5f, 1.5f), 0, 0);
                            RollBall(b + ballVel * dt, dt);
                            sfx?.Touch(.6f);
                            Finish(LiveOutcome.LostBall, db > 1.2f ? "TOQUE LONGO DEMAIS" : "DESARMADO");
                            return;
                        }
                        stunUntil[d] = Time.time + .9f; // errou o bote e passou direto
                    }
                    else if (Time.time > mv.lungeUntil) { mv.lunging = false; stunUntil[d] = Time.time + .55f; }
                    continue;
                }

                // bola solta longe do seu pé: quem chegar primeiro leva
                if (!skillActive && dist < .6f && db > 1.5f) { Finish(LiveOutcome.LostBall, "TOQUE LONGO DEMAIS"); return; }

                Vector3 aim, face;
                float speedCap = sp;
                var toGoalB = Flat(goal - mv.seenBall).normalized;
                if (dist > 4.5f)
                {
                    // longe: corre para cortar o caminho, mirando onde a bola vai estar
                    aim = mv.seenBall + mv.seenVel * Mathf.Clamp(dist / Mathf.Max(1f, sp), 0, 1.2f) * .8f;
                    if (d != chaser) aim = Vector3.Lerp(aim, mv.seenBall + toGoalB * 2f, .4f); // zagueiro guarda a posição
                    face = aim;
                }
                else
                {
                    // perto: fica entre a bola e o gol, de frente, recuando no seu ritmo (jockey)
                    aim = mv.seenBall + toGoalB * Mathf.Lerp(1.2f, 1.9f, (dist - 1f) / 3.5f) + mv.seenVel * .25f;
                    speedCap = Mathf.Min(sp, Mathf.Max(mv.seenVel.magnitude * 1.08f, 1.8f));
                    face = b;
                }
                var dv = Arrive(d.position, aim, speedCap, 1f);
                // virar o corpo custa velocidade (só quando corre de frente)
                if (dist > 4.5f && dv.sqrMagnitude > .01f)
                    dv *= Mathf.Lerp(.45f, 1f, (Vector3.Dot(d.forward, dv.normalized) + 1f) * .5f);
                Steer(d, dv, dist > 4.5f ? 6.5f + opp01 * 2f : 9f, dt, face, dist > 4.5f ? 300f : 520f);

                // o bote: perto, de frente e quando ele achar a hora (às vezes antes, às vezes espera)
                if (dist < 1.9f && Time.time > mv.nextLunge && tackleCd <= 0)
                {
                    mv.lunging = true;
                    mv.lungeUntil = Time.time + .38f;
                    mv.lungeDir = Flat(b + ballVel * .2f - d.position).normalized;
                    mv.nextLunge = Time.time + R(1.2f, 2.2f) / mv.aggr;
                    tackleCd = .6f;
                    anyLunge = true;
                }
                else if (dist < 3f && Time.time > mv.nextLunge - .1f && db > 1.2f) mv.nextLunge = Time.time; // toque longo convida o bote
            }
            hud.DribbleReady(nearest < 3.2f || anyLunge);

            // o goleiro sai do gol quando você entra na área, fechando o ângulo (e a torcida levanta)
            if (b.z > -16f)
            {
                A.Crowd?.Excite(.3f, .5f);
                var spot = new Vector3(b.x * .5f, 0, Mathf.Max(b.z + 2.2f, -5f));
                Steer(keeper, Arrive(keeper.position, spot, 3f + opp01 * 1.4f, 1.2f), 9f, dt, b, 360f);
                keeperZ = keeper.position.z;
                if (HorizDist(keeper.position, b) < .95f) Finish(LiveOutcome.Saved, "O GOLEIRO FICOU COM ELA");
            }
            else
            {
                // fora da área, ele acompanha o ângulo da bola em pequenos passos
                var spot = new Vector3(Mathf.Clamp(b.x * .12f, -1.2f, 1.2f), 0, keeperZ);
                Steer(keeper, Arrive(keeper.position, spot, 2f, .8f), 6f, dt, b, 300f);
            }
        }

        /// <summary>Posiciona a bola no chão girando conforme rola.</summary>
        void RollBall(Vector3 p, float dt)
        {
            var t = A.Ball.transform;
            var prev = Flat(t.position);
            SetBall(p);
            var step = Flat(p) - prev;
            float d = step.magnitude;
            if (d > 1e-4f) t.rotation = Quaternion.AngleAxis(d / Arena.BallRadius * Mathf.Rad2Deg, Vector3.Cross(Vector3.up, step / d)) * t.rotation;
        }

        /// <summary>Câmera nos olhos do jogador: segue com leve atraso, balança com a passada e abre ao arrancar.</summary>
        void CarryCamera(Vector3 b, bool sprint, float dt)
        {
            float spd = carryVel.magnitude;
            stepPhase += dt * Mathf.Lerp(1.6f, 3.4f, Mathf.Clamp01(spd / 8.5f)) * (spd > .4f ? 1f : 0f);
            float bobAmt = Mathf.Clamp01(spd / 5f);
            var toGoal = Flat(Vector3.zero - me).normalized;
            var side = new Vector3(toGoal.z, 0, -toGoal.x);
            var eye = me - toGoal * 1.75f + Vector3.up * 1.64f
                + Vector3.up * Mathf.Abs(Mathf.Sin(stepPhase * Mathf.PI)) * .045f * bobAmt
                + side * Mathf.Sin(stepPhase * Mathf.PI) * .025f * bobAmt;
            var look = new Vector3(Mathf.Lerp(b.x, 0, .55f), .9f, Mathf.Lerp(b.z, 0, .75f));
            if (!camInit) { camLook = look; camInit = true; }
            var pos = Vector3.SmoothDamp(A.Cam.transform.position, eye, ref camVel, .1f);
            camLook = Vector3.Lerp(camLook, look, 1f - Mathf.Exp(-4f * dt));
            A.PlaceCamera(pos, camLook);
            fovBoost = Mathf.Lerp(fovBoost, sprint && spd > 4f ? 9f : 0f, 1f - Mathf.Exp(-3f * dt));
            hud.SetSpeed(Mathf.Clamp01((spd - 4f) / 3.5f) * (sprint ? 1f : .35f));
            A.FovBoost = fovBoost;
            // inclina levemente nas curvas
            A.Cam.transform.rotation *= Quaternion.Euler(0, 0, -Vector3.Dot(carryVel, side) * .5f);
        }

        void PassButton()
        {
            if (!CanAct() || mate == null) return;
            if (crossMode) { Cross(); return; }
            var run = Flat(mateTarget - mate.position);
            var lead = Flat(mate.position) + (run.sqrMagnitude > .01f ? run.normalized * Mathf.Min(3.5f, run.magnitude) : Vector3.zero);
            Pass(lead, .5f);
        }

        void DribbleButton()
        {
            if (!CanAct() || !carry) return;
            actCd = Time.time + .45f;
            var b = Flat(A.Ball.transform.position);
            Transform near = null; float nd = 3.2f;
            foreach (var d in pressers)
            {
                if (d == null || (stunUntil.TryGetValue(d, out var u) && Time.time < u)) continue;
                float dist = HorizDist(d.position, b);
                if (dist < nd) { nd = dist; near = d; }
            }
            burstUntil = Time.time + .5f;
            if (near == null) return; // sem marcador perto: só uma arrancada
            // driblar no momento do bote é o certo: ele já se jogou e não volta
            bool timing = Mv(near).lunging;
            float p = Mathf.Clamp(.48f + (Stat(Attr.Dri) - 60) * .011f - opp01 * .18f + (timing ? .3f : 0f), .25f, .93f);
            if (Rng.Chance(p))
            {
                Mv(near).lunging = false;
                stunUntil[near] = Time.time + (timing ? 1.8f : 1.3f);
                // corte para o lado contrário ao marcador: bola e corpo saem juntos
                float away = Mathf.Sign(b.x - near.position.x);
                if (away == 0) away = 1;
                var cut = new Vector3(away * 3.8f, 0, 2.2f);
                ballVel = cut * 1.15f;
                carryVel += cut * .8f;
                lastTouch = touchAt = Time.time;
                sfx?.Touch(.5f);
                sfx?.Cheer(.35f);
                hud.Banner(timing ? "QUE DRIBLE!" : "PASSOU!", Theme.FeedGold);
                AddPoints("Drible", MatchEngine.Pts.Dribble);
                A.Crowd?.Excite(.3f, 1f);
                StartCoroutine(HideBanner(.6f));
            }
            else if (Rng.Chance(.5)) Finish(LiveOutcome.LostBall, "DESARMADO");
            else tackleCd = Mathf.Max(tackleCd, .3f);
        }

        IEnumerator HideBanner(float t)
        {
            yield return new WaitForSeconds(t);
            if (phase == Phase.Aim) hud.EndBanner();
        }

        void TackleButton(int screenSide)
        {
            if (!CanAct() || type != "defesa") return;
            DefenseTry(screenSide * (A.Cam.transform.right.x >= 0 ? 1 : -1));
        }

        void CrossStep()
        {
            if (!crossLaunched && Time.time >= crossLaunchAt)
            {
                crossLaunched = true;
                const float T = 1.15f;
                var start = A.Ball.transform.position;
                // cruzamento com efeito leve fechando na área
                float side = start.x > 0 ? 1 : -1;
                float tt = Launch(start, headPoint, HorizDist(start, headPoint) / T, new Vector3(0, side * R(6, 14), 0));
                float arrive = Time.time + tt;
                windowOpen = arrive - .3f - Stat(Attr.Fis) * .002f;
                windowClose = arrive + .12f;
                headArrive = arrive;
            }
            if (!crossLaunched) return;
            hud.ShowNow(Time.time >= windowOpen && Time.time <= windowClose);
            if (defending) CorteCamera();
            else
            {
                var look = Vector3.Lerp(new Vector3(0, 1.3f, 0), A.Ball.transform.position, .5f);
                A.Cam.transform.rotation = Quaternion.Slerp(A.Cam.transform.rotation, Quaternion.LookRotation(look - A.Cam.transform.position), 4f * Time.deltaTime);
            }
            // contato na hora em que a bola chega; a qualidade é o pico do salto casando com a chegada
            if (jumpQueued && !headDone && Time.time >= headArrive - .015f)
            {
                headDone = true;
                float peak = jumpAt + .28f;
                float q = Mathf.Clamp01(1f - Mathf.Abs(peak - headArrive) / .32f);
                if (q < .12f)
                {
                    if (defending) AttackerHeads(peak < headArrive ? "SUBIU CEDO DEMAIS" : "SUBIU ATRASADO");
                    else Finish(LiveOutcome.Missed, peak < headArrive ? "SUBIU CEDO DEMAIS" : "SUBIU ATRASADO");
                }
                else if (defending) CorteContact(q);
                else HeaderContact(q);
                return;
            }
            if (Time.time > windowClose + .15f && !headDone)
            {
                if (defending) AttackerHeads("A BOLA PASSOU POR VOCÊ");
                else Finish(LiveOutcome.Missed, "A BOLA PASSOU");
            }
        }

        void DefenseStep(float dt)
        {
            var ap = attacker.position;
            float d = meZ - ap.z;
            // vem na sua direção (pode ser na diagonal) e corta para um lado perto de você
            var toMe = Flat(defSpot + Vector3.forward * 1.5f - ap); toMe = toMe.sqrMagnitude > .01f ? toMe.normalized : Vector3.forward;
            if (toMe.z < .5f) toMe = new Vector3(toMe.x, 0, .5f).normalized;
            var vel = cutStart > 0 ? new Vector3(cutSide * 4.5f, 0, attackerSpeed * .8f) : toMe * attackerSpeed;
            var amv = Mv(attacker);
            amv.vel = Vector3.MoveTowards(amv.vel, vel, 14f * dt); // o corte tem arranque, não é um teletransporte
            ap += amv.vel * dt;
            attacker.position = ap;
            if (phase != Phase.Done) SetBall(ap + new Vector3(cutStart > 0 ? cutSide * .3f : 0, 0, .7f));
            if (phase == Phase.Done) return;

            if (d <= 5.6f && cutStart < 0) // o corpo denuncia o lado do drible
                attacker.rotation = Quaternion.Slerp(attacker.rotation, Quaternion.Euler(0, 0, -cutSide * 14f), 8f * dt);
            if (cutStart < 0 && d <= 4.2f) cutStart = Time.time;
            if (ap.z > meZ + .6f) { attackerRunsOn = true; Finish(LiveOutcome.Beaten); }
        }

        void PassStep()
        {
            if (Time.time < passArrive) return;
            var bp = A.Ball.transform.position;
            if (HorizDist(mate.position, bp) < 2.4f)
            {
                mateHasBall = true;
                passFlight = false;
                AddPoints(crossPass ? "Cruzamento" : "Passe certo", MatchEngine.Pts.Pass);
                Mv(mate).vel *= .15f; // domina e para em cima da bola
                A.Ball.isKinematic = true;
                A.Ball.transform.position = Flat(mate.position) + mate.forward * .6f + Vector3.up * (crossPass ? 1.85f : Arena.BallRadius);
                if (crossPass) Rig(mate).Set(PersonRig.Mode.Jump);
                phase = Phase.Watch;
                // tabela: o companheiro devolve de primeira no espaço à sua frente (só uma vez por lance)
                bool tabela = !crossPass && !tabelaDone && (type == "meio" ? Rng.Chance(.65) : Rng.Chance(.35));
                if (tabela) StartCoroutine(ReturnPass());
                else StartCoroutine(MateShoot());
            }
            else Finish(LiveOutcome.PassIntercepted);
        }

        bool tabelaDone;

        /// <summary>Tabela: o companheiro devolve rasteiro na frente do jogador, que volta a conduzir e decide.</summary>
        IEnumerator ReturnPass()
        {
            tabelaDone = true;
            yield return new WaitForSeconds(.4f);
            if (phase != Phase.Watch) yield break;
            mateHasBall = false;
            var from = Flat(mate.position) + mate.forward * .6f;
            var toGoal = Flat(Vector3.zero - me).normalized;
            var spot = me + toGoal * R(4.5f, 6.5f) + new Vector3(toGoal.z, 0, -toGoal.x) * R(-1.5f, 1.5f);
            spot.z = Mathf.Min(spot.z, -7f);
            A.Ball.isKinematic = true;
            SetBall(from);
            ballVel = Flat(spot - from).normalized * Mathf.Clamp(HorizDist(from, spot) * 1.1f, 7f, 15f);
            lastTouch = Time.time;
            sfx?.Touch(.5f);
            carry = true; phase = Phase.Aim; passFlight = false;
            startTime = Time.time; aimTimeout = Mathf.Max(aimTimeout, 10f);
            hud.Controls(true, true, false, pressers.Count > 0, true, false);
            hud.Finesse.gameObject.SetActive(true);
            hud.Banner("TABELA!", Theme.FeedGold);
            StartCoroutine(HideBanner(.6f));
            AddPoints("Tabela", 3);
            mateTarget = Flat(mate.position) + toGoal * 6f; // ele segue para a área
        }

        IEnumerator MateShoot()
        {
            yield return new WaitForSeconds(.35f);
            if (phase != Phase.Watch) yield break;
            float sigma = Mathf.Max(.3f, 1.3f - (m.My.str - 50) * .02f) * (crossPass ? 1.3f : 1f); // de cabeça é menos preciso
            float sideSign = Rng.Chance(.5) ? 1 : -1;
            var target = new Vector3(sideSign * R(1.6f, 3.2f) + (float)Rng.Gauss() * sigma, R(.3f, 2f) + (float)Rng.Gauss() * sigma * .6f, 0);
            curveAccel = 0;
            mateShotFlight = true;
            Launch(A.Ball.transform.position, target, crossPass ? 15f : 22f);
            sfx?.Kick(.6f);
            gk.OnShot(A.Ball.transform.position, A.Ball.linearVelocity, 0, false, null);
            shotTime = Time.time;
            phase = Phase.Flight;
        }

        // ---------- goleiro e checagens ----------
        static Vector3 Cross(Vector3 a, Vector3 b, float z)
        {
            float t = Mathf.Abs(b.z - a.z) < 1e-4f ? 1 : (z - a.z) / (b.z - a.z);
            return Vector3.Lerp(a, b, Mathf.Clamp01(t));
        }

        void Deflect(float zFactor)
        {
            var v = A.Ball.linearVelocity;
            A.Ball.linearVelocity = new Vector3(v.x * .4f + R(-3, 3), Mathf.Abs(v.y) * .5f + 1.5f, v.z * zFactor);
            ballSpin = new Vector3(R(-15, 15), R(-15, 15), 0);
            sfx?.Touch(.8f);
        }

        void CheckCrossings()
        {
            var b = A.Ball.transform.position;
            foreach (var d in blockers)
            {
                float z = d.position.z;
                if (prevBall.z < z && b.z >= z)
                {
                    var c = Cross(prevBall, b, z);
                    if (Mathf.Abs(c.x - d.position.x) < .42f && c.y < (wallJump ? 2.15f : 1.9f))
                    {
                        Deflect(-.35f);
                        Finish(LiveOutcome.Blocked);
                        return;
                    }
                }
            }
            if (KeeperCheck(prevBall, b)) return;
            if (prevBall.z < 0 && b.z >= 0)
            {
                var c = Cross(prevBall, b, 0);
                bool inside = Mathf.Abs(c.x) < Arena.GoalHalfWidth - Arena.BallRadius && c.y < Arena.GoalHeight - Arena.BallRadius;
                if (inside) StartCoroutine(BallIntoNet(c, A.Ball.linearVelocity));
                Finish(inside ? LiveOutcome.Goal : LiveOutcome.Missed);
                return;
            }
            float t = Time.time - shotTime;
            if (t > 4f || (t > .6f && A.Ball.linearVelocity.z < -.5f)) { Finish(LiveOutcome.Missed); return; }
            prevBall = b;
        }

        // ---------- fim do lance ----------
        void Finish(LiveOutcome o, string customText = null)
        {
            if (phase == Phase.Done) return;
            if (mateShotFlight) o = o == LiveOutcome.Goal ? LiveOutcome.Assist : LiveOutcome.TeammateMissed;
            phase = Phase.Done;
            charging = false;
            hud.Power(false); hud.Reticle(false); hud.Timing(false); hud.Curve(false); hud.SetSpeed(0);
            hud.HideControls();
            hud.DribbleReady(false);
            hud.ShowNow(false);
            hud.SetHint("");
            hud.Banner(customText ?? Label(o), Good(o) ? Theme.FeedGold : Color.white);
            // os pontos do resultado entram na nota quando o lance volta para a partida; aqui só aparecem
            var (pl, pp) = MatchEngine.OutcomePoints(o == LiveOutcome.Goal && mateShotFlight ? LiveOutcome.Assist : o, type);
            if (pp != 0) hud.Popup((pp > 0 ? "+" : "") + pp + " " + pl.ToUpperInvariant(), pp > 0 ? Theme.Turf : Theme.Red);
            if (o == LiveOutcome.LostBall || o == LiveOutcome.PassIntercepted) sfx?.Boo();
            // a arquibancada reage: gol explode, chance perdida levanta todo mundo
            if (o == LiveOutcome.Goal || o == LiveOutcome.Assist) A.Crowd?.Excite(defending ? .3f : 1f, 5f);
            else if (o == LiveOutcome.Saved || o == LiveOutcome.Missed || o == LiveOutcome.Blocked || o == LiveOutcome.TeammateMissed) A.Crowd?.Excite(.55f, 1.8f);
            else if (o == LiveOutcome.PenaltyWon || o == LiveOutcome.TackleWon || o == LiveOutcome.Cleared) A.Crowd?.Excite(.45f, 1.6f);
            if (Good(o)) sfx?.Cheer(o == LiveOutcome.TackleWon ? .5f : 1f);
            else if (o == LiveOutcome.Saved || o == LiveOutcome.Missed || o == LiveOutcome.Blocked || o == LiveOutcome.TeammateMissed) sfx?.Groan();
            if (o == LiveOutcome.Goal || o == LiveOutcome.Assist) { sfx?.Whistle(); Sfx.Play(Sfx.Kind.Net, .8f); GameSettings.Buzz(); }
            if (o == LiveOutcome.Goal || o == LiveOutcome.Assist)
            {
                if (mate != null) Rig(mate).Set(PersonRig.Mode.Celebrate);
                StartCoroutine(Shake(.35f, .06f));
            }
            StartCoroutine(End(o));
        }

        static bool Good(LiveOutcome o) => o == LiveOutcome.Goal || o == LiveOutcome.Assist || o == LiveOutcome.TackleWon || o == LiveOutcome.PenaltyWon || o == LiveOutcome.Cleared;

        static string Label(LiveOutcome o)
        {
            switch (o)
            {
                case LiveOutcome.Goal: return "GOOOL!";
                case LiveOutcome.Assist: return "GOL! ASSISTÊNCIA SUA";
                case LiveOutcome.Saved: return "DEFENDEU!";
                case LiveOutcome.Blocked: return "BLOQUEADO";
                case LiveOutcome.TeammateMissed: return "O COMPANHEIRO PERDEU";
                case LiveOutcome.PassIntercepted: return "PASSE ERRADO";
                case LiveOutcome.LostBall: return "PERDEU A BOLA";
                case LiveOutcome.TackleWon: return "DESARMOU!";
                case LiveOutcome.Beaten: return "ELE PASSOU";
                case LiveOutcome.PenaltyWon: return "PÊNALTI!";
                case LiveOutcome.Cleared: return "CORTOU!";
                default: return "PARA FORA";
            }
        }

        /// <summary>
        /// Gol: a bola entra freando, estica a rede em volta dela, cai lá dentro e fica (sem quicar de volta para o campo).
        /// </summary>
        IEnumerator BallIntoNet(Vector3 entry, Vector3 vel)
        {
            const float Back = 2.0f; // fundo da rede
            A.Ball.isKinematic = true;
            float vz = Mathf.Max(4f, vel.z);
            float push = Mathf.Clamp(vel.magnitude * .022f, .22f, .62f);       // quanto a rede estufa
            var end = new Vector3(Mathf.Clamp(entry.x + vel.x / vz * Back, -3.3f, 3.3f), Mathf.Clamp(entry.y + vel.y / vz * Back * .5f, .25f, 2.0f), Back + push);
            float t1 = Mathf.Clamp(Back / vz, .07f, .3f) + .16f;
            var spin = Vector3.Cross(Vector3.up, vel.normalized) * vel.magnitude * 40f;
            float t0 = Time.time;
            while (Time.time - t0 < t1 && A != null)
            {
                float u = (Time.time - t0) / t1, e = 1f - (1f - u) * (1f - u) * (1f - u); // freia ao bater na rede
                var p = Vector3.Lerp(entry, end, e);
                A.Ball.transform.position = p;
                A.Ball.transform.Rotate(spin * Time.deltaTime, Space.World);
                if (p.z > Back - .25f) A.NearNet?.Hold(new Vector3(p.x, p.y, Back), p.z - (Back - .25f));
                yield return null;
            }
            // a rede devolve um pouco, solta a bola e balança; a bola cai e rola até parar lá dentro
            A?.NearNet?.Release();
            var drop = new Vector3(end.x * .95f, Arena.BallRadius, Back - .45f);
            var from = end;
            t0 = Time.time;
            while (Time.time - t0 < .55f && A != null)
            {
                float u = (Time.time - t0) / .55f;
                var p = Vector3.Lerp(from, drop, u);
                p.y = Mathf.Lerp(from.y, Arena.BallRadius, u * u) + Mathf.Sin(u * Mathf.PI) * .08f;
                A.Ball.transform.position = p;
                yield return null;
            }
            for (float t = 0; t < .5f && A != null; t += Time.deltaTime)
            {
                // quiquezinho final
                var p = drop + new Vector3(0, Mathf.Abs(Mathf.Sin(t * 14f)) * .12f * (1 - t / .5f), -t * .3f);
                A.Ball.transform.position = p;
                yield return null;
            }
        }

        IEnumerator Shake(float duration, float amount)
        {
            float t = 0;
            while (t < duration && A != null)
            {
                t += Time.deltaTime;
                A.Cam.transform.position += new Vector3(R(-amount, amount), R(-amount, amount), 0);
                yield return null;
            }
        }

        IEnumerator End(LiveOutcome o)
        {
            // gol seu: comemoração em terceira pessoa (o passe para o gol do companheiro só tem o grito)
            if (o == LiveOutcome.Goal && !defending) yield return Celebration();
            else yield return new WaitForSeconds(1.9f);
            var cb = onDone;
            onDone = null;
            cb?.Invoke(o);
        }

        void OnDestroy()
        {
            if (A != null) A.Destroy();
            A = null;
        }
    }
}
