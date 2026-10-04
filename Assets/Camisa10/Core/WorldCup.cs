using System;
using System.Collections.Generic;
using System.Linq;

namespace Camisa10.Core
{
    /// <summary>
    /// Copa do Mundo: nos anos de Copa (2026, 2030...), no fim da temporada, quem for convocado disputa o torneio
    /// pela Seleção, jogando cada partida (fase de grupos, oitavas, quartas, semifinal e final).
    /// </summary>
    public partial class Game
    {
        public static readonly string[] WcStages = { "Fase de grupos · 1º jogo", "Fase de grupos · 2º jogo", "Fase de grupos · 3º jogo", "Oitavas de final", "Quartas de final", "Semifinal", "Final" };

        /// <summary>Seleções que podem cruzar o caminho do Brasil (força aproximada e cores).</summary>
        public static readonly (string name, int str, string c1, string c2)[] Nations =
        {
            ("Argentina", 88, "#75AADB", "#FFFFFF"), ("França", 88, "#1F2D5C", "#FFFFFF"), ("Espanha", 88, "#C60B1E", "#FFC400"),
            ("Inglaterra", 86, "#FFFFFF", "#1F2D5C"), ("Portugal", 85, "#C8102E", "#046A38"), ("Alemanha", 84, "#FFFFFF", "#111111"),
            ("Holanda", 84, "#F36C21", "#111111"), ("Bélgica", 81, "#C8102E", "#111111"), ("Itália", 82, "#0066B3", "#FFFFFF"),
            ("Croácia", 80, "#FFFFFF", "#C8102E"), ("Uruguai", 80, "#5CBFEB", "#111111"), ("Colômbia", 80, "#FCD116", "#003893"),
            ("Marrocos", 79, "#C1272D", "#006233"), ("Noruega", 79, "#BA0C2F", "#FFFFFF"), ("Suíça", 78, "#D52B1E", "#FFFFFF"),
            ("Dinamarca", 78, "#C8102E", "#FFFFFF"), ("Japão", 77, "#1A2B6D", "#FFFFFF"), ("Estados Unidos", 77, "#FFFFFF", "#1A2B6D"),
            ("Senegal", 77, "#FFFFFF", "#00853F"), ("México", 76, "#006847", "#FFFFFF"), ("Equador", 76, "#FFD100", "#034EA2"),
            ("Coreia do Sul", 75, "#C8102E", "#111111"), ("Canadá", 74, "#D80621", "#FFFFFF"), ("Egito", 74, "#C8102E", "#FFFFFF"),
            ("Austrália", 72, "#FFCD00", "#00843D"), ("Gana", 73, "#FFFFFF", "#111111"), ("Arábia Saudita", 70, "#006C35", "#FFFFFF"),
        };

        public static bool IsWorldCupYear(int year) => year >= 2026 && (year - 2026) % 4 == 0;
        public bool WorldCupActive => S.wc != null && (S.wc.phase == "group" || S.wc.phase == "ko");
        public WcMatch WcNext => WorldCupActive ? S.wc.matches.FirstOrDefault(x => !x.played) : null;

        Club BrazilClub() => new Club { id = "sel_bra", name = "Brasil", league = "wc", str = (int)Clamp(84 + (Ovr - 80) * .15, 81, 88), c1 = "#FEDD00", c2 = "#0033A0" };
        static Club NationClub(string name)
        {
            var n = Array.Find(Nations, x => x.name == name);
            return new Club { id = "sel_" + name, name = name, league = "wc", str = n.str == 0 ? 75 : n.str, c1 = n.c1 ?? "#888888", c2 = n.c2 ?? "#FFFFFF" };
        }

        /// <summary>No fim da temporada de um ano de Copa: convocação e sorteio do grupo.</summary>
        void WorldCupAtSeasonEnd(SeasonSummary sm)
        {
            if (!IsWorldCupYear(S.year)) return;
            S.wc = new WorldCupState { year = S.year };
            bool called = S.player.injury == 0 && (NationalScore >= 78 || (S.calledUp && NationalScore >= 74));
            if (!called)
            {
                // fica de fora: acompanha pela TV o resultado da Seleção
                string fate = Rng.Pick(new[] { "caiu nas quartas de final", "parou nas oitavas", "perdeu a semifinal nos pênaltis", "foi campeã do mundo", "chegou à final e perdeu" });
                sm.notes.Add($"Você ficou fora da lista da Copa do Mundo {S.year}. Pela TV, a Seleção {fate}.");
                AddNews($"Lista da Copa do Mundo {S.year} divulgada: você ficou de fora.");
                S.wc.phase = "done";
                return;
            }
            var pool = Nations.Where(n => n.str <= 84).OrderBy(_ => Rng.Value).Take(3).ToList();
            for (int i = 0; i < 3; i++) S.wc.matches.Add(new WcMatch { stage = i, opp = pool[i].name, c1 = pool[i].c1, c2 = pool[i].c2 });
            S.wc.phase = "group";
            S.worldCups++;
            S.player.fame += 4; S.player.moral += 10;
            sm.notes.Add($"CONVOCADO PARA A COPA DO MUNDO {S.year}! Grupo do Brasil: {string.Join(", ", pool.Select(p => p.name))}.");
            AddNews($"Você está na lista do Brasil para a Copa do Mundo {S.year}!");
            Unlock("copa_jogou");
        }

        /// <summary>Monta a próxima partida da Copa para ser jogada (ou nulo se não houver).</summary>
        public MatchEngine NewWorldCupMatch()
        {
            var next = WcNext;
            if (next == null) return null;
            double ns = NationalScore;
            var role = S.player.injury > 0 ? Role.Lesionado : ns >= 90 ? Role.Estrela : ns >= 82 ? Role.Titular : Role.Reserva;
            return new MatchEngine(this, BrazilClub(), NationClub(next.opp), next.stage == 0 && S.year == 2026 ? false : Rng.Chance(.5), role);
        }

        /// <summary>Registra a partida da Copa e avança o torneio.</summary>
        public string FinishWorldCupMatch(MatchEngine m)
        {
            var wc = S.wc; var w = WcNext;
            if (w == null) return null;
            w.played = true; w.gf = m.Gf; w.ga = m.Ga; w.myGoals = m.Goals; w.rating = m.Plays ? m.Rating : 0;
            if (m.Plays) { S.caps++; S.intlGoals += m.Goals; S.player.fame += (float)(1.5 + m.Goals * 1.5 + (m.Rating >= 8 ? 1.5 : 0)); }
            string stage = WcStages[w.stage];
            string line = $"Copa do Mundo, {stage.ToLowerInvariant()}: Brasil {m.Gf} x {m.Ga} {w.opp}.";
            if (m.Goals > 0) line += $" Você marcou {m.Goals}!";

            if (w.stage < 3)
            {
                wc.groupPts += m.Gf > m.Ga ? 3 : m.Gf == m.Ga ? 1 : 0;
                w.won = m.Gf > m.Ga;
                if (w.stage == 2)
                {
                    bool through = wc.groupPts >= 5 || (wc.groupPts >= 4 && Rng.Chance(.85)) || (wc.groupPts == 3 && Rng.Chance(.35));
                    if (through) { line += $" Classificados com {wc.groupPts} pontos!"; AddKo(3); wc.phase = "ko"; }
                    else { line += $" Eliminados na fase de grupos com {wc.groupPts} pontos."; wc.eliminated = true; wc.phase = "done"; S.player.moral -= 12; }
                }
            }
            else
            {
                bool won = m.Gf > m.Ga;
                if (m.Gf == m.Ga)
                {
                    w.pens = true;
                    won = Rng.Chance(Clamp(.5 + (BrazilClub().str - NationClub(w.opp).str) * .01, .3, .7));
                    line += won ? " Vitória nos pênaltis!" : " Derrota nos pênaltis.";
                }
                w.won = won;
                if (!won) { wc.eliminated = true; wc.phase = "done"; S.player.moral -= 15; line += $" O Brasil está fora da Copa ({stage.ToLowerInvariant()})."; }
                else if (w.stage == 6)
                {
                    wc.champion = true; wc.phase = "done";
                    string title = $"Campeão da Copa do Mundo {wc.year}";
                    S.titles.Add(title);
                    S.season.summary?.titles.Add(title);
                    S.player.fame += 15; S.player.moral += 25;
                    S.player.money += R1000(Math.Max(250000, S.contract.salary * 10));
                    line = $"É CAMPEÃO DO MUNDO! Brasil {m.Gf} x {m.Ga} {w.opp} na final" + (w.pens ? " (nos pênaltis)" : "") + "." + (m.Goals > 0 ? $" Você marcou {m.Goals} na final!" : "");
                    Unlock("copa_mundo");
                }
                else { line += $" Próxima fase: {WcStages[w.stage + 1].ToLowerInvariant()}."; AddKo(w.stage + 1); }
            }
            AddNews(line);
            Normalize();
            return line;
        }

        /// <summary>Sorteia o adversário do mata-mata (mais forte a cada fase).</summary>
        void AddKo(int stage)
        {
            var used = new HashSet<string>(S.wc.matches.Select(x => x.opp));
            var pool = Nations.Where(n => !used.Contains(n.name)).ToList();
            var opp = Rng.Weighted(pool.Select(n => (n, Math.Pow(n.str / 70.0, 2 + (stage - 3) * 3))).ToList());
            S.wc.matches.Add(new WcMatch { stage = stage, opp = opp.name, c1 = opp.c1, c2 = opp.c2 });
        }

        /// <summary>Pula a Copa: as partidas que faltam são simuladas.</summary>
        public void SimulateRestOfWorldCup()
        {
            int guard = 0;
            while (WorldCupActive && guard++ < 10)
            {
                var m = NewWorldCupMatch();
                var w = WcNext;
                double my = BrazilClub().str, op = NationClub(w.opp).str;
                m.Gf = Rng.Poisson(1.3 * Math.Pow(my / op, 2.5)); m.Ga = Rng.Poisson(1.3 * Math.Pow(op / my, 2.5));
                m.Plays = m.Role == Role.Titular || m.Role == Role.Estrela;
                m.Goals = m.Plays ? Enumerable.Range(0, m.Gf).Count(_ => Rng.Chance(IsAttacker ? .3 : .1)) : 0;
                FinishWorldCupMatch(m);
            }
        }
    }
}
