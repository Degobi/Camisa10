using System;
using System.Collections.Generic;

namespace Camisa10.Core
{
    /// <summary>Um compromisso do calendário: rodada da liga, fase da copa, data de seleções, janela de transferências ou Copa do Mundo.</summary>
    public class CalendarItem
    {
        public string kind;       // liga | copa | selecao | janela | mundial
        public int round;         // rodada da liga (contada a partir de 1) depois da qual acontece
        public Club opp;          // adversário (liga)
        public bool home, derby, rival;
        public string label;
    }

    /// <summary>Calendário da temporada mostrado na Central: os próximos jogos e as datas que importam.</summary>
    public partial class Game
    {
        /// <summary>Rodada em que a janela do meio da temporada abre (depois do jogo desta rodada).</summary>
        public int WindowRound => SeasonRounds / 2;

        public List<CalendarItem> Upcoming(int n)
        {
            var list = new List<CalendarItem>();
            var se = S.season;
            if (se.phase == "end") return list;
            string me = S.contract.club;
            for (int w = se.week; w < SeasonRounds && list.Count < n; w++)
            {
                var pr = se.rounds[w].games.Find(x => x.home == me || x.away == me);
                if (pr == null) continue;
                bool home = pr.home == me;
                var opp = ClubById(home ? pr.away : pr.home);
                list.Add(new CalendarItem { kind = "liga", round = w + 1, opp = opp, home = home, derby = IsDerby(MyClub.name, opp.name), rival = opp.id == S.rival.club && !string.IsNullOrEmpty(S.rival.name) });
                int played = w + 1; // as datas extras acontecem depois desta rodada
                int ci = Array.IndexOf(CupWeeks, played);
                if (ci >= 0 && !se.cupOut && !se.cupWon && ci == se.cup.Count) list.Add(new CalendarItem { kind = "copa", round = played, label = CupStages[ci] });
                if (Array.IndexOf(NationalWeeks, played) >= 0) list.Add(new CalendarItem { kind = "selecao", round = played, label = "Seleções" });
                if (played == WindowRound) list.Add(new CalendarItem { kind = "janela", round = played, label = "Janela" });
            }
            if (list.Count < n && IsWorldCupYear(S.year)) list.Add(new CalendarItem { kind = "mundial", round = SeasonRounds, label = $"Copa {S.year}" });
            if (list.Count > n) list.RemoveRange(n, list.Count - n);
            return list;
        }
    }
}
