using System;
using System.Collections.Generic;
using System.Linq;

namespace Camisa10.Core
{
    /// <summary>Conquista de carreira com recompensa em dinheiro e fama.</summary>
    public class AchievementDef
    {
        public string Id, Name, Desc;
        public long Money;
        public float Fame;
    }

    /// <summary>
    /// O lado "futebol de verdade" da carreira: artilharia com jogadores reais, copa em mata-mata,
    /// convocação para a seleção, craque do mês, prêmios de fim de temporada, Bola de Ouro e conquistas.
    /// </summary>
    public partial class Game
    {
        // rodadas depois das quais acontece cada fase da copa, a data de seleções e o prêmio do mês
        public static readonly int[] CupWeeks = { 4, 8, 12, 16 };
        public static readonly int[] NationalWeeks = { 6, 12 };
        public static readonly string[] CupStages = { "Oitavas de final", "Quartas de final", "Semifinal", "Final" };

        public static string CupName(string league) => League(league)?.Cup ?? "Copa Nacional";

        // clubes que entram só na copa (além dos clubes da liga)
        static readonly Dictionary<string, (string name, int str, string c1, string c2)[]> CupGuests = new Dictionary<string, (string, int, string, string)[]>
        {
            ["br"] = new[] { ("Internacional", 70, "#D7141A", "#FFFFFF"), ("Atlético Mineiro", 71, "#111111", "#FFFFFF"), ("Santos", 64, "#FFFFFF", "#111111"),
                ("Fortaleza", 62, "#0033A0", "#D7141A"), ("Athletico Paranaense", 63, "#C8102E", "#111111"), ("Red Bull Bragantino", 64, "#FFFFFF", "#D7141A") },
            ["ib"] = new[] { ("Girona", 72, "#D7141A", "#FFFFFF"), ("Osasuna", 68, "#D7141A", "#0A2240"), ("Getafe", 66, "#004FA3", "#FFFFFF"),
                ("Mallorca", 67, "#D7141A", "#111111"), ("Rayo Vallecano", 66, "#FFFFFF", "#D7141A"), ("Espanyol", 65, "#0072CE", "#FFFFFF") },
            ["en"] = new[] { ("Everton", 70, "#003399", "#FFFFFF"), ("Fulham", 70, "#FFFFFF", "#111111"), ("Crystal Palace", 72, "#1B458F", "#C4122E"),
                ("Wolves", 69, "#FDB913", "#231F20"), ("Bournemouth", 71, "#DA291C", "#111111"), ("Brentford", 70, "#E30613", "#FFFFFF") },
        };

        public static readonly AchievementDef[] Achievements =
        {
            new AchievementDef { Id = "copa_jogou", Name = "Disputou uma Copa do Mundo", Desc = "Seja convocado para uma Copa do Mundo.", Money = 50000, Fame = 5 },
            new AchievementDef { Id = "copa_mundo", Name = "Campeão do Mundo", Desc = "Vença a Copa do Mundo com a Seleção.", Money = 500000, Fame = 12 },
            new AchievementDef { Id = "estreia", Name = "Estreia profissional", Desc = "Entre em campo pela primeira vez.", Money = 5000, Fame = 1 },
            new AchievementDef { Id = "primeiro_gol", Name = "O primeiro a gente nunca esquece", Desc = "Marque seu primeiro gol.", Money = 10000, Fame = 2 },
            new AchievementDef { Id = "hat_trick", Name = "Hat-trick", Desc = "Faça três gols no mesmo jogo.", Money = 30000, Fame = 3 },
            new AchievementDef { Id = "nota_9", Name = "Atuação de gala", Desc = "Tire nota 9 ou mais numa partida.", Money = 20000, Fame = 2 },
            new AchievementDef { Id = "gols_25", Name = "25 gols", Desc = "Chegue a 25 gols na carreira.", Money = 25000, Fame = 2 },
            new AchievementDef { Id = "gols_50", Name = "50 gols", Desc = "Chegue a 50 gols na carreira.", Money = 60000, Fame = 3 },
            new AchievementDef { Id = "gols_100", Name = "Centenário", Desc = "Chegue a 100 gols na carreira.", Money = 150000, Fame = 5 },
            new AchievementDef { Id = "gols_200", Name = "Lenda do gol", Desc = "Chegue a 200 gols na carreira.", Money = 400000, Fame = 6 },
            new AchievementDef { Id = "jogos_50", Name = "50 jogos", Desc = "Dispute 50 jogos.", Money = 20000, Fame = 1 },
            new AchievementDef { Id = "jogos_100", Name = "100 jogos", Desc = "Dispute 100 jogos.", Money = 50000, Fame = 2 },
            new AchievementDef { Id = "jogos_250", Name = "Veterano", Desc = "Dispute 250 jogos.", Money = 150000, Fame = 3 },
            new AchievementDef { Id = "craque_mes", Name = "Craque do mês", Desc = "Seja eleito o melhor do mês.", Money = 15000, Fame = 2 },
            new AchievementDef { Id = "titulo", Name = "Primeiro título", Desc = "Seja campeão da liga.", Money = 80000, Fame = 4 },
            new AchievementDef { Id = "copa", Name = "Rei de copas", Desc = "Vença a copa nacional.", Money = 60000, Fame = 3 },
            new AchievementDef { Id = "artilheiro", Name = "Artilheiro", Desc = "Termine como artilheiro da liga.", Money = 60000, Fame = 4 },
            new AchievementDef { Id = "selecao", Name = "Convocado", Desc = "Seja chamado para a Seleção Brasileira.", Money = 40000, Fame = 5 },
            new AchievementDef { Id = "gol_selecao", Name = "Gol com a amarelinha", Desc = "Marque pela Seleção.", Money = 40000, Fame = 4 },
            new AchievementDef { Id = "europa", Name = "Rumo à Europa", Desc = "Assine com um clube da Espanha ou da Inglaterra.", Money = 50000, Fame = 4 },
            new AchievementDef { Id = "milionario", Name = "Milionário", Desc = "Junte R$ 1 milhão.", Money = 0, Fame = 2 },
            new AchievementDef { Id = "top3_bola", Name = "Entre os melhores do mundo", Desc = "Fique no top 3 da Bola de Ouro.", Money = 150000, Fame = 5 },
            new AchievementDef { Id = "bola_ouro", Name = "Bola de Ouro", Desc = "Seja eleito o melhor jogador do mundo.", Money = 500000, Fame = 8 },
        };

        bool SeasonRecorded => S.career.Any(c => c.year == S.season.year);
        public int CareerApps => S.career.Sum(c => c.apps) + (SeasonRecorded ? 0 : S.season.stats.apps);
        public int CareerGoals => S.career.Sum(c => c.goals) + (SeasonRecorded ? 0 : S.season.stats.goals) + S.cupGoalsTotal;
        public bool HasAchievement(string id) => S.achievements.Contains(id);

        /// <summary>Completa saves antigos e começa a artilharia/copa de uma temporada em andamento.</summary>
        void EnsureGlory()
        {
            var se = S.season;
            if (S.achievements == null) S.achievements = new List<string>();
            if (S.monthly == null) S.monthly = new List<string>();
            if (S.ballon == null) S.ballon = new List<RankRow>();
            if (se.scorers == null) se.scorers = new List<ScorerRow>();
            if (se.cup == null) se.cup = new List<CupTie>();
            if (se.scorers.Count == 0 && se.league != null && se.phase != "end")
            {
                InitScorers();
                for (int w = 0; w < se.week; w++) ScorersRound();
            }
        }

        void InitScorers()
        {
            var se = S.season;
            se.scorers = new List<ScorerRow>();
            foreach (var c in S.clubs.Where(c => c.league == se.league))
            {
                if (!GameData.Squads.TryGetValue(c.name, out var squad)) continue;
                double k = Math.Pow(c.str / 75.0, 2);
                double[] rates = { .44, .3, .2, .12, .1 };
                for (int i = 1; i < squad.Length; i++)
                    se.scorers.Add(new ScorerRow { name = squad[i], club = c.id, rate = (float)(rates[Math.Min(i - 1, rates.Length - 1)] * k) });
            }
        }

        void ScorersRound()
        {
            foreach (var r in S.season.scorers) r.goals += Rng.Poisson(r.rate);
        }

        /// <summary>Artilharia da liga com você incluído (nome, clube, gols, é você).</summary>
        public List<(string name, string club, int goals, bool me)> TopScorers(int n)
        {
            var list = S.season.scorers.Select(r => (r.name, r.club, r.goals, false)).ToList();
            list.Add((S.player.name, S.contract.club, S.season.stats.goals, true));
            return list.OrderByDescending(x => x.Item3).ThenBy(x => x.Item4 ? 0 : 1).Take(n).ToList();
        }

        public int MyScorerRank => TopScorers(999).FindIndex(x => x.me) + 1;

        /// <summary>Chamado ao fim de cada rodada, depois do jogo do jogador.</summary>
        void GloryAfterRound(MatchEngine m)
        {
            var se = S.season;
            ScorersRound();
            int week = se.week + 1; // rodadas já jogadas
            if (Array.IndexOf(CupWeeks, week) >= 0)
            {
                MonthAward(week);
                if (!se.cupOut && !se.cupWon) PlayCupTie();
            }
            if (Array.IndexOf(NationalWeeks, week) >= 0) NationalWindow();
            CheckAchievements(m);
        }

        void MonthAward(int week)
        {
            var se = S.season; var st = se.stats;
            var window = st.ratings.Skip(se.monthMark).ToList();
            se.monthMark = st.ratings.Count;
            if (window.Count < 3) return;
            float avg = window.Average();
            if (avg < 7.3f || !Rng.Chance(Clamp((avg - 7.0) / 1.0, .25, .95))) return;
            string label = $"Craque do mês {GameData.Of(League(se.league).Name)} {se.year} (rodadas {week - 3} a {week})";
            S.monthly.Add(label);
            S.player.fame += 2;
            S.player.moral += 5;
            AddNews($"Você foi eleito o craque do mês {GameData.Of(League(se.league).Name)}! Nota média {Fmt.Rating(avg)} nas últimas rodadas.");
            Unlock("craque_mes");
        }

        void PlayCupTie()
        {
            var se = S.season; var p = S.player; var me = MyClub;
            int stage = se.cup.Count;
            if (stage >= CupStages.Length) return;

            // adversário: clubes da liga e convidados; nas fases finais, os mais fortes pesam mais
            var used = new HashSet<string>(se.cup.Select(t => t.opp));
            var pool = S.clubs.Where(c => c.league == se.league && c.id != me.id && !used.Contains(c.name))
                .Select(c => (c.name, c.str, c.c1, c.c2)).ToList();
            if (CupGuests.TryGetValue(se.league, out var guests)) pool.AddRange(guests.Where(g => !used.Contains(g.name)));
            var opp = Rng.Weighted(pool.Select(c => (c, Math.Pow(c.str / 60.0, 2 + stage * 2))).ToList());

            bool fit = p.injury == 0 && p.energy >= 25;
            double my = me.str + (fit ? (Ovr - me.str) * .3 + (FormAvg - 6.5) * 2 : -1);
            bool home = Rng.Chance(.5);
            double lt = 1.3 * Math.Pow(my / opp.str, 2.5) * (home ? 1.12 : .9);
            double lo = 1.3 * Math.Pow(opp.str / my, 2.5) * (home ? .9 : 1.12);
            int gf = Rng.Poisson(lt), ga = Rng.Poisson(lo);
            bool pens = gf == ga;
            bool won = pens ? Rng.Chance(Clamp(.5 + (my - opp.str) * .01, .3, .7)) : gf > ga;

            int mine = 0;
            if (fit)
            {
                double share = (IsAttacker ? .34 : IsDefensive ? .08 : .18) * Clamp(Ovr / 75.0, .6, 1.4);
                for (int i = 0; i < gf; i++) if (Rng.Chance(share)) mine++;
            }
            var tie = new CupTie { stage = stage, opp = opp.name, c1 = opp.c1, c2 = opp.c2, gf = gf, ga = ga, myGoals = mine, won = won, pens = pens, home = home };
            se.cup.Add(tie);
            se.cupGoals += mine;
            S.cupGoalsTotal += mine;
            if (fit) p.energy -= 8;

            string cup = CupName(se.league);
            string score = $"{me.name} {gf} x {ga} {opp.name}" + (pens ? (won ? " (vitória nos pênaltis)" : " (derrota nos pênaltis)") : "");
            string goals = mine > 0 ? $" Você marcou {mine}." : fit ? "" : " Você não jogou.";
            if (!won)
            {
                se.cupOut = true;
                AddNews($"{cup}, {CupStages[stage].ToLowerInvariant()}: {score}. Eliminados.{goals}");
                p.moral -= 4;
                return;
            }
            if (stage == CupStages.Length - 1)
            {
                se.cupWon = true;
                long prize = R1000(Math.Max(20000, S.contract.salary * 4));
                p.money += prize;
                p.fame += 4; p.moral += 10;
                AddNews($"CAMPEÃO DA {cup.ToUpperInvariant()}! {score}.{goals} Prêmio de {Fmt.Money(prize)} para você.");
                Unlock("copa");
            }
            else
            {
                p.fame += 1; p.moral += 3;
                AddNews($"{cup}, {CupStages[stage].ToLowerInvariant()}: {score}. Classificados para a {CupStages[stage + 1].ToLowerInvariant()}!{goals}");
            }
        }

        /// <summary>Data de seleções: convocação depende de geral, forma e fama.</summary>
        public double NationalScore => Ovr + (FormAvg - 6.5) * 6 + S.player.fame * .08;

        void NationalWindow()
        {
            var p = S.player;
            bool was = S.calledUp;
            S.calledUp = p.injury == 0 && NationalScore >= 80;
            if (!S.calledUp)
            {
                if (was) AddNews("O técnico da Seleção divulgou a lista e você ficou de fora desta vez.");
                else if (NationalScore >= 76) AddNews("Você foi observado pela comissão técnica da Seleção. Continue jogando bem.");
                return;
            }
            int games = 2, played = 0, goals = 0;
            for (int i = 0; i < games; i++)
            {
                if (!Rng.Chance(NationalScore >= 86 ? .9 : .55)) continue;
                played++;
                double gp = (IsAttacker ? .35 : IsDefensive ? .06 : .15) * Clamp(Ovr / 80.0, .7, 1.3);
                while (Rng.Chance(gp) && goals < 3) { goals++; gp *= .4; }
            }
            S.caps += played;
            S.intlGoals += goals;
            p.fame += 2.5f; p.moral += 6; p.energy -= 6;
            string txt = was ? "Convocado de novo para a Seleção Brasileira." : "CONVOCADO! Você está na lista da Seleção Brasileira.";
            txt += played > 0 ? $" Jogou {played} partida(s)" + (goals > 0 ? $" e marcou {goals} gol(s)." : ".") : " Ficou no banco nos dois jogos.";
            AddNews(txt);
            Unlock("selecao");
            if (S.intlGoals > 0) Unlock("gol_selecao");
            Normalize();
        }

        /// <summary>Prêmios de fim de temporada (chamado dentro de EndSeason, antes de somar títulos e prêmios).</summary>
        void GloryEndSeason(SeasonSummary sm, float avg)
        {
            var se = S.season; var st = se.stats; var L = League(se.league);
            if (se.cupWon) sm.titles.Add($"Campeão da {CupName(se.league)} {se.year}");

            int top = se.scorers.Count > 0 ? se.scorers.Max(r => r.goals) : 0;
            if (st.goals > 0 && st.goals >= top) { sm.awards.Add($"Artilheiro {GameData.Of(L.Name)} {se.year} ({st.goals} gols)"); Unlock("artilheiro"); }
            else if (se.scorers.Count > 0)
            {
                var best = se.scorers.OrderByDescending(r => r.goals).First();
                sm.notes.Add($"Artilheiro {GameData.Of(L.Name)}: {best.name} ({ClubById(best.club)?.name}), com {best.goals} gols. Você fez {st.goals}.");
            }
            if (avg >= 7.5 && st.apps >= 10) sm.awards.Add($"Craque {GameData.Of(L.Name)} {se.year}");
            else if (avg >= 7.15 && st.apps >= 10) sm.awards.Add($"Seleção {GameData.Of(L.Name)} {se.year}");
            if (S.player.age <= 21 && avg >= 7 && st.apps >= 10) sm.awards.Add($"Revelação {GameData.Of(L.Name)} {se.year}");

            // Bola de Ouro: os craques reais de cada clube contra a sua temporada
            var rows = new List<RankRow>();
            foreach (var c in S.clubs)
            {
                if (!GameData.Squads.TryGetValue(c.name, out var squad) || squad.Length < 2) continue;
                double lg = League(c.league)?.Prestige ?? 0;
                for (int i = 1; i <= Math.Min(2, squad.Length - 1); i++)
                {
                    if (i == 2 && c.str < 84) break;
                    double sc = c.str * .9 + 8 + lg + Rng.Gauss() * 3 - (i - 1) * 3;
                    var srow = c.league == se.league ? se.scorers.Find(r => r.name == squad[i]) : null;
                    if (srow != null) sc += srow.goals * .25;
                    rows.Add(new RankRow { name = squad[i], club = c.name, score = (float)sc });
                }
            }
            double mine = Ovr * .9 + (st.apps > 0 ? (avg - 6.8) * 6 : -6) + (st.goals + se.cupGoals) * .25 + sm.titles.Count * 3
                + S.player.fame * .05 + (S.calledUp ? 2 : 0) + L.Prestige;
            rows.Add(new RankRow { name = S.player.name, club = MyClub.name, score = (float)mine, me = true });
            rows = rows.OrderByDescending(r => r.score).ToList();
            int pos = rows.FindIndex(r => r.me) + 1;
            S.ballon = rows.Take(10).ToList();
            if (pos > 10) S.ballon.Add(rows[pos - 1]);
            S.ballonYear = se.year;
            if (S.bestBallon == 0 || pos < S.bestBallon) S.bestBallon = pos;
            if (pos == 1) { sm.awards.Add($"Bola de Ouro {se.year}"); Unlock("bola_ouro"); Unlock("top3_bola"); }
            else if (pos <= 3) { sm.notes.Add($"Bola de Ouro: você ficou em {pos}º lugar. Venceu {rows[0].name}."); Unlock("top3_bola"); }
            else if (pos <= 10) sm.notes.Add($"Bola de Ouro: você ficou em {pos}º lugar. Venceu {rows[0].name}.");
            else sm.notes.Add($"Bola de Ouro {se.year}: {rows[0].name} ({rows[0].club}) foi o vencedor.");
        }

        void CheckAchievements(MatchEngine m)
        {
            if (m != null && m.Plays)
            {
                Unlock("estreia");
                if (m.Goals >= 3) Unlock("hat_trick");
                if (m.Rating >= 9) Unlock("nota_9");
            }
            int goals = CareerGoals, apps = CareerApps;
            if (goals >= 1) Unlock("primeiro_gol");
            if (goals >= 25) Unlock("gols_25");
            if (goals >= 50) Unlock("gols_50");
            if (goals >= 100) Unlock("gols_100");
            if (goals >= 200) Unlock("gols_200");
            if (apps >= 50) Unlock("jogos_50");
            if (apps >= 100) Unlock("jogos_100");
            if (apps >= 250) Unlock("jogos_250");
            if (S.titles.Any(t => GameData.Leagues.Any(l => t.StartsWith($"Campeão {GameData.Of(l.Name)} ")))) Unlock("titulo");
            if (MyClub != null && MyClub.league != "br") Unlock("europa");
            if (S.player.money >= 1000000) Unlock("milionario");
        }

        void Unlock(string id)
        {
            if (S.achievements.Contains(id)) return;
            var a = Array.Find(Achievements, x => x.Id == id);
            if (a == null) return;
            S.achievements.Add(id);
            S.player.money += a.Money;
            S.player.fame += a.Fame;
            AddNews($"Conquista desbloqueada: {a.Name}" + (a.Money > 0 ? $" (+{Fmt.Money(a.Money)})." : "."));
        }
    }
}
