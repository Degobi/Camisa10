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

        // contra-ataque
        bool running;
        float runSpeed, chaserSpeed;
        Transform chaser;

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
                    aimTimeout = 6;
                    hint = "Cara a cara com o goleiro! Deslize para chutar e mire nos cantos.";
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
                    hint = "Falta! Curve o dedo para a bola contornar a barreira.";
                    break;
                }
                case "contra":
                {
                    var b = new Vector3(R(-8, 8), 0, -34);
                    PlaceBallAndCamera(b);
                    running = true;
                    runSpeed = 5.6f + Stat(Attr.Vel) * .035f;
                    float side = Rng.Chance(.5) ? 1 : -1;
                    chaser = A.Person("Zagueiro", oppShirt, oppShorts, b + new Vector3(side * 5, 0, -1), 0);
                    chaserSpeed = 6.4f + opp01 * 1.6f;
                    aimTimeout = 99;
                    hint = "Contra-ataque! Você está correndo com a bola. Chute antes que o zagueiro chegue.";
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
                    hint = "Deslize até o companheiro que está correndo para lançar. Se preferir, arrisque o chute de longe.";
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
                    hint = "Escanteio! Quando aparecer AGORA, deslize em direção ao gol para cabecear.";
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
                    hint = "Ele vem para cima de você. Leia o corpo dele e deslize para o lado em que ele cortar, na hora certa.";
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
                    hint = "Deslize em direção ao gol para chutar, ou para o companheiro para passar. Curve o dedo para dar efeito.";
                    break;
                }
            }
            hud.SetHint(hint);
            hud.Pad.OnSwipe = OnSwipe;
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
            running = false;
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
            int side = Mathf.Abs(dx) < 40f ? 0 : (dx > 0 ? 1 : -1) * (A.Cam.transform.right.x >= 0 ? 1 : -1);
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
                    if (running) RunStep(dt);
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
            if (chaser != null) Rig(chaser).Set(phase == Phase.Aim ? PersonRig.Mode.Run : PersonRig.Mode.Idle, chaserSpeed);
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

        void RunStep(float dt)
        {
            var b = A.Ball.transform.position;
            var dir = Flat(new Vector3(b.x * .6f, 0, -8f) - b).normalized;
            if (b.z < -9.5f) { b += dir * runSpeed * dt; SetBall(b); }
            var eye = b - dir * 2f + Vector3.up * (1.6f + Mathf.Sin(Time.time * 12f) * .04f);
            A.PlaceCamera(eye, new Vector3(0, .9f, 0));

            chaser.position = Vector3.MoveTowards(chaser.position, Flat(b), chaserSpeed * dt);
            Face(chaser, b);
            if (HorizDist(chaser.position, b) < 1f) { Finish(LiveOutcome.LostBall); return; }

            if (b.z > -17f)
            {
                keeper.position = Vector3.MoveTowards(keeper.position, new Vector3(b.x * .45f, 0, -3.5f), 3f * dt);
                keeperZ = keeper.position.z;
                if (HorizDist(keeper.position, b) < 1.3f) Finish(LiveOutcome.Saved, "O GOLEIRO FICOU COM ELA");
            }
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
