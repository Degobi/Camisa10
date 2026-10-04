using System;
using System.Collections.Generic;

namespace Camisa10.Core
{
    public enum FeedKind { Info, TeamGoal, OppGoal, Me }
    public enum StepResult { Continue, AwaitChoice, Finished }

    /// <summary>Resultado de um lance jogado em primeira pessoa (cena 3D).</summary>
    public enum LiveOutcome { Goal, Saved, Missed, Blocked, Assist, TeammateMissed, PassIntercepted, LostBall, TackleWon, Beaten }

    public class FeedLine { public string Text; public FeedKind Kind; }

    public class MomentOption
    {
        public string Label, Kind; // goal | assist | follow | safe | tackle | contain | foul
        public double Base, K;
        public (Attr attr, float weight)[] Skill;
    }

    public class Moment { public string Type, Text; public MomentOption[] Options; }

    /// <summary>
    /// Simula uma partida em lances. O time joga sozinho (gols por Poisson) e,
    /// nos minutos em que o jogador participa, o jogo para e espera a decisão dele.
    /// </summary>
    public class MatchEngine
    {
        struct Ev { public int m; public char t; } // g=gol nosso, a=gol deles, i=entrada, m=lance

        readonly Game g;
        readonly List<Ev> queue = new List<Ev>();
        Player P => g.S.player;

        public Club My, Opp;
        public bool Home, Plays, Yellow, Sent, Done, GoalFlash;
        public Role Role;
        public int Gf, Ga, Goals, Assists, Minute;
        public float Rating = 6f;
        public char Result;
        public Moment Current;
        public readonly List<FeedLine> Feed = new List<FeedLine>();

        public MatchEngine(Game game)
        {
            g = game;
            var f = g.CurrentFixture();
            My = g.MyClub; Opp = f.opp; Home = f.home;
            Role = g.S.season.role == Role.None ? g.ComputeRole() : g.S.season.role;

            int inMin = -1, n = 0;
            if (Role == Role.Titular) { inMin = 0; n = 3; }
            else if (Role == Role.Estrela) { inMin = 0; n = 4; }
            else if (Role == Role.Reserva && Rng.Chance(.6)) { inMin = Rng.RangeInt(56, 74); n = Rng.Chance(.5) ? 2 : 1; }
            Plays = inMin >= 0;

            double lt = 1.3 * Math.Pow((double)My.str / Opp.str, 2.5) * (Home ? 1.12 : .9);
            double lo = 1.3 * Math.Pow((double)Opp.str / My.str, 2.5) * (Home ? .9 : 1.12);
            if (Plays)
            {
                double share = (90 - inMin) / 90.0;
                lt *= 1 - .25 * share;
                if (g.IsDefensive) lo *= 1 - .2 * share;
            }
            for (int i = Rng.Poisson(lt); i > 0; i--) queue.Add(new Ev { m = Rng.RangeInt(1, 90), t = 'g' });
            for (int i = Rng.Poisson(lo); i > 0; i--) queue.Add(new Ev { m = Rng.RangeInt(1, 90), t = 'a' });
            if (Plays)
            {
                if (inMin > 0) queue.Add(new Ev { m = inMin, t = 'i' });
                var mins = new HashSet<int>();
                while (mins.Count < n) mins.Add(Rng.RangeInt(inMin + 3, 89));
                foreach (int mm in mins) queue.Add(new Ev { m = mm, t = 'm' });
            }
            queue.Sort((a, b) => a.m != b.m ? a.m.CompareTo(b.m) : a.t == 'i' ? -1 : b.t == 'i' ? 1 : 0);

            Add(Home ? $"Apita o árbitro. {My.name} recebe o {Opp.name}." : $"Apita o árbitro. {My.name} visita o {Opp.name}.", FeedKind.Info);
            if (Role == Role.Reserva) Add(Plays ? "Você começa no banco." : "Você começa no banco e aguarda uma chance.", FeedKind.Info);
            if (Role == Role.Lesionado) Add("Você acompanha da tribuna, ainda em recuperação.", FeedKind.Info);
            if (Role == Role.Poupado) Add("Você foi poupado e assiste ao jogo do banco.", FeedKind.Info);
        }

        void Add(string text, FeedKind kind) => Feed.Add(new FeedLine { Text = text, Kind = kind });

        public StepResult Step()
        {
            if (Done) return StepResult.Finished;
            if (Current != null) return StepResult.AwaitChoice;
            GoalFlash = false;
            while (true)
            {
                if (queue.Count == 0) { End(); return StepResult.Finished; }
                var e = queue[0];
                queue.RemoveAt(0);
                Minute = e.m;
                switch (e.t)
                {
                    case 'g': Gf++; GoalFlash = true; Add($"{e.m}' Gol do {My.name}! {GameData.Outfield(My.name)} marca.", FeedKind.TeamGoal); return StepResult.Continue;
                    case 'a': Ga++; Add($"{e.m}' Gol do {Opp.name}. {GameData.Outfield(Opp.name)} marca.", FeedKind.OppGoal); return StepResult.Continue;
                    case 'i': Add($"{e.m}' Substituição: você entra em campo.", FeedKind.Me); return StepResult.Continue;
                    default:
                        if (Sent) continue;
                        Current = Build(Rng.Weighted(GameData.Positions[P.pos].Moments));
                        return StepResult.AwaitChoice;
                }
            }
        }

        public double Prob(MomentOption o)
        {
            if (o.Kind == "foul") return o.Base;
            double skill = 0;
            foreach (var s in o.Skill) skill += s.weight * P.Get(s.attr);
            double diff = skill - (Opp.str - 4) + (Home ? 2 : 0) + (P.energy - 60) * .05 + (P.moral - 50) * .04;
            return Game.Clamp(o.Base + diff * o.K, .05, .85);
        }

        static readonly string[] GoalTexts = { "Chute colocado no ângulo!", "Bomba no canto, sem chance para o goleiro!", "Por baixo das pernas do goleiro!", "Cavadinha de categoria!", "Chute cruzado, rede balançando!" };
        static readonly string[] MissTexts = { "O goleiro defende.", "Para fora, por pouco.", "Na trave!", "O zagueiro trava o chute." };

        /// <summary>Resolve a escolha. Retorna true quando a jogada continua em um novo lance (ex.: cara a cara).</summary>
        public bool Choose(int index)
        {
            if (Current == null) return false;
            var mo = Current; var opt = mo.Options[index];
            bool ok = Rng.Chance(Prob(opt));
            int m = Minute;
            Current = null; GoalFlash = false;
            switch (opt.Kind)
            {
                case "goal":
                    if (ok) { Gf++; Goals++; Rating += 1.1f; GoalFlash = true; Add($"{m}' GOL SEU! {(mo.Type == "cabeceio" ? "Cabeçada firme no canto!" : Rng.Pick(GoalTexts))}", FeedKind.TeamGoal); }
                    else { Rating -= .15f; Add($"{m}' {Rng.Pick(MissTexts)}", FeedKind.Me); }
                    break;
                case "assist":
                    if (ok) { Gf++; Assists++; Rating += .7f; GoalFlash = true; Add($"{m}' Gol do {My.name}! Assistência sua para {GameData.Outfield(My.name)}.", FeedKind.TeamGoal); }
                    else if (Rng.Chance(.5)) { Rating -= .25f; Add($"{m}' O passe é interceptado.", FeedKind.Me); }
                    else { Rating += .1f; Add($"{m}' Boa jogada sua, mas o companheiro desperdiça.", FeedKind.Me); }
                    break;
                case "follow":
                    if (ok) { Rating += .3f; Add($"{m}' Você passa pela marcação!", FeedKind.Me); Current = Build("cara"); return true; }
                    Rating -= .3f; Add($"{m}' Você perde a bola.", FeedKind.Me);
                    break;
                case "safe":
                    if (ok) { Rating += .1f; Add($"{m}' Bola segura, o time respira.", FeedKind.Me); }
                    else { Rating -= .15f; Add($"{m}' Você é desarmado.", FeedKind.Me); }
                    break;
                case "tackle":
                    if (ok) { Rating += .5f; Add($"{m}' Desarme limpo!", FeedKind.Me); }
                    else
                    {
                        Rating -= .4f;
                        if (Rng.Chance(.45)) { Ga++; Add($"{m}' {GameData.Outfield(Opp.name)} passa por você e marca. Gol do {Opp.name}.", FeedKind.OppGoal); }
                        else Add($"{m}' Ele passa, mas o goleiro salva.", FeedKind.Me);
                    }
                    break;
                case "contain":
                    if (ok) { Rating += .3f; Add($"{m}' Você fecha o espaço e a jogada morre.", FeedKind.Me); }
                    else
                    {
                        Rating -= .3f;
                        if (Rng.Chance(.3)) { Ga++; Add($"{m}' A bola passa e o {Opp.name} marca.", FeedKind.OppGoal); }
                        else Add($"{m}' Ele finaliza, mas para fora.", FeedKind.Me);
                    }
                    break;
                case "foul":
                    if (Rng.Chance(.5))
                    {
                        if (Yellow) { Sent = true; Rating -= 1.5f; Add($"{m}' Segundo amarelo. Você está expulso.", FeedKind.OppGoal); }
                        else { Yellow = true; Rating -= .2f; Add($"{m}' Falta e cartão amarelo.", FeedKind.Me); }
                    }
                    else { Rating += .1f; Add($"{m}' Falta feita, jogada parada.", FeedKind.Me); }
                    break;
            }
            return false;
        }

        /// <summary>Aplica o resultado de um lance jogado na cena 3D.</summary>
        public void ResolveLive(LiveOutcome o)
        {
            if (Current == null) return;
            var type = Current.Type;
            int m = Minute;
            Current = null; GoalFlash = false;
            switch (o)
            {
                case LiveOutcome.Goal:
                    Gf++; Goals++; Rating += 1.1f; GoalFlash = true;
                    Add($"{m}' GOL SEU! {(type == "cabeceio" ? "Cabeçada firme no canto!" : type == "falta" ? "Cobrança perfeita!" : Rng.Pick(GoalTexts))}", FeedKind.TeamGoal);
                    break;
                case LiveOutcome.Assist:
                    Gf++; Assists++; Rating += .7f; GoalFlash = true;
                    Add($"{m}' Gol do {My.name}! Assistência sua para {GameData.Outfield(My.name)}.", FeedKind.TeamGoal);
                    break;
                case LiveOutcome.Saved: Rating -= .05f; Add($"{m}' Grande defesa de {GameData.Keeper(Opp.name)}, do {Opp.name}.", FeedKind.Me); break;
                case LiveOutcome.Missed: Rating -= .2f; Add($"{m}' {(type == "cabeceio" ? "A cabeçada sai sem direção." : "O chute vai para fora.")}", FeedKind.Me); break;
                case LiveOutcome.Blocked: Rating -= .1f; Add($"{m}' A defesa bloqueia o chute.", FeedKind.Me); break;
                case LiveOutcome.TeammateMissed: Rating += .15f; Add($"{m}' Belo passe seu, mas o companheiro desperdiça.", FeedKind.Me); break;
                case LiveOutcome.PassIntercepted: Rating -= .25f; Add($"{m}' O passe não chega.", FeedKind.Me); break;
                case LiveOutcome.LostBall: Rating -= .3f; Add($"{m}' Você perde a bola.", FeedKind.Me); break;
                case LiveOutcome.TackleWon: Rating += .5f; Add($"{m}' Desarme limpo!", FeedKind.Me); break;
                case LiveOutcome.Beaten:
                    Rating -= .4f;
                    if (Rng.Chance(.45)) { Ga++; Add($"{m}' {GameData.Outfield(Opp.name)} passa por você e marca. Gol do {Opp.name}.", FeedKind.OppGoal); }
                    else Add($"{m}' Ele passa por você, mas o goleiro salva.", FeedKind.Me);
                    break;
            }
        }

        void End()
        {
            Result = Gf > Ga ? 'w' : Gf < Ga ? 'l' : 'd';
            Minute = 90;
            if (Plays)
            {
                Rating += Result == 'w' ? .3f : Result == 'l' ? -.3f : 0f;
                if (Ga == 0 && g.IsDefensive) Rating += .4f;
                Rating = (float)Game.Clamp(Math.Round(Rating * 10) / 10.0, 3, 10);
            }
            Add($"90' Fim de jogo: {My.name} {Gf} x {Ga} {Opp.name}.", FeedKind.Info);
            Done = true; GoalFlash = false;
        }

        // ---------- catálogo de lances ----------
        static MomentOption O(string label, string kind, double b, double k, params (Attr, float)[] skill) =>
            new MomentOption { Label = label, Kind = kind, Base = b, K = k, Skill = skill };

        static readonly Dictionary<string, (string[] texts, MomentOption[] options)> Defs = new Dictionary<string, (string[], MomentOption[])>
        {
            ["chance"] = (new[] { "Você recebe na entrada da área, de frente para o gol.", "A bola sobra para você dentro da área.", "Cruzamento rasteiro chega até você na marca do pênalti." },
                new[] { O("Chutar", "goal", .28, .011, (Attr.Fin, 1f)), O("Driblar o zagueiro", "follow", .45, .014, (Attr.Dri, .6f), (Attr.Vel, .4f)), O("Tocar para o companheiro", "assist", .35, .012, (Attr.Pas, 1f)) }),
            ["meio"] = (new[] { "Você domina no meio-campo com espaço para pensar.", "A bola chega em você na intermediária e o adversário está recuado." },
                new[] { O("Lançamento longo", "assist", .26, .012, (Attr.Pas, 1f)), O("Tabelar", "follow", .42, .018, (Attr.Pas, .5f), (Attr.Dri, .5f)), O("Segurar e girar o jogo", "safe", .85, .01, (Attr.Fis, .6f), (Attr.Pas, .4f)) }),
            ["contra"] = (new[] { "Contra-ataque! Você puxa a bola com o campo aberto.", "Roubada de bola e você sai em velocidade pelo lado." },
                new[] { O("Arrancar em velocidade", "follow", .45, .014, (Attr.Vel, .7f), (Attr.Dri, .3f)), O("Chutar de longe", "goal", .1, .008, (Attr.Fin, 1f)), O("Passe em profundidade", "assist", .32, .012, (Attr.Pas, 1f)) }),
            ["falta"] = (new[] { "Falta perigosa perto da área. A bola é sua." },
                new[] { O("Bater direto", "goal", .12, .008, (Attr.Fin, 1f)), O("Cruzar na área", "assist", .25, .012, (Attr.Pas, 1f)) }),
            ["defesa"] = (new[] { "O atacante adversário avança em velocidade pelo seu setor.", "Bola enfiada nas costas da defesa. Você é o último homem.", "O meia deles tenta a jogada individual na sua frente." },
                new[] { O("Dar o bote", "tackle", .5, .02, (Attr.Def, .8f), (Attr.Fis, .2f)), O("Acompanhar e fechar o espaço", "contain", .6, .018, (Attr.Def, .5f), (Attr.Vel, .5f)), O("Fazer falta tática", "foul", .9, 0, (Attr.Fis, 1f)) }),
            ["cabeceio"] = (new[] { "Escanteio a favor. Você sobe para a área.", "Falta lateral levantada na área. Você ataca a bola." },
                new[] { O("Cabecear para o gol", "goal", .2, .01, (Attr.Fis, .5f), (Attr.Fin, .5f)), O("Escorar para o meio", "assist", .3, .012, (Attr.Pas, 1f)) }),
            ["cara"] = (new[] { "Você fica cara a cara com o goleiro!" },
                new[] { O("Chutar forte", "goal", .4, .012, (Attr.Fin, 1f)), O("Driblar o goleiro", "goal", .34, .012, (Attr.Dri, 1f)), O("Rolar para o companheiro", "assist", .55, .01, (Attr.Pas, 1f)) }),
        };

        static Moment Build(string type)
        {
            var d = Defs[type];
            return new Moment { Type = type, Text = Rng.Pick(d.texts), Options = d.options };
        }
    }
}
