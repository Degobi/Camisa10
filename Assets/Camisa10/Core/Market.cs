using System;
using System.Collections.Generic;
using System.Linq;

namespace Camisa10.Core
{
    /// <summary>
    /// Mercado durante a temporada: rumores na imprensa, janela de transferências no meio do campeonato (com multa rescisória,
    /// empréstimo para quem não joga e o empresário negociando), pedido para ser negociado e volta do empréstimo no fim do ano.
    /// Arábia e MLS continuam só por convite/sondagem.
    /// </summary>
    public partial class Game
    {
        public bool WindowOpen => S.windowOffers.Count > 0 && S.season.phase != "end";
        public bool OnLoan => !string.IsNullOrEmpty(S.loanFrom);
        public Club LoanParent => OnLoan ? ClubById(S.loanFrom) : null;

        void EnsureMarket()
        {
            if (S.windowOffers == null) S.windowOffers = new List<ContractOffer>();
        }

        /// <summary>Clubes que podem querer você agora (mesma régua das propostas de fim de temporada).</summary>
        List<Club> InterestedClubs(int slack)
        {
            var p = S.player; int o = Ovr;
            bool Eligible(Club c)
            {
                var L = League(c.league);
                if (L == null) return false;
                if (c.league == "sa" || c.league == "us") return S.interestLeague == c.league;
                if (L.MinFame <= 0 && L.MinOvr <= 0) return true;
                if (p.age < L.MinAge && p.fame < L.MinFame + 25) return false;
                return p.fame >= L.MinFame || o >= L.MinOvr;
            }
            return S.clubs.Where(c => c.id != S.contract.club && c.id != S.loanFrom && c.str >= o - 10 - slack && c.str <= o + 5 && Eligible(c)).ToList();
        }

        // ---------- rumores ----------
        void MarketWeek()
        {
            var se = S.season;
            int played = se.week + 1; // rodada que acabou de ser jogada
            // rumores nas rodadas antes da janela
            if (!OnLoan && string.IsNullOrEmpty(se.rumor) && played >= WindowRound - 4 && played < WindowRound && (S.player.fame >= 12 || se.transferRequest || AvgRating >= 7.2))
            {
                if (Rng.Chance(se.transferRequest ? .6 : .3))
                {
                    var cands = InterestedClubs(0).Where(c => c.str >= MyClub.str - 2).ToList();
                    if (cands.Count > 0)
                    {
                        var c = cands[Rng.RangeInt(0, cands.Count - 1)];
                        se.rumor = c.id;
                        AddNews($"Rumor: o {c.name} estaria interessado em você.");
                        Mail("imprensa", Rng.Pick(InboxData.Press), $"Rumor: {c.name} de olho em você",
                            $"Fontes ligadas ao {c.name} dizem que o clube acompanha os seus jogos e pode fazer uma proposta na janela do meio da temporada. Você confirma o interesse?",
                            "rumor", c.id, "\"Fico feliz, mas estou focado aqui\"", "\"Todo jogador sonha com um clube assim\"");
                    }
                }
            }
            if (played == WindowRound) OpenWindow();
            if (played == WindowRound + 2) CloseWindow();
        }

        // ---------- janela do meio da temporada ----------
        void OpenWindow()
        {
            var se = S.season; var p = S.player;
            S.windowOffers.Clear();
            if (OnLoan || p.injury > 2) return;
            var cands = InterestedClubs(0).Where(c => c.str >= MyClub.str - (se.transferRequest ? 8 : 0)).ToList();
            Rng.Shuffle(cands);
            var rumor = ClubById(se.rumor);
            if (rumor != null) { cands.Remove(rumor); cands.Insert(0, rumor); }
            int n = (se.transferRequest ? 2 : 0) + (p.fame > 30 ? 1 : 0) + (AvgRating >= 7.3 ? 1 : 0);
            n = Math.Min(3, n);
            foreach (var c in cands.Take(n))
            {
                if (!se.transferRequest && c != rumor && !Rng.Chance(.55)) continue;
                var o = MakeOffer(c, false);
                o.signing = R1000(o.signing * .6); // no meio do ano as luvas são menores
                // clube grande paga a multa: a diretoria não pode segurar
                o.clauseMet = c.str >= MyClub.str + 4 && p.fame >= 25 && Rng.Chance(.3);
                S.windowOffers.Add(o);
                string what = o.clauseMet ? $"O {c.name} depositou a sua multa rescisória ({Fmt.Money(S.contract.clause)}). A diretoria não tem como segurar: a decisão é só sua."
                    : $"O {c.name} fez uma proposta oficial. Salário de {Fmt.Money(o.salary)} por semana, {o.years} temporada(s) e luvas de {Fmt.Money(o.signing)}." +
                      (se.transferRequest ? "" : " A diretoria ainda precisa liberar.");
                Mail("empresario", AgentName, $"Proposta do {c.name}", what + " A janela fecha em duas rodadas.", "janela", o.id,
                    "Aceitar a proposta", "Pedir ao empresário para negociar", "Recusar");
            }
            // empréstimo: jovem sem minutos vai rodar num clube menor
            int played = se.week + 1;
            if (p.age <= 21 && se.stats.apps < played * .4)
            {
                var small = S.clubs.Where(c => c.str <= MyClub.str - 4 && c.str >= MyClub.str - 16 && (c.league == MyClub.league || c.league == "br") && c.id != S.contract.club).ToList();
                if (small.Count > 0)
                {
                    var c = small[Rng.RangeInt(0, small.Count - 1)];
                    var o = new ContractOffer { id = NewId(), club = c.id, salary = S.contract.salary, bonus = S.contract.bonus, clause = S.contract.clause, years = 0, loan = true };
                    S.windowOffers.Add(o);
                    Mail("diretoria", BoardName, "Proposta de empréstimo",
                        $"Você está jogando pouco e isso atrapalha a sua evolução. O {c.name} quer você emprestado até o fim da temporada, com o salário pago por eles. Você volta para cá no fim do ano.",
                        "janela", o.id, "Aceitar o empréstimo", "Ficar e brigar por espaço");
                }
            }
            if (S.windowOffers.Count > 0) AddNews($"Janela de transferências aberta: {S.windowOffers.Count} proposta(s) para você.");
            else if (se.transferRequest) Mail("empresario", AgentName, "Janela sem propostas", "Ofereci você para vários clubes, mas ninguém fez proposta concreta nesta janela. No fim da temporada o mercado se abre de novo.");
        }

        void CloseWindow()
        {
            if (S.windowOffers.Count == 0) return;
            S.windowOffers.Clear();
            foreach (var m in S.inbox.Where(x => x.kind == "janela" && x.NeedsReply)) m.answer = "A janela de transferências fechou sem resposta.";
            AddNews("A janela de transferências fechou.");
        }

        /// <summary>Resposta à proposta da janela pela caixa de mensagens.</summary>
        string WindowReply(InboxMsg m, int option)
        {
            var o = S.windowOffers.Find(x => x.id == m.data);
            if (o == null) return "A proposta não está mais de pé.";
            if (option == 0) return AcceptWindowOffer(o.id);
            if (!o.loan && option == 1)
            {
                string r = HaggleWindow(o.id);
                // a proposta melhorada volta para a caixa, esperando resposta
                var o2 = S.windowOffers.Find(x => x.id == o.id);
                if (o2 != null) Mail("empresario", AgentName, $"Nova proposta do {ClubById(o2.club).name}", $"Negociei e eles melhoraram: {Fmt.Money(o2.salary)} por semana e luvas de {Fmt.Money(o2.signing)}.",
                    "janela", o2.id, "Aceitar a proposta", "Recusar");
                return r;
            }
            S.windowOffers.Remove(o);
            if (o.loan) { S.player.coach += 2; return "Você fica e vai brigar por espaço. O técnico gostou da disposição."; }
            AddFans(3);
            return $"Você recusou o {ClubById(o.club).name}. A torcida gostou de saber.";
        }

        public string HaggleWindow(string id)
        {
            var o = S.windowOffers.Find(x => x.id == id);
            if (o == null || o.haggled || o.loan) return null;
            o.haggled = true;
            var c = ClubById(o.club);
            if (Rng.Chance(Clamp(.5 + (Ovr - c.str) * .04 + S.player.fame / 200.0, .2, .85)))
            {
                o.salary = R100(o.salary * 1.12); o.signing = R1000(o.signing * 1.2); o.bonus = GoalBonusFor(o.salary);
                return $"Seu empresário arrancou mais do {c.name}.";
            }
            S.windowOffers.Remove(o);
            return $"O {c.name} não gostou da pressão e retirou a proposta.";
        }

        /// <summary>Aceita a proposta da janela: a diretoria pode segurar (a menos que paguem a multa ou você tenha pedido para sair).</summary>
        public string AcceptWindowOffer(string id)
        {
            var o = S.windowOffers.Find(x => x.id == id);
            if (o == null) return null;
            var c = ClubById(o.club);
            if (!o.loan && !o.clauseMet && !S.season.transferRequest && !Rng.Chance(.65))
            {
                S.windowOffers.Remove(o);
                S.player.moral -= 5;
                Mail("diretoria", BoardName, "Negociação recusada", $"Não vamos vender você para o {c.name} no meio da temporada. Você é importante para o nosso projeto.");
                return $"A diretoria não liberou a sua saída para o {c.name}.";
            }
            S.windowOffers.Clear();
            foreach (var m in S.inbox.Where(x => x.kind == "janela" && x.NeedsReply)) m.answer = "Você já decidiu o seu destino nesta janela.";
            MoveMidSeason(o);
            return o.loan ? $"Emprestado ao {c.name} até o fim da temporada!" : $"Bem-vindo ao {c.name}!";
        }

        /// <summary>Troca de clube no meio da temporada. Outra liga: o campeonato é remontado e você segue na mesma rodada.</summary>
        void MoveMidSeason(ContractOffer o)
        {
            var se = S.season; var p = S.player;
            var old = MyClub; var c = ClubById(o.club);
            if (o.loan) S.loanFrom = old.id;
            if (o.loan) S.contract.club = c.id;
            else
            {
                S.contract = new Contract { club = c.id, salary = o.salary, years = o.years, bonus = o.bonus, clause = o.clause };
                long net = R1000(o.signing * (1 - TaxRate - AgentFee));
                p.money += net; S.seasonNet += net;
            }
            ReputationNewClub();
            p.coach = 50; p.moral += 5;
            if (!o.loan) p.fame += (float)(2 * League(c.league).FameMult);
            se.transferRequest = false; se.forceExit = false; se.rumor = null; se.negotiated = true;
            S.interestLeague = null;
            if (c.league != se.league) SwitchLeague(c.league);
            else MakeObjectives(se.week, (SeasonRounds - se.week) / (double)SeasonRounds);
            if (S.rival.club == c.id) RelocateRival();
            AddNews(o.loan ? $"Empréstimo: você vai defender o {c.name} até o fim da temporada." : $"Transferência no meio da temporada: você deixou o {old.name} e assinou com o {c.name}.");
            Mail("tecnico", CoachName, $"Bem-vindo ao {c.name}", "Chegou na hora certa. O campeonato está na metade e eu preciso de você pronto já na próxima rodada. Os objetivos foram ajustados para o resto da temporada.");
            CheckAchievements(null);
            Normalize();
            if (se.phase == "match") se.role = ComputeRole();
        }

        public void DeclineWindowOffer(string id)
        {
            var o = S.windowOffers.Find(x => x.id == id);
            if (o == null) return;
            S.windowOffers.Remove(o);
            foreach (var m in S.inbox.Where(x => x.kind == "janela" && x.data == id && x.NeedsReply)) m.answer = "Você recusou pela aba Contrato.";
        }

        /// <summary>Mudou de liga com a temporada em andamento: nova tabela, rodadas já jogadas simuladas, artilharia nova, sem copa.</summary>
        void SwitchLeague(string lg)
        {
            var se = S.season;
            int week = se.week;
            var ids = S.clubs.Where(x => x.league == lg).Select(x => x.id).ToList();
            Rng.Shuffle(ids);
            se.league = lg;
            se.rounds = MakeFixtures(ids);
            se.table = ids.Select(id => new TableRow { club = id }).ToList();
            for (int w = 0; w < week && w < se.rounds.Count; w++)
                foreach (var pr in se.rounds[w].games) SimOther(pr.home, pr.away);
            InitScorers();
            for (int w = 0; w < week; w++) ScorersRound();
            if (!se.cupWon) se.cupOut = true; // já jogou a copa pelo outro clube
            MakeObjectives(week, (SeasonRounds - week) / (double)SeasonRounds);
            if (RivalClub == null || RivalClub.league != lg) RelocateRival();
        }

        // ---------- pedido para sair ----------
        public bool CanRequestTransfer => S.season.phase != "end" && !S.season.transferRequest && !OnLoan && S.season.week < WindowRound;

        public string RequestTransfer()
        {
            if (!CanRequestTransfer) return null;
            var p = S.player;
            S.season.transferRequest = true; S.season.forceExit = true;
            p.coach -= 12; AddFans(-10); AddSquad(-4);
            Normalize();
            AddNews("Você pediu para ser negociado. A notícia caiu como uma bomba no clube.");
            Mail("diretoria", BoardName, "Pedido de transferência", $"Recebemos o seu pedido. Vamos ouvir propostas na janela do meio da temporada (depois da rodada {WindowRound}) e no fim do ano. Até lá, esperamos profissionalismo.");
            return "Pedido feito. Você está na lista de transferências.";
        }

        /// <summary>Fim de temporada do emprestado: volta para o clube dono do contrato.</summary>
        void EndLoan(SeasonSummary sm)
        {
            if (!OnLoan) return;
            var parent = LoanParent; var loanClub = MyClub;
            S.loanFrom = null;
            if (parent == null) return;
            S.contract.club = parent.id;
            ReputationNewClub();
            float avg = AvgRating;
            S.player.coach = (float)Clamp(50 + (S.season.stats.apps > 0 ? (avg - 6.6) * 15 : -5), 30, 75);
            sm.notes.Add($"Fim do empréstimo ao {loanClub.name}: você volta para o {parent.name}.");
            Mail("diretoria", "Diretoria · " + parent.name, "De volta para casa",
                S.season.stats.apps > 0 ? $"Acompanhamos o seu empréstimo ao {loanClub.name}: {S.season.stats.apps} jogos e nota média {Fmt.Rating(avg)}. Bem-vindo de volta." : "O empréstimo terminou. Bem-vindo de volta.");
        }
    }
}
