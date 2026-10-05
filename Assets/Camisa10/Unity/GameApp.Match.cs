using System;
using System.Collections.Generic;
using System.Collections;
using Camisa10.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Camisa10.UI
{
    public partial class GameApp
    {
        GameObject matchRoot;
        Text scoreText, clockText;
        RectTransform feedContent, dock;
        ScrollRect feedScroll;
        Image dockImg;
        int feedShown;
        bool fast;
        Coroutine matchCo;
        Chance3D chance;
        ChanceHud chanceHud;
        bool topFillWasOn;

        bool worldCupMatch; // partida da Copa do Mundo (não conta para a liga)

        void StartWorldCupMatch()
        {
            match = game.NewWorldCupMatch();
            if (match == null) return;
            worldCupMatch = true;
            fast = false;
            feedShown = 0;
            BuildMatchOverlay();
            RefreshMatch();
            RunLoop(.7f);
        }

        void StartMatch()
        {
            worldCupMatch = false;
            match = new MatchEngine(game);
            fast = false;
            feedShown = 0;
            BuildMatchOverlay();
            RefreshMatch();
            RunLoop(.7f);
        }

        void RunLoop(float delay)
        {
            if (matchCo != null) StopCoroutine(matchCo);
            matchCo = StartCoroutine(MatchLoop(delay));
        }

        IEnumerator MatchLoop(float delay)
        {
            yield return new WaitForSeconds(delay);
            while (match != null)
            {
                var r = match.Step();
                RefreshMatch();
                if (r != StepResult.Continue) yield break;
                yield return new WaitForSeconds(fast ? .08f : .65f);
            }
        }

        void BuildMatchOverlay()
        {
            var root = UIKit.Img(overlays, Color.white, false, "Match");
            root.sprite = Procedural.BackdropSprite();
            UIKit.Stretch(root.rectTransform);
            matchRoot = root.gameObject;
            var rv = UIKit.V(matchRoot, 0, 0);
            rv.padding = new RectOffset(40, 40, 24, 30);
            rv.spacing = 18;

            // placar no topo, como a barra de transmissão
            var board = UIKit.Img(root.transform, Theme.Alpha(Theme.Bar, .92f), true, "Placar");
            UIKit.LE(board, 170, 170);
            var sc = UIKit.H(board.gameObject, 10, TextAnchor.MiddleCenter);
            sc.padding = new RectOffset(30, 30, 14, 14);
            TeamColumn(board.transform, match.My, Color.white, 96);
            var mid = UIKit.Column(board.transform, 0, TextAnchor.MiddleCenter);
            UIKit.LE(mid, prefW: 360, minW: 300);
            scoreText = UIKit.Txt(mid, "0 x 0", 96, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            clockText = UIKit.Txt(mid, "0'", 32, Theme.Turf, FontStyle.Bold, TextAnchor.MiddleCenter);
            TeamColumn(board.transform, match.Opp, Color.white, 96);

            // lances à esquerda, painel de ação à direita
            var body = UIKit.Cols(root.transform, 24);
            UIKit.LE(body, flexH: 1, minH: 200);
            var feedTile = UIKit.Tile(body, "Lance a lance", Theme.Cyan, 1.5f, 10, 0);
            var (fs, fc) = UIKit.Scroll(feedTile, 20, 12);
            feedScroll = fs; feedContent = fc;
            UIKit.LE(fs, minH: 200, flexH: 1);

            dockImg = UIKit.Img(body, Theme.Dock, true, "Dock");
            dock = dockImg.rectTransform;
            UIKit.LE(dockImg, flexW: 1, prefW: 0, minW: 0);
            UIKit.V(dockImg.gameObject, 36, 18, TextAnchor.MiddleCenter);
        }

        void RefreshMatch()
        {
            if (match == null) return;
            scoreText.text = $"{match.Gf} x {match.Ga}";
            scoreText.color = match.GoalFlash ? Theme.FeedGold : Color.white;
            clockText.text = match.Done ? "FIM DE JOGO" : match.Minute + "'";

            while (feedShown < match.Feed.Count)
            {
                var f = match.Feed[feedShown++];
                Color col = f.Kind == FeedKind.TeamGoal ? Theme.FeedGold : f.Kind == FeedKind.OppGoal ? Theme.FeedRed
                    : f.Kind == FeedKind.Me ? Color.white : Theme.FeedText;
                UIKit.Txt(feedContent, f.Text, 28, col, f.Kind == FeedKind.Info ? FontStyle.Normal : FontStyle.Bold);
            }

            UIKit.Clear(dock);
            if (match.Current != null)
            {
                // lance decisivo: entra a cena 3D em primeira pessoa
                dockImg.color = Theme.Dock;
                UIKit.Txt(dock, $"{match.Minute}' {match.Current.Text}", 34, Theme.Ink, FontStyle.Bold);
                if (chance == null) StartChance();
            }
            else if (match.Done)
            {
                dockImg.color = Theme.Dock;
                string res = match.Result == 'w' ? "Vitória" : match.Result == 'l' ? "Derrota" : "Empate";
                UIKit.Txt(dock, res.ToUpperInvariant(), 52, match.Result == 'w' ? Theme.Turf : match.Result == 'l' ? Theme.Red : Theme.Ink,
                    FontStyle.Bold, TextAnchor.MiddleCenter);
                if (match.Plays)
                {
                    UIKit.Label(dock, "Sua nota", Theme.Muted, 22).alignment = TextAnchor.MiddleCenter;
                    UIKit.Txt(dock, Fmt.Rating(match.Rating), 110, Theme.Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
                    string extra = match.Sent ? ", expulso" : match.Yellow ? ", 1 amarelo" : "";
                    UIKit.Txt(dock, $"{match.Goals} gol(s), {match.Assists} assistência(s){extra}", 26, Theme.Muted, FontStyle.Normal, TextAnchor.UpperCenter);
                    UIKit.Txt(dock, PointsBreakdown(match), 24, Theme.Ink, FontStyle.Normal, TextAnchor.UpperCenter);
                }
                else UIKit.Muted(dock, "Você não entrou em campo nesta rodada.").alignment = TextAnchor.UpperCenter;
                UIKit.GoldBtn(dock, "Concluir rodada", EndMatchUI);
            }
            else
            {
                dockImg.color = Theme.Alpha(Theme.Dock, .6f);
                UIKit.Label(dock, "Partida em andamento", Theme.Muted, 22).alignment = TextAnchor.MiddleCenter;
                if (match.Plays && match.Actions.Count > 0)
                    UIKit.Txt(dock, $"Nota ao vivo {Fmt.Rating(match.Rating)}  ·  {(match.Points >= 0 ? "+" : "")}{match.Points} pts", 30, Theme.Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
                UIKit.Txt(dock, "Os lances decisivos são seus: quando chegar a hora, você joga em primeira pessoa.", 26, Theme.Ink,
                    FontStyle.Normal, TextAnchor.MiddleCenter);
                UIKit.Btn(dock, fast ? "Velocidade normal" : "Acelerar", Theme.Chip, Color.white,
                    () => { fast = !fast; RefreshMatch(); }, 84, 28);
            }

            Canvas.ForceUpdateCanvases();
            feedScroll.verticalNormalizedPosition = 0;
        }

        /// <summary>"Gol +10 · Drible ×2 +6 · Passe errado −2": de onde veio a nota.</summary>
        static string PointsBreakdown(MatchEngine m)
        {
            var groups = new List<(string label, int count, int pts)>();
            foreach (var a in m.Actions)
            {
                int i = groups.FindIndex(x => x.label == a.Label);
                if (i < 0) groups.Add((a.Label, 1, a.Points));
                else groups[i] = (a.Label, groups[i].count + 1, groups[i].pts + a.Points);
            }
            if (groups.Count == 0) return "Nenhuma ação sua registrada.";
            var parts = new List<string>();
            foreach (var x in groups)
            {
                string col = x.pts >= 0 ? "#19E68C" : "#FF5468";
                parts.Add($"{x.label}{(x.count > 1 ? " ×" + x.count : "")} <color={col}>{(x.pts > 0 ? "+" : "")}{x.pts}</color>");
            }
            return string.Join("  ·  ", parts) + $"\n<b>Total {(m.Points >= 0 ? "+" : "")}{m.Points} pts</b>  (nota = 6,0 + pontos ÷ 10)";
        }

        void StartChance()
        {
            if (matchCo != null) StopCoroutine(matchCo);
            topFillWasOn = topFill.gameObject.activeSelf;
            safe.gameObject.SetActive(false);
            background.gameObject.SetActive(false);
            // a faixa preta de fundo cobre a tela inteira: sem esconder, a câmera 3D do lance fica invisível
            letterbox.gameObject.SetActive(false);
            topFill.gameObject.SetActive(false);
            chanceHud = ChanceHud.Build(canvasRoot);
            chance = Chance3D.Play(transform, game, match, chanceHud, OnChanceDone);
            Debug.Log("[Camisa 10] Lance 3D iniciado (versão com campo visível).");
        }

        void OnChanceDone(LiveOutcome outcome)
        {
            if (chance != null) Destroy(chance.gameObject);
            chance = null;
            chanceHud?.Destroy();
            chanceHud = null;
            safe.gameObject.SetActive(true);
            background.gameObject.SetActive(true);
            letterbox.gameObject.SetActive(true);
            topFill.gameObject.SetActive(topFillWasOn);
            if (match == null) return;
            match.ResolveLive(outcome);
            RefreshMatch();
            RunLoop(.5f);
        }

        // Alternativa por botões (não usada na partida 3D; mantida para testes rápidos)
        void OptionButton(int i)
        {
            var o = match.Current.Options[i];
            double pr = match.Prob(o);
            string lvl = pr >= .6 ? "alta" : pr >= .35 ? "média" : "baixa";
            Color tagBg = pr >= .6 ? Theme.ChanceHigh : pr >= .35 ? Theme.ChanceMid : Theme.ChanceLow;

            var im = UIKit.Img(dock, Theme.Chip, true, "Option");
            var b = im.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            int idx = i;
            b.onClick.AddListener(() => Choose(idx));
            var h = UIKit.H(im.gameObject, 16);
            h.padding = new RectOffset(36, 28, 0, 0);
            UIKit.LE(im, 96, 96);
            var l = UIKit.Txt(im.transform, o.Label, 30, Theme.Ink, FontStyle.Bold, TextAnchor.MiddleLeft);
            UIKit.LE(l, flexW: 1, minW: 0);
            UIKit.Tag(im.transform, "Chance " + lvl, tagBg, Theme.Ink);
        }

        void Choose(int idx)
        {
            if (match == null || match.Current == null) return;
            bool continues = match.Choose(idx);
            RefreshMatch();
            if (!continues) RunLoop(fast ? .08f : .65f);
        }

        void EndMatchUI()
        {
            if (match == null || !match.Done) return;
            if (matchCo != null) StopCoroutine(matchCo);
            string wcLine = worldCupMatch ? game.FinishWorldCupMatch(match) : null;
            if (!worldCupMatch) game.FinishMatch(match);
            worldCupMatch = false;
            match = null;
            Destroy(matchRoot);
            Commit(null, true);
            if (game.S.mailsThisRound > 0) Toast(game.S.mailsThisRound == 1 ? "Você tem uma mensagem nova." : $"Você tem {game.S.mailsThisRound} mensagens novas.");
            if (wcLine != null) ShowModal(game.S.wc.champion ? "CAMPEÃO DO MUNDO!" : "Copa do Mundo", wcLine, ("Continuar", (Action)CloseModal, true));
        }
    }
}
