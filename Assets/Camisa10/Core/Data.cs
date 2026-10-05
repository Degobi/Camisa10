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
                Moments = new[] { ("chance", .3), ("contra", .18), ("falta", .12), ("meio", .1), ("penalti", .06), ("rebote", .1), ("cabeceio", .09), ("cara", .05) } },
            ["MEI"] = new PositionDef { Code = "MEI", Name = "Meia", W = new[] { .15f, .35f, .25f, .1f, .1f, .05f }, Base = new[] { 45, 53, 50, 47, 43, 35 },
                Moments = new[] { ("chance", .2), ("meio", .28), ("contra", .14), ("falta", .14), ("penalti", .04), ("cruzamento", .1), ("rebote", .05), ("defesa", .05) } },
            ["VOL"] = new PositionDef { Code = "VOL", Name = "Volante", W = new[] { .05f, .3f, .05f, .05f, .25f, .3f }, Base = new[] { 37, 49, 41, 43, 51, 51 },
                Moments = new[] { ("meio", .32), ("defesa", .33), ("corte", .15), ("chance", .12), ("falta", .08) } },
            ["ZAG"] = new PositionDef { Code = "ZAG", Name = "Zagueiro", W = new[] { 0f, .1f, 0f, .1f, .35f, .45f }, Base = new[] { 31, 41, 33, 43, 53, 53 },
                Moments = new[] { ("defesa", .4), ("corte", .35), ("cabeceio", .25) } },
            ["LAT"] = new PositionDef { Code = "LAT", Name = "Lateral", W = new[] { 0f, .2f, .1f, .3f, .1f, .3f }, Base = new[] { 35, 47, 45, 53, 45, 49 },
                Moments = new[] { ("defesa", .32), ("cruzamento", .3), ("contra", .18), ("corte", .12), ("meio", .08) } },
        };

        // ---------- ligas e clubes (todos fictícios) ----------
        public class LeagueDef
        {
            public string Id, Name, Country, Cup;
            public double WageMult, FameMult;
            /// <summary>Imposto de renda efetivo sobre salário e prêmios (a Arábia não cobra: é parte do atrativo).</summary>
            public double Tax = .275;
            /// <summary>Para receber proposta: fama OU geral mínimos (0 = qualquer um). MinAge: ligas que preferem veteranos.</summary>
            public float MinFame; public int MinOvr, MinAge;
            /// <summary>Peso da liga na Bola de Ouro (Europa forte = +, mercados menores = -).</summary>
            public double Prestige;
            public (string name, int str, string c1, string c2)[] Clubs;
        }

        // Clubes e jogadores reais (pedido do dono em 04/10/2026). Elencos conforme a temporada 2025/26;
        // para atualizar transferências, edite só as listas abaixo. O primeiro nome de cada elenco é o goleiro.
        public static readonly LeagueDef[] Leagues =
        {
            new LeagueDef { Id = "br", Tax = 0.275, Name = "Brasileirão", Country = "Brasil", Cup = "Copa do Brasil", WageMult = 1, FameMult = 1, Prestige = -3, Clubs = new[] {
                ("Flamengo", 78, "#C8102E", "#111111"), ("Palmeiras", 77, "#006437", "#FFFFFF"), ("Botafogo", 74, "#111111", "#FFFFFF"),
                ("Cruzeiro", 72, "#003DA5", "#FFFFFF"), ("Fluminense", 70, "#7A0026", "#00613C"), ("São Paulo", 68, "#FFFFFF", "#C8102E"),
                ("Corinthians", 66, "#FFFFFF", "#111111"), ("Bahia", 63, "#0057B8", "#E30613"), ("Vasco da Gama", 60, "#111111", "#FFFFFF"),
                ("Grêmio", 57, "#0D80BF", "#111111") } },
            new LeagueDef { Id = "ib", Tax = 0.4, Name = "La Liga", Country = "Espanha", Cup = "Copa del Rey", WageMult = 1.6, FameMult = 1.4, MinFame = 22, MinOvr = 68, Prestige = 2, Clubs = new[] {
                ("Real Madrid", 87, "#FFFFFF", "#FEBE10"), ("Barcelona", 86, "#A50044", "#004D98"), ("Atlético de Madrid", 82, "#CB3524", "#FFFFFF"),
                ("Athletic Bilbao", 77, "#EE2523", "#FFFFFF"), ("Villarreal", 76, "#FFE667", "#005187"), ("Real Betis", 74, "#00954C", "#FFFFFF"),
                ("Real Sociedad", 73, "#0067B1", "#FFFFFF"), ("Sevilla", 70, "#FFFFFF", "#D71920"), ("Valencia", 68, "#FFFFFF", "#111111"),
                ("Celta de Vigo", 66, "#8AC3EE", "#FFFFFF") } },
            new LeagueDef { Id = "en", Tax = 0.4, Name = "Premier League", Country = "Inglaterra", Cup = "FA Cup", WageMult = 2, FameMult = 1.6, MinFame = 32, MinOvr = 72, Prestige = 2.5, Clubs = new[] {
                ("Liverpool", 88, "#C8102E", "#FFFFFF"), ("Manchester City", 87, "#6CABDD", "#FFFFFF"), ("Arsenal", 87, "#EF0107", "#FFFFFF"),
                ("Chelsea", 83, "#034694", "#FFFFFF"), ("Manchester United", 80, "#DA291C", "#111111"), ("Newcastle", 79, "#111111", "#FFFFFF"),
                ("Tottenham", 78, "#FFFFFF", "#132257"), ("Aston Villa", 77, "#670E36", "#95BFE5"), ("Brighton", 73, "#0057B8", "#FFFFFF"),
                ("West Ham", 70, "#7A263A", "#1BB1E7") } },
            new LeagueDef { Id = "it", Tax = 0.38, Name = "Serie A", Country = "Itália", Cup = "Coppa Italia", WageMult = 1.5, FameMult = 1.35, MinFame = 22, MinOvr = 68, Prestige = 1.5, Clubs = new[] {
                ("Inter de Milão", 85, "#0068A8", "#111111"), ("Napoli", 84, "#12A0D7", "#FFFFFF"), ("Milan", 82, "#FB090B", "#111111"),
                ("Juventus", 82, "#111111", "#FFFFFF"), ("Atalanta", 80, "#1E71B8", "#111111"), ("Roma", 80, "#8E1F2F", "#F0BC42"),
                ("Lazio", 77, "#87D8F7", "#FFFFFF"), ("Fiorentina", 76, "#482E92", "#FFFFFF"), ("Bologna", 77, "#1A2F48", "#A21C26"),
                ("Como", 74, "#1E5CA8", "#FFFFFF") } },
            new LeagueDef { Id = "fr", Tax = 0.4, Name = "Ligue 1", Country = "França", Cup = "Coupe de France", WageMult = 1.4, FameMult = 1.3, MinFame = 18, MinOvr = 66, Prestige = 1, Clubs = new[] {
                ("Paris Saint-Germain", 87, "#004170", "#DA291C"), ("Olympique de Marseille", 78, "#FFFFFF", "#2FAEE0"), ("Monaco", 77, "#E51B22", "#FFFFFF"),
                ("Lille", 76, "#E01E13", "#1B2A5B"), ("Lyon", 76, "#FFFFFF", "#1C2D6E"), ("Nice", 73, "#C8102E", "#111111"),
                ("Lens", 74, "#FFD100", "#E30613"), ("Rennes", 72, "#E2001A", "#111111"), ("Strasbourg", 72, "#009FE3", "#FFFFFF"),
                ("Nantes", 68, "#FCD405", "#008845") } },
            new LeagueDef { Id = "tr", Tax = 0.2, Name = "Süper Lig", Country = "Turquia", Cup = "Copa da Turquia", WageMult = 1.2, FameMult = 1.1, MinFame = 12, MinOvr = 63, Prestige = -1, Clubs = new[] {
                ("Galatasaray", 79, "#A90432", "#FDB912"), ("Fenerbahçe", 78, "#002D72", "#FFED00"), ("Beşiktaş", 75, "#111111", "#FFFFFF"),
                ("Trabzonspor", 72, "#7B1C33", "#00A5DB"), ("Başakşehir", 70, "#ED6B21", "#1C2D5A"), ("Samsunspor", 69, "#E30613", "#FFFFFF"),
                ("Göztepe", 68, "#FFD100", "#E30613"), ("Kasımpaşa", 66, "#1C2D5A", "#FFFFFF"), ("Antalyaspor", 66, "#E30613", "#FFFFFF"),
                ("Konyaspor", 66, "#00A651", "#FFFFFF") } },
            new LeagueDef { Id = "sa", Tax = 0, Name = "Saudi Pro League", Country = "Arábia Saudita", Cup = "Copa do Rei", WageMult = 2.8, FameMult = .8, MinFame = 30, MinOvr = 72, MinAge = 27, Prestige = -3, Clubs = new[] {
                ("Al-Hilal", 81, "#005BAC", "#FFFFFF"), ("Al-Nassr", 80, "#FFE600", "#0033A0"), ("Al-Ittihad", 79, "#FFE500", "#111111"),
                ("Al-Ahli", 78, "#00A859", "#FFFFFF"), ("Al-Qadsiah", 75, "#F47B20", "#111111"), ("Al-Ettifaq", 72, "#00843D", "#E2001A"),
                ("Al-Shabab", 72, "#FFFFFF", "#111111"), ("Al-Taawoun", 70, "#FFD700", "#111111"), ("Al-Fateh", 67, "#003F87", "#FFFFFF"),
                ("Al-Khaleej", 66, "#FFD700", "#00843D") } },
            new LeagueDef { Id = "us", Tax = 0.3, Name = "MLS", Country = "Estados Unidos", Cup = "US Open Cup", WageMult = 1.3, FameMult = 1.2, MinFame = 20, MinOvr = 64, MinAge = 26, Prestige = -2.5, Clubs = new[] {
                ("Inter Miami", 76, "#F7B5CD", "#231F20"), ("LAFC", 75, "#111111", "#C39E6D"), ("LA Galaxy", 72, "#00245D", "#FFD200"),
                ("Columbus Crew", 72, "#FEDD00", "#111111"), ("FC Cincinnati", 72, "#F05323", "#263B80"), ("Seattle Sounders", 71, "#5D9741", "#005595"),
                ("Orlando City", 71, "#633492", "#FDE192"), ("Atlanta United", 70, "#80000A", "#221F1F"), ("New York City FC", 70, "#6CACE4", "#041E42"),
                ("Toronto FC", 68, "#B81137", "#455560") } },
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
            // seleções (Copa do Mundo)
            ["Brasil"] = new[] { "Alisson", "Vinícius Júnior", "Raphinha", "Rodrygo", "Bruno Guimarães", "Marquinhos", "Estêvão" },
            ["Argentina"] = new[] { "Emiliano Martínez", "Messi", "Julián Álvarez", "Lautaro Martínez", "Mac Allister", "Enzo Fernández" },
            ["França"] = new[] { "Maignan", "Mbappé", "Dembélé", "Tchouaméni", "Olise", "Saliba" },
            ["Espanha"] = new[] { "Unai Simón", "Lamine Yamal", "Pedri", "Rodri", "Nico Williams", "Dani Olmo" },
            ["Inglaterra"] = new[] { "Pickford", "Harry Kane", "Bellingham", "Saka", "Declan Rice", "Foden" },
            ["Portugal"] = new[] { "Diogo Costa", "Cristiano Ronaldo", "Bruno Fernandes", "Bernardo Silva", "Rafael Leão", "Vitinha" },
            ["Alemanha"] = new[] { "Ter Stegen", "Musiala", "Wirtz", "Havertz", "Kimmich" },
            ["Holanda"] = new[] { "Verbruggen", "Gakpo", "Van Dijk", "Frenkie de Jong", "Xavi Simons" },
            ["Bélgica"] = new[] { "Courtois", "De Bruyne", "Doku", "Lukaku", "Trossard" },
            ["Itália"] = new[] { "Donnarumma", "Retegui", "Barella", "Tonali", "Chiesa" },
            ["Croácia"] = new[] { "Livaković", "Modrić", "Kramarić", "Gvardiol", "Kovačić" },
            ["Uruguai"] = new[] { "Rochet", "Valverde", "Darwin Núñez", "Ronald Araújo", "Ugarte" },
            ["Colômbia"] = new[] { "Camilo Vargas", "Luis Díaz", "James Rodríguez", "Jhon Durán", "Lerma" },
            ["Marrocos"] = new[] { "Bono", "Hakimi", "Brahim Díaz", "En-Nesyri", "Amrabat" },
            ["Noruega"] = new[] { "Nyland", "Haaland", "Ødegaard", "Sørloth" },
            ["Suíça"] = new[] { "Kobel", "Xhaka", "Embolo", "Akanji" },
            ["Dinamarca"] = new[] { "Schmeichel", "Højlund", "Eriksen", "Højbjerg" },
            ["Japão"] = new[] { "Suzuki", "Kubo", "Mitoma", "Endo" },
            ["Estados Unidos"] = new[] { "Matt Turner", "Pulisic", "McKennie", "Balogun" },
            ["Senegal"] = new[] { "Édouard Mendy", "Sadio Mané", "Ismaïla Sarr", "Nicolas Jackson" },
            ["México"] = new[] { "Malagón", "Raúl Jiménez", "Edson Álvarez", "Santiago Giménez" },
            ["Equador"] = new[] { "Galíndez", "Moisés Caicedo", "Enner Valencia", "Hincapié" },
            ["Coreia do Sul"] = new[] { "Jo Hyeon-woo", "Son Heung-min", "Lee Kang-in", "Kim Min-jae" },
            ["Canadá"] = new[] { "Crépeau", "Alphonso Davies", "Jonathan David" },
            ["Egito"] = new[] { "El-Shenawy", "Salah", "Marmoush" },
            ["Austrália"] = new[] { "Mat Ryan", "Irvine", "Boyle" },
            ["Gana"] = new[] { "Ati-Zigi", "Kudus", "Semenyo" },
            ["Arábia Saudita"] = new[] { "Al-Owais", "Salem Al-Dawsari", "Al-Buraikan" },
            // Serie A
            ["Inter de Milão"] = new[] { "Sommer", "Lautaro Martínez", "Barella", "Çalhanoğlu", "Marcus Thuram", "Dimarco" },
            ["Napoli"] = new[] { "Meret", "McTominay", "De Bruyne", "Højlund", "Politano", "Di Lorenzo" },
            ["Milan"] = new[] { "Maignan", "Rafael Leão", "Pulisic", "Modrić", "Gimenez", "Fofana" },
            ["Juventus"] = new[] { "Di Gregorio", "Vlahović", "Kenan Yıldız", "Jonathan David", "Bremer", "Locatelli" },
            ["Atalanta"] = new[] { "Carnesecchi", "Lookman", "De Ketelaere", "Ederson", "Scamacca" },
            ["Roma"] = new[] { "Svilar", "Dybala", "Pellegrini", "Dovbyk", "Koné", "Soulé" },
            ["Lazio"] = new[] { "Provedel", "Zaccagni", "Castellanos", "Guendouzi", "Pedro" },
            ["Fiorentina"] = new[] { "De Gea", "Moise Kean", "Gudmundsson", "Dzeko", "Mandragora" },
            ["Bologna"] = new[] { "Skorupski", "Orsolini", "Castro", "Ferguson", "Bernardeschi" },
            ["Como"] = new[] { "Butez", "Nico Paz", "Morata", "Da Cunha", "Perrone" },
            // Ligue 1
            ["Paris Saint-Germain"] = new[] { "Chevalier", "Dembélé", "Vitinha", "Kvaratskhelia", "Hakimi", "Marquinhos", "Doué" },
            ["Olympique de Marseille"] = new[] { "Rulli", "Greenwood", "Aubameyang", "Rabiot", "Højbjerg" },
            ["Monaco"] = new[] { "Köhn", "Pogba", "Golovin", "Minamino", "Akliouche" },
            ["Lille"] = new[] { "Özer", "Giroud", "Haraldsson", "André", "Mukau" },
            ["Lyon"] = new[] { "Lucas Perri", "Tolisso", "Malick Fofana", "Tessmann", "Mikautadze" },
            ["Nice"] = new[] { "Bulka", "Moffi", "Boga", "Sanson", "Diop" },
            ["Lens"] = new[] { "Risser", "Thauvin", "Saïd", "Sotoca", "Aguilar" },
            ["Rennes"] = new[] { "Samba", "Kalimuendo", "Embolo", "Blas", "Lepaul" },
            ["Strasbourg"] = new[] { "Petrović", "Emegha", "Panichelli", "Enciso", "Diarra" },
            ["Nantes"] = new[] { "Lopes", "Abline", "Mohamed", "Lepenant", "Mwanga" },
            // Süper Lig
            ["Galatasaray"] = new[] { "Uğurcan Çakır", "Osimhen", "Icardi", "Leroy Sané", "Barış Alper Yılmaz", "Torreira" },
            ["Fenerbahçe"] = new[] { "Ederson", "En-Nesyri", "Asensio", "Talisca", "Kerem Aktürkoğlu", "Fred" },
            ["Beşiktaş"] = new[] { "Ersin Destanoğlu", "Rafa Silva", "Abraham", "Ndidi", "Orkun Kökçü" },
            ["Trabzonspor"] = new[] { "Onana", "Nwakaeme", "Muçi", "Zubkov", "Savic" },
            ["Başakşehir"] = new[] { "Muhammed Şengezer", "Piątek", "Bertuğ Yıldırım", "Kemen", "Shomurodov" },
            ["Samsunspor"] = new[] { "Okan Kocuk", "Marius", "Holse", "Mouandilmadji" },
            ["Göztepe"] = new[] { "Lis", "Juan", "Romulo", "Olaitan" },
            ["Kasımpaşa"] = new[] { "Gianniotis", "Fall", "Winck", "Haris Hajradinović" },
            ["Antalyaspor"] = new[] { "Abdullah Yiğiter", "Storm", "Van de Streek", "Sander van de Streek" },
            ["Konyaspor"] = new[] { "Deniz Ertaş", "Umut Nayir", "Bardhi", "Jin-ho Jo" },
            // Saudi Pro League
            ["Al-Hilal"] = new[] { "Bono", "Rúben Neves", "Malcom", "Milinković-Savić", "Darwin Núñez", "Cancelo" },
            ["Al-Nassr"] = new[] { "Bento", "Cristiano Ronaldo", "Sadio Mané", "João Félix", "Coman", "Brozović" },
            ["Al-Ittihad"] = new[] { "Rajković", "Benzema", "Kanté", "Bergwijn", "Fabinho", "Moussa Diaby" },
            ["Al-Ahli"] = new[] { "Mendy", "Mahrez", "Toney", "Kessié", "Galeno", "Demiral" },
            ["Al-Qadsiah"] = new[] { "Casteels", "Quiñones", "Nacho", "Retegui", "Otávio" },
            ["Al-Ettifaq"] = new[] { "Rodák", "Wijnaldum", "Moussa Dembélé", "Álvaro Medrán" },
            ["Al-Shabab"] = new[] { "Grohe", "Carrasco", "Bonsu Baah", "Hamdallah" },
            ["Al-Taawoun"] = new[] { "Mailson", "Musa Barrow", "Flávio", "Al-Ahmad" },
            ["Al-Fateh"] = new[] { "Dmitrović", "Batna", "Vargas", "Bendebka" },
            ["Al-Khaleej"] = new[] { "Moris", "Fortounis", "Masouras", "Kurdi" },
            // MLS
            ["Inter Miami"] = new[] { "Ustari", "Messi", "Luis Suárez", "De Paul", "Segovia", "Jordi Alba" },
            ["LAFC"] = new[] { "Lloris", "Son Heung-min", "Bouanga", "Delgado", "Tillman" },
            ["LA Galaxy"] = new[] { "Mićović", "Riqui Puig", "Paintsil", "Pec", "Reus" },
            ["Columbus Crew"] = new[] { "Schulte", "Rossi", "Hernández", "Nagbe", "Arfsten" },
            ["FC Cincinnati"] = new[] { "Celentano", "Evander", "Denkey", "Valenzuela" },
            ["Seattle Sounders"] = new[] { "Frei", "Morris", "Rusnák", "De la Vega", "Roldan" },
            ["Orlando City"] = new[] { "Gallese", "Muriel", "Ojeda", "Enrique", "McGuire" },
            ["Atlanta United"] = new[] { "Guzan", "Almirón", "Latte Lath", "Slisz" },
            ["New York City FC"] = new[] { "Freese", "Martínez", "Moralez", "Wolf", "Ojeda" },
            ["Toronto FC"] = new[] { "Sean Johnson", "Bernardeschi", "Insigne", "Osorio" },
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

        /// <summary>"do Brasileirão", "da La Liga", "da Premier League".</summary>
        public static string Of(string league) => (league == "Brasileirão" ? "do " : "da ") + league;

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

        /// <summary>Comemorações de gol (id do gesto no jogador 3D, nome na tela). Id vazio = uma diferente a cada gol.</summary>
        public static readonly string[][] Celebrations =
        {
            new[] { "", "Variar" }, new[] { "aviao", "Aviãozinho" }, new[] { "joelhada", "Joelhada" }, new[] { "soco", "Soco no ar" },
            new[] { "silencio", "Silêncio" }, new[] { "coracao", "Coração" }, new[] { "danca", "Dancinha" },
        };

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
