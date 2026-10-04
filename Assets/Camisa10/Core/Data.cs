using System.Collections.Generic;

namespace Camisa10.Core
{
    public enum Attr { Fin, Pas, Dri, Vel, Fis, Def }
    public enum Role { None, Titular, Estrela, Reserva, Lesionado, Poupado }

    /// <summary>Todo o conteúdo fixo do jogo: posições, ligas, clubes fictícios, marcas e itens.</summary>
    public static class GameData
    {
        public static readonly Attr[] Attrs = { Attr.Fin, Attr.Pas, Attr.Dri, Attr.Vel, Attr.Fis, Attr.Def };

        public static string Label(Attr a)
        {
            switch (a)
            {
                case Attr.Fin: return "Finalização";
                case Attr.Pas: return "Passe";
                case Attr.Dri: return "Drible";
                case Attr.Vel: return "Velocidade";
                case Attr.Fis: return "Físico";
                default: return "Defesa";
            }
        }

        // ---------- posições ----------
        public class PositionDef
        {
            public string Code, Name;
            public float[] W;      // pesos no geral, na ordem de Attrs
            public int[] Base;     // atributos iniciais aos 17 anos
            public (string type, double weight)[] Moments; // tipos de lance na partida
        }

        public static readonly string[] PositionOrder = { "ATA", "MEI", "VOL", "ZAG", "LAT" };

        public static readonly Dictionary<string, PositionDef> Positions = new Dictionary<string, PositionDef>
        {
            ["ATA"] = new PositionDef { Code = "ATA", Name = "Atacante", W = new[] { .35f, .1f, .2f, .2f, .15f, 0f }, Base = new[] { 53, 43, 49, 51, 47, 25 },
                Moments = new[] { ("chance", .45), ("contra", .25), ("falta", .15), ("meio", .15) } },
            ["MEI"] = new PositionDef { Code = "MEI", Name = "Meia", W = new[] { .15f, .35f, .25f, .1f, .1f, .05f }, Base = new[] { 45, 53, 50, 47, 43, 35 },
                Moments = new[] { ("chance", .25), ("meio", .4), ("contra", .2), ("falta", .15) } },
            ["VOL"] = new PositionDef { Code = "VOL", Name = "Volante", W = new[] { .05f, .3f, .05f, .05f, .25f, .3f }, Base = new[] { 37, 49, 41, 43, 51, 51 },
                Moments = new[] { ("meio", .4), ("defesa", .45), ("chance", .15) } },
            ["ZAG"] = new PositionDef { Code = "ZAG", Name = "Zagueiro", W = new[] { 0f, .1f, 0f, .1f, .35f, .45f }, Base = new[] { 31, 41, 33, 43, 53, 53 },
                Moments = new[] { ("defesa", .7), ("cabeceio", .3) } },
            ["LAT"] = new PositionDef { Code = "LAT", Name = "Lateral", W = new[] { 0f, .2f, .1f, .3f, .1f, .3f }, Base = new[] { 35, 47, 45, 53, 45, 49 },
                Moments = new[] { ("defesa", .45), ("contra", .3), ("meio", .25) } },
        };

        // ---------- ligas e clubes (todos fictícios) ----------
        public class LeagueDef
        {
            public string Id, Name;
            public double WageMult, FameMult;
            public (string name, int str, string c1, string c2)[] Clubs;
        }

        // Clubes e jogadores reais (pedido do dono em 04/10/2026). Elencos conforme a temporada 2025/26;
        // para atualizar transferências, edite só as listas abaixo. O primeiro nome de cada elenco é o goleiro.
        public static readonly LeagueDef[] Leagues =
        {
            new LeagueDef { Id = "br", Name = "Brasileirão", WageMult = 1, FameMult = 1, Clubs = new[] {
                ("Flamengo", 78, "#C8102E", "#111111"), ("Palmeiras", 77, "#006437", "#FFFFFF"), ("Botafogo", 74, "#111111", "#FFFFFF"),
                ("Cruzeiro", 72, "#003DA5", "#FFFFFF"), ("Fluminense", 70, "#7A0026", "#00613C"), ("São Paulo", 68, "#FFFFFF", "#C8102E"),
                ("Corinthians", 66, "#FFFFFF", "#111111"), ("Bahia", 63, "#0057B8", "#E30613"), ("Vasco da Gama", 60, "#111111", "#FFFFFF"),
                ("Grêmio", 57, "#0D80BF", "#111111") } },
            new LeagueDef { Id = "ib", Name = "La Liga", WageMult = 1.6, FameMult = 1.4, Clubs = new[] {
                ("Real Madrid", 87, "#FFFFFF", "#FEBE10"), ("Barcelona", 86, "#A50044", "#004D98"), ("Atlético de Madrid", 82, "#CB3524", "#FFFFFF"),
                ("Athletic Bilbao", 77, "#EE2523", "#FFFFFF"), ("Villarreal", 76, "#FFE667", "#005187"), ("Real Betis", 74, "#00954C", "#FFFFFF"),
                ("Real Sociedad", 73, "#0067B1", "#FFFFFF"), ("Sevilla", 70, "#FFFFFF", "#D71920"), ("Valencia", 68, "#FFFFFF", "#111111"),
                ("Celta de Vigo", 66, "#8AC3EE", "#FFFFFF") } },
            new LeagueDef { Id = "en", Name = "Premier League", WageMult = 2, FameMult = 1.6, Clubs = new[] {
                ("Liverpool", 88, "#C8102E", "#FFFFFF"), ("Manchester City", 87, "#6CABDD", "#FFFFFF"), ("Arsenal", 87, "#EF0107", "#FFFFFF"),
                ("Chelsea", 83, "#034694", "#FFFFFF"), ("Manchester United", 80, "#DA291C", "#111111"), ("Newcastle", 79, "#111111", "#FFFFFF"),
                ("Tottenham", 78, "#FFFFFF", "#132257"), ("Aston Villa", 77, "#670E36", "#95BFE5"), ("Brighton", 73, "#0057B8", "#FFFFFF"),
                ("West Ham", 70, "#7A263A", "#1BB1E7") } },
        };

        public static readonly Dictionary<string, string[]> Squads = new Dictionary<string, string[]>
        {
            ["Flamengo"] = new[] { "Rossi", "Arrascaeta", "Pedro", "Bruno Henrique", "Jorginho", "Léo Ortiz" },
            ["Palmeiras"] = new[] { "Weverton", "Raphael Veiga", "Vitor Roque", "Gustavo Gómez", "Flaco López", "Estêvão Willian" },
            ["Botafogo"] = new[] { "John", "Savarino", "Alex Telles", "Marlon Freitas", "Arthur Cabral" },
            ["Cruzeiro"] = new[] { "Cássio", "Matheus Pereira", "Kaio Jorge", "Gabigol", "Lucas Romero" },
            ["Fluminense"] = new[] { "Fábio", "Ganso", "Germán Cano", "Thiago Silva", "Martinelli" },
            ["São Paulo"] = new[] { "Rafael", "Lucas Moura", "Calleri", "Oscar", "Arboleda" },
            ["Corinthians"] = new[] { "Hugo Souza", "Memphis Depay", "Yuri Alberto", "Rodrigo Garro", "Matheuzinho" },
            ["Bahia"] = new[] { "Marcos Felipe", "Everton Ribeiro", "Cauly", "Willian José", "Jean Lucas" },
            ["Vasco da Gama"] = new[] { "Léo Jardim", "Philippe Coutinho", "Vegetti", "Paulo Henrique", "Nuno Moreira" },
            ["Grêmio"] = new[] { "Tiago Volpi", "Cristian Olivera", "Braithwaite", "Kannemann", "Edenílson" },
            ["Real Madrid"] = new[] { "Courtois", "Mbappé", "Vinícius Júnior", "Bellingham", "Rodrygo", "Valverde" },
            ["Barcelona"] = new[] { "Joan García", "Lamine Yamal", "Raphinha", "Pedri", "Lewandowski", "Gavi" },
            ["Atlético de Madrid"] = new[] { "Oblak", "Julián Álvarez", "Griezmann", "Koke", "Giuliano Simeone" },
            ["Athletic Bilbao"] = new[] { "Unai Simón", "Nico Williams", "Iñaki Williams", "Sancet", "Berenguer" },
            ["Villarreal"] = new[] { "Luiz Júnior", "Gerard Moreno", "Pépé", "Parejo", "Ayoze Pérez" },
            ["Real Betis"] = new[] { "Adrián", "Isco", "Antony", "Fornals", "Cucho Hernández" },
            ["Real Sociedad"] = new[] { "Remiro", "Oyarzabal", "Kubo", "Brais Méndez", "Zubimendi" },
            ["Sevilla"] = new[] { "Nyland", "Isaac Romero", "Rubén Vargas", "Saúl", "Agoumé" },
            ["Valencia"] = new[] { "Dimitrievski", "Hugo Duro", "Gayà", "Diego López", "Pepelu" },
            ["Celta de Vigo"] = new[] { "Radu", "Iago Aspas", "Borja Iglesias", "Bryan Zaragoza", "Mingueza" },
            ["Liverpool"] = new[] { "Alisson", "Salah", "Wirtz", "Van Dijk", "Gakpo", "Isak" },
            ["Manchester City"] = new[] { "Donnarumma", "Haaland", "Rodri", "Foden", "Doku", "Reijnders" },
            ["Arsenal"] = new[] { "Raya", "Saka", "Ødegaard", "Rice", "Gyökeres", "Martinelli" },
            ["Chelsea"] = new[] { "Robert Sánchez", "Cole Palmer", "Enzo Fernández", "Estêvão", "João Pedro" },
            ["Manchester United"] = new[] { "Lammens", "Bruno Fernandes", "Matheus Cunha", "Mbeumo", "Casemiro" },
            ["Newcastle"] = new[] { "Pope", "Bruno Guimarães", "Joelinton", "Gordon", "Tonali" },
            ["Tottenham"] = new[] { "Vicario", "Richarlison", "Kudus", "Maddison", "Romero" },
            ["Aston Villa"] = new[] { "Emiliano Martínez", "Watkins", "McGinn", "Rogers", "Tielemans" },
            ["Brighton"] = new[] { "Verbruggen", "Mitoma", "Welbeck", "Baleba", "Minteh" },
            ["West Ham"] = new[] { "Areola", "Bowen", "Paquetá", "Füllkrug", "Soucek" },
        };

        static readonly string[] NoSquad = { "o goleiro", "o camisa 9" };
        static string[] SquadOf(string club) => club != null && Squads.TryGetValue(club, out var s) ? s : NoSquad;

        /// <summary>Goleiro titular do clube.</summary>
        public static string Keeper(string club) => SquadOf(club)[0];

        /// <summary>Um jogador de linha do clube, sorteado.</summary>
        public static string Outfield(string club)
        {
            var s = SquadOf(club);
            return s.Length > 1 ? s[Rng.RangeInt(1, s.Length - 1)] : s[0];
        }

        // ---------- patrocínios ----------
        public class SponsorCat
        {
            public string Id, Name;
            public int MinFame;
            public double Factor;
            public string[] Brands;
        }

        public static readonly SponsorCat[] SponsorCats =
        {
            new SponsorCat { Id = "chuteira", Name = "Chuteira", MinFame = 0, Factor = 1, Brands = new[] { "Volt", "Strika", "Aurum", "Kairos", "Nimbus" } },
            new SponsorCat { Id = "energetico", Name = "Energético", MinFame = 12, Factor = .6, Brands = new[] { "Raio Drink", "Turbo Max", "Fôlego" } },
            new SponsorCat { Id = "relogio", Name = "Relógio", MinFame = 30, Factor = 1.2, Brands = new[] { "Cronos", "Meridian", "Tempora" } },
            new SponsorCat { Id = "games", Name = "Games", MinFame = 40, Factor = 1, Brands = new[] { "PixelForge", "Arena Play", "NextLevel" } },
            new SponsorCat { Id = "banco", Name = "Banco digital", MinFame = 55, Factor = 1.5, Brands = new[] { "Banco Nuvem", "Pago+", "Conta Ágil" } },
        };

        public static SponsorCat Sponsor(string id) => System.Array.Find(SponsorCats, c => c.Id == id);

        public static readonly Dictionary<string, string[]> BootPalettes = new Dictionary<string, string[]>
        {
            [""] = new[] { "#111111", "#FFFFFF", "#9E9E9E" },
            ["Volt"] = new[] { "#F2C230", "#111111", "#FFFFFF", "#1E88E5", "#E53935" },
            ["Strika"] = new[] { "#E53935", "#111111", "#FFFFFF", "#FF8A65", "#1E88E5" },
            ["Aurum"] = new[] { "#C9A227", "#1B1B1B", "#F5F5F5", "#7B1FA2", "#00897B" },
            ["Kairos"] = new[] { "#00ACC1", "#0D47A1", "#FFFFFF", "#8BC34A", "#FF7043" },
            ["Nimbus"] = new[] { "#ECEFF1", "#263238", "#FF4081", "#3949AB", "#FFD54F" },
        };

        public static readonly string[] Soles = { "#222222", "#FFFFFF", "#B0BEC5" };

        // ---------- treino ----------
        public class Intensity { public string Id, Name, Hint; public double Xp, Energy, Risk; }

        public static readonly Intensity[] Intensities =
        {
            new Intensity { Id = "leve", Name = "Leve", Xp = 4, Energy = 18, Risk = 0, Hint = "Recupera energia e evolui pouco." },
            new Intensity { Id = "normal", Name = "Normal", Xp = 7, Energy = 6, Risk = .02, Hint = "Equilíbrio entre evolução e descanso." },
            new Intensity { Id = "intenso", Name = "Intenso", Xp = 11, Energy = -8, Risk = .07, Hint = "Evolui mais, cansa e pode causar lesão." },
        };

        public static Intensity GetIntensity(string id) => System.Array.Find(Intensities, i => i.Id == id) ?? Intensities[1];

        // ---------- estilo de vida ----------
        public class Item { public string Id, Cat, Name; public long Price; public int Fame, Moral; }

        public static readonly Item[] Items =
        {
            new Item { Id = "carro1", Cat = "Carro", Name = "Carro popular zero", Price = 80000, Fame = 1, Moral = 4 },
            new Item { Id = "casa1", Cat = "Casa", Name = "Apartamento para a família", Price = 450000, Fame = 1, Moral = 8 },
            new Item { Id = "carro2", Cat = "Carro", Name = "Esportivo importado", Price = 900000, Fame = 4, Moral = 6 },
            new Item { Id = "relogio", Cat = "Estilo", Name = "Relógio de luxo", Price = 1200000, Fame = 3, Moral = 4 },
            new Item { Id = "casa2", Cat = "Casa", Name = "Casa com piscina", Price = 3500000, Fame = 4, Moral = 8 },
            new Item { Id = "carro3", Cat = "Carro", Name = "Superesportivo", Price = 5000000, Fame = 8, Moral = 8 },
            new Item { Id = "casa3", Cat = "Casa", Name = "Mansão à beira-mar", Price = 25000000, Fame = 9, Moral = 10 },
        };

        // ---------- textos ----------
        public static string RoleName(Role r)
        {
            switch (r)
            {
                case Role.Estrela: return "Titular absoluto";
                case Role.Titular: return "Titular";
                case Role.Reserva: return "Reserva";
                case Role.Lesionado: return "Lesionado";
                case Role.Poupado: return "Poupado";
                default: return "";
            }
        }

        public static string RoleHint(Role r)
        {
            switch (r)
            {
                case Role.Estrela: return "Você é peça-chave. Terá mais lances decisivos na partida.";
                case Role.Titular: return "O técnico confia em você. Você joga desde o início.";
                case Role.Reserva: return "Você começa no banco e pode entrar no segundo tempo. Treine, cuide da relação com o técnico e da forma para virar titular.";
                case Role.Lesionado: return "Você está no departamento médico e vai acompanhar da tribuna.";
                case Role.Poupado: return "Você foi poupado nesta rodada para recuperar energia.";
                default: return "";
            }
        }
    }
}
