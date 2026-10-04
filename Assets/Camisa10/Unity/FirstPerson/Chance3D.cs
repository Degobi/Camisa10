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
    public class Chance3D : MonoBehaviour
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

        // goleiro
        Transform keeper;
        float keeperZ = -.6f, kReact, kSpeed, kMax, kStartX, kTargetX, kTargetY;
        bool kCommitted;

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
        Vector3 carryVel;
        Transform chaser;
        readonly List<Transform> pressers = new List<Transform>();
        readonly Dictionary<Transform, float> stunUntil = new Dictionary<Transform, float>();

        // cabeceio
        Vector3 headPoint;
        bool crossLaunched;
        float crossLaunchAt, windowOpen = -1, windowClose = -1;

        // pé do jogador em primeira pessoa (mostra a chuteira personalizada)
        Transform foot;
        float kickT = -1;
        float introUntil;

        // desarme
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

            var boards = new[] { m.My.c1, "#F2C230", m.Opp.c1, "#E53935", "#1E88E5", "#111111", m.My.c2 };
            A = Arena.Build(transform, boards);
            A.OnView = t => hud.SetView(t);
            hud.SetView(A.View);
            Debug.Log($"[Camisa 10] Lance 3D: câmera {(A.Cam.enabled ? "ligada" : "desligada")}, imagem {A.View.width}x{A.View.height}, " +
                $"{UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length} objetos visíveis na cena.");

            Color oppShirt = Theme.Hex(m.Opp.c1), oppShorts = Theme.Hex(m.Opp.c2);
            Color myShirt = Theme.Hex(m.My.c1), myShorts = Theme.Hex(m.My.c2);

            keeper = A.Person("Goleiro", Theme.Hex("#C6E03A"), Theme.Hex("#222222"), new Vector3(0, 0, keeperZ), 180, true);
            Rig(keeper).Set(PersonRig.Mode.Ready);
            kReact = Mathf.Lerp(.34f, .16f, opp01);
            kSpeed = Mathf.Lerp(3.2f, 4.6f, opp01);
            kMax = Mathf.Lerp(2.1f, 2.7f, opp01);

            hud.SetTop($"{m.Minute}'   {m.My.name} {m.Gf} x {m.Ga} {m.Opp.name}");
            string hint;
            switch (type)
            {
                case "cara":
                {
                    var b = new Vector3(R(-3, 3), 0, -R(9, 12));
                    PlaceBallAndCamera(b);
                    keeperZ = -2.6f;
                    keeper.position = new Vector3(b.x * .3f, 0, keeperZ);
                    kMax += .3f;
                    carry = true; carrySpeed = 3.4f + Stat(Attr.Vel) * .02f; aimTimeout = 12;
                    hint = "Cara a cara! Conduza com o joystick, toque em CHUTAR ou deslize o dedo para mirar no canto.";
                    break;
                }
                case "falta":
                {
                    var b = new Vector3(R(-7, 7), 0, -R(20, 26));
                    PlaceBallAndCamera(b);
                    Vector3 dir = (Vector3.zero - b).normalized, perp = new Vector3(dir.z, 0, -dir.x), wallC = b + dir * 9.15f;
                    float nearSide = b.x >= 0 ? 1 : -1;
                    for (int i = 0; i < 4; i++)
                        blockers.Add(A.Person("Barreira", oppShirt, oppShorts, wallC + perp * ((i - 1.5f) * .55f) + new Vector3(nearSide * .6f, 0, 0), 180));
                    wallJump = true;
                    keeper.position = new Vector3(-nearSide * .7f, 0, keeperZ);
                    hint = "Falta! Deslize curvando o dedo para contornar a barreira, ou toque em CHUTAR.";
                    break;
                }
                case "contra":
                {
                    var b = new Vector3(R(-8, 8), 0, -34);
                    PlaceBallAndCamera(b);
                    carry = true; autoRun = true;
                    carrySpeed = 4.8f + Stat(Attr.Vel) * .03f;
                    float side = Rng.Chance(.5) ? 1 : -1;
                    chaser = A.Person("Zagueiro", oppShirt, oppShorts, b + new Vector3(side * 6, 0, -2.5f), 0);
                    chaserSpeed = carrySpeed * (.92f + opp01 * .12f);
                    pressers.Add(chaser);
                    aimTimeout = 20;
                    hint = "Contra-ataque! Segure CORRER para fugir do zagueiro e use DRIBLE quando ele chegar perto.";
                    break;
                }
                case "meio":
                {
                    var b = new Vector3(R(-8, 8), 0, -36);
                    PlaceBallAndCamera(b);
                    float side = Rng.Chance(.5) ? 1 : -1;
                    mate = A.Person("Companheiro", myShirt, myShorts, new Vector3(side * R(6, 12), 0, -26), 0);
                    mateTarget = new Vector3(side * R(1, 5), 0, -11);
                    mateSpeed = 5.5f;
                    marker = A.Person("Zagueiro", oppShirt, oppShorts, mate.position + new Vector3(-side * 2, 0, 2), 180);
                    blockers.Add(A.Person("Zagueiro", oppShirt, oppShorts, new Vector3(b.x * .5f + R(-2, 2), 0, -22), 180));
                    pressers.Add(blockers[blockers.Count - 1]);
                    carry = true; carrySpeed = 3.6f + Stat(Attr.Vel) * .02f; aimTimeout = 16;
                    hint = "Toque em PASSE para lançar o companheiro, ou conduza e arrisque o chute.";
                    break;
                }
                case "cabeceio":
                {
                    headPoint = new Vector3(R(-2.5f, 2.5f), 1.85f, -R(7, 10));
                    A.PlaceCamera(new Vector3(headPoint.x, 1.75f, headPoint.z - 1.2f), new Vector3(0, 1.3f, 0));
                    float side = Rng.Chance(.5) ? 1 : -1;
                    SetBall(new Vector3(side * 33.5f, 0, -.5f));
                    crossLaunchAt = Time.time + .8f;
                    blockers.Add(A.Person("Zagueiro", oppShirt, oppShorts, new Vector3(headPoint.x + side * .9f, 0, headPoint.z + .8f), 180));
                    aimTimeout = 99;
                    hint = "Escanteio! Quando aparecer AGORA, toque em CABECEAR ou deslize para o canto.";
                    break;
                }
                case "defesa":
                {
                    var me = new Vector3(R(-4, 4), 0, -14);
                    meZ = me.z;
                    A.PlaceCamera(me + new Vector3(0, 1.7f, 0), me + new Vector3(0, 1f, -10));
                    attacker = A.Person("Atacante", oppShirt, oppShorts, me + new Vector3(R(-1.5f, 1.5f), 0, -16), 0);
                    SetBall(attacker.position + new Vector3(0, 0, .7f));
                    attackerSpeed = 5.5f + opp01 * 1.5f;
                    cutSide = Rng.Chance(.5) ? 1 : -1;
                    aimTimeout = 99;
                    hint = "Ele vem para cima de você. Leia o corpo dele e toque em DESARME do lado em que ele cortar.";
                    break;
                }
                default: // "chance" e qualquer outro tipo de finalização
                {
                    var b = new Vector3(R(-7, 7), 0, -R(15, 19));
                    PlaceBallAndCamera(b);
                    blockers.Add(A.Person("Zagueiro", oppShirt, oppShorts,
                        Vector3.Lerp(b, new Vector3(0, 0, -.5f), .28f) + new Vector3(R(-1.2f, 1.2f), 0, 0), 180));
                    float side = b.x > 0 ? -1 : 1;
                    mate = A.Person("Companheiro", myShirt, myShorts, new Vector3(b.x + side * R(7, 10), 0, b.z + 2), 0);
                    mateTarget = new Vector3(side * R(2, 5), 0, -8);
                    mateSpeed = 4.5f;
                    marker = A.Person("Zagueiro", oppShirt, oppShorts, mate.position + new Vector3(-side * 1.5f, 0, 1.5f), 180);
                    pressers.Add(blockers[0]);
                    carry = true; carrySpeed = 3.4f + Stat(Attr.Vel) * .02f; aimTimeout = 14;
                    hint = "Conduza com o joystick e drible o zagueiro. CHUTAR ou deslize o dedo para mirar; PASSE para o companheiro.";
                    break;
                }
            }
            presserSpeed = 2.8f + opp01 * 1.3f;
            hud.SetHint(hint);
            hud.Pad.OnSwipe = OnSwipe;
            hud.Shoot.OnPress = ShootButton;
            hud.PassBtn.OnPress = PassButton;
            hud.Dribble.OnPress = DribbleButton;
            hud.TackleL.OnPress = () => TackleButton(-1);
            hud.TackleR.OnPress = () => TackleButton(1);
            hud.Controls(carry, type != "defesa", mate != null, pressers.Count > 0, carry, type == "defesa");
            if (type == "cabeceio") hud.Shoot.GetComponentInChildren<UnityEngine.UI.Text>().text = "CABECEAR";
            foreach (var bl in blockers) Rig(bl).Set(wallJump ? PersonRig.Mode.Idle : PersonRig.Mode.Ready);
            if (type != "defesa" && type != "cabeceio") CreateFoot();

            // apresentação do lance antes de liberar o controle
            introUntil = Time.time + 1.4f;
            hud.Intro($"{m.Minute}'  {m.Current.Text}");
            startTime = introUntil;
            prevBall = A.Ball.transform.position;
        }

        // ---------- utilidades ----------
        static float R(float a, float b) => (float)Rng.RangeF(a, b);
        static PersonRig Rig(Transform t) => t != null ? t.GetComponent<PersonRig>() : null;

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

        static void Face(Transform t, Vector3 target)
        {
            var d = Flat(target - t.position);
            if (d.sqrMagnitude > .01f) t.rotation = Quaternion.LookRotation(d);
        }

        void Launch(Vector3 start, Vector3 target, float speed)
        {
            float T = Mathf.Max(.25f, HorizDist(start, target) / Mathf.Max(1f, speed));
            var v = new Vector3((target.x - start.x) / T, (target.y - start.y) / T + .5f * Gravity * T, (target.z - start.z) / T);
            A.Ball.isKinematic = false;
            A.Ball.position = start;
            A.Ball.linearVelocity = v;
            A.Ball.angularVelocity = new Vector3(R(-10, 10), -curveAccel * 3f, 0);
            kCommitted = false;
            prevBall = A.Ball.transform.position;
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
            curveAccel = Mathf.Clamp(bend / (Mathf.Min(Screen.width, Screen.height) * .12f), -1f, 1f) * (3f + Stat(Attr.Dri) * .05f);
            carry = false;
            hud.HideControls();
            kickT = 0;
            foreach (var bl in blockers) if (wallJump) Rig(bl).Set(PersonRig.Mode.Jump);
            Launch(start, target, speed);
            shotTime = Time.time;
            phase = Phase.Flight;
            hud.SetHint("");
            hud.ShowNow(false);
        }

        void Pass(Vector3 groundPoint, float power01)
        {
            var start = A.Ball.transform.position;
            float sigma = Mathf.Max(.2f, 1.8f - Stat(Attr.Pas) * .014f);
            var target = new Vector3(groundPoint.x + (float)Rng.Gauss() * sigma, Arena.BallRadius, groundPoint.z + (float)Rng.Gauss() * sigma);
            float speed = Mathf.Lerp(14f, 22f, power01);
            curveAccel = 0;
            passFlight = true;
            mateTarget = Flat(target);
            passArrive = Time.time + Mathf.Max(.25f, HorizDist(start, target) / speed);
            carry = false;
            hud.HideControls();
            kickT = 0;
            Launch(start, target, speed);
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
            if (kickT >= 0) kickT += dt;
            if (phase == Phase.Aim && Time.time < introUntil) { PlaceFoot(); return; }
            hud.EndIntro();
            UpdateRigs();

            if (mate != null && !mateHasBall)
            {
                mate.position = Vector3.MoveTowards(mate.position, mateTarget, mateSpeed * dt);
                Face(mate, mateTarget);
            }
            if (marker != null && mate != null)
            {
                marker.position = Vector3.MoveTowards(marker.position, mate.position + new Vector3(0, 0, 1.2f), 4.2f * dt);
                Face(marker, mate.position);
            }

            switch (phase)
            {
                case Phase.Aim:
                    if (carry) CarryStep(dt);
                    if (type == "cabeceio") CrossStep();
                    if (type == "defesa") DefenseStep(dt);
                    if (phase == Phase.Aim && Time.time - startTime > aimTimeout) Finish(LiveOutcome.LostBall, "DEMOROU DEMAIS");
                    break;
                case Phase.Flight:
                    if (foot != null && kickT > .5f) { Destroy(foot.gameObject); foot = null; }
                    else PlaceFootKick();
                    if (passFlight) PassStep();
                    else { KeeperUpdate(dt); CheckCrossings(); }
                    FollowBall(dt);
                    break;
                case Phase.Watch:
                    FollowBall(dt);
                    break;
                case Phase.Done:
                    if (attackerRunsOn) DefenseStep(dt);
                    if (!A.Ball.isKinematic && A.Ball.transform.position.z > 1.7f) A.Ball.linearVelocity *= .9f; // a rede segura a bola
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
            if (mate != null) Rig(mate).Set(mateHasBall || HorizDist(mate.position, mateTarget) < .05f ? PersonRig.Mode.Idle : PersonRig.Mode.Run, mateSpeed);
            if (marker != null) Rig(marker).Set(PersonRig.Mode.Run, 4.2f);
            foreach (var d in pressers)
            {
                bool stunned = stunUntil.TryGetValue(d, out var until) && Time.time < until;
                Rig(d).Set(stunned ? PersonRig.Mode.Stumble : phase == Phase.Aim && carry ? PersonRig.Mode.Run : PersonRig.Mode.Ready, d == chaser ? chaserSpeed : presserSpeed);
            }
            if (attacker != null && (phase == Phase.Aim || attackerRunsOn)) Rig(attacker).Set(PersonRig.Mode.Run, attackerSpeed);
            if (phase == Phase.Aim) PlaceFoot();
        }

        void FixedUpdate()
        {
            if (A == null || phase != Phase.Flight || passFlight || curveAccel == 0) return;
            if (A.Ball.transform.position.y > .15f) A.Ball.AddForce(new Vector3(curveAccel, 0, 0), ForceMode.Acceleration);
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
            var b = Flat(A.Ball.transform.position);
            Vector2 st = hud.Stick.Value;
            bool sprint = hud.Sprint.Held && stamina > .02f;
            stamina = Mathf.Clamp01(stamina + (sprint ? -.42f : .16f) * dt);
            hud.SetStamina(stamina);

            // o joystick é relativo à câmera, que sempre olha para o gol: para cima = em direção ao gol
            var want = new Vector3(st.x, 0, st.y);
            if (autoRun) want.z = Mathf.Max(want.z, .45f);
            if (want.sqrMagnitude > 1f) want.Normalize();
            float speed = carrySpeed * (sprint ? 1.4f : 1f) * (Time.time < burstUntil ? 1.35f : 1f);
            carryVel = Vector3.Lerp(carryVel, want * speed, 1f - Mathf.Exp(-7f * dt));
            b += carryVel * dt;
            b.x = Mathf.Clamp(b.x, -26f, 26f);
            b.z = Mathf.Clamp(b.z, -45f, -5.5f);

            // a bola vai alguns passos à frente, como na condução de verdade
            float touch = carryVel.magnitude > .5f ? Mathf.Abs(Mathf.Sin(Time.time * 7f)) * .35f : 0;
            var ballPos = b + (carryVel.sqrMagnitude > .01f ? carryVel.normalized : Vector3.forward) * (.15f + touch);
            SetBall(ballPos);
            var toGoal = Flat(new Vector3(0, 0, 0) - b).normalized;
            var eye = b - toGoal * 2.3f + Vector3.up * (1.62f + (carryVel.magnitude > 1 ? Mathf.Sin(Time.time * 12f) * .03f : 0));
            A.PlaceCamera(eye, new Vector3(b.x * .35f, .9f, 0));

            // marcadores: correm para cortar o caminho; o carrinho tem chance, não é certeiro
            tackleCd -= dt;
            float nearest = 99;
            foreach (var d in pressers)
            {
                if (d == null) continue;
                bool stunned = stunUntil.TryGetValue(d, out var until) && Time.time < until;
                float dist = HorizDist(d.position, b);
                if (!stunned) nearest = Mathf.Min(nearest, dist);
                if (stunned) continue;
                var target = b + carryVel * .35f;
                float sp = d == chaser ? chaserSpeed : presserSpeed;
                d.position = Vector3.MoveTowards(d.position, Flat(target), sp * dt);
                Face(d, b);
                if (dist < 1.05f && tackleCd <= 0)
                {
                    tackleCd = .9f;
                    float p = Mathf.Clamp(.4f + opp01 * .2f - (Stat(Attr.Dri) - 50) * .006f - (sprint ? 0 : .05f), .12f, .65f);
                    if (Rng.Chance(p)) { Finish(LiveOutcome.LostBall, "DESARMADO"); return; }
                    stunUntil[d] = Time.time + .7f; // errou o bote
                }
            }
            hud.DribbleReady(nearest < 3.2f);

            // o goleiro sai do gol quando você entra na área
            if (b.z > -16f)
            {
                keeper.position = Vector3.MoveTowards(keeper.position, new Vector3(b.x * .5f, 0, Mathf.Max(b.z + 2.2f, -5f)), (2.6f + opp01 * 1.2f) * dt);
                Face(keeper, b);
                keeperZ = keeper.position.z;
                if (HorizDist(keeper.position, b) < 1.15f) Finish(LiveOutcome.Saved, "O GOLEIRO FICOU COM ELA");
            }
        }

        void ShootButton()
        {
            if (!CanAct()) return;
            if (type == "cabeceio") { HeaderButton(); return; }
            if (type == "defesa") return;
            float kx = keeper.position.x;
            float side = Mathf.Abs(kx) < .3f ? (Rng.Chance(.5) ? 1 : -1) : (kx > 0 ? -1 : 1);
            var target = new Vector3(side * R(2.2f, 3.0f), R(.35f, 1.7f), 0);
            float bend = 0;
            if (type == "falta")
            {
                // por cima e com efeito para dentro, contornando a barreira
                target = new Vector3(side * R(3.2f, 4f), R(1.7f, 2.2f), 0);
                bend = -side * Mathf.Min(Screen.width, Screen.height) * .08f;
            }
            Shot(target, .62f, bend, Stat(Attr.Fin), 1f);
        }

        void HeaderButton()
        {
            if (!crossLaunched || Time.time < windowOpen - .05f) { Finish(LiveOutcome.Missed, "FORA DO TEMPO"); return; }
            if (Time.time > windowClose) return;
            A.Ball.isKinematic = true;
            A.Ball.transform.position = headPoint;
            float side = keeper.position.x > 0 ? -1 : 1;
            Shot(new Vector3(side * R(2f, 3f), R(.3f, 1.2f), 0), .5f, 0, (Stat(Attr.Fin) + Stat(Attr.Fis)) / 2f, .72f);
        }

        void PassButton()
        {
            if (!CanAct() || mate == null) return;
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
            float p = Mathf.Clamp(.52f + (Stat(Attr.Dri) - 60) * .011f - opp01 * .18f, .25f, .88f);
            if (Rng.Chance(p))
            {
                stunUntil[near] = Time.time + 1.6f;
                // corte para o lado contrário ao marcador
                float away = Mathf.Sign(b.x - near.position.x);
                if (away == 0) away = 1;
                carryVel += new Vector3(away * 4.5f, 0, 1.5f);
                hud.Banner("PASSOU!", Theme.FeedGold);
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
                Launch(start, headPoint, HorizDist(start, headPoint) / T);
                float arrive = Time.time + T;
                windowOpen = arrive - .3f - Stat(Attr.Fis) * .002f;
                windowClose = arrive + .12f;
            }
            if (!crossLaunched) return;
            hud.ShowNow(Time.time >= windowOpen && Time.time <= windowClose);
            var look = Vector3.Lerp(new Vector3(0, 1.3f, 0), A.Ball.transform.position, .5f);
            A.Cam.transform.rotation = Quaternion.Slerp(A.Cam.transform.rotation, Quaternion.LookRotation(look - A.Cam.transform.position), 4f * Time.deltaTime);
            if (Time.time > windowClose + .15f) Finish(LiveOutcome.Missed, "A BOLA PASSOU");
        }

        void DefenseStep(float dt)
        {
            var ap = attacker.position;
            float d = meZ - ap.z;
            var vel = cutStart > 0 ? new Vector3(cutSide * 4.5f, 0, attackerSpeed * .8f) : new Vector3(0, 0, attackerSpeed);
            ap += vel * dt;
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
                A.Ball.isKinematic = true;
                A.Ball.transform.position = Flat(mate.position) + mate.forward * .6f + Vector3.up * Arena.BallRadius;
                phase = Phase.Watch;
                StartCoroutine(MateShoot());
            }
            else Finish(LiveOutcome.PassIntercepted);
        }

        IEnumerator MateShoot()
        {
            yield return new WaitForSeconds(.35f);
            if (phase != Phase.Watch) yield break;
            float sigma = Mathf.Max(.3f, 1.3f - (m.My.str - 50) * .02f);
            float sideSign = Rng.Chance(.5) ? 1 : -1;
            var target = new Vector3(sideSign * R(1.6f, 3.2f) + (float)Rng.Gauss() * sigma, R(.3f, 2f) + (float)Rng.Gauss() * sigma * .6f, 0);
            curveAccel = 0;
            mateShotFlight = true;
            Launch(A.Ball.transform.position, target, 22f);
            shotTime = Time.time;
            phase = Phase.Flight;
        }

        // ---------- goleiro e checagens ----------
        Vector3 Predict(float zPlane)
        {
            var p = A.Ball.transform.position; var v = A.Ball.linearVelocity;
            float t = (zPlane - p.z) / Mathf.Max(.1f, v.z);
            return new Vector3(p.x + v.x * t + .5f * curveAccel * t * t * .35f, p.y + v.y * t - .5f * Gravity * t * t, zPlane);
        }

        void KeeperUpdate(float dt)
        {
            if (!kCommitted && Time.time - shotTime >= kReact && A.Ball.linearVelocity.z > .5f)
            {
                var p = Predict(keeperZ);
                kStartX = keeper.position.x;
                kTargetX = Mathf.Clamp(p.x, kStartX - kMax, kStartX + kMax);
                kTargetY = p.y;
                kCommitted = true;
            }
            if (!kCommitted) return;
            var kp = keeper.position;
            kp.x = Mathf.MoveTowards(kp.x, kTargetX, kSpeed * dt);
            float dive = Mathf.Clamp((kTargetX - kStartX) / Mathf.Max(.1f, kMax), -1f, 1f);
            float lift = Mathf.Clamp(kTargetY - 1.3f, 0, .7f);
            kp.y = Mathf.MoveTowards(kp.y, lift, 2f * dt);
            keeper.position = kp;
            keeper.rotation = Quaternion.Slerp(keeper.rotation, Quaternion.Euler(0, 180, dive * 65f), 6f * dt);
            if (Mathf.Abs(dive) > .3f) Rig(keeper).Set(PersonRig.Mode.Dive);
        }

        static Vector3 Cross(Vector3 a, Vector3 b, float z)
        {
            float t = Mathf.Abs(b.z - a.z) < 1e-4f ? 1 : (z - a.z) / (b.z - a.z);
            return Vector3.Lerp(a, b, Mathf.Clamp01(t));
        }

        void Deflect(float zFactor)
        {
            var v = A.Ball.linearVelocity;
            A.Ball.linearVelocity = new Vector3(v.x * .4f + R(-3, 3), Mathf.Abs(v.y) * .5f + 1.5f, v.z * zFactor);
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
            if (prevBall.z < keeperZ && b.z >= keeperZ)
            {
                var c = Cross(prevBall, b, keeperZ);
                var k = keeper.position;
                float reach = .55f + (Mathf.Abs(kTargetX - kStartX) > .6f ? .45f : 0f);
                if (Mathf.Abs(c.x - k.x) <= reach && c.y <= 2.3f + k.y)
                {
                    Deflect(-.25f);
                    Finish(LiveOutcome.Saved);
                    return;
                }
            }
            if (prevBall.z < 0 && b.z >= 0)
            {
                var c = Cross(prevBall, b, 0);
                bool inside = Mathf.Abs(c.x) < Arena.GoalHalfWidth - Arena.BallRadius && c.y < Arena.GoalHeight - Arena.BallRadius;
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
            hud.HideControls();
            hud.DribbleReady(false);
            hud.ShowNow(false);
            hud.SetHint("");
            hud.Banner(customText ?? Label(o), Good(o) ? Theme.FeedGold : Color.white);
            if (o == LiveOutcome.Goal || o == LiveOutcome.Assist)
            {
                if (mate != null) Rig(mate).Set(PersonRig.Mode.Celebrate);
                StartCoroutine(Shake(.35f, .06f));
            }
            StartCoroutine(End(o));
        }

        static bool Good(LiveOutcome o) => o == LiveOutcome.Goal || o == LiveOutcome.Assist || o == LiveOutcome.TackleWon;

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
                default: return "PARA FORA";
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
            yield return new WaitForSeconds(1.9f);
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
