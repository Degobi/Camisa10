using System;
using System.Collections.Generic;
using System.Linq;
using Camisa10.Core;

// Uso: dotnet run -- [carreiras] [semente]
// Joga carreiras inteiras com resultados de lance sorteados e confere se as regras se comportam.
static class Program
{
    static readonly string[] Positions = { "ATA", "MEI", "VOL", "ZAG", "LAT" };

    static int Main(string[] args)
    {
        int n = args.Length > 0 ? int.Parse(args[0]) : 3;
        int seed = args.Length > 1 ? int.Parse(args[1]) : 42;
        Rng.Seed(seed);
        int errors = 0;
        var allRatings = new List<float>();
        int wcPlayed = 0, wcWon = 0, events = 0, penalties = 0, foreign = 0, actions = 0, mails = 0, replies = 0, captains = 0, press = 0, motm = 0;
        var leaguesSeen = new Dictionary<string, int>();

        for (int k = 0; k < n; k++)
        {
            string pos = Positions[k % Positions.Length];
            var g = Game.NewCareer("Simulado " + (k + 1), pos, new[] { 4, 4, 3, 3, 3, 3 });
            int seasons = 0;
            var moneyBySeason = new List<long>(); var netBySeason = new List<long>(); var objBySeason = new List<string>();
            try
            {
                while (!g.S.retired && seasons < 22)
                {
                    // temporada
                    while (g.S.season.phase != "end")
                    {
                        if (g.S.season.phase == "train")
                        {
                            if (g.S.player.injury > 0) g.Physio();
                            else g.Train(Game.MainAttr(g.S.player.pos), "normal", out _);
                            // agenda: até 3 ações possíveis, sorteadas
                            var acts = BusinessData.Actions.Select(a => a.Id).Where(id => g.CanDoAction(id, out _)).ToList();
                            Rng.Shuffle(acts);
                            foreach (var id in acts.Take(3)) if (g.CanDoAction(id, out _)) { g.DoAction(id); actions++; }
                            // compra o bem mais barato que ainda não tem quando sobra bastante dinheiro
                            var item = GameData.Items.Where(x => !g.S.owned.Contains(x.Id)).OrderBy(x => x.Price).FirstOrDefault();
                            if (item != null && g.S.player.money > item.Price * 3) g.Buy(item.Id);
                            // caixa de mensagens: responde o que pede resposta
                            foreach (var msg in g.S.inbox.Where(x => x.NeedsReply).ToList())
                                if (g.Reply(msg.id, Rng.RangeInt(0, msg.options.Count - 1)) != null) replies++;
                            if (Rng.Chance(.42))
                            {
                                var ev = GameEvents.Roll(g);
                                if (ev != null) { GameEvents.Resolve(g, ev, Rng.RangeInt(0, ev.Options.Length - 1)); events++; }
                            }
                            continue;
                        }
                        var m = new MatchEngine(g);
                        Play(m, ref penalties);
                        if (m.Plays) allRatings.Add(m.Rating);
                        if (m.Motm) motm++;
                        g.FinishMatch(m);
                        // entrevista coletiva em metade dos jogos, com respostas sorteadas
                        if ((m.Plays || m.Role == Role.Reserva) && Rng.Chance(.5))
                            foreach (var q in PressConference.Build(g, m)) { PressConference.Answer(g, q, Rng.RangeInt(0, q.Answers.Length - 1)); press++; }
                    }
                    // Copa do Mundo (anos de Copa)
                    if (g.WorldCupActive) wcPlayed++;
                    while (g.WorldCupActive)
                    {
                        var wm = g.NewWorldCupMatch();
                        Play(wm, ref penalties);
                        g.FinishWorldCupMatch(wm);
                    }
                    if (g.S.wc != null && g.S.wc.champion && g.S.wc.year == g.S.year) wcWon++;
                    seasons++;
                    objBySeason.Add($"{g.S.season.objMet}/{g.S.season.objectives.Count}");
                    moneyBySeason.Add(g.S.player.money);
                    netBySeason.Add(g.S.seasonNet);
                    var lg = g.MyClub.league;
                    leaguesSeen[lg] = leaguesSeen.TryGetValue(lg, out var c) ? c + 1 : 1;
                    if (lg != "br") foreign++;
                    if (g.MustRetire) break;
                    // contrato: aceita a melhor proposta (ou a primeira) quando o contrato acaba ou aparece liga melhor
                    if (!g.CanStartNextSeason || (g.S.offers.Count > 0 && Rng.Chance(.4)))
                    {
                        var best = g.S.offers.OrderByDescending(o => o.salary).FirstOrDefault();
                        if (best != null) g.AcceptOffer(best.id);
                    }
                    if (!g.CanStartNextSeason) break;
                    g.NextSeason();
                }
            }
            catch (Exception e)
            {
                errors++;
                Console.WriteLine($"ERRO na carreira {k + 1} ({pos}), temporada {seasons}: {e}");
            }
            var s = g.S;
            mails += s.inbox.Count; if (s.achievements.Contains("capitao")) captains++;
            Console.WriteLine($"Carreira {k + 1} ({pos}): {seasons} temporadas, {g.CareerApps} jogos, {g.CareerGoals} gols, geral {g.Ovr}, idade {s.player.age}, " +
                $"Seleção {s.caps} jogos/{s.intlGoals} gols, Copas {s.worldCups}, títulos {s.titles.Count}: {string.Join("; ", s.titles.Take(6))}");
            Console.WriteLine($"   Clubes: {string.Join(" → ", s.career.Select(r => r.club).Distinct())}");
            Console.WriteLine($"   Dinheiro no fim de cada temporada: {string.Join(" | ", moneyBySeason.Select(x => Fmt.Money(x)))}");
            Console.WriteLine($"   Objetivos cumpridos: {string.Join(" ", objBySeason)}");
            Console.WriteLine($"   Saldo de cada temporada: {string.Join(" | ", netBySeason.Select(x => Fmt.Money(x)))}");
            Console.WriteLine($"   Bens: {string.Join(", ", s.owned)}. Vida: {g.LoveLine()}. Rival {s.rival.name}: {s.rival.careerGoals} gols na carreira, duelos {s.rival.won}/{s.rival.duels}.");
            Console.WriteLine($"   Reputação: técnico {s.player.coach:0}, elenco {s.player.squad:0}, torcida {s.player.fans:0}{(s.captain ? ", capitão" : "")}. Mensagens na caixa: {s.inbox.Count}.");
        }
        Console.WriteLine();
        Console.WriteLine($"Notas: média {allRatings.DefaultIfEmpty(0).Average():0.00}, mín {allRatings.DefaultIfEmpty(0).Min():0.0}, máx {allRatings.DefaultIfEmpty(0).Max():0.0} ({allRatings.Count} jogos)");
        Console.WriteLine($"Distribuição: <6: {allRatings.Count(r => r < 6) * 100 / Math.Max(1, allRatings.Count)}%  6-7: {allRatings.Count(r => r >= 6 && r < 7) * 100 / Math.Max(1, allRatings.Count)}%  7-8: {allRatings.Count(r => r >= 7 && r < 8) * 100 / Math.Max(1, allRatings.Count)}%  8+: {allRatings.Count(r => r >= 8) * 100 / Math.Max(1, allRatings.Count)}%");
        Console.WriteLine($"Copas do Mundo disputadas: {wcPlayed}, vencidas: {wcWon}. Eventos: {events}. Ações da agenda: {actions}. Pênaltis no jogo: {penalties}.");
        Console.WriteLine($"Craque do jogo: {motm} vezes ({motm * 100 / Math.Max(1, allRatings.Count)}% dos jogos). Perguntas de coletiva respondidas: {press}.");
        Console.WriteLine($"Respostas a mensagens: {replies}. Carreiras que chegaram a capitão: {captains} de {n}.");
        Console.WriteLine($"Temporadas por liga: {string.Join(", ", leaguesSeen.Select(x => x.Key + "=" + x.Value))}. Fora do Brasil: {foreign}.");
        Console.WriteLine(errors == 0 ? "OK: nenhuma exceção." : $"{errors} carreira(s) com erro.");
        return errors == 0 ? 0 : 1;
    }

    /// <summary>Joga a partida: nos lances sorteia o resultado (como se o jogador estivesse jogando) e algumas ações no meio.</summary>
    static void Play(MatchEngine m, ref int penalties)
    {
        int guard = 0;
        while (!m.Done && guard++ < 200)
        {
            var r = m.Step();
            if (r != StepResult.AwaitChoice || m.Current == null) continue;
            string type = m.Current.Type;
            if (type == "penalti") penalties++;
            // ações dentro do lance: dribles, firulas e passes
            if (type != "defesa" && type != "penalti" && type != "falta" && type != "cabeceio")
            {
                if (Rng.Chance(.45)) m.Score("Drible", MatchEngine.Pts.Dribble);
                if (Rng.Chance(.15)) m.Score("Elástico", MatchEngine.Pts.SkillHard);
                if (Rng.Chance(.3)) m.Score("Passe certo", MatchEngine.Pts.Pass);
            }
            LiveOutcome o;
            switch (type)
            {
                case "defesa": o = Rng.Chance(.5) ? LiveOutcome.TackleWon : LiveOutcome.Beaten; break;
                case "corte": o = Rng.Chance(.6) ? LiveOutcome.Cleared : LiveOutcome.Beaten; break;
                case "penalti": o = Rng.Chance(.72) ? LiveOutcome.Goal : Rng.Chance(.6) ? LiveOutcome.Saved : LiveOutcome.Missed; break;
                default:
                    double x = Rng.Value;
                    o = x < .28 ? LiveOutcome.Goal : x < .45 ? LiveOutcome.Saved : x < .58 ? LiveOutcome.Missed : x < .65 ? LiveOutcome.Blocked
                        : x < .72 ? LiveOutcome.Assist : x < .78 ? LiveOutcome.PassIntercepted : x < .82 ? LiveOutcome.PenaltyWon : LiveOutcome.LostBall;
                    break;
            }
            m.ResolveLive(o);
        }
    }
}
