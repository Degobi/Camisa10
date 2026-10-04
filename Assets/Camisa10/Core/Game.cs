using System;
using System.Collections.Generic;
using System.Linq;

namespace Camisa10.Core
{
    /// <summary>
    /// Regras da carreira. Não depende da Unity: pode ser testado em console e reaproveitado num backend ASP.NET depois.
    /// </summary>
    public partial class Game
    {
        public const int RoundsPerSeason = 18;
        public GameState S;
        public long Counter; // contraproposta pendente na negociação (não é salva)

        public Game(GameState state) { S = state; SyncClubs(); EnsureBusiness(); }

        // ---------- utilidades ----------
        public static double Clamp(double v, double a, double b) => v < a ? a : v > b ? b : v;
        public static long R100(double v) => (long)Math.Round(v / 100.0) * 100;
        public static long R1000(double v) => (long)Math.Round(v / 1000.0) * 1000;
        public static long R10k(double v) => (long)Math.Round(v / 10000.0) * 10000;
        static int idCounter;
        public static string NewId() => DateTime.UtcNow.Ticks.ToString("x") + (idCounter++);

        /// <summary>Saves antigos tinham clubes fictícios: troca nome e cores pelos atuais, mantendo o id.</summary>
        void SyncClubs()
        {
            foreach (var L in GameData.Leagues)
                for (int i = 0; i < L.Clubs.Length; i++)
                {
                    var c = S.clubs.Find(x => x.id == L.Id + i);
                    if (c == null) continue;
                    c.name = L.Clubs[i].name; c.c1 = L.Clubs[i].c1; c.c2 = L.Clubs[i].c2;
                }
        }

        public Club ClubById(string id) => S.clubs.Find(c => c.id == id);
        public Club MyClub => ClubById(S.contract.club);
        public static GameData.LeagueDef League(string id) => Array.Find(GameData.Leagues, l => l.Id == id);

        public static int Overall(string pos, int[] attrs)
        {
            var w = GameData.Positions[pos].W;
            double s = 0;
            for (int i = 0; i < 6; i++) s += w[i] * attrs[i];
            return (int)Math.Round(s);
        }

        public int Ovr => Overall(S.player.pos, S.player.attrs);
        public float FormAvg => S.player.form.Count == 0 ? 6.5f : S.player.form.Average();
        public float AvgRating => S.season.stats.ratings.Count == 0 ? 0f : S.season.stats.ratings.Average();
        public bool IsAttacker => S.player.pos == "ATA" || S.player.pos == "MEI";
        public bool IsDefensive => S.player.pos == "ZAG" || S.player.pos == "VOL" || S.player.pos == "LAT";

        public static Attr MainAttr(string pos)
        {
            var w = GameData.Positions[pos].W;
            int best = 0;
            for (int i = 1; i < 6; i++) if (w[i] > w[best]) best = i;
            return (Attr)best;
        }

        public Attr WeakestAttr()
        {
            var w = GameData.Positions[S.player.pos].W;
            int best = -1;
            for (int i = 0; i < 6; i++)
                if (w[i] > 0 && (best < 0 || S.player.attrs[i] < S.player.attrs[best])) best = i;
            return (Attr)best;
        }

        public long MarketValue()
        {
            var p = S.player;
            int o = Ovr;
            double v = 500000 * Math.Exp((o - 55) * .2);
            int a = p.age;
            v *= a <= 21 ? 1.4 : a <= 27 ? 1 : a <= 30 ? .75 : a <= 33 ? .45 : .25;
            if (a <= 23) v *= 1 + Math.Max(0, p.pot - o) / 50.0;
            return R1000(v);
        }

        public double ClubWage(Club c) => 2500 * Math.Exp((c.str - 55) * .15) * League(c.league).WageMult;
        public long FairSalary(Club c) => R100(ClubWage(c) * Clamp(Math.Exp((Ovr - c.str) * .08), .3, 1.8) * (1 + S.player.fame / 300.0));
        long GoalBonusFor(long salary) => R100(salary * (IsAttacker ? .12 : .25));

        public void Normalize()
        {
            var p = S.player;
            p.energy = (float)Clamp(p.energy, 0, 100);
            p.moral = (float)Clamp(p.moral, 0, 100);
            p.fame = (float)Clamp(p.fame, 0, 100);
            p.coach = (float)Clamp(p.coach, 0, 100);
        }

        public void AddNews(string text)
        {
            S.news.Insert(0, new News { when = $"{S.year}, rodada {Math.Min(RoundsPerSeason, S.season.week + 1)}", text = text });
            if (S.news.Count > 40) S.news.RemoveRange(40, S.news.Count - 40);
        }

        // ---------- nova carreira ----------
        public static Game NewCareer(string name, string pos, int[] alloc)
        {
            var s = new GameState { year = 2026 };
            foreach (var L in GameData.Leagues)
                for (int i = 0; i < L.Clubs.Length; i++)
                {
                    var c = L.Clubs[i];
                    s.clubs.Add(new Club { id = L.Id + i, name = c.name, league = L.Id, str = c.str, c1 = c.c1, c2 = c.c2 });
                }

            var def = GameData.Positions[pos];
            var p = s.player;
            p.name = string.IsNullOrWhiteSpace(name) ? "Craque" : name.Trim();
            p.pos = pos;
            p.age = 17;
            for (int i = 0; i < 6; i++) p.attrs[i] = def.Base[i] + alloc[i];
            p.pot = Rng.RangeInt(78, 95);
            p.energy = 90; p.moral = 65; p.fame = 3; p.coach = 50; p.money = 5000;

            var g = new Game(s);
            string start = Rng.Pick(new[] { "br6", "br7", "br8", "br9" });
            s.contract = new Contract { club = start, years = 3 };
            s.contract.salary = g.FairSalary(g.ClubById(start));
            s.contract.bonus = g.GoalBonusFor(s.contract.salary);
            s.contract.clause = R10k(g.MarketValue() * 2);
            g.StartSeason();
            g.AddNews($"Você assinou seu primeiro contrato profissional com o {g.MyClub.name}. Salário de {Fmt.Money(s.contract.salary)} por semana.");
            return g;
        }

        // ---------- temporada e tabela ----------
        public void StartSeason()
        {
            string lg = MyClub.league;
            var ids = S.clubs.Where(c => c.league == lg).Select(c => c.id).ToList();
            Rng.Shuffle(ids);
            var se = new Season { year = S.year, league = lg, rounds = MakeFixtures(ids), topScorer = Rng.RangeInt(13, 22) };
            foreach (var id in ids) se.table.Add(new TableRow { club = id });
            S.season = se;
            GenSponsorOffers();
        }

        static List<Round> MakeFixtures(List<string> ids)
        {
            int n = ids.Count;
            var arr = new List<string>(ids);
            var first = new List<Round>();
            for (int r = 0; r < n - 1; r++)
            {
                var rd = new Round();
                for (int i = 0; i < n / 2; i++)
                {
                    string a = arr[i], b = arr[n - 1 - i];
                    rd.games.Add((r + i) % 2 == 0 ? new Pair { home = a, away = b } : new Pair { home = b, away = a });
                }
                first.Add(rd);
                string last = arr[n - 1];
                arr.RemoveAt(n - 1);
                arr.Insert(1, last);
            }
            var all = new List<Round>(first);
            foreach (var rd in first)
            {
                var back = new Round();
                foreach (var pr in rd.games) back.games.Add(new Pair { home = pr.away, away = pr.home });
                all.Add(back);
            }
            return all;
        }

        TableRow Row(string id) => S.season.table.Find(t => t.club == id);

        public void ApplyResult(string h, string a, int gh, int ga)
        {
            var H = Row(h); var A = Row(a);
            H.p++; A.p++; H.gf += gh; H.ga += ga; A.gf += ga; A.ga += gh;
            if (gh > ga) { H.w++; A.l++; H.pts += 3; }
            else if (gh < ga) { A.w++; H.l++; A.pts += 3; }
            else { H.d++; A.d++; H.pts++; A.pts++; }
        }

        void SimOther(string h, string a)
        {
            double sh = ClubById(h).str, sa = ClubById(a).str;
            ApplyResult(h, a, Rng.Poisson(1.3 * Math.Pow(sh / sa, 2.5) * 1.12), Rng.Poisson(1.3 * Math.Pow(sa / sh, 2.5) * .9));
        }

        public List<TableRow> SortedTable() =>
            S.season.table.OrderByDescending(t => t.pts).ThenByDescending(t => t.Gd).ThenByDescending(t => t.gf).ToList();

        public (Pair pair, bool home, Club opp) CurrentFixture()
        {
            string me = S.contract.club;
            var pr = S.season.rounds[S.season.week].games.Find(x => x.home == me || x.away == me);
            bool home = pr.home == me;
            return (pr, home, ClubById(home ? pr.away : pr.home));
        }

        // ---------- patrocínios ----------
        public void GenSponsorOffers()
        {
            var p = S.player;
            var L = League(MyClub.league);
            S.sponsorOffers.Clear();
            foreach (var cat in GameData.SponsorCats)
            {
                if (S.activeSponsors.Exists(d => d.cat == cat.Id) || p.fame < cat.MinFame) continue;
                if (cat.Id != "chuteira" && Rng.Value > .55) continue;
                long per = R1000(12000 * Math.Exp(p.fame / 14.0) * cat.Factor * L.FameMult * Rng.RangeF(.85, 1.15));
                var goal = MakeGoal();
                S.sponsorOffers.Add(new SponsorDeal
                {
                    id = NewId(), cat = cat.Id, brand = Rng.Pick(cat.Brands), perSeason = per, seasons = Rng.RangeInt(1, 3),
                    goalKind = goal.kind, goalTarget = goal.target, bonus = R1000(per * .5)
                });
            }
        }

        (string kind, float target) MakeGoal()
        {
            int o = Ovr;
            if (S.player.pos == "ATA") return ("gols", Rng.RangeInt(5, 8) + (o >= 70 ? 4 : 0) + (o >= 80 ? 4 : 0));
            if (S.player.pos == "MEI") return ("part", Rng.RangeInt(5, 8) + (o >= 72 ? 4 : 0));
            return ("nota", (float)Math.Round(Rng.RangeF(6.6, 7.1), 1));
        }

        public string GoalText(SponsorDeal d)
        {
            if (d.goalKind == "gols") return $"Meta: {d.goalTarget:0} gols na temporada";
            if (d.goalKind == "part") return $"Meta: {d.goalTarget:0} gols + assistências na temporada";
            return $"Meta: nota média {Fmt.Rating(d.goalTarget)} ou mais (mínimo 8 jogos)";
        }

        public bool GoalMet(SponsorDeal d)
        {
            var st = S.season.stats;
            if (d.goalKind == "gols") return st.goals >= d.goalTarget;
            if (d.goalKind == "part") return st.goals + st.assists >= d.goalTarget;
            return st.apps >= 8 && AvgRating >= d.goalTarget;
        }

        public float GoalProgress01(SponsorDeal d)
        {
            var st = S.season.stats;
            float v = d.goalKind == "gols" ? st.goals : d.goalKind == "part" ? st.goals + st.assists : AvgRating;
            return (float)Clamp(v / Math.Max(1f, d.goalTarget), 0, 1);
        }

        public string GoalProgressText(SponsorDeal d)
        {
            var st = S.season.stats;
            string t = d.goalKind == "gols" ? $"{st.goals} de {d.goalTarget:0} gols"
                : d.goalKind == "part" ? $"{st.goals + st.assists} de {d.goalTarget:0}"
                : st.apps > 0 ? $"Nota média atual {Fmt.Rating(AvgRating)} em {st.apps} jogos" : "Nenhum jogo ainda";
            return GoalMet(d) ? t + ", meta batida" : t;
        }

        public string AcceptSponsor(string id)
        {
            var o = S.sponsorOffers.Find(x => x.id == id);
            if (o == null || S.activeSponsors.Exists(d => d.cat == o.cat)) return null;
            o.seasonsLeft = o.seasons;
            S.activeSponsors.Add(o);
            S.sponsorOffers.Remove(o);
            if (o.cat == "chuteira")
            {
                var pal = GameData.BootPalettes[o.brand];
                S.player.boot.brand = o.brand; S.player.boot.c1 = pal[0]; S.player.boot.c2 = pal[1];
            }
            S.player.moral += 3;
            Normalize();
            AddNews($"Você fechou patrocínio com a {o.brand} ({GameData.Sponsor(o.cat).Name.ToLowerInvariant()}).");
            return $"Patrocínio com a {o.brand} assinado.";
        }

        public void DeclineSponsor(string id) => S.sponsorOffers.RemoveAll(x => x.id == id);
        public double SponsorWeekly => S.activeSponsors.Sum(d => d.perSeason / (double)RoundsPerSeason);

        // ---------- semana ----------
        public Role ComputeRole()
        {
            var p = S.player;
            if (p.injury > 0) return Role.Lesionado;
            if (S.season.rested) return Role.Poupado;
            var c = MyClub;
            double sc = Ovr + (p.coach - 50) * .12 + (p.moral - 50) * .05 + (FormAvg - 6.5) * 2 + (p.energy < 35 ? -6 : 0);
            if (sc >= c.str + 6) return Role.Estrela;
            if (sc >= c.str - 4) return Role.Titular;
            return Role.Reserva;
        }

        public string Train(Attr attr, string intensityId, out bool injured)
        {
            var p = S.player;
            var it = GameData.GetIntensity(intensityId);
            int i = (int)attr;
            double ageMult = p.age <= 21 ? 1.3 : p.age <= 25 ? 1 : p.age <= 29 ? .7 : p.age <= 32 ? .4 : .2;
            double potMult = Ovr >= p.pot ? .15 : 1;
            p.xp[i] += (float)(it.Xp * ageMult * potMult);
            int up = 0;
            for (int g = 0; g < 5; g++)
            {
                double need = 10 + Math.Max(0, p.attrs[i] - 55) * .9;
                if (p.xp[i] < need || p.attrs[i] >= 99) break;
                p.xp[i] -= (float)need; p.attrs[i]++; up++;
            }
            p.energy = (float)Clamp(p.energy + it.Energy, 0, 100);
            string label = GameData.Label(attr);
            string msg = up > 0 ? $"{label} subiu para {p.attrs[i]}." : $"Treino de {label.ToLowerInvariant()} concluído. A evolução está a caminho.";
            injured = false;
            if (Rng.Chance(it.Risk + (p.energy < 35 ? .08 : 0)))
            {
                p.injury = Rng.RangeInt(1, 3);
                injured = true;
                msg += $" Você se lesionou e fica fora por {p.injury} rodada(s).";
                AddNews($"Lesão no treino: {p.injury} rodada(s) fora.");
            }
            S.season.phase = "match";
            S.season.role = ComputeRole();
            return msg;
        }

        public void Physio()
        {
            S.player.energy = (float)Clamp(S.player.energy + 20, 0, 100);
            S.season.phase = "match";
            S.season.role = Role.Lesionado;
        }

        public void FinishMatch(MatchEngine m)
        {
            var p = S.player; var se = S.season; var st = se.stats; var L = League(se.league);
            var f = CurrentFixture();
            ApplyResult(f.pair.home, f.pair.away, f.home ? m.Gf : m.Ga, f.home ? m.Ga : m.Gf);
            foreach (var pr in se.rounds[se.week].games) if (pr != f.pair) SimOther(pr.home, pr.away);

            if (m.Plays)
            {
                st.apps++; st.goals += m.Goals; st.assists += m.Assists; st.ratings.Add(m.Rating);
                p.form.Add(m.Rating); if (p.form.Count > 5) p.form.RemoveAt(0);
                p.moral += (m.Rating - 6.5f) * 4 + (m.Result == 'w' ? 3 : m.Result == 'l' ? -3 : 0);
                p.coach += (m.Rating - 6.5f) * 3;
                p.fame += (float)((m.Goals * .9 + m.Assists * .5 + (m.Rating >= 8 ? 1 : 0)) * L.FameMult * (1 - p.fame / 115.0));
                p.energy -= m.Role == Role.Reserva ? 10 : 22;
                if (m.Sent) p.coach -= 5;
                if (p.energy < 30 && Rng.Chance(.1))
                {
                    p.injury = Rng.RangeInt(1, 3);
                    AddNews($"Você saiu de campo com dores. Lesão: {p.injury} rodada(s) fora.");
                }
            }
            else
            {
                if (m.Role == Role.Reserva) p.moral -= 3;
                p.energy += 8;
            }
            if (m.Role == Role.Lesionado && p.injury > 0) p.injury--;
            p.fame -= .3f;
            p.money += S.contract.salary + S.contract.bonus * m.Goals + (long)Math.Round(SponsorWeekly);
            WeekBusiness();
            Normalize();

            string line = $"{m.My.name} {m.Gf} x {m.Ga} {m.Opp.name}";
            if (m.Plays)
            {
                line += $". Sua nota: {Fmt.Rating(m.Rating)}";
                if (m.Goals > 0) line += $", {m.Goals} gol(s)";
                if (m.Assists > 0) line += $", {m.Assists} assistência(s)";
            }
            AddNews(line + ".");

            se.lastRole = m.Role; se.week++; se.rested = false; se.role = Role.None;
            if (se.week >= RoundsPerSeason) EndSeason();
            else
            {
                se.phase = "train";
                if (se.week == 9)
                {
                    GenSponsorOffers();
                    if (S.sponsorOffers.Count > 0) AddNews("Novas propostas de patrocínio chegaram.");
                }
            }
        }

        // ---------- fim de temporada ----------
        string Develop()
        {
            var p = S.player;
            int before = Ovr, gap = p.pot - before;
            for (int i = 0; i < 6; i++)
            {
                int d = 0;
                if (p.age <= 23 && gap > 0) d = Rng.RangeInt(0, 2) + (gap > 15 ? 1 : 0);
                else if (p.age <= 27 && gap > 0) d = Rng.RangeInt(0, 1);
                else if (p.age >= 31)
                {
                    bool physical = i == (int)Attr.Vel || i == (int)Attr.Fis;
                    d = -(physical ? Rng.RangeInt(1, 3) : Rng.RangeInt(0, 1));
                    if (p.age >= 34) d -= Rng.RangeInt(1, 2);
                }
                p.attrs[i] = (int)Clamp(p.attrs[i] + d, 10, 99);
            }
            int diff = Ovr - before;
            return diff > 0 ? $"Seu geral subiu {diff} ponto(s) nas férias."
                : diff < 0 ? $"A idade pesou: seu geral caiu {-diff} ponto(s)." : "Seu geral se manteve nas férias.";
        }

        void EndSeason()
        {
            var se = S.season; var p = S.player; var L = League(se.league); var st = se.stats;
            int rank = SortedTable().FindIndex(t => t.club == S.contract.club) + 1;
            float avg = AvgRating;
            var sm = new SeasonSummary { rank = rank };

            if (rank == 1) sm.titles.Add($"Campeão da {L.Name} {se.year}");
            if (st.goals >= se.topScorer) sm.awards.Add($"Artilheiro da {L.Name} {se.year}");
            if (avg >= 7.5 && st.apps >= 10) sm.awards.Add($"Craque da {L.Name} {se.year}");
            if (p.age <= 21 && avg >= 7 && st.apps >= 10) sm.awards.Add($"Revelação da {L.Name} {se.year}");
            if (Ovr >= 86 && avg >= 7.6 && p.fame >= 75) sm.awards.Add($"Prêmio Craque Mundial {se.year}");

            foreach (var d in S.activeSponsors.ToList())
            {
                if (GoalMet(d)) { p.money += d.bonus; sm.notes.Add($"{d.brand} pagou o bônus de {Fmt.Money(d.bonus)} pela meta batida."); }
                else sm.notes.Add($"Você não bateu a meta da {d.brand}.");
                d.seasonsLeft--;
                if (d.seasonsLeft <= 0) { S.activeSponsors.Remove(d); sm.notes.Add($"O contrato com a {d.brand} terminou."); }
            }

            var c = MyClub;
            S.career.Add(new CareerRecord { year = se.year, club = c.name, c1 = c.c1, c2 = c.c2, rank = rank, apps = st.apps, goals = st.goals,
                assists = st.assists, avg = st.apps > 0 ? (float)Math.Round(avg, 1) : 0 });
            S.titles.AddRange(sm.titles);
            S.awards.AddRange(sm.awards);
            p.fame += (float)((sm.titles.Count * 4 + sm.awards.Count * 3) * (1 - p.fame / 120.0));
            S.contract.years--;
            p.age++;
            sm.notes.Add(Develop());
            Normalize();

            se.phase = "end";
            se.summary = sm;
            S.offers = GenOffers();
            AddNews($"Temporada {se.year} encerrada. {c.name} terminou em {rank}º lugar.");
            if (S.offers.Count > 0) AddNews($"Você recebeu {S.offers.Count} proposta(s) de contrato.");
        }

        ContractOffer MakeOffer(Club c, bool renewal)
        {
            var p = S.player;
            long sal = R100(FairSalary(c) * Rng.RangeF(.9, 1.1) * (renewal ? 1 : 1.05));
            return new ContractOffer
            {
                id = NewId(), club = c.id, salary = sal, years = p.age >= 31 ? Rng.RangeInt(1, 2) : Rng.RangeInt(2, 5),
                bonus = GoalBonusFor(sal), signing = R1000(sal * (renewal ? Rng.RangeInt(2, 4) : Rng.RangeInt(4, 10))),
                clause = R10k(MarketValue() * Rng.RangeF(1.6, 2.6)), renewal = renewal
            };
        }

        List<ContractOffer> GenOffers()
        {
            var p = S.player; int o = Ovr; var se = S.season;
            bool Eligible(Club c) => c.league == "br" || (c.league == "ib" && (p.fame >= 22 || o >= 68)) || (c.league == "en" && (p.fame >= 32 || o >= 72));
            var cands = S.clubs.Where(c => c.id != S.contract.club && c.str >= o - 12 && c.str <= o + 5 && Eligible(c)).ToList();
            Rng.Shuffle(cands);
            int n = Math.Min(4, 1 + (p.fame > 30 ? 1 : 0) + (se.forceExit ? 1 : 0) + (AvgRating >= 7 ? 1 : 0));
            var list = cands.Take(n).Select(c => MakeOffer(c, false)).ToList();
            if (p.coach >= 30 && o >= MyClub.str - 10) list.Insert(0, MakeOffer(MyClub, true));
            if (list.Count == 0)
            {
                var fallback = S.clubs.Where(c => c.league == "br" && c.id != S.contract.club).OrderBy(c => c.str).First();
                list.Add(MakeOffer(fallback, false));
            }
            return list;
        }

        public bool CanStartNextSeason => S.contract.years > 0;
        public bool MustRetire => S.player.age >= 38;

        public void NextSeason()
        {
            S.year++;
            foreach (var c in S.clubs) c.str = (int)Clamp(c.str + Rng.RangeInt(-2, 2), 50, 92);
            S.offers.Clear();
            StartSeason();
            AddNews($"Começa a temporada {S.year}. Objetivo: evoluir e brilhar no {MyClub.name}.");
        }

        public string AcceptOffer(string id)
        {
            var o = S.offers.Find(x => x.id == id);
            if (o == null) return null;
            var c = ClubById(o.club);
            bool changed = o.club != S.contract.club;
            S.contract = new Contract { club = o.club, salary = o.salary, years = o.years, bonus = o.bonus, clause = o.clause };
            S.player.money += o.signing;
            if (changed)
            {
                S.player.coach = 50; S.player.moral += 5; S.player.fame += (float)(3 * League(c.league).FameMult);
                AddNews($"Contratado! Você assinou com o {c.name} por {o.years} temporada(s). Luvas de {Fmt.Money(o.signing)}.");
            }
            else AddNews($"Renovação assinada com o {c.name} por {o.years} temporada(s).");
            Normalize();
            S.offers.Clear();
            return changed ? $"Bem-vindo ao {c.name}!" : "Contrato renovado.";
        }

        public string Haggle(string id)
        {
            var o = S.offers.Find(x => x.id == id);
            if (o == null || o.haggled) return null;
            o.haggled = true;
            var c = ClubById(o.club);
            double chance = Clamp(.5 + (Ovr - c.str) * .04 + S.player.fame / 200.0, .2, .85);
            if (Rng.Chance(chance))
            {
                o.salary = R100(o.salary * 1.15); o.signing = R1000(o.signing * 1.2); o.bonus = R100(o.bonus * 1.15);
                return $"O {c.name} aceitou melhorar a proposta.";
            }
            S.offers.Remove(o);
            return $"O {c.name} retirou a proposta.";
        }

        public string Negotiate(long ask, int years)
        {
            var se = S.season; var c = MyClub; var p = S.player;
            double max = FairSalary(c) * 1.2;
            string msg;
            if (ask <= max)
            {
                S.contract.salary = ask; S.contract.years = years; S.contract.bonus = GoalBonusFor(ask);
                S.contract.clause = R10k(MarketValue() * 2);
                se.negotiated = true; p.coach += 2; Counter = 0;
                AddNews($"Novo contrato com o {c.name}: {Fmt.Money(ask)} por semana, {years} temporada(s).");
                msg = "A diretoria aceitou. Contrato assinado.";
            }
            else if (ask <= max * 1.2)
            {
                Counter = R100(max);
                msg = $"A diretoria fez uma contraproposta de {Fmt.Money(Counter)} por semana.";
            }
            else
            {
                se.attempts--; p.coach -= 3; Counter = 0;
                msg = se.attempts > 0 ? $"A diretoria recusou. Restam {se.attempts} tentativa(s) nesta temporada."
                    : "A diretoria encerrou as conversas até a próxima temporada.";
                if (se.attempts <= 0) se.negotiated = true;
            }
            Normalize();
            return msg;
        }

        public string Buy(string itemId)
        {
            var it = Array.Find(GameData.Items, x => x.Id == itemId);
            if (it == null || S.owned.Contains(it.Id) || S.player.money < it.Price) return null;
            S.player.money -= it.Price;
            S.owned.Add(it.Id);
            S.player.fame += it.Fame; S.player.moral += it.Moral;
            Normalize();
            AddNews($"Você comprou: {it.Name.ToLowerInvariant()}.");
            return "Compra feita.";
        }
    }
}
