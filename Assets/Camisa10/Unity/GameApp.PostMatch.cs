using System;
using System.Collections.Generic;
using System.Linq;
using Camisa10.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Camisa10.UI
{
    // Pós-jogo: placar final, nota com a origem dos pontos, lances da partida, craque do jogo, repercussão
    // (moral, técnico, elenco, torcida, fama) e a entrevista coletiva opcional.
    public partial class GameApp
    {
        struct Snap { public float moral, coach, squad, fans, fame; public long money; }

        GameObject postRoot;
        MatchEngine postMatch;
        Snap postBefore;
        string postWc;
        bool pressDone;

        Snap TakeSnap()
        {
            var p = game.S.player;
            return new Snap { moral = p.moral, coach = p.coach, squad = p.squad, fans = p.fans, fame = p.fame, money = p.money };
        }

        void ShowPostMatch(MatchEngine m, Snap before, string wcLine)
        {
            postMatch = m; postBefore = before; postWc = wcLine; pressDone = false;
            BuildPostMatch();
        }

        void BuildPostMatch()
        {
            if (postRoot != null) Destroy(postRoot);
            var m = postMatch;
            var root = UIKit.Img(overlays, Color.white, false, "PosJogo");
            root.sprite = Procedural.BackdropSprite();
            UIKit.Stretch(root.rectTransform);
            postRoot = root.gameObject;
            var rv = UIKit.V(postRoot, 0, 18);
            rv.padding = new RectOffset(40, 40, 24, 28);

            // placar final
            var board = UIKit.Img(root.transform, Theme.Alpha(Theme.Bar, .92f), true, "Placar");
            UIKit.LE(board, 176, 176);
            var sc = UIKit.H(board.gameObject, 10, TextAnchor.MiddleCenter);
            sc.padding = new RectOffset(30, 30, 12, 12);
            TeamColumn(board.transform, m.My, Color.white, 96);
            var mid = UIKit.Column(board.transform, 0, TextAnchor.MiddleCenter);
            UIKit.LE(mid, prefW: 520, minW: 360);
            string comp = postWc != null || m.My.league == "wc" ? "Copa do Mundo" : $"{Game.League(game.S.season.league).Name} · fim de jogo";
            UIKit.Label(mid, comp, Theme.Turf, 24).alignment = TextAnchor.MiddleCenter;
            UIKit.Txt(mid, $"{m.Gf} x {m.Ga}", 96, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            string res = m.Result == 'w' ? "VITÓRIA" : m.Result == 'l' ? "DERROTA" : "EMPATE";
            UIKit.Txt(mid, res, 30, m.Result == 'w' ? Theme.Turf : m.Result == 'l' ? Theme.Red : Theme.Muted, FontStyle.Bold, TextAnchor.MiddleCenter);
            TeamColumn(board.transform, m.Opp, Color.white, 96);

            var body = UIKit.Cols(root.transform, 22);
            UIKit.LE(body, flexH: 1, minH: 200);

            // sua atuação
            var perf = UIKit.Tile(body, "Sua atuação", Theme.Gold, 1.1f, 26, 6);
            if (m.Plays)
            {
                var top = UIKit.Row(perf, 22);
                var nota = UIKit.Txt(top, Fmt.Rating(m.Rating), 110, m.Rating >= 7 ? Theme.Gold : m.Rating < 6 ? Theme.Red : Theme.Ink, FontStyle.Bold, TextAnchor.MiddleLeft);
                UIKit.LE(nota, 120, 120, prefW: 190, minW: 190);
                var info = UIKit.Column(top, 6);
                UIKit.LE(info, flexW: 1, minW: 0);
                string extra = m.Sent ? " · expulso" : m.Yellow ? " · 1 amarelo" : "";
                UIKit.Txt(info, $"{m.Goals} gol(s) · {m.Assists} assistência(s){extra}", 26, Theme.Ink, FontStyle.Bold);
                UIKit.Muted(info, $"{(m.Points >= 0 ? "+" : "")}{m.Points} pontos · nota = 6,0 + pontos ÷ 10", 22);
                if (m.Motm) UIKit.Tag(info, "Craque do jogo", Theme.Gold, Theme.GoldInk);
                var acts = m.Actions;
                int show = Mathf.Min(acts.Count, 8);
                for (int i = 0; i < show; i++) ActionLine(perf, acts[i].Minute + "'", acts[i].Label, acts[i].Points);
                if (acts.Count > show)
                {
                    int rest = acts.Skip(show).Sum(a => a.Points);
                    ActionLine(perf, "", $"Mais {acts.Count - show} ação(ões)", rest);
                }
            }
            else UIKit.Muted(perf, m.Role == Role.Lesionado ? "Você acompanhou da tribuna, em recuperação." : m.Role == Role.Poupado ? "Você foi poupado nesta rodada." : "Você ficou no banco e não entrou em campo.");

            // lances
            var plays = UIKit.Tile(body, "Lances da partida", Theme.Cyan, 1.1f, 26, 6);
            var key = m.Feed.Where(f => f.Kind != FeedKind.Info).ToList();
            if (key.Count == 0) UIKit.Muted(plays, "Jogo truncado, sem grandes lances.");
            foreach (var f in key.Skip(Mathf.Max(0, key.Count - 9)))
            {
                Color col = f.Kind == FeedKind.TeamGoal ? Theme.FeedGold : f.Kind == FeedKind.OppGoal ? Theme.FeedRed : Theme.Ink;
                var t = UIKit.Txt(plays, f.Text, 23, col, f.Kind == FeedKind.Me ? FontStyle.Normal : FontStyle.Bold);
                t.verticalOverflow = VerticalWrapMode.Truncate;
                UIKit.LE(t, 56, 30);
            }
            UIKit.Space(plays, 6);
            var motm = UIKit.Row(plays, 12);
            UIKit.Label(motm, "Craque do jogo", Theme.Gold, 22);
            UIKit.Txt(motm, m.MotmName + (m.Motm ? " (você)" : ""), 26, m.Motm ? Theme.Gold : Theme.Ink, FontStyle.Bold);

            // repercussão e entrevista
            var side = UIKit.Stack(body, .85f, 18);
            var rep = UIKit.Tile(side, "Repercussão", Theme.Turf, 1, 26, 6);
            var p = game.S.player;
            Delta(rep, "Moral", p.moral - postBefore.moral);
            Delta(rep, "Técnico", p.coach - postBefore.coach);
            Delta(rep, "Elenco", p.squad - postBefore.squad);
            Delta(rep, "Torcida", p.fans - postBefore.fans);
            Delta(rep, "Fama", p.fame - postBefore.fame);
            long dm = p.money - postBefore.money;
            var mr = UIKit.Row(rep, 12);
            UIKit.LE(UIKit.Txt(mr, "Dinheiro na rodada", 26, Theme.Muted), flexW: 1, minW: 0);
            UIKit.Txt(mr, (dm >= 0 ? "+" : "") + Fmt.Money(dm), 26, dm >= 0 ? Theme.Turf : Theme.Red, FontStyle.Bold, TextAnchor.UpperRight);
            bool canPress = m.Plays || m.Role == Role.Reserva;
            if (!pressDone && canPress)
            {
                UIKit.Muted(rep, "Os repórteres esperam na sala de imprensa. O que você disser mexe com técnico, elenco e torcida.", 20);
                UIKit.Primary(side, "Entrevista coletiva", () => PressStep(PressConference.Build(game, m), 0));
            }
            else if (pressDone) UIKit.Btn(side, "Entrevista feita", Theme.Chip, Theme.Turf, null, 92, 30).interactable = false;
            UIKit.GoldBtn(side, "Continuar", ClosePostMatch);
        }

        void ActionLine(Transform parent, string minute, string label, int pts)
        {
            var r = UIKit.Row(parent, 12);
            UIKit.LE(UIKit.Txt(r, minute, 22, Theme.Muted, FontStyle.Bold), prefW: 56, minW: 56);
            UIKit.LE(UIKit.Txt(r, label, 24, Theme.Ink), flexW: 1, minW: 0);
            UIKit.Txt(r, (pts > 0 ? "+" : "") + pts, 24, pts >= 0 ? Theme.Turf : Theme.Red, FontStyle.Bold, TextAnchor.UpperRight);
        }

        void Delta(Transform parent, string label, float d)
        {
            var r = UIKit.Row(parent, 12);
            UIKit.LE(UIKit.Txt(r, label, 26, Theme.Muted), flexW: 1, minW: 0);
            int v = Mathf.RoundToInt(d);
            string s = Mathf.Abs(d) < .5f ? "=" : (v > 0 ? "+" : "") + v;
            UIKit.Txt(r, s, 26, Mathf.Abs(d) < .5f ? Theme.Muted : d > 0 ? Theme.Turf : Theme.Red, FontStyle.Bold, TextAnchor.UpperRight);
        }

        /// <summary>Uma pergunta por vez: escolhe a resposta, vê a repercussão e segue para a próxima.</summary>
        void PressStep(List<PressQuestion> qs, int i)
        {
            if (i >= qs.Count)
            {
                pressDone = true;
                CloseModal();
                game.AddNews("Entrevista coletiva depois do jogo: suas respostas repercutiram.");
                Save();
                BuildPostMatch();
                return;
            }
            var q = qs[i];
            var buttons = new List<(string, Action, bool)>();
            for (int k = 0; k < q.Answers.Length; k++)
            {
                int idx = k;
                buttons.Add((q.Answers[k].Label, () =>
                {
                    string r = PressConference.Answer(game, q, idx);
                    ShowChoice(q.Reporter, r, (i + 1 < qs.Count ? "Próxima pergunta" : "Encerrar a coletiva", () => PressStep(qs, i + 1), true));
                }, k == 0));
            }
            ShowChoice($"Coletiva · pergunta {i + 1} de {qs.Count}", $"{q.Reporter}: \"{q.Text}\"", buttons.ToArray());
        }

        void ClosePostMatch()
        {
            if (postRoot != null) Destroy(postRoot);
            postRoot = null;
            var wc = postWc;
            postMatch = null; postWc = null;
            Commit(null, true);
            if (game.S.mailsThisRound > 0) Toast(game.S.mailsThisRound == 1 ? "Você tem uma mensagem nova." : $"Você tem {game.S.mailsThisRound} mensagens novas.");
            if (wc != null) ShowModal(game.S.wc.champion ? "CAMPEÃO DO MUNDO!" : "Copa do Mundo", wc, ("Continuar", (Action)CloseModal, true));
            else if (GalaPending) ShowGala(); // fim do campeonato: a gala de premiação
        }

        /// <summary>Janela com as opções empilhadas (respostas longas cabem inteiras).</summary>
        void ShowChoice(string title, string text, params (string label, Action action, bool primary)[] buttons)
        {
            CloseModal();
            var dim = UIKit.Img(overlays, new Color(0, 0, 0, .72f), false, "Modal");
            UIKit.Stretch(dim.rectTransform);
            modal = dim.gameObject;
            var sheet = UIKit.Img(dim.transform, Theme.CardHi, true, "Dialog");
            var rt = sheet.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(1180, 0);
            UIKit.V(sheet.gameObject, 44, 16);
            sheet.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var strip = UIKit.Img(sheet.transform, Theme.Cyan, false, "Strip");
            strip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var srt = strip.rectTransform;
            srt.anchorMin = new Vector2(0, 1); srt.anchorMax = Vector2.one; srt.pivot = new Vector2(.5f, 1);
            srt.sizeDelta = new Vector2(0, 5); srt.anchoredPosition = Vector2.zero;
            UIKit.Label(sheet.transform, title, Theme.Cyan, 24);
            UIKit.Txt(sheet.transform, text, 34, Theme.Ink, FontStyle.Bold);
            UIKit.Space(sheet.transform, 4);
            foreach (var (label, action, primary) in buttons)
            {
                var b = primary ? UIKit.Primary(sheet.transform, label, action) : UIKit.Ghost(sheet.transform, label, action);
                UIKit.LE(b, 80, 80);
            }
        }
    }
}
