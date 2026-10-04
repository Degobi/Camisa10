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
        public long money;
        public List<float> form = new List<float>();
        public BootStyle boot = new BootStyle();
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

    [Serializable] public class SeasonStats { public int apps, goals, assists; public List<float> ratings = new List<float>(); }

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

    [Serializable] public class CareerRecord { public int year, rank, apps, goals, assists; public string club, c1, c2; public float avg; }
    [Serializable] public class News { public string when, text; }

    [Serializable]
    public class GameState
    {
        public const int CurrentVersion = 2;
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
    }
}
