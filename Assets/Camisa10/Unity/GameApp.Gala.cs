using System.Linq;
using Camisa10.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Camisa10.UI
{
    // Gala de fim de temporada: as taças e prêmios do ano (troféus 3D) e o pódio da Bola de Ouro, uma vez por temporada.
    public partial class GameApp
    {
        GameObject galaRoot;

        bool GalaPending => game != null && game.S.season.phase == "end" && !game.S.season.galaShown;

        void ShowGala()
        {
            var se = game.S.season; var sm = se.summary; var S = game.S;
            se.galaShown = true;
            Save();
            if (galaRoot != null) Destroy(galaRoot);
            var root = UIKit.Img(overlays, Color.white, false, "Gala");
            root.sprite = Procedural.BackdropSprite();
            UIKit.Stretch(root.rectTransform);
            galaRoot = root.gameObject;
            var rv = UIKit.V(galaRoot, 0, 18);
            rv.padding = new RectOffset(48, 48, 30, 30);

            var head = UIKit.Column(root.transform, 0, TextAnchor.UpperCenter);
            UIKit.Label(head, $"Gala da temporada {se.year}", Theme.Gold, 30).alignment = TextAnchor.UpperCenter;
            UIKit.Txt(head, $"{game.MyClub.name} terminou em {sm.rank}º · {se.stats.goals} gol(s) e {se.stats.assists} assistência(s) em {se.stats.apps} jogo(s)",
                30, Theme.Ink, FontStyle.Bold, TextAnchor.UpperCenter);

            var body = UIKit.Cols(root.transform, 24);
            UIKit.LE(body, flexH: 1, minH: 300);

            // as taças e os prêmios do ano
            var tro = UIKit.Tile(body, "Suas conquistas", Theme.Gold, 1.25f, 26, 10);
            var won = sm.titles.Select(t => (t, true)).Concat(sm.awards.Select(a => (a, false))).ToList();
            if (won.Count == 0)
            {
                UIKit.Txt(tro, "Nenhum troféu desta vez.", 34, Theme.Muted, FontStyle.Bold, TextAnchor.MiddleCenter);
                UIKit.Muted(tro, "Títulos, artilharia, seleção do campeonato e Bola de Ouro aparecem aqui. A próxima temporada é uma chance nova.").alignment = TextAnchor.UpperCenter;
            }
            else
            {
                var grid = UIKit.Grid(tro, new Vector2(250, 270), 4, 14);
                foreach (var (text, title) in won.Take(8)) TrophyCard(grid.transform, text, title);
            }

            // pódio da Bola de Ouro
            var bo = UIKit.Tile(body, $"Bola de Ouro {S.ballonYear}", Theme.Gold, 1f, 26, 10);
            if (S.ballon.Count >= 3)
            {
                var pod = UIKit.Row(bo, 12, TextAnchor.LowerCenter);
                UIKit.LE(pod, 330, 330);
                Podium(pod, S.ballon[1], 2, 190);
                Podium(pod, S.ballon[0], 1, 250);
                Podium(pod, S.ballon[2], 3, 150);
                int mine = S.ballon.FindIndex(r => r.me);
                string where = mine < 0 ? "Você ficou fora do top 10." : mine < 3 ? "" : mine < 10 ? $"Você ficou em {mine + 1}º lugar." : "Você ficou fora do top 10.";
                if (mine == 0) where = "VOCÊ É O MELHOR JOGADOR DO MUNDO!";
                if (where != "") UIKit.Txt(bo, where, 30, mine == 0 ? Theme.Gold : Theme.Ink, FontStyle.Bold, TextAnchor.UpperCenter);
            }
            else UIKit.Muted(bo, "Ranking indisponível.");

            UIKit.GoldBtn(root.transform, "Continuar", () => { if (galaRoot != null) Destroy(galaRoot); galaRoot = null; Render(true); });
        }

        void Podium(Transform parent, RankRow r, int pos, float h)
        {
            var col = UIKit.Column(parent, 6, TextAnchor.LowerCenter);
            UIKit.LE(col, flexW: 1, prefW: 0, minW: 0);
            if (pos == 1) Trophy(col, true, 110, "Bola de Ouro");
            var club = game.S.clubs.Find(c => c.name == r.club);
            if (club != null) UIKit.Crest(col, club, 40, 48).GetComponent<LayoutElement>().flexibleWidth = 0;
            var nm = UIKit.Txt(col, r.me ? r.name + " (você)" : r.name, 24, r.me ? Theme.Turf : Theme.Ink, FontStyle.Bold, TextAnchor.LowerCenter);
            nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 14; nm.resizeTextMaxSize = 24;
            UIKit.LE(nm, 30, 30);
            var block = UIKit.Img(col, pos == 1 ? Theme.Gold : pos == 2 ? Theme.Silver : Theme.Bronze, true, "Degrau");
            UIKit.LE(block, h * .45f, h * .45f);
            var t = UIKit.Txt(block.transform, pos + "º", 46, Theme.GoldInk, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.Stretch(t.rectTransform);
        }
    }
}
