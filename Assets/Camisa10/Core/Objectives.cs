using System;
using System.Collections.Generic;
using System.Linq;

namespace Camisa10.Core
{
    /// <summary>Objetivo da temporada definido pelo técnico (individual) ou pela diretoria (do clube).</summary>
    [Serializable]
    public class Objective
    {
        public string kind;           // gols | part | jogos | nota | rank | copa
        public float target, start;   // start: contador no momento em que o objetivo foi definido (troca de clube no meio do ano)
        public bool club;             // objetivo do clube, cobrado pela diretoria
        public string result = "";    // "" em andamento | ok | falhou
    }

    /// <summary>
    /// Objetivos da temporada, como no modo carreira dos consoles: no começo do ano o técnico e a diretoria dizem o que esperam
    /// (gols, jogos, nota, posição na tabela, copa). No fim, o resultado mexe na relação com o técnico, na fama, no bônus e na renovação.
    /// </summary>
    public partial class Game
    {
        void EnsureObjectives()
        {
            var se = S.season;
            if (se.objectives == null) se.objectives = new List<Objective>();
            if (se.objectives.Count == 0 && se.league != null && se.phase != "end" && MyClub != null) MakeObjectives(0, 1);
        }

        /// <summary>Posição do clube entre os da liga, pela força do elenco (1 = favorito).</summary>
        int StrengthRank(Club c) => S.clubs.Count(x => x.league == c.league && x.str > c.str) + 1;

        /// <summary>Define os objetivos. fraction: parte da temporada que ainda falta (1 no começo; menos na janela do meio).</summary>
        void MakeObjectives(int fromWeek, double fraction)
        {
            var se = S.season; var st = se.stats; var c = MyClub;
            int rounds = SeasonRounds;
            // o papel esperado vem do geral comparado com o elenco (a forma e o técnico entram depois, jogando)
            double d = Ovr - c.str;
            int level = d >= 6 ? 2 : d >= -4 ? 1 : 0; // 0 reserva, 1 titular, 2 estrela
            var list = new List<Objective>();
            double games = rounds * fraction;
            string pos = S.player.pos;
            if (pos == "ATA") list.Add(new Objective { kind = "gols", target = (float)Math.Max(2, Math.Round(games * new[] { .3, .55, .8 }[level])), start = st.goals });
            else if (pos == "MEI") list.Add(new Objective { kind = "part", target = (float)Math.Max(2, Math.Round(games * new[] { .3, .5, .7 }[level])), start = st.goals + st.assists });
            else list.Add(new Objective { kind = "nota", target = new[] { 6.6f, 6.9f, 7.2f }[level] });
            list.Add(new Objective { kind = "jogos", target = (float)Math.Max(3, Math.Round(games * new[] { .35, .62, .75 }[level])), start = st.apps });
            if (pos != "ATA" && pos != "MEI" && level == 2 && Rng.Chance(.5)) list[1] = new Objective { kind = "nota", target = 7.0f };
            if (list[0].kind == "nota" && list[1].kind == "nota") list[1] = new Objective { kind = "jogos", target = (float)Math.Round(games * .7), start = st.apps };

            // a diretoria: posição na tabela de acordo com o tamanho do clube, ou a copa
            int sr = StrengthRank(c);
            int goal = sr <= 1 ? 1 : sr <= 3 ? 3 : sr <= 6 ? 6 : 8;
            bool cupAlive = !se.cupOut && !se.cupWon && fromWeek < CupWeeks[0];
            if (cupAlive && Rng.Chance(.35)) list.Add(new Objective { kind = "copa", target = sr <= 3 ? 4 : sr <= 6 ? 2 : 1, club = true });
            else list.Add(new Objective { kind = "rank", target = goal, club = true });
            se.objectives = list;
        }

        public int MyRank => SortedTable().FindIndex(t => t.club == S.contract.club) + 1;

        public string ObjText(Objective o)
        {
            switch (o.kind)
            {
                case "gols": return $"Marcar {o.target:0} gols no campeonato";
                case "part": return $"Somar {o.target:0} gols e assistências";
                case "jogos": return $"Entrar em campo em {o.target:0} jogos";
                case "nota": return $"Nota média {Fmt.Rating(o.target)} ou mais";
                case "rank": return o.target <= 1 ? "Ser campeão da liga" : $"Terminar entre os {o.target:0} primeiros";
                case "copa": return o.target >= 4 ? $"Ganhar a {CupName(S.season.league)}" : o.target >= 2 ? $"Chegar à semifinal da {CupName(S.season.league)}" : $"Passar das oitavas da {CupName(S.season.league)}";
                default: return "";
            }
        }

        float ObjValue(Objective o)
        {
            var se = S.season; var st = se.stats;
            switch (o.kind)
            {
                case "gols": return st.goals - o.start;
                case "part": return st.goals + st.assists - o.start;
                case "jogos": return st.apps - o.start;
                case "nota": return AvgRating;
                case "rank": return MyRank;
                case "copa": return se.cup.Count(t => t.won);
                default: return 0;
            }
        }

        /// <summary>Cumprido agora (para nota: com pelo menos um terço dos jogos).</summary>
        public bool ObjMet(Objective o)
        {
            float v = ObjValue(o);
            if (o.kind == "rank") return v >= 1 && v <= o.target;
            if (o.kind == "nota") return S.season.stats.apps >= Math.Max(6, SeasonRounds / 3) && v >= o.target - .001f;
            return v >= o.target;
        }

        /// <summary>Já não dá mais (eliminado da copa antes da fase pedida).</summary>
        public bool ObjLost(Objective o) => o.kind == "copa" && S.season.cupOut && !ObjMet(o);

        public float ObjProgress01(Objective o)
        {
            float v = ObjValue(o);
            if (o.kind == "rank") return v <= 0 ? 0 : (float)Clamp((10.5 - v) / (10.5 - o.target), 0, 1);
            if (o.kind == "nota") return S.season.stats.apps == 0 ? 0 : (float)Clamp((v - 5.5) / (o.target - 5.5), 0, 1);
            return (float)Clamp(v / Math.Max(1f, o.target), 0, 1);
        }

        public string ObjProgressText(Objective o)
        {
            float v = ObjValue(o); var st = S.season.stats;
            switch (o.kind)
            {
                case "rank": return S.season.table.All(t => t.p == 0) ? "Campeonato ainda não começou" : $"Hoje: {v:0}º lugar";
                case "nota": return st.apps == 0 ? "Nenhum jogo ainda" : $"Hoje: {Fmt.Rating(v)} em {st.apps} jogo(s)";
                case "copa": return S.season.cupWon ? "Campeão!" : S.season.cupOut ? "Eliminado" : $"{v:0} fase(s) vencida(s)";
                default: return $"{v:0} de {o.target:0}";
            }
        }

        /// <summary>Fim de temporada: cada objetivo cumprido rende bônus e confiança; cada falha custa relação com o técnico.</summary>
        void ObjectivesEndSeason(SeasonSummary sm)
        {
            var se = S.season; var p = S.player;
            if (se.objectives == null || se.objectives.Count == 0) return;
            int met = 0; long gross = 0;
            var lines = new List<string>();
            foreach (var o in se.objectives)
            {
                bool ok = ObjMet(o);
                o.result = ok ? "ok" : "falhou";
                if (ok) { met++; gross += R1000(S.contract.salary * (o.club ? 1.5 : 2)); }
                p.coach += ok ? (o.club ? 3 : 4) : (o.club ? -2 : -4);
                lines.Add($"{(ok ? "Cumprido" : "Não cumprido")}: {ObjText(o)}");
            }
            se.objMet = met;
            p.fame += met * .7f;
            p.moral += (met - se.objectives.Count / 2f) * 3;
            long net = R1000(gross * (1 - TaxRate));
            p.money += net; S.seasonNet += net;
            sm.notes.Insert(0, $"Objetivos da temporada: {met} de {se.objectives.Count} cumpridos" + (net > 0 ? $" (bônus de {Fmt.Money(net)} líquidos)." : "."));
            string verdict = met == se.objectives.Count ? "Temporada exemplar. A diretoria quer conversar sobre o seu futuro com o maior prazer."
                : met == 0 ? "Não foi o que esperávamos de você. Vamos avaliar com calma os próximos passos."
                : "Uma temporada com altos e baixos. Esperamos mais regularidade no ano que vem.";
            Mail("diretoria", BoardName, $"Balanço da temporada {se.year}", string.Join("\n", lines) + "\n\n" + verdict + (net > 0 ? $"\n\nBônus por objetivos: {Fmt.Money(net)} (já descontado o imposto)." : ""));
        }

        /// <summary>Mensagem do técnico na metade do campeonato, com o andamento dos objetivos.</summary>
        void ObjectivesMidSeason()
        {
            var se = S.season;
            if (se.objectives == null || se.objectives.Count == 0) return;
            int ok = se.objectives.Count(o => ObjProgress01(o) >= .5f || ObjMet(o));
            var lines = se.objectives.Select(o => $"• {ObjText(o)}: {ObjProgressText(o)}");
            Mail("tecnico", CoachName, "Metade da temporada", "Chegamos na metade do campeonato. Como estão os seus objetivos:\n" + string.Join("\n", lines) + "\n\n" +
                (ok == se.objectives.Count ? "Está no caminho certo. Não tire o pé." : ok == 0 ? "Precisamos de uma reação no segundo turno." : "Dá para chegar lá, mas precisa acelerar."));
        }

        /// <summary>Texto da diretoria apresentando os objetivos (começo de temporada).</summary>
        string ObjectivesLetter() => "Estes são os objetivos que combinamos para você nesta temporada:\n" +
            string.Join("\n", S.season.objectives.Select(o => $"• {ObjText(o)}" + (o.club ? " (meta do clube)" : ""))) +
            "\n\nCada objetivo cumprido vale bônus no fim do ano e pesa na renovação do contrato.";
    }
}
