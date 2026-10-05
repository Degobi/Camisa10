using System;

namespace Camisa10.Core
{
    /// <summary>
    /// Reputação em três frentes, como no I Am Playr: técnico (player.coach), elenco (player.squad) e torcida (player.fans).
    /// O elenco pesa na escalação e na braçadeira de capitão; a torcida canta o seu nome no estádio (ou vaia).
    /// </summary>
    public partial class Game
    {
        /// <summary>Saves antigos não tinham elenco nem torcida: começa no meio (a torcida ainda não te conhece).</summary>
        void EnsureReputation()
        {
            if (S.repInit) return;
            S.repInit = true;
            S.player.squad = 50;
            S.player.fans = 45;
        }

        /// <summary>Soma com retorno decrescente perto do topo (ou do fundo, se for perda).</summary>
        public static float Gain(float cur, float delta) => delta >= 0 ? delta * (float)Clamp(1.15 - cur / 110.0, .15, 1) : delta * (float)Clamp(.3 + cur / 90.0, .3, 1);

        public void AddSquad(float d) { S.player.squad = (float)Clamp(S.player.squad + Gain(S.player.squad, d), 0, 100); }
        public void AddFans(float d) { S.player.fans = (float)Clamp(S.player.fans + Gain(S.player.fans, d), 0, 100); }

        public static string SquadLabel(float v) => v >= 80 ? "Liderança do grupo" : v >= 62 ? "Querido no vestiário" : v >= 40 ? "Respeitado" : v >= 22 ? "Isolado" : "Clima ruim";
        public static string FansLabel(float v) => v >= 85 ? "Ídolo da torcida" : v >= 65 ? "Xodó da arquibancada" : v >= 42 ? "Aprovado" : v >= 25 ? "Desconfiança" : "Vaiado";

        /// <summary>A torcida grita o seu nome (o lance 3D usa para o canto da arquibancada).</summary>
        public bool FansChant => S.player.fans >= 78;
        public bool FansBoo => S.player.fans < 25;

        /// <summary>Clássicos e rivalidades locais de cada liga (pesam em dobro para a torcida).</summary>
        static readonly string[][] Derbies =
        {
            new[] { "Flamengo", "Fluminense" }, new[] { "Flamengo", "Vasco da Gama" }, new[] { "Flamengo", "Botafogo" }, new[] { "Fluminense", "Vasco da Gama" },
            new[] { "Botafogo", "Fluminense" }, new[] { "Botafogo", "Vasco da Gama" }, new[] { "Palmeiras", "Corinthians" }, new[] { "Palmeiras", "São Paulo" },
            new[] { "Corinthians", "São Paulo" }, new[] { "Cruzeiro", "Grêmio" }, new[] { "Real Madrid", "Barcelona" }, new[] { "Real Madrid", "Atlético de Madrid" },
            new[] { "Real Betis", "Sevilla" }, new[] { "Athletic Bilbao", "Real Sociedad" }, new[] { "Liverpool", "Manchester United" },
            new[] { "Manchester City", "Manchester United" }, new[] { "Arsenal", "Tottenham" }, new[] { "Chelsea", "Arsenal" }, new[] { "Inter de Milão", "Milan" },
            new[] { "Roma", "Lazio" }, new[] { "Juventus", "Inter de Milão" }, new[] { "Paris Saint-Germain", "Olympique de Marseille" }, new[] { "Lyon", "Olympique de Marseille" },
            new[] { "Galatasaray", "Fenerbahçe" }, new[] { "Beşiktaş", "Galatasaray" }, new[] { "Fenerbahçe", "Beşiktaş" }, new[] { "Al-Hilal", "Al-Nassr" },
            new[] { "Al-Ittihad", "Al-Ahli" }, new[] { "LA Galaxy", "LAFC" }, new[] { "New York City FC", "Toronto FC" },
        };

        public static bool IsDerby(string a, string b)
        {
            foreach (var d in Derbies)
                if ((d[0] == a && d[1] == b) || (d[0] == b && d[1] == a)) return true;
            return false;
        }

        /// <summary>Depois de cada rodada: a torcida reage ao jogo e o vestiário à sua postura. Tudo volta devagar para o meio.</summary>
        void ReputationAfterMatch(MatchEngine m)
        {
            var p = S.player;
            bool derby = m.Opp != null && m.My != null && IsDerby(m.My.name, m.Opp.name);
            if (m.Plays)
            {
                float f = m.Goals * 1.1f + (m.Rating - 6.8f) * 1.1f + (m.Result == 'w' ? .6f : m.Result == 'l' ? -.8f : 0);
                if (derby) f *= 2;
                AddFans(f);
                AddSquad(m.Assists * .7f + (m.Result == 'w' ? .3f : 0) - (m.Sent ? 4 : 0));
            }
            else if (m.Role == Role.Reserva) AddFans(-.2f);
            p.squad += (50 - p.squad) * .02f;
            p.fans += (50 - p.fans) * .03f;
            if (S.captain) { p.moral += .5f; p.fame += .04f; }
            CaptaincyCheck();
        }

        /// <summary>Braçadeira: o técnico oferece quando o vestiário te tem como líder; perde quem racha o grupo.</summary>
        void CaptaincyCheck()
        {
            var p = S.player;
            if (S.captain)
            {
                if (p.squad < 48 || p.coach < 32)
                {
                    S.captain = false;
                    p.moral -= 6;
                    AddNews("Você perdeu a braçadeira de capitão.");
                    Mail("tecnico", CoachName, "A braçadeira", "Conversei com a diretoria e decidimos passar a braçadeira para outro jogador. O grupo precisa de outra liderança neste momento.");
                }
                return;
            }
            if (S.captainOffered || p.age < 21 || CareerApps < 25 || S.season.week < 4) return;
            if (p.squad >= 75 && p.coach >= 60 && p.fans >= 55)
            {
                S.captainOffered = true;
                Mail("tecnico", CoachName, "Quero você como capitão",
                    $"O vestiário te respeita e a torcida está com você. Quero que você seja o capitão do {MyClub.name} a partir da próxima rodada. É uma responsabilidade grande: o capitão fala pelo grupo, dentro e fora de campo.",
                    "capitao", null, "Aceitar a braçadeira", "Ainda não me sinto pronto");
            }
        }

        /// <summary>Troca de clube: a braçadeira e o vestiário ficam para trás (a torcida nova começa desconfiada).</summary>
        void ReputationNewClub()
        {
            S.captain = false; S.captainOffered = false;
            S.player.squad = 50;
            S.player.fans = (float)Clamp(35 + S.player.fame * .3, 35, 65);
        }
    }
}
