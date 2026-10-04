using System.Linq;
using Camisa10.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Camisa10.UI
{
    // Sala de troféus, Bola de Ouro, conquistas e os blocos de artilharia, copa e seleção na Central.
    public partial class GameApp
    {
        Image Trophy(Transform parent, bool gold, float h, string title = null)
        {
            var im = UIKit.Img(parent, Color.white, false, "Taca");
            // troféu 3D com o formato da taça de verdade; sem ele, o desenho simples
            im.sprite = TrophyStudio.Get(title != null ? TrophyStudio.KindFor(title) : gold ? "brasileirao" : "estrela") ?? Procedural.TrophySprite(gold);
            im.preserveAspect = true;
            UIKit.LE(im, h, h, prefW: h * .84f, minW: h * .84f);
            return im;
        }

        /// <summary>Terceira linha da Central: artilharia, copa e seleção.</summary>
        void HomeGlory()
        {
            var se = game.S.season;
            var row = UIKit.Cols(content);

            var art = UIKit.Tile(row, "Artilharia", Theme.Gold, 1.25f, 24, 4);
            int i = 0;
            foreach (var t in game.TopScorers(6))
            {
                i++;
                var club = game.ClubById(t.club);
                var line = UIKit.Img(art, t.me ? Theme.Alpha(Theme.Turf, .18f) : new Color(0, 0, 0, 0), true, "Line");
                var h = UIKit.H(line.gameObject, 12);
                h.padding = new RectOffset(10, 10, 4, 4);
                Color col = t.me ? Theme.Turf : Theme.Ink;
                UIKit.LE(UIKit.Txt(line.transform, i + "º", 25, Theme.Muted, FontStyle.Bold), prefW: 44);
                if (club != null) UIKit.Crest(line.transform, club, 26, 32);
                UIKit.LE(UIKit.Txt(line.transform, t.me ? t.name + " (você)" : t.name, 25, col, t.me ? FontStyle.Bold : FontStyle.Normal), flexW: 1, minW: 0);
                UIKit.LE(UIKit.Txt(line.transform, t.goals + " gols", 25, col, FontStyle.Bold, TextAnchor.UpperRight), prefW: 110);
            }
            int myRank = game.MyScorerRank;
            if (myRank > 6) UIKit.Muted(art, $"Você é o {myRank}º da artilharia, com {se.stats.goals} gol(s).", 22);

            var cup = UIKit.Tile(row, Game.CupName(se.league), Theme.Cyan, 1f, 24, 8);
            string status = se.cupWon ? "Campeão!" : se.cupOut ? "Eliminado" : se.cup.Count < Game.CupStages.Length
                ? Game.CupStages[se.cup.Count] : "Campeão!";
            HeadRow(cup, status, se.cupWon ? "Taça" : se.cupOut ? "Fora" : "Vivo",
                se.cupWon ? Theme.Gold : se.cupOut ? Theme.Red : Theme.Turf, se.cupWon ? Theme.GoldInk : se.cupOut ? Color.white : Theme.TurfInk);
            if (se.cup.Count == 0)
                UIKit.Muted(cup, $"Mata-mata com {Game.CupStages.Length} fases. A estreia é depois da rodada {Game.CupWeeks[0]}.", 22);
            foreach (var t in se.cup)
            {
                var r = UIKit.Row(cup, 10);
                UIKit.LE(UIKit.Label(r, Game.CupStages[t.stage], Theme.Muted, 20), prefW: 190);
                UIKit.Crest(r, new Club { name = t.opp, c1 = t.c1, c2 = t.c2 }, 26, 32);
                string score = $"{t.gf} x {t.ga} {t.opp}" + (t.pens ? " (pên.)" : "");
                UIKit.LE(UIKit.Txt(r, score, 24, t.won ? Theme.Ink : Theme.Red, FontStyle.Normal), flexW: 1, minW: 0);
                if (t.myGoals > 0) UIKit.Tag(r, t.myGoals + " gol" + (t.myGoals > 1 ? "s" : ""), Theme.Gold, Theme.GoldInk);
            }
            if (!se.cupOut && !se.cupWon && se.cup.Count < Game.CupStages.Length)
            {
                int next = Game.CupWeeks[se.cup.Count];
                UIKit.Muted(cup, $"Próximo jogo da copa depois da rodada {next}.", 22);
            }

            var nat = UIKit.Tile(row, "Seleção Brasileira", Theme.Gold, .8f, 24, 8);
            var head = UIKit.Row(nat, 12);
            var flag = UIKit.Crest(head, new Club { name = "Seleção", c1 = "#FFDF00", c2 = "#009C3B" }, 50, 60);
            var col2 = UIKit.Column(head, 0);
            UIKit.LE(col2, flexW: 1, minW: 0);
            UIKit.Txt(col2, game.S.calledUp ? "Convocado" : "Fora da lista", 30, game.S.calledUp ? Theme.Turf : Theme.Muted, FontStyle.Bold);
            UIKit.Muted(col2, $"{game.S.caps} jogo(s), {game.S.intlGoals} gol(s)", 22);
            int nextCall = Game.NationalWeeks.FirstOrDefault(w => w > se.week);
            UIKit.Muted(nat, nextCall > 0 ? $"Próxima convocação depois da rodada {nextCall}." : "Próxima convocação na temporada que vem.", 22);
            float need = Mathf.Clamp01((float)(game.NationalScore - 60) / 20f);
            UIKit.Label(nat, "Chance de convocação", Theme.Muted, 20);
            UIKit.Bar(nat, need, need >= 1 ? Theme.Turf : Theme.Gold);
        }

        void BuildTrophies()
        {
            var S = game.S;
            var row1 = UIKit.Cols(content);

            // títulos
            var t = UIKit.Tile(row1, "Sala de troféus", Theme.Gold, 1.3f);
            var hr = UIKit.Row(t, 18);
            Trophy(hr, true, 110);
            var hc = UIKit.Column(hr, 2);
            UIKit.LE(hc, flexW: 1, minW: 0);
            UIKit.Txt(hc, S.titles.Count == 0 ? "Nenhum título ainda" : $"{S.titles.Count} título(s)", 40, Theme.Gold, FontStyle.Bold);
            UIKit.Muted(hc, $"{S.awards.Count} prêmio(s) individual(is) · {S.monthly.Count} craque do mês", 24);
            if (S.titles.Count == 0) UIKit.Muted(t, "Seja campeão da liga ou da copa para encher a estante.");
            var grid = UIKit.Grid(t, new Vector2(218, 230), 3, 12);
            foreach (var title in S.titles) TrophyCard(grid.transform, title, true);
            foreach (var award in S.awards) TrophyCard(grid.transform, award, false);

            // Bola de Ouro
            var b = UIKit.Tile(row1, S.ballonYear > 0 ? $"Bola de Ouro {S.ballonYear}" : "Bola de Ouro", Theme.Gold, 1f, 24, 4);
            if (S.ballon.Count == 0) UIKit.Muted(b, "O ranking dos melhores do mundo sai no fim da temporada. Jogue bem, ganhe títulos e faça gols para entrar na lista.");
            for (int i = 0; i < S.ballon.Count; i++)
            {
                var rr = S.ballon[i];
                int pos = rr.me && i >= 10 ? game.S.bestBallon : i + 1;
                var line = UIKit.Img(b, rr.me ? Theme.Alpha(Theme.Turf, .18f) : new Color(0, 0, 0, 0), true, "Line");
                var h = UIKit.H(line.gameObject, 10);
                h.padding = new RectOffset(10, 10, 4, 4);
                Color col = rr.me ? Theme.Turf : i == 0 ? Theme.Gold : Theme.Ink;
                UIKit.LE(UIKit.Txt(line.transform, rr.me && i >= 10 ? "-" : (i + 1) + "º", 24, Theme.Muted, FontStyle.Bold), prefW: 46);
                var club = S.clubs.Find(c => c.name == rr.club);
                if (club != null) UIKit.Crest(line.transform, club, 24, 30);
                UIKit.LE(UIKit.Txt(line.transform, rr.me ? rr.name + " (você)" : rr.name, 24, col, i == 0 || rr.me ? FontStyle.Bold : FontStyle.Normal), flexW: 1, minW: 0);
            }
            if (S.bestBallon > 0) UIKit.Muted(b, $"Sua melhor colocação: {S.bestBallon}º lugar.", 22);

            // carreira em números
            var n = UIKit.Tile(row1, "Carreira", Theme.Cyan, .8f);
            UIKit.KV(n, "Jogos", game.CareerApps.ToString());
            UIKit.KV(n, "Gols", game.CareerGoals.ToString());
            UIKit.KV(n, "Gols na copa", S.cupGoalsTotal.ToString());
            UIKit.KV(n, "Seleção", $"{S.caps} jogos, {S.intlGoals} gols");
            UIKit.KV(n, "Conquistas", $"{S.achievements.Count}/{Game.Achievements.Length}");

            // conquistas
            var row2 = UIKit.Cols(content);
            var a = UIKit.Tile(row2, "Conquistas", Theme.Purple);
            UIKit.Muted(a, "Cada conquista dá dinheiro e fama na hora.", 22);
            var ag = UIKit.Grid(a, new Vector2(400, 120), 4, 14);
            foreach (var def in Game.Achievements)
            {
                bool got = game.HasAchievement(def.Id);
                var card = UIKit.Img(ag.transform, got ? Theme.Alpha(Theme.Gold, .22f) : Theme.Alpha(Theme.Chip, .7f), true, "Conquista");
                var v = UIKit.V(card.gameObject, 14, 2);
                v.padding = new RectOffset(18, 18, 12, 10);
                UIKit.Txt(card.transform, (got ? "★ " : "") + def.Name, 26, got ? Theme.Gold : Theme.Ink, FontStyle.Bold);
                UIKit.Txt(card.transform, def.Desc, 21, Theme.Muted);
                UIKit.Txt(card.transform, def.Money > 0 ? $"{Fmt.Money(def.Money)} · fama +{def.Fame:0}" : $"fama +{def.Fame:0}", 20, got ? Theme.Turf : Theme.Muted);
            }

            if (S.monthly.Count > 0)
            {
                var m = UIKit.Tile(row2, "Craque do mês", Theme.Turf, .5f);
                foreach (var s in S.monthly.AsEnumerable().Reverse().Take(8)) UIKit.Body(m, "• " + s);
            }
        }

        void TrophyCard(Transform parent, string text, bool title)
        {
            var card = UIKit.Img(parent, Theme.Alpha(title ? Theme.Gold : Theme.Chip, title ? .16f : .7f), true, "Trofeu");
            var v = UIKit.V(card.gameObject, 10, 4, TextAnchor.UpperCenter);
            Trophy(card.transform, title, 140, text);
            var tx = UIKit.Txt(card.transform, text, 21, title ? Theme.Gold : Theme.Ink, FontStyle.Bold, TextAnchor.UpperCenter);
            tx.resizeTextForBestFit = true; tx.resizeTextMinSize = 14; tx.resizeTextMaxSize = 21;
            UIKit.LE(tx, 56, 56);
        }
    }
}
