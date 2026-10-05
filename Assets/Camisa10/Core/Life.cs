using System;
using System.Linq;

namespace Camisa10.Core
{
    /// <summary>Nomes e profissões fictícios da vida fora de campo (namorada e rival não são pessoas reais).</summary>
    public static class LifeData
    {
        public static readonly string[] PartnerNames =
        {
            "Camila", "Juliana", "Beatriz", "Larissa", "Mariana", "Isabela", "Fernanda", "Gabriela", "Letícia", "Carolina",
            "Bruna", "Amanda", "Rafaela", "Natália", "Yasmin", "Luana", "Débora", "Priscila", "Thaís", "Bianca",
        };

        /// <summary>Profissão e se é famosa (famosa: mais fama, mais paparazzi, mais ciúme nas redes).</summary>
        public static readonly (string job, bool famous)[] PartnerJobs =
        {
            ("influenciadora digital", true), ("cantora", true), ("atriz de novela", true), ("modelo", true), ("apresentadora de TV", true),
            ("jornalista esportiva", false), ("nutricionista", false), ("estudante de medicina", false), ("advogada", false),
            ("fisioterapeuta", false), ("arquiteta", false), ("professora", false), ("dentista", false), ("designer", false),
        };

        public static readonly string[] RivalFirst =
        {
            "Rafael", "Matheus", "Gustavo", "Kaique", "Vinícius", "Enzo", "Davi", "Luan", "Breno", "Caio", "Thiago", "Ruan", "Wesley", "Igor",
        };

        public static readonly string[] RivalLast =
        {
            "Duarte", "Moreira", "Bastos", "Siqueira", "Prates", "Valente", "Rangel", "Furtado", "Peixoto", "Aragão", "Toledo", "Brandão",
        };

        public static string StageName(int stage)
        {
            switch (stage)
            {
                case 1: return "Ficando";
                case 2: return "Namorando";
                case 3: return "Noivos";
                case 4: return "Casados";
                default: return "Solteiro";
            }
        }

        /// <summary>Programa com os amigos do elenco (aba Vida): gasta um horário da agenda e aproxima o vestiário.</summary>
        public class SocialDef
        {
            public string Id, Name, Hint, Icon;
            public double Cost;      // em reais, ajustado pelo custo de vida do país
            public float Squad, Moral, Energy, Fame;
            public Func<Game, bool> Available;
        }

        public static readonly SocialDef[] Social =
        {
            new SocialDef { Id = "games", Icon = "GAME", Name = "Noite de games online", Hint = "Campeonato de videogame com a turma do time.", Cost = 0, Squad = 3, Moral = 2, Energy = -1 },
            new SocialDef { Id = "pagode", Icon = "SOM", Name = "Pagode na casa de um companheiro", Hint = "Você leva as bebidas. Resenha garantida.", Cost = 600, Squad = 6, Moral = 4, Energy = -6 },
            new SocialDef { Id = "futevolei", Icon = "BOLA", Name = "Futevôlei com o elenco", Hint = "Na praia ou na quadra, com foto para as redes.", Cost = 300, Squad = 4, Moral = 3, Energy = -5, Fame = .4f },
            new SocialDef { Id = "aniversario", Icon = "FEST", Name = "Aniversário de um companheiro", Hint = "Leve um presente à altura.", Cost = 1500, Squad = 6, Moral = 3, Energy = -4 },
            new SocialDef { Id = "churrasco", Icon = "CASA", Name = "Churrasco na sua casa", Hint = "Precisa de casa própria. O elenco inteiro aparece.", Cost = 2500, Squad = 9, Moral = 5, Energy = -3, Fame = .3f,
                Available = g => g.S.owned.Exists(id => id.StartsWith("casa")) },
            new SocialDef { Id = "viagem", Icon = "VIAG", Name = "Viagem de folga com a turma", Hint = "Dois dias fora com os amigos do time. Descansa e une o grupo.", Cost = 12000, Squad = 10, Moral = 8, Energy = 6 },
        };

        /// <summary>Manutenção semanal de um bem: seguro, IPVA, condomínio, IPTU, funcionários (fração do preço).</summary>
        public static double UpkeepRate(string cat) => cat == "Carro" ? .003 : cat == "Casa" ? .0012 : .0004;
    }

    /// <summary>
    /// Vida fora de campo e dinheiro do dia a dia: o salário chega com imposto (de acordo com o país), comissão do
    /// empresário, custo de vida e manutenção do que você comprou. Namorada e rival têm vida própria a cada rodada.
    /// </summary>
    public partial class Game
    {
        public const double AgentFee = .08;

        public int SeasonRounds => S.season.rounds != null && S.season.rounds.Count > 0 ? S.season.rounds.Count : RoundsPerSeason;

        void EnsureLife()
        {
            if (S.love == null) S.love = new Relationship();
            if (S.rival == null) S.rival = new Rival();
            if (S.lastWeek == null) S.lastWeek = new Statement();
            if (string.IsNullOrEmpty(S.rival.name) && S.season.league != null) NewRival();
        }

        // ---------- custo de vida ----------
        double CostMult => (League(MyClub.league).WageMult + 1) / 2;
        public double TaxRate => League(MyClub.league).Tax;
        public long LivingCost => R100(700 * CostMult + S.contract.salary * .06);
        public long PartnerCost => S.love.stage >= 2 ? R100(250 + S.contract.salary * .025) * (S.love.stage >= 4 ? 2 : 1) : 0;

        public static long ItemUpkeep(GameData.Item it) => R100(it.Price * LifeData.UpkeepRate(it.Cat));
        public long UpkeepWeekly => S.owned.Sum(id => { var it = Array.Find(GameData.Items, x => x.Id == id); return it == null ? 0 : ItemUpkeep(it); });

        /// <summary>Salário e prêmios da rodada com tudo o que sai da conta. Negócios já entraram em WeekBusiness.</summary>
        void PayWeek(int goals)
        {
            var w = new Statement
            {
                salary = S.contract.salary,
                bonus = S.contract.bonus * goals,
                sponsors = (long)Math.Round(SponsorWeekly),
                business = S.businessIncome,
                taxRate = (float)TaxRate,
            };
            w.tax = R100((w.salary + w.bonus) * TaxRate + w.sponsors * Math.Min(TaxRate, .15)); // patrocínio entra pela empresa do jogador
            w.agent = R100((w.salary + w.sponsors) * AgentFee);
            w.living = LivingCost;
            w.upkeep = UpkeepWeekly;
            w.partner = PartnerCost;
            S.lastWeek = w;
            // os negócios já foram somados na virada; aqui entra o resto
            long net = w.salary + w.bonus + w.sponsors - w.Expenses;
            S.player.money += net;
            S.seasonNet += net + w.business;
            Debt();
        }

        /// <summary>No vermelho a moral cai; depois de 4 rodadas o banco toma o bem mais caro (vendido por 60%).</summary>
        void Debt()
        {
            var p = S.player;
            if (p.money >= 0) { S.debtWeeks = 0; return; }
            S.debtWeeks++;
            p.moral -= 3;
            if (S.debtWeeks == 1) AddNews("Sua conta ficou no vermelho. Corte gastos ou venda alguma coisa.");
            if (S.debtWeeks >= 4)
            {
                var it = GameData.Items.Where(x => S.owned.Contains(x.Id)).OrderByDescending(x => x.Price).FirstOrDefault();
                if (it != null)
                {
                    S.owned.Remove(it.Id);
                    p.money += R1000(it.Price * .6);
                    p.moral -= 8; p.fame -= 2;
                    AddNews($"Dívida: o banco tomou {it.Name.ToLowerInvariant()} e vendeu por {Fmt.Money(R1000(it.Price * .6))}.");
                    S.debtWeeks = 0;
                }
            }
        }

        /// <summary>Vende um bem por 70% do preço (carro usado, imóvel com desconto para vender rápido).</summary>
        public string SellItem(string itemId)
        {
            var it = Array.Find(GameData.Items, x => x.Id == itemId);
            if (it == null || !S.owned.Contains(it.Id)) return null;
            long v = R1000(it.Price * .7);
            S.owned.Remove(it.Id);
            S.player.money += v;
            S.player.moral -= 2;
            Normalize();
            AddNews($"Você vendeu: {it.Name.ToLowerInvariant()}, por {Fmt.Money(v)}.");
            return $"Vendido por {Fmt.Money(v)}.";
        }

        // ---------- programas com o elenco ----------
        public long SocialCost(LifeData.SocialDef d) => R100(d.Cost * CostMult);

        public bool CanSocial(string id, out string why)
        {
            var d = Array.Find(LifeData.Social, x => x.Id == id);
            why = null;
            if (d == null) { why = "Indisponível."; return false; }
            if (S.season.phase == "end") { why = "Férias."; return false; }
            if (ActionsLeft <= 0) { why = "Agenda cheia."; return false; }
            if (S.season.doneActions.Contains("s:" + id)) { why = "Já feito."; return false; }
            if (d.Available != null && !d.Available(this)) { why = "Precisa de casa."; return false; }
            if (d.Energy < 0 && S.player.energy + d.Energy < 5) { why = "Sem energia."; return false; }
            if (SocialCost(d) > S.player.money) { why = "Sem dinheiro."; return false; }
            return true;
        }

        /// <summary>Faz o programa com os amigos: gasta um horário da agenda, dinheiro e energia; aproxima o vestiário.</summary>
        public string DoSocial(string id)
        {
            if (!CanSocial(id, out string why)) return why;
            var d = Array.Find(LifeData.Social, x => x.Id == id);
            var p = S.player;
            S.season.actionsUsed++;
            S.season.doneActions.Add("s:" + id);
            long cost = SocialCost(d);
            p.money -= cost; p.energy += d.Energy; p.moral += d.Moral; p.fame += d.Fame;
            AddSquad(d.Squad);
            string mate = Teammate();
            string msg;
            switch (id)
            {
                case "games": msg = $"Você e {mate} viraram a madrugada no online. Ele pediu revanche."; break;
                case "pagode": msg = $"Pagode na casa de {mate} até tarde. O grupo está fechado com você."; break;
                case "futevolei": msg = $"Futevôlei com {mate} e a turma. O vídeo bombou nas redes."; break;
                case "aniversario": msg = $"Aniversário de {mate}: o seu presente ({Fmt.Money(cost)}) foi o assunto da festa."; break;
                case "churrasco": msg = "Churrasco na sua casa com o elenco inteiro. Até o técnico apareceu para uma foto."; break;
                default: msg = $"Viagem com {mate} e a turma ({Fmt.Money(cost)}). Voltaram mais unidos e descansados."; break;
            }
            // noite longa às vezes vira fofoca
            if ((id == "pagode" || id == "viagem") && Rng.Chance(.15)) { p.coach -= 3; msg += " Teve foto vazada e o técnico torceu o nariz."; }
            Normalize();
            if (S.season.phase == "match") S.season.role = ComputeRole();
            return msg;
        }

        // ---------- namorada ----------
        public void StartDating()
        {
            var (job, famous) = Rng.Pick(LifeData.PartnerJobs);
            S.love = new Relationship { name = Rng.Pick(LifeData.PartnerNames), job = job, famous = famous, stage = 1, affection = 55 };
        }

        public string LoveLine()
        {
            var l = S.love;
            if (l.stage == 0) return "Solteiro";
            return $"{LifeData.StageName(l.stage)}: {l.name}, {l.job}";
        }

        void Breakup(string why)
        {
            var l = S.love; var p = S.player;
            bool married = l.stage >= 4;
            p.moral -= married ? 20 : 12;
            if (married)
            {
                long half = Math.Max(0, R1000(p.money * .3));
                p.money -= half;
                AddNews($"Divórcio: você e {l.name} se separaram. {why} A partilha levou {Fmt.Money(half)}.");
            }
            else AddNews($"Fim de namoro: você e {l.name} terminaram. {why}");
            if (l.famous) p.fame += 1; // a separação vira assunto
            S.love = new Relationship();
        }

        void LoveWeek()
        {
            var l = S.love; var p = S.player;
            if (l.stage == 0) return;
            l.weeks++;
            l.affection -= l.stage >= 3 ? 1.6f : 2.4f; // sem atenção a relação esfria
            if (l.affection >= 70) p.moral += 1.2f;
            if (l.famous && l.stage >= 2) p.fame += .12f * (float)(1 - p.fame / 120.0);
            if (l.affection < 12) Breakup(l.stage >= 2 ? "Ela disse que você só pensa em futebol." : "Não passou de uma fase.");
            l.affection = (float)Clamp(l.affection, 0, 100);
        }

        // ---------- rival ----------
        void NewRival()
        {
            var others = S.clubs.Where(c => c.league == MyClub.league && c.id != S.contract.club).ToList();
            if (others.Count == 0) return;
            S.rival = new Rival
            {
                name = Rng.Pick(LifeData.RivalFirst) + " " + Rng.Pick(LifeData.RivalLast),
                club = others[Rng.RangeInt(0, others.Count - 1)].id,
                age = S.player.age + Rng.RangeInt(-1, 1), heat = 20,
            };
        }

        public Club RivalClub => ClubById(S.rival.club);

        double RivalRate => S.player.pos == "ATA" ? .42 : S.player.pos == "MEI" ? .24 : .07;

        /// <summary>A cada rodada o rival joga também; contra o clube dele vira duelo pessoal.</summary>
        void RivalWeek(MatchEngine m)
        {
            var r = S.rival;
            if (string.IsNullOrEmpty(r.name) || RivalClub == null) return;
            int g = Rng.Poisson(RivalRate * (RivalClub.str / 72.0));
            r.goals += g; r.careerGoals += g;
            r.heat = (float)Clamp(r.heat - .6, 0, 100);
            var f = CurrentFixture();
            if (f.opp.id != r.club || m == null || !m.Plays) return;
            r.duels++;
            float his = (float)Math.Round(Rng.RangeF(5.8, 8.2), 1);
            bool won = m.Rating >= his;
            if (won) r.won++;
            r.heat = (float)Clamp(r.heat + 8, 0, 100);
            S.player.moral += won ? 4 : -3;
            S.player.fame += won ? .8f : 0;
            AddNews(won ? $"Duelo com {r.name}: sua nota {Fmt.Rating(m.Rating)} contra {Fmt.Rating(his)} dele. A imprensa diz que você levou a melhor."
                : $"Duelo com {r.name}: ele brilhou mais (nota {Fmt.Rating(his)} contra {Fmt.Rating(m.Rating)}). As redes não perdoaram.");
        }

        /// <summary>Troca de liga: o rival também se muda (a rivalidade continua) ou some do noticiário.</summary>
        void RivalNewSeason()
        {
            var r = S.rival;
            if (string.IsNullOrEmpty(r.name)) { NewRival(); return; }
            r.goals = 0;
            r.age++;
            RelocateRival();
        }

        /// <summary>O rival vai para um clube forte da sua liga quando você muda de liga (ou de clube para o dele).</summary>
        void RelocateRival()
        {
            var r = S.rival;
            if (string.IsNullOrEmpty(r.name)) return;
            if (RivalClub == null || RivalClub.league != MyClub.league || r.club == S.contract.club)
            {
                var others = S.clubs.Where(c => c.league == MyClub.league && c.id != S.contract.club).OrderByDescending(c => c.str).Take(5).ToList();
                if (others.Count == 0) return;
                r.club = others[Rng.RangeInt(0, others.Count - 1)].id;
                AddNews($"{r.name}, seu rival de sempre, foi contratado pelo {RivalClub.name}. A disputa continua.");
            }
        }

        void LifeWeek(MatchEngine m)
        {
            LoveWeek();
            RivalWeek(m);
        }
    }
}
