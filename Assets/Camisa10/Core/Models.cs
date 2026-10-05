using System;
using System.Collections.Generic;

namespace Camisa10.Core
{
    // Classes simples com campos públicos: compatíveis com JsonUtility da Unity para salvar.

    [Serializable] public class Club { public string id, name, league; public int str; public string c1, c2; }

    [Serializable] public class BootStyle { public string brand = ""; public string c1 = "#111111", c2 = "#FFFFFF", sole = "#222222"; }

    [Serializable]
    public class Player
    {
        public string name, pos;
        public int age, pot, injury;
        public int[] attrs = new int[6];
        public float[] xp = new float[6];
        public float energy, moral, fame, coach;
        public float squad, fans; // reputação com o elenco e com a torcida (0 a 100)
        public long money;
        public List<float> form = new List<float>();
        public BootStyle boot = new BootStyle();
        public string celebration = ""; // comemoração escolhida ("" = uma diferente a cada gol)
        public int Get(Attr a) => attrs[(int)a];
    }

    [Serializable] public class Contract { public string club; public long salary, bonus, clause; public int years; }
    [Serializable] public class Pair { public string home, away; }
    [Serializable] public class Round { public List<Pair> games = new List<Pair>(); }

    [Serializable]
    public class TableRow
    {
        public string club;
        public int p, w, d, l, gf, ga, pts;
        public int Gd => gf - ga;
    }

    [Serializable] public class SeasonStats { public int apps, goals, assists, motm; public List<float> ratings = new List<float>(); }

    [Serializable]
    public class SeasonSummary
    {
        public int rank;
        public List<string> titles = new List<string>(), awards = new List<string>(), notes = new List<string>();
    }

    [Serializable]
    public class Season
    {
        public int year, week, attempts = 3, topScorer;
        public string league, phase = "train"; // train | match | end
        public List<Round> rounds = new List<Round>();
        public List<TableRow> table = new List<TableRow>();
        public SeasonStats stats = new SeasonStats();
        public Role role, lastRole;
        public bool negotiated, forceExit, rested;
        public int actionsUsed; // ações da agenda gastas nesta rodada
        public List<string> doneActions = new List<string>();
        public SeasonSummary summary = new SeasonSummary();
        public List<ScorerRow> scorers = new List<ScorerRow>(); // artilharia da liga (jogadores reais)
        public List<CupTie> cup = new List<CupTie>();           // jogos da copa nesta temporada
        public bool cupOut, cupWon;
        public int cupGoals, monthMark;                         // gols na copa; início da janela do prêmio do mês
        public List<Objective> objectives = new List<Objective>(); // objetivos do técnico e da diretoria
        public int objMet = -1;                                  // objetivos cumpridos (fim de temporada; -1 = ainda não avaliado)
    }

    [Serializable]
    public class SponsorDeal
    {
        public string id, cat, brand, goalKind; // goalKind: gols | part | nota
        public long perSeason, bonus;
        public int seasons, seasonsLeft;
        public float goalTarget;
    }

    [Serializable]
    public class ContractOffer
    {
        public string id, club;
        public long salary, bonus, signing, clause;
        public int years;
        public bool renewal, haggled;
    }

    // ---------- negócios no futebol ----------
    [Serializable] public class StockQuote { public string id; public long price; public List<long> hist = new List<long>(); }
    [Serializable] public class Holding { public string id; public int qty; public long cost; } // cost = total pago pelas ações em carteira
    [Serializable] public class Venture { public string id; public int level = 1; }

    // ---------- glórias: artilharia, copa, seleção, prêmios e conquistas ----------
    [Serializable] public class ScorerRow { public string name, club; public int goals; public float rate; }
    [Serializable] public class CupTie { public int stage; public string opp, c1, c2; public int gf, ga, myGoals; public bool won, pens, home; }
    [Serializable] public class RankRow { public string name, club; public float score; public bool me; }

    // ---------- vida fora de campo ----------
    /// <summary>Relacionamento: 0 solteiro, 1 ficando, 2 namorando, 3 noivos, 4 casados.</summary>
    [Serializable] public class Relationship { public string name = "", job = ""; public int stage, weeks; public float affection = 50; public bool famous; }

    /// <summary>Rival fora de campo: outro jovem da mesma posição, comparado com você pela imprensa.</summary>
    [Serializable] public class Rival { public string name = "", club = ""; public int age, goals, careerGoals, duels, won; public float heat; }

    /// <summary>Extrato da última rodada: o que entrou e o que saiu da conta.</summary>
    [Serializable]
    public class Statement
    {
        public long salary, bonus, sponsors, business, tax, agent, living, upkeep, partner;
        public float taxRate;
        public long Income => salary + bonus + sponsors + business;
        public long Expenses => tax + agent + living + upkeep + partner;
        public long Net => Income - Expenses;
    }

    [Serializable] public class CareerRecord { public int year, rank, apps, goals, assists; public string club, c1, c2; public float avg; }
    [Serializable] public class News { public string when, text; }

    // ---------- Copa do Mundo ----------
    [Serializable] public class WcMatch { public int stage; public string opp, c1, c2; public int gf, ga, myGoals; public bool played, pens, won; public float rating; }

    [Serializable]
    public class WorldCupState
    {
        public int year;
        public string phase = "off"; // off | group | ko | done
        public List<WcMatch> matches = new List<WcMatch>();
        public int groupPts;
        public bool champion, eliminated;
    }

    [Serializable]
    public class GameState
    {
        public const int CurrentVersion = 3;
        public int version = CurrentVersion, year;
        // identidade e controle de versão do save: base para sincronizar com um servidor no modo online
        public string profileId;          // id único e permanente desta carreira
        public long createdUtc, savedUtc; // DateTime.UtcNow.Ticks
        public int rev;                   // sobe a cada salvamento; o servidor usa para detectar conflito entre aparelhos
        public bool retired;
        public Player player = new Player();
        public List<Club> clubs = new List<Club>();
        public Contract contract = new Contract();
        public Season season = new Season();
        public List<SponsorDeal> activeSponsors = new List<SponsorDeal>(), sponsorOffers = new List<SponsorDeal>();
        public List<ContractOffer> offers = new List<ContractOffer>();
        public List<CareerRecord> career = new List<CareerRecord>();
        public List<string> titles = new List<string>(), awards = new List<string>(), owned = new List<string>();
        public List<News> news = new List<News>();
        public List<StockQuote> quotes = new List<StockQuote>();
        public List<Holding> holdings = new List<Holding>();
        public List<Venture> ventures = new List<Venture>();
        public long businessIncome; // lucro dos negócios na última rodada
        public List<string> achievements = new List<string>(), monthly = new List<string>();
        public int caps, intlGoals, cupGoalsTotal, bestBallon;
        public bool calledUp;
        public List<RankRow> ballon = new List<RankRow>(); // último ranking da Bola de Ouro
        public int ballonYear;
        public WorldCupState wc = new WorldCupState();
        public int worldCups; // Copas disputadas
        public string interestLeague; // liga que mandou sondagem (garante proposta dela no fim da temporada)
        public Relationship love = new Relationship();
        public Rival rival = new Rival();
        public Statement lastWeek = new Statement();
        public long seasonNet;  // saldo acumulado na temporada (salário e negócios menos impostos e despesas)
        public int debtWeeks;   // rodadas seguidas no vermelho
        // reputação e capitania
        public bool repInit, captain, captainOffered;
        // caixa de mensagens
        public List<InboxMsg> inbox = new List<InboxMsg>();
        public string agent;    // nome do empresário (fictício)
        // especialidades e contadores que liberam algumas delas
        public List<string> traits = new List<string>();
        public int fkGoals, penGoals;
        [NonSerialized] public int mailsThisRound;
    }
}
