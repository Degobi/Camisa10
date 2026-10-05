using System;
using System.Collections.Generic;
using System.Linq;
using Camisa10.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Camisa10.UI
{
    /// <summary>
    /// Telas fora de campo, desenhadas para o celular deitado: blocos lado a lado como num menu de modo carreira.
    /// Cada tela monta linhas de colunas (UIKit.Cols) com blocos (UIKit.Tile) dentro da área rolável.
    /// </summary>
    public partial class GameApp
    {
        Text shirtName;

        // ---------- componentes compartilhados ----------
        Button Chip(Transform parent, string label, bool selected, Action onClick, int height = 72)
            => UIKit.Btn(parent, label, selected ? Theme.Turf : Theme.Chip, selected ? Theme.TurfInk : Theme.Ink, onClick, height, 24);

        void Meters(Transform parent, bool twoColumns)
        {
            var p = game.S.player;
            if (!twoColumns)
            {
                Meter(parent, "Energia", p.energy);
                Meter(parent, "Moral", p.moral);
                Meter(parent, "Fama", p.fame);
                Reputation(parent);
                return;
            }
            var cols = UIKit.Cols(parent, 30);
            var a = UIKit.Stack(cols, 1, 14);
            var b = UIKit.Stack(cols, 1, 14);
            Meter(a, "Energia", p.energy);
            Meter(a, "Moral", p.moral);
            Meter(a, "Fama", p.fame);
            Reputation(b);
        }

        /// <summary>Reputação em três frentes: técnico, elenco e torcida.</summary>
        void Reputation(Transform parent)
        {
            var p = game.S.player;
            Meter(parent, "Técnico", p.coach);
            Meter(parent, "Elenco · " + Game.SquadLabel(p.squad).ToLowerInvariant(), p.squad);
            Meter(parent, "Torcida · " + Game.FansLabel(p.fans).ToLowerInvariant(), p.fans);
        }

        void Meter(Transform parent, string label, float v)
        {
            var cell = UIKit.Column(parent, 6);
            var r = UIKit.Row(cell);
            var l = UIKit.Muted(r, label, 24);
            UIKit.LE(l, flexW: 1, minW: 0);
            UIKit.Txt(r, Mathf.RoundToInt(v).ToString(), 26, Theme.Ink, FontStyle.Bold, TextAnchor.UpperRight);
            UIKit.Bar(cell, v / 100f, v < 30 ? Theme.Red : v < 60 ? Theme.Gold : Theme.Turf);
        }

        void TeamColumn(Transform parent, Club c, Color textColor, float crestH = 100)
        {
            var col = UIKit.Column(parent, 8, TextAnchor.UpperCenter);
            UIKit.LE(col, flexW: 1, minW: 0, prefW: 0);
            UIKit.Crest(col, c, crestH * .83f, crestH);
            UIKit.Txt(col, c.name, 28, textColor, FontStyle.Bold, TextAnchor.UpperCenter);
        }

        RectTransform SubCard(Transform parent, int pad = 22, int spacing = 10)
        {
            var im = UIKit.Img(parent, Theme.Chip, true, "SubCard");
            UIKit.V(im.gameObject, pad, spacing);
            return im.rectTransform;
        }

        /// <summary>Linha de bloco com título à esquerda e um selo opcional à direita.</summary>
        RectTransform HeadRow(Transform parent, string title, string tag = null, Color? tagBg = null, Color? tagFg = null)
        {
            var r = UIKit.Row(parent, 14);
            var t = UIKit.Txt(r, title, 34, Theme.Ink, FontStyle.Bold);
            UIKit.LE(t, flexW: 1, minW: 0);
            if (!string.IsNullOrEmpty(tag)) UIKit.Tag(r, tag, tagBg ?? Theme.Chip, tagFg ?? Theme.Ink);
            return r;
        }

        static string Signed(float v) => v > 0 ? "+" + v.ToString("0") : v.ToString("0");

        void Shirt(Transform parent, string name, float size = 420)
        {
            var img = UIKit.Img(parent, Color.white, false, "Shirt");
            img.sprite = Procedural.ShirtSprite();
            img.preserveAspect = true;
            UIKit.LE(img, size, size);
            float k = size / 560f;
            var nameRt = UIKit.Rect("Name", img.transform);
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(.5f, .5f);
            nameRt.sizeDelta = new Vector2(330 * k, 70 * k);
            nameRt.anchoredPosition = new Vector2(0, 95 * k);
            shirtName = nameRt.gameObject.AddComponent<Text>();
            UIKit.Style(shirtName, FontStyle.Bold); shirtName.fontSize = (int)(50 * k);
            shirtName.color = Color.white; shirtName.alignment = TextAnchor.MiddleCenter;
            shirtName.resizeTextForBestFit = true; shirtName.resizeTextMinSize = 14; shirtName.resizeTextMaxSize = (int)(50 * k);
            shirtName.raycastTarget = false;
            shirtName.text = ShirtLabel(name);
            var num = UIKit.Txt(img.transform, "10", (int)(250 * k), Theme.Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
            var nrt = num.rectTransform;
            nrt.anchorMin = nrt.anchorMax = new Vector2(.5f, .5f);
            nrt.sizeDelta = new Vector2(400 * k, 280 * k);
            nrt.anchoredPosition = new Vector2(0, -125 * k);
        }

        static string ShirtLabel(string name)
        {
            string n = string.IsNullOrWhiteSpace(name) ? "SEU NOME" : name.Trim().ToUpperInvariant();
            return n.Length > 14 ? n.Substring(0, 14) : n;
        }

        // ---------- criação ----------
        void BuildCreate()
        {
            var cols = UIKit.Cols(content);

            var left = UIKit.Stack(cols, .8f, 10);
            left.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            Shirt(left, newName, 400);
            UIKit.Txt(left, "CAMISA 10", 72, Theme.Ink, FontStyle.Bold, TextAnchor.UpperCenter);
            UIKit.Label(left, "Modo carreira · jogador", Theme.Turf, 26).alignment = TextAnchor.UpperCenter;
            UIKit.Txt(left, "Do primeiro contrato à aposentadoria. Treino, agenda, contratos, patrocínios e negócios: tudo muda sua carreira.",
                26, Theme.Muted, FontStyle.Normal, TextAnchor.UpperCenter);

            var mid = UIKit.Stack(cols, 1);
            var c1 = UIKit.Tile(mid, "Nome do jogador", Theme.Cyan);
            UIKit.Input(c1, newName, "Como vai aparecer na camisa", 22, s => { newName = s; if (shirtName != null) shirtName.text = ShirtLabel(s); });

            var c2 = UIKit.Tile(mid, "Posição", Theme.Cyan);
            var grid = UIKit.Grid(c2, new Vector2(170, 104), 3, 14);
            foreach (var code in GameData.PositionOrder)
            {
                bool sel = newPos == code;
                var im = UIKit.Img(grid.transform, sel ? Theme.Turf : Theme.Chip, true, "Pos");
                var b = im.gameObject.AddComponent<Button>();
                b.targetGraphic = im;
                string captured = code;
                b.onClick.AddListener(() => { newPos = captured; Render(); });
                UIKit.V(im.gameObject, 8, 0, TextAnchor.MiddleCenter);
                UIKit.Txt(im.transform, code, 40, sel ? Theme.TurfInk : Theme.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
                UIKit.Txt(im.transform, GameData.Positions[code].Name, 22, sel ? Theme.TurfInk : Theme.Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            }

            int used = alloc.Sum(), pointsLeft = AllocPoints - used;
            var preview = new int[6];
            for (int i = 0; i < 6; i++) preview[i] = GameData.Positions[newPos].Base[i] + alloc[i];

            var right = UIKit.Stack(cols, 1.1f);
            var c3 = UIKit.Tile(right, "Atributos", Theme.Gold);
            HeadRow(c3, $"Geral inicial {Game.Overall(newPos, preview)}", $"{pointsLeft} ponto(s)", Theme.Gold, Theme.GoldInk);
            UIKit.Muted(c3, $"Distribua {AllocPoints} pontos (até {AllocMax} por atributo).");
            foreach (var a in GameData.Attrs)
            {
                int i = (int)a;
                var r = UIKit.Row(c3, 12);
                var l = UIKit.Body(r, GameData.Label(a));
                UIKit.LE(l, flexW: 1, minW: 0);
                var minus = UIKit.Ghost(r, "-", () => { if (alloc[i] > 0) { alloc[i]--; Render(); } });
                UIKit.LE(minus, 64, 64, prefW: 80);
                minus.interactable = alloc[i] > 0;
                var v = UIKit.Txt(r, preview[i].ToString(), 36, Theme.Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
                UIKit.LE(v, prefW: 70);
                var plus = UIKit.Ghost(r, "+", () => { if (alloc[i] < AllocMax && alloc.Sum() < AllocPoints) { alloc[i]++; Render(); } });
                UIKit.LE(plus, 64, 64, prefW: 80);
                plus.interactable = pointsLeft > 0 && alloc[i] < AllocMax;
            }
            UIKit.GoldBtn(right, "Assinar o primeiro contrato", () =>
            {
                game = Game.NewCareer(newName, newPos, (int[])alloc.Clone());
                trainAttr = Game.MainAttr(newPos);
                tab = "home";
                Commit(null, true);
            });
            UIKit.Muted(right, "Você começa aos 17 anos em um clube pequeno do Brasileirão.").alignment = TextAnchor.UpperCenter;
        }

        // ---------- central ----------
        void BuildHome()
        {
            var se = game.S.season;
            if (se.phase == "end") { BuildSeasonEnd(); return; }
            var p = game.S.player;
            var f = game.CurrentFixture();

            CalendarStrip();

            // linha 1: próximo jogo, semana, condição
            var row1 = UIKit.Cols(content);

            var leftCol = UIKit.Stack(row1, 1.25f);
            bool derby = Game.IsDerby(game.MyClub.name, f.opp.name);
            var next = UIKit.Tile(leftCol, derby ? "Próximo jogo · clássico" : "Próximo jogo", derby ? Theme.Red : Theme.Turf);
            UIKit.Muted(next, $"{Game.League(se.league).Name} · rodada {se.week + 1} de {game.SeasonRounds} · {(f.home ? "em casa" : "fora de casa")}");
            var vs = UIKit.Row(next, 10, TextAnchor.MiddleCenter);
            TeamColumn(vs, f.home ? game.MyClub : f.opp, Theme.Ink, 100);
            var x = UIKit.Txt(vs, "VS", 44, Theme.Muted, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.LE(x, prefW: 90);
            TeamColumn(vs, f.home ? f.opp : game.MyClub, Theme.Ink, 100);
            int oppPos = game.SortedTable().FindIndex(t => t.club == f.opp.id) + 1;
            string extra = f.opp.id == game.S.rival.club && !string.IsNullOrEmpty(game.S.rival.name) ? $" Duelo com o seu rival, {game.S.rival.name}." : derby ? " A torcida não aceita perder." : "";
            UIKit.Muted(next, $"Adversário: {oppPos}º na tabela, força {f.opp.str}.{extra}").alignment = TextAnchor.UpperCenter;
            ObjectivesTile(leftCol);

            var week = UIKit.Tile(row1, se.phase == "train" ? "Semana de treino" : "Dia de jogo", Theme.Cyan, 1f);
            if (se.phase == "train")
            {
                if (p.injury > 0)
                {
                    HeadRow(week, "Departamento médico", "Lesionado", Theme.Red, Color.white);
                    UIKit.Muted(week, $"Você está fora por mais {p.injury} rodada(s). Faça fisioterapia para recuperar energia.");
                    UIKit.Primary(week, "Fazer fisioterapia", () => { game.Physio(); Commit("Sessão de fisioterapia feita."); });
                }
                else
                {
                    UIKit.Muted(week, "Escolha o foco e a intensidade do treino.");
                    var grid = UIKit.Grid(week, new Vector2(164, 66), 3, 12);
                    foreach (var a in GameData.Attrs)
                    {
                        var attr = a;
                        Chip(grid.transform, $"{GameData.Label(a)} {p.Get(a)}", trainAttr == a, () => { trainAttr = attr; Render(); }, 66);
                    }
                    var ir = UIKit.Row(week, 12);
                    foreach (var it in GameData.Intensities)
                    {
                        string id = it.Id;
                        var b = Chip(ir, it.Name, intensity == id, () => { intensity = id; Render(); }, 62);
                        UIKit.LE(b, flexW: 1, prefW: 0);
                    }
                    UIKit.Muted(week, GameData.GetIntensity(intensity).Hint, 24);
                    UIKit.Primary(week, "Treinar", DoTrain);
                }
            }
            else
            {
                var role = se.role == Role.None ? game.ComputeRole() : se.role;
                bool watch = role == Role.Lesionado || role == Role.Poupado;
                Color tagBg = role == Role.Reserva ? Theme.Gold : watch ? Theme.Red : Theme.Turf;
                Color tagFg = role == Role.Reserva ? Theme.GoldInk : watch ? Color.white : Theme.TurfInk;
                HeadRow(week, "Escalação", GameData.RoleName(role), tagBg, tagFg);
                UIKit.Muted(week, GameData.RoleHint(role));
                UIKit.GoldBtn(week, watch ? "Acompanhar a partida" : "Ir para o jogo", StartMatch);
            }

            var cond = UIKit.Tile(row1, "Condição", Theme.Gold, .8f);
            Meters(cond, false);
            UIKit.Muted(cond, $"Forma: {(p.form.Count > 0 ? Fmt.Rating(game.FormAvg) : "sem jogos")}");

            // linha 2: tabela, agenda e negócios, notícias
            var row2 = UIKit.Cols(content);
            var table = UIKit.Tile(row2, Game.League(se.league).Name, Theme.Turf, 1.25f, 24, 2);
            BuildTable(table);

            var midStack = UIKit.Stack(row2, 1f);
            var ag = UIKit.Tile(midStack, "Agenda da semana", Theme.Purple);
            HeadRow(ag, game.ActionsLeft > 0 ? $"{game.ActionsLeft} horário(s) livre(s)" : "Agenda cheia",
                $"{BusinessData.ActionsPerWeek - game.ActionsLeft}/{BusinessData.ActionsPerWeek}", Theme.Purple, Color.white);
            UIKit.Muted(ag, "Imprensa, técnico, patrocinadores, redes e vida pessoal. Cada escolha mexe na sua carreira.");
            UIKit.Ghost(ag, "Abrir agenda", () => { tab = "agenda"; Render(true); });

            var biz = UIKit.Tile(midStack, "Negócios", Theme.Gold);
            UIKit.KV(biz, "Carteira de ações", Fmt.Money(game.PortfolioValue));
            UIKit.KV(biz, "Empresas (renda por rodada)", Fmt.Money(game.VenturesWeekly));
            UIKit.Ghost(biz, "Ver negócios", () => { tab = "business"; Render(true); });

            var right = UIKit.Stack(row2, .8f);
            HomeInbox(right);
            var news = UIKit.Tile(right, "Notícias", Theme.Muted);
            if (game.S.news.Count == 0) UIKit.Muted(news, "Nada por aqui ainda.");
            foreach (var item in game.S.news.Take(3))
            {
                var col = UIKit.Column(news, 2);
                UIKit.Label(col, item.when, Theme.Muted, 20);
                UIKit.Txt(col, item.text, 24, Theme.Ink);
            }

            HomeGlory();
        }

        /// <summary>Faixa do calendário: os próximos jogos (escudo, casa/fora, clássico e rival em destaque) e as datas especiais.</summary>
        void CalendarStrip()
        {
            var items = game.Upcoming(9);
            if (items.Count == 0) return;
            var t = UIKit.Tile(content, "Calendário", Theme.Cyan, 1, 18, 8);
            var row = UIKit.Row(t, 10, TextAnchor.UpperLeft);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            UIKit.LE(row, 150, 150);
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                bool next = i == 0;
                Color edge = it.kind != "liga" ? Theme.Gold : it.derby ? Theme.Red : it.rival ? Theme.Purple : next ? Theme.Turf : Theme.Line;
                var cell = UIKit.Img(row, next ? Theme.Alpha(Theme.Turf, .14f) : Theme.Alpha(Theme.Chip, .85f), true, "Dia");
                UIKit.LE(cell, flexW: 1, prefW: 0, minW: 0);
                var o = cell.gameObject.AddComponent<Outline>();
                o.effectColor = edge; o.effectDistance = new Vector2(2, -2);
                UIKit.V(cell.gameObject, 8, 2, TextAnchor.UpperCenter);
                if (it.kind == "liga")
                {
                    UIKit.Txt(cell.transform, next ? "PRÓXIMO" : "R" + it.round, 18, next ? Theme.Turf : Theme.Muted, FontStyle.Bold, TextAnchor.UpperCenter);
                    UIKit.Crest(cell.transform, it.opp, 46, 54).GetComponent<LayoutElement>().flexibleWidth = 0;
                    string sub = it.derby ? "CLÁSSICO" : it.rival ? "RIVAL" : it.home ? "CASA" : "FORA";
                    UIKit.Txt(cell.transform, sub, 18, it.derby ? Theme.Red : it.rival ? Theme.Purple : Theme.Ink, FontStyle.Bold, TextAnchor.UpperCenter);
                    var nm = UIKit.Txt(cell.transform, it.opp.name, 16, Theme.Muted, FontStyle.Normal, TextAnchor.UpperCenter);
                    nm.verticalOverflow = VerticalWrapMode.Truncate;
                    UIKit.LE(nm, 20, 20);
                }
                else
                {
                    string big = it.kind == "copa" ? "COPA" : it.kind == "selecao" ? "BRA" : it.kind == "janela" ? "MERCADO" : "MUNDIAL";
                    UIKit.Txt(cell.transform, "após R" + it.round, 18, Theme.Muted, FontStyle.Bold, TextAnchor.UpperCenter);
                    UIKit.Space(cell.transform, 6);
                    var bt = UIKit.Txt(cell.transform, big, 30, Theme.Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
                    bt.resizeTextForBestFit = true; bt.resizeTextMinSize = 16; bt.resizeTextMaxSize = 30;
                    UIKit.LE(bt, 40, 40);
                    var lb = UIKit.Txt(cell.transform, it.kind == "copa" ? it.label : it.kind == "selecao" ? "Data de seleções" : it.kind == "janela" ? "Janela de transferências" : it.label, 16, Theme.Ink, FontStyle.Normal, TextAnchor.UpperCenter);
                    lb.resizeTextForBestFit = true; lb.resizeTextMinSize = 12; lb.resizeTextMaxSize = 16;
                    UIKit.LE(lb, 38, 38);
                }
            }
        }

        /// <summary>Objetivos da temporada com barra de progresso (resultado final depois do último jogo).</summary>
        void ObjectivesTile(Transform parent)
        {
            var se = game.S.season;
            if (se.objectives == null || se.objectives.Count == 0) return;
            var t = UIKit.Tile(parent, "Objetivos da temporada", Theme.Gold, 1, 24, 6);
            foreach (var o in se.objectives)
            {
                bool done = o.result == "ok" || (o.result == "" && game.ObjMet(o));
                bool lost = o.result == "falhou" || (o.result == "" && game.ObjLost(o));
                var r = UIKit.Row(t, 12);
                UIKit.Tag(r, o.club ? "Diretoria" : "Técnico", o.club ? Theme.Alpha(Theme.Cyan, .2f) : Theme.Alpha(Theme.Turf, .2f), o.club ? Theme.Cyan : Theme.Turf);
                var l = UIKit.Txt(r, game.ObjText(o), 25, Theme.Ink, FontStyle.Bold);
                UIKit.LE(l, flexW: 1, minW: 0);
                string status = o.result == "ok" ? "Cumprido" : o.result == "falhou" ? "Não cumprido" : game.ObjProgressText(o);
                var st = UIKit.Txt(r, status, 22, done ? Theme.Turf : lost ? Theme.Red : Theme.Muted, FontStyle.Bold, TextAnchor.MiddleRight);
                UIKit.LE(st, minW: st.preferredWidth + 4);
                UIKit.Bar(t, done ? 1 : game.ObjProgress01(o), done ? Theme.Turf : lost ? Theme.Red : Theme.Gold, 10);
            }
        }

        void DoTrain()
        {
            string msg = game.Train(trainAttr, intensity, out bool injured);
            GameEvent ev = null;
            if (!injured && Rng.Chance(.42)) ev = GameEvents.Roll(game);
            Commit(msg);
            if (ev != null) ShowEvent(ev);
        }

        void BuildTable(Transform card)
        {
            TableLine(card, "#", null, "Clube", "P", "J", "SG", false, true);
            var sorted = game.SortedTable();
            for (int i = 0; i < sorted.Count; i++)
            {
                var t = sorted[i];
                var club = game.ClubById(t.club);
                TableLine(card, (i + 1).ToString(), club, club.name, t.pts.ToString(), t.p.ToString(), t.Gd.ToString(),
                    t.club == game.S.contract.club, false);
            }
        }

        void TableLine(Transform parent, string pos, Club club, string name, string pts, string j, string sg, bool me, bool head)
        {
            var bg = UIKit.Img(parent, me ? Theme.Alpha(Theme.Turf, .18f) : new Color(0, 0, 0, 0), true, "Line");
            var h = UIKit.H(bg.gameObject, 12);
            h.padding = new RectOffset(10, 10, 5, 5);
            Color col = head ? Theme.Muted : me ? Theme.Turf : Theme.Ink;
            var style = me || head ? FontStyle.Bold : FontStyle.Normal;
            int size = head ? 20 : 25;
            UIKit.LE(UIKit.Txt(bg.transform, pos, size, col, style), prefW: 40);
            if (club != null) UIKit.Crest(bg.transform, club, 26, 32);
            else UIKit.LE(UIKit.Rect("Gap", bg.transform), prefW: 26);
            UIKit.LE(UIKit.Txt(bg.transform, head ? name.ToUpperInvariant() : name, size, col, style), flexW: 1, minW: 0);
            UIKit.LE(UIKit.Txt(bg.transform, pts, size, col, FontStyle.Bold, TextAnchor.UpperRight), prefW: 56);
            UIKit.LE(UIKit.Txt(bg.transform, j, size, col, style, TextAnchor.UpperRight), prefW: 50);
            UIKit.LE(UIKit.Txt(bg.transform, sg, size, col, style, TextAnchor.UpperRight), prefW: 60);
        }

        void BuildSeasonEnd()
        {
            var se = game.S.season; var sm = se.summary; var st = se.stats; var p = game.S.player; var c = game.MyClub;
            bool forced = game.MustRetire, expired = game.S.contract.years <= 0;

            var cols = UIKit.Cols(content);
            var a = UIKit.Tile(cols, $"Temporada {se.year} encerrada", Theme.Turf);
            HeadRow(a, c.name, $"{sm.rank}º lugar", Theme.Turf, Theme.TurfInk);
            UIKit.KV(a, "Jogos", st.apps.ToString());
            UIKit.KV(a, "Gols", st.goals.ToString());
            UIKit.KV(a, "Assistências", st.assists.ToString());
            UIKit.KV(a, "Nota média", st.apps > 0 ? Fmt.Rating(game.AvgRating) : "-");
            UIKit.KV(a, "Idade agora", $"{p.age} anos");
            UIKit.KV(a, "Geral", game.Ovr.ToString());

            var mid = UIKit.Tile(cols, "Balanço", Theme.Gold);
            if (sm.titles.Count + sm.awards.Count > 0)
            {
                UIKit.Label(mid, "Conquistas", Theme.Gold, 22);
                foreach (var t in sm.titles.Concat(sm.awards)) UIKit.Body(mid, "• " + t);
                UIKit.Space(mid, 8);
            }
            foreach (var t in sm.notes) UIKit.Body(mid, "• " + t);

            if (game.S.wc != null && game.S.wc.year == se.year && game.S.wc.matches.Count > 0) WorldCupTile(cols);
            else ObjectivesTile(cols);

            var f = UIKit.Tile(cols, "Seu futuro", Theme.Cyan);
            string txt = expired ? "Seu contrato terminou. Escolha uma proposta para continuar jogando."
                : $"Você tem contrato com o {c.name} por mais {game.S.contract.years} temporada(s).";
            if (game.S.offers.Count > 0) txt += $" Há {game.S.offers.Count} proposta(s) esperando na aba Contrato.";
            UIKit.Body(f, txt);
            if (game.S.offers.Count > 0) UIKit.Ghost(f, "Ver propostas", () => { tab = "contract"; Render(true); });
            if (forced) UIKit.Muted(f, "Aos 38 anos, chegou a hora de pendurar as chuteiras.");
            else
            {
                var next = UIKit.GoldBtn(f, $"Começar temporada {game.S.year + 1}", () =>
                {
                    if (!game.CanStartNextSeason) return;
                    game.NextSeason();
                    tab = "home";
                    Commit(null, true);
                });
                next.interactable = game.CanStartNextSeason;
                if (game.WorldCupActive) UIKit.Muted(f, "Termine a Copa do Mundo antes de começar a próxima temporada.", 22);
            }
            if (p.age >= 34 || forced || (expired && game.S.offers.Count == 0))
                UIKit.Ghost(f, "Encerrar a carreira", () => { game.S.retired = true; Commit(null, true); });
        }

        // ---------- agenda (ações da semana) ----------
        void BuildAgenda()
        {
            var se = game.S.season;
            var top = UIKit.Cols(content);
            var info = UIKit.Tile(top, "Agenda da semana", Theme.Purple, 1.4f);
            if (se.phase == "end")
            {
                HeadRow(info, "Férias");
                UIKit.Muted(info, "A agenda volta quando a próxima temporada começar.");
                return;
            }
            HeadRow(info, game.ActionsLeft > 0 ? $"Você tem {game.ActionsLeft} horário(s) livre(s) até o jogo" : "Agenda cheia nesta rodada",
                $"Rodada {se.week + 1}", Theme.Purple, Color.white);
            UIKit.Muted(info, "Fora de campo também se constrói uma carreira. Cada ação gasta um horário e mexe na energia, na moral, na fama, na relação com o técnico ou no seu bolso. Os horários voltam depois do jogo.");
            var cond = UIKit.Tile(top, "Como você está", Theme.Gold, 1f);
            Meters(cond, true);

            var defs = BusinessData.Actions;
            const int perRow = 5;
            for (int i = 0; i < defs.Length; i += perRow)
            {
                var row = UIKit.Cols(content);
                for (int k = i; k < i + perRow; k++)
                {
                    if (k >= defs.Length) { UIKit.LE(UIKit.Rect("Gap", row), flexW: 1, prefW: 0, minW: 0); continue; }
                    ActionCard(row, defs[k]);
                }
            }
        }

        void ActionCard(Transform row, BusinessData.ActionDef a)
        {
            bool done = game.S.season.doneActions.Contains(a.Id);
            bool can = game.CanDoAction(a.Id, out string why);
            var t = UIKit.Tile(row, null, done ? Theme.Turf : can ? Theme.Purple : Theme.Line, 1f, 22, 10);
            var head = UIKit.Row(t, 14);
            var icon = UIKit.Img(head, done ? Theme.Turf : Theme.Alpha(Theme.Purple, .85f), true, "Icon");
            UIKit.LE(icon, 64, 64, prefW: 64, minW: 64);
            var it = UIKit.Txt(icon.transform, done ? "OK" : a.Icon, 22, done ? Theme.TurfInk : Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.Stretch(it.rectTransform);
            var name = UIKit.Txt(head, a.Name, 26, Theme.Ink, FontStyle.Bold, TextAnchor.MiddleLeft);
            UIKit.LE(name, flexW: 1, minW: 0);
            var hint = UIKit.Muted(t, a.Hint, 22);
            UIKit.LE(hint, flexH: 1);
            string energy = a.Energy == 0 ? "Energia: sem custo" : $"Energia {Signed(a.Energy)}";
            long cost = game.ActionCost(a.Id);
            if (cost > 0) energy += $" · {Fmt.Money(cost)}";
            UIKit.Txt(t, energy, 22, a.Energy > 0 ? Theme.Turf : a.Energy < 0 ? Theme.Gold : Theme.Muted, FontStyle.Bold);
            string id = a.Id;
            Button b;
            if (done) b = UIKit.Btn(t, "Feito", Theme.Chip, Theme.Turf, null, 64, 24);
            else if (can) b = UIKit.Btn(t, "Fazer", Theme.Purple, Color.white, () => Commit(game.DoAction(id)), 64, 24);
            else b = UIKit.Btn(t, why ?? "Indisponível", Theme.Chip, Theme.Muted, null, 64, 22);
            b.interactable = !done && can;
        }

        // ---------- negócios (bolsa e empresas) ----------
        void BuildBusiness()
        {
            var p = game.S.player;
            var top = UIKit.Cols(content);

            var pat = UIKit.Tile(top, "Patrimônio", Theme.Gold, 1f);
            long total = p.money + game.PortfolioValue + game.VenturesValue;
            UIKit.Txt(pat, Fmt.Money(total), 56, Theme.Gold, FontStyle.Bold);
            UIKit.KV(pat, "Em conta", Fmt.Money(p.money));
            UIKit.KV(pat, "Ações (valor de mercado)", Fmt.Money(game.PortfolioValue));
            UIKit.KV(pat, "Empresas (valor estimado)", Fmt.Money(game.VenturesValue));

            var flow = UIKit.Tile(top, "Renda por rodada", Theme.Turf, 1f);
            UIKit.KV(flow, "Salário", Fmt.Money(game.S.contract.salary));
            UIKit.KV(flow, "Patrocínios", Fmt.Money(game.SponsorWeekly));
            UIKit.KV(flow, "Empresas (previsto)", Fmt.Money(game.VenturesWeekly));
            UIKit.KV(flow, "Empresas (última rodada)", Fmt.Money(game.S.businessIncome));

            long cost = game.PortfolioCost, value = game.PortfolioValue, res = value - cost;
            var port = UIKit.Tile(top, "Carteira", Theme.Cyan, 1f);
            UIKit.KV(port, "Investido", Fmt.Money(cost));
            UIKit.KV(port, "Vale hoje", Fmt.Money(value));
            var rr = UIKit.Row(port);
            UIKit.LE(UIKit.Txt(rr, "Resultado", 28, Theme.Muted), flexW: 1, minW: 0);
            UIKit.Txt(rr, (res >= 0 ? "+" : "") + Fmt.Money(res), 28, res >= 0 ? Theme.Turf : Theme.Red, FontStyle.Bold, TextAnchor.UpperRight);
            UIKit.Muted(port, "Preços mudam a cada rodada. As SAFs acompanham a campanha do clube na tabela.", 22);

            var row = UIKit.Cols(content);
            var mkt = UIKit.Tile(row, "Bolsa do futebol", Theme.Cyan, 1.45f, 24, 8);
            UIKit.Muted(mkt, $"Negociação em lotes de {BusinessData.LotSize} ações.", 22);
            foreach (var d in BusinessData.Stocks) StockLine(mkt, d);

            var ven = UIKit.Tile(row, "Empreendimentos", Theme.Gold, 1f, 24, 8);
            UIKit.Muted(ven, $"Negócios próprios rendem toda rodada. Amplie até o nível {BusinessData.MaxVentureLevel}.", 22);
            foreach (var d in BusinessData.Ventures) VentureLine(ven, d);
        }

        void StockLine(Transform parent, BusinessData.StockDef d)
        {
            var q = game.Quote(d.Id);
            if (q == null) return;
            var h = game.HoldingOf(d.Id);
            double ch = game.QuoteChange(d.Id);
            var line = SubCard(parent, 16, 6);
            var r = UIKit.Row(line, 14);

            var tick = UIKit.Img(r, Theme.Alpha(Theme.Cyan, .16f), true, "Ticker");
            UIKit.LE(tick, 56, 56, prefW: 120, minW: 120);
            var tt = UIKit.Txt(tick.transform, d.Id, 22, Theme.Cyan, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.Stretch(tt.rectTransform);

            var nameCol = UIKit.Column(r, 0);
            UIKit.LE(nameCol, flexW: 1, minW: 0, prefW: 0);
            UIKit.Txt(nameCol, d.Name, 25, Theme.Ink, FontStyle.Bold);
            UIKit.Muted(nameCol, h != null ? $"{d.Sector} · você tem {h.qty}" : d.Sector, 20);

            Sparkline(r, q.hist, ch >= 0 ? Theme.Turf : Theme.Red);

            var priceCol = UIKit.Column(r, 0, TextAnchor.UpperRight);
            UIKit.LE(priceCol, prefW: 140, minW: 140);
            UIKit.Txt(priceCol, Fmt.Cents(q.price), 25, Theme.Ink, FontStyle.Bold, TextAnchor.UpperRight);
            UIKit.Txt(priceCol, Fmt.Pct(ch), 20, ch >= 0 ? Theme.Turf : Theme.Red, FontStyle.Bold, TextAnchor.UpperRight);

            string id = d.Id;
            var buy = UIKit.Btn(r, "Comprar", Theme.Turf, Theme.TurfInk, () => Commit(game.BuyStock(id, 1)), 56, 22);
            UIKit.LE(buy, 56, 56, prefW: 150, minW: 130);
            buy.interactable = game.S.player.money >= game.LotPrice(id);
            var sell = UIKit.Btn(r, "Vender", Theme.Card, Theme.Ink, () => Commit(game.SellStock(id, 1)), 56, 22);
            UIKit.LE(sell, 56, 56, prefW: 130, minW: 110);
            sell.interactable = h != null && h.qty > 0;
        }

        /// <summary>Mini gráfico de barras com o histórico do preço.</summary>
        static void Sparkline(Transform parent, List<long> hist, Color color)
        {
            var box = UIKit.Rect("Spark", parent);
            UIKit.LE(box, 48, 48, prefW: 150, minW: 150);
            if (hist == null || hist.Count == 0) return;
            int n = Mathf.Min(16, hist.Count);
            var pts = hist.Skip(hist.Count - n).ToList();
            long min = pts.Min(), max = pts.Max();
            float span = Mathf.Max(1, max - min);
            for (int i = 0; i < n; i++)
            {
                var bar = UIKit.Img(box, i == n - 1 ? color : Theme.Alpha(color, .45f), false, "B");
                bar.raycastTarget = false;
                var rt = bar.rectTransform;
                float v = .15f + .85f * ((pts[i] - min) / span);
                rt.anchorMin = new Vector2(i / (float)n, 0);
                rt.anchorMax = new Vector2((i + .72f) / n, v);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }
        }

        void VentureLine(Transform parent, BusinessData.VentureDef d)
        {
            var v = game.VentureOf(d.Id);
            var line = SubCard(parent, 16, 6);
            var r = UIKit.Row(line, 14);
            var col = UIKit.Column(r, 0);
            UIKit.LE(col, flexW: 1, minW: 0, prefW: 0);
            UIKit.Txt(col, v == null ? d.Name : $"{d.Name} · nível {v.level}", 25, Theme.Ink, FontStyle.Bold);
            string sub = v != null
                ? $"{d.Cat} · rende {Fmt.Money(game.VentureWeekly(d.Id))} por rodada"
                : $"{d.Cat} · {d.Hint}";
            UIKit.Muted(col, sub, 20);

            string id = d.Id;
            bool can = game.CanBuyVenture(id, out string why);
            bool maxed = v != null && v.level >= BusinessData.MaxVentureLevel;
            string label = maxed ? "Nível máximo" : can ? (v == null ? "Abrir " : "Ampliar ") + Fmt.Money(game.VentureCost(id)) : why;
            var b = UIKit.Btn(r, label, can ? Theme.Gold : Theme.Card, can ? Theme.GoldInk : Theme.Muted,
                () => Commit(game.BuyVenture(id)), 56, 22);
            UIKit.LE(b, 56, 56, prefW: 260, minW: 220);
            b.interactable = can;
        }

        // ---------- jogador ----------
        void BuildPlayer()
        {
            var p = game.S.player; var c = game.MyClub; var st = game.S.season.stats;
            var cols = UIKit.Cols(content);

            // carta do jogador
            var cardCol = UIKit.Stack(cols, .8f);
            var outer = UIKit.Img(cardCol, Theme.Gold, true, "Carta");
            UIKit.V(outer.gameObject, 6, 0);
            var inner = UIKit.Img(outer.transform, Theme.CardHi, true, "Inner");
            UIKit.V(inner.gameObject, 22, 12);
            var band = UIKit.Img(inner.transform, Color.white, false, "Band");
            band.sprite = Procedural.StripesSprite(c.c1, c.c2);
            UIKit.LE(band, 190, 190);

            var ob = UIKit.Img(band.transform, Theme.Gold, true, "Ovr");
            var ort = ob.rectTransform;
            ort.anchorMin = ort.anchorMax = new Vector2(0, 1); ort.pivot = new Vector2(0, 1);
            ort.anchoredPosition = new Vector2(18, -18); ort.sizeDelta = new Vector2(150, 154);
            UIKit.V(ob.gameObject, 8, 0, TextAnchor.MiddleCenter);
            UIKit.Txt(ob.transform, game.Ovr.ToString(), 84, Theme.GoldInk, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.Txt(ob.transform, p.pos, 30, Theme.GoldInk, FontStyle.Bold, TextAnchor.MiddleCenter);

            var pill = UIKit.Img(band.transform, Theme.Alpha(Theme.Bar, .85f), true, "Club");
            var prt = pill.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(1, 1); prt.pivot = new Vector2(1, 1);
            prt.anchoredPosition = new Vector2(-18, -18);
            var ph = UIKit.H(pill.gameObject, 10);
            ph.padding = new RectOffset(14, 18, 8, 8);
            var csf = pill.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIKit.Crest(pill.transform, c, 26, 32);
            UIKit.Txt(pill.transform, c.name, 24, Theme.Ink, FontStyle.Bold);

            UIKit.Txt(inner.transform, p.name.ToUpperInvariant(), 48, Theme.Ink, FontStyle.Bold);
            UIKit.Muted(inner.transform, $"{p.age} anos · {GameData.Positions[p.pos].Name} · Brasil", 24);
            // selos: capitão e especialidades liberadas
            if (game.S.captain || game.S.traits.Count > 0)
            {
                var badges = UIKit.Grid(inner.transform, new Vector2(140, 44), 3, 8);
                if (game.S.captain) TraitBadge(badges.transform, "C", "Capitão", true);
                foreach (var id in game.S.traits) { var td = Game.Trait(id); if (td != null) TraitBadge(badges.transform, td.Icon, td.Name, false); }
            }
            var ag = UIKit.Cols(inner.transform, 14);
            var ca = UIKit.Stack(ag, 1, 8);
            var cb = UIKit.Stack(ag, 1, 8);
            for (int i = 0; i < GameData.Attrs.Length; i++)
            {
                var a = GameData.Attrs[i];
                var cell = UIKit.Img(i % 2 == 0 ? ca : cb, Theme.Chip, true, "Attr");
                var h = UIKit.H(cell.gameObject, 10);
                h.padding = new RectOffset(18, 18, 8, 8);
                var l = UIKit.Muted(cell.transform, GameData.Label(a), 22);
                UIKit.LE(l, flexW: 1, minW: 0);
                int val = p.Get(a);
                UIKit.Txt(cell.transform, val.ToString(), 34, val >= 75 ? Theme.Turf : val >= 60 ? Theme.Gold : Theme.Ink, FontStyle.Bold, TextAnchor.MiddleRight);
            }

            // condição e temporada
            var mid = UIKit.Stack(cols, 1f);
            var cond = UIKit.Tile(mid, "Condição", Theme.Gold);
            Meters(cond, false);
            UIKit.Muted(cond, $"Forma nas últimas partidas: {(p.form.Count > 0 ? Fmt.Rating(game.FormAvg) : "sem jogos ainda")}. Valor de mercado: {Fmt.Money(game.MarketValue())}.", 22);
            var sc = UIKit.Tile(mid, $"Temporada {game.S.season.year}", Theme.Turf);
            UIKit.KV(sc, "Jogos", st.apps.ToString());
            UIKit.KV(sc, "Gols", st.goals.ToString());
            UIKit.KV(sc, "Assistências", st.assists.ToString());
            UIKit.KV(sc, "Nota média", st.apps > 0 ? Fmt.Rating(game.AvgRating) : "-");
            UIKit.KV(sc, "Craque do jogo", st.motm.ToString());

            // especialidades: o que já tem e como liberar as outras
            var sp = UIKit.Tile(mid, "Especialidades", Theme.Purple, 1, 24, 6);
            UIKit.Muted(sp, "Liberadas por atributos e conquistas. Valem para sempre e ajudam no lance.", 20);
            foreach (var td in Game.Traits)
            {
                bool has = game.HasTrait(td.Id);
                var r = UIKit.Row(sp, 12);
                var ic = UIKit.Img(r, has ? Theme.Purple : Theme.Chip, true, "Icone");
                UIKit.LE(ic, 44, 44, prefW: 64, minW: 64);
                var it = UIKit.Txt(ic.transform, td.Icon, 20, has ? Color.white : Theme.Muted, FontStyle.Bold, TextAnchor.MiddleCenter);
                UIKit.Stretch(it.rectTransform);
                var c2 = UIKit.Column(r, 0);
                UIKit.LE(c2, flexW: 1, minW: 0);
                UIKit.Txt(c2, td.Name, 24, has ? Theme.Ink : Theme.Muted, FontStyle.Bold);
                UIKit.Txt(c2, has ? td.Effect : td.How, 20, has ? Theme.Turf : Theme.Muted);
            }

            // chuteira e histórico
            var right = UIKit.Stack(cols, 1.1f);
            var bc = UIKit.Tile(right, "Chuteira", Theme.Cyan);
            UIKit.Muted(bc, string.IsNullOrEmpty(p.boot.brand) ? "Sem patrocínio" : "Patrocínio " + p.boot.brand, 22);
            var boot = UIKit.Img(bc, Color.white, false, "Boot");
            boot.sprite = Procedural.BootSprite(p.boot);
            boot.preserveAspect = true;
            UIKit.LE(boot, 210, 210);
            if (!string.IsNullOrEmpty(p.boot.brand))
            {
                var bt = UIKit.Txt(boot.transform, p.boot.brand, 26, Theme.Hex(p.boot.c2), FontStyle.Bold, TextAnchor.MiddleCenter);
                UIKit.Style(bt, FontStyle.BoldAndItalic);
                bt.rectTransform.anchorMin = new Vector2(.36f, .38f);
                bt.rectTransform.anchorMax = new Vector2(.66f, .5f);
                bt.rectTransform.offsetMin = Vector2.zero; bt.rectTransform.offsetMax = Vector2.zero;
            }
            string[] pal = GameData.BootPalettes.TryGetValue(p.boot.brand ?? "", out var found) ? found : GameData.BootPalettes[""];
            Swatches(bc, "Cor principal", pal, p.boot.c1, hex => p.boot.c1 = hex);
            Swatches(bc, "Detalhes", pal, p.boot.c2, hex => p.boot.c2 = hex);
            Swatches(bc, "Solado", GameData.Soles, p.boot.sole, hex => p.boot.sole = hex);
            if (string.IsNullOrEmpty(p.boot.brand)) UIKit.Muted(bc, "Feche com uma marca de chuteira na aba Patrocínio para liberar mais cores.", 22);

            // comemoração de gol (a cena em terceira pessoa depois do gol)
            var cc = UIKit.Tile(right, "Comemoração", Theme.Gold);
            UIKit.Muted(cc, "O que você faz depois de marcar (a cena pode ser pulada).", 20);
            for (int r0 = 0; r0 < GameData.Celebrations.Length; r0 += 4)
            {
                var row = UIKit.Row(cc, 10);
                for (int i = r0; i < Mathf.Min(r0 + 4, GameData.Celebrations.Length); i++)
                {
                    string id = GameData.Celebrations[i][0];
                    bool sel = (p.celebration ?? "") == id;
                    var celebBtn = UIKit.Btn(row, GameData.Celebrations[i][1], sel ? Theme.Turf : Theme.Chip, sel ? Theme.TurfInk : Theme.Ink,
                        () => { p.celebration = id; Commit(); }, 64, 22);
                    UIKit.LE(celebBtn, flexW: 1, prefW: 0, minW: 0);
                }
            }

            var hc = UIKit.Tile(right, "Histórico", Theme.Purple, 1, 24, 4);
            if (game.S.career.Count == 0) UIKit.Muted(hc, "Sua primeira temporada ainda está em andamento.");
            else
            {
                HistoryLine(hc, "Ano", null, "Clube", "J", "G", "A", "Nota", true);
                foreach (var rec in game.S.career)
                    HistoryLine(hc, rec.year.ToString(), rec, rec.club, rec.apps.ToString(), rec.goals.ToString(), rec.assists.ToString(),
                        rec.apps > 0 ? Fmt.Rating(rec.avg) : "-", false);
            }
            if (game.S.titles.Count + game.S.awards.Count > 0)
            {
                UIKit.Space(hc, 12);
                UIKit.Label(hc, "Títulos e prêmios", Theme.Gold, 22);
                foreach (var t in game.S.titles.Concat(game.S.awards)) UIKit.Body(hc, "• " + t);
            }
        }

        void TraitBadge(Transform parent, string icon, string name, bool gold)
        {
            var b = UIKit.Img(parent, gold ? Theme.Gold : Theme.Alpha(Theme.Purple, .9f), true, "Selo");
            var h = UIKit.H(b.gameObject, 6, TextAnchor.MiddleCenter);
            h.padding = new RectOffset(8, 8, 4, 4);
            var t = UIKit.Txt(b.transform, gold ? "CAPITÃO" : name.ToUpperInvariant(), 18, gold ? Theme.GoldInk : Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = 18;
        }

        void HistoryLine(Transform parent, string year, CareerRecord rec, string club, string j, string g, string a, string nota, bool head)
        {
            var row = UIKit.Row(parent, 10);
            row.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(0, 0, 4, 4);
            Color col = head ? Theme.Muted : Theme.Ink;
            int size = head ? 20 : 24;
            UIKit.LE(UIKit.Txt(row, year, size, col), prefW: 70);
            if (rec != null) UIKit.Crest(row, new Club { name = rec.club, c1 = rec.c1, c2 = rec.c2 }, 24, 30);
            else UIKit.LE(UIKit.Rect("Gap", row), prefW: 24);
            UIKit.LE(UIKit.Txt(row, club, size, col), flexW: 1, minW: 0);
            foreach (var v in new[] { j, g, a }) UIKit.LE(UIKit.Txt(row, v, size, col, FontStyle.Normal, TextAnchor.UpperRight), prefW: 44);
            UIKit.LE(UIKit.Txt(row, nota, size, col, FontStyle.Bold, TextAnchor.UpperRight), prefW: 64);
        }

        void Swatches(Transform parent, string label, string[] colors, string current, Action<string> set)
        {
            UIKit.Label(parent, label, Theme.Muted, 20);
            var row = UIKit.Row(parent, 16);
            foreach (var hex in colors)
            {
                string h = hex;
                var im = UIKit.Img(row, Theme.Hex(hex), false, "Swatch");
                im.sprite = UIKit.Circle;
                UIKit.LE(im, 56, 56, prefW: 56, minW: 56);
                var b = im.gameObject.AddComponent<Button>();
                b.targetGraphic = im;
                b.onClick.AddListener(() => { set(h); Commit(); });
                bool sel = string.Equals(hex, current, StringComparison.OrdinalIgnoreCase);
                var o = im.gameObject.AddComponent<Outline>();
                o.effectColor = sel ? Theme.Turf : Theme.Line;
                o.effectDistance = sel ? new Vector2(5, -5) : new Vector2(2, -2);
            }
        }

        // ---------- contrato ----------
        void BuildContract()
        {
            var k = game.S.contract; var c = game.MyClub; var se = game.S.season;
            var cols = UIKit.Cols(content);

            var cur = UIKit.Tile(cols, "Contrato atual", Theme.Turf, .85f);
            var hr = UIKit.Row(cur, 14);
            UIKit.Crest(hr, c, 50, 60);
            UIKit.LE(UIKit.Txt(hr, c.name, 34, Theme.Ink, FontStyle.Bold, TextAnchor.MiddleLeft), flexW: 1, minW: 0);
            UIKit.KV(cur, "Salário", Fmt.Money(k.salary) + " por semana");
            UIKit.KV(cur, "Bônus por gol", Fmt.Money(k.bonus));
            UIKit.KV(cur, "Multa rescisória", Fmt.Money(k.clause));
            UIKit.KV(cur, "Duração", k.years > 0 ? $"mais {k.years} temporada(s)" : "terminou");
            UIKit.KV(cur, "Valor de mercado", Fmt.Money(game.MarketValue()));
            if (game.OnLoan) UIKit.KV(cur, "Emprestado pelo", game.LoanParent?.name ?? "-");
            if (se.transferRequest) UIKit.Tag(UIKit.Row(cur), "Na lista de transferências", Theme.Red, Color.white);
            if (game.CanRequestTransfer)
                UIKit.Ghost(cur, "Pedir para ser negociado", () => ShowModal("Pedir para ser negociado?",
                    $"A diretoria vai ouvir propostas na janela do meio da temporada (depois da rodada {game.WindowRound}) e no fim do ano. O técnico, o elenco e a torcida não vão gostar.",
                    ("Pedir para sair", (Action)(() => { CloseModal(); Commit(game.RequestTransfer()); }), true), ("Cancelar", (Action)CloseModal, false)));

            if (se.phase == "end")
            {
                var oc = UIKit.Tile(cols, "Propostas", Theme.Gold, 1.6f);
                if (game.S.offers.Count == 0) UIKit.Muted(oc, "Nenhuma proposta no momento.");
                var offers = game.S.offers.ToList();
                for (int i = 0; i < offers.Count; i += 2)
                {
                    var pair = UIKit.Cols(oc, 16);
                    for (int j = i; j < i + 2; j++)
                    {
                        if (j >= offers.Count) { UIKit.LE(UIKit.Rect("Gap", pair), flexW: 1, prefW: 0, minW: 0); continue; }
                        OfferCard(pair, offers[j], false);
                    }
                }
                UIKit.Muted(oc, "Pedir mais pode melhorar salário e luvas, mas o clube pode desistir.", 22);
            }
            else if (game.WindowOpen)
            {
                var wc = UIKit.Tile(cols, "Janela de transferências", Theme.Gold, 1.6f);
                UIKit.Muted(wc, $"A janela fecha depois da rodada {game.WindowRound + 2}. Trocar de clube agora: você segue o campeonato no clube novo (outra liga: tabela nova, e a copa fica para o ano que vem).", 22);
                var offers = game.S.windowOffers.ToList();
                for (int i = 0; i < offers.Count; i += 2)
                {
                    var pair = UIKit.Cols(wc, 16);
                    for (int j = i; j < i + 2; j++)
                    {
                        if (j >= offers.Count) { UIKit.LE(UIKit.Rect("Gap", pair), flexW: 1, prefW: 0, minW: 0); continue; }
                        OfferCard(pair, offers[j], true);
                    }
                }
            }
            else if (!se.negotiated)
            {
                if (negSal < k.salary) negSal = k.salary;
                long max = Math.Max(k.salary * 3, k.salary + 1000);
                long step = Math.Max(100, Game.R100(k.salary * .05));
                var nc = UIKit.Tile(cols, "Negociar com a diretoria", Theme.Gold, 1.6f);
                UIKit.Muted(nc, $"Peça aumento ou mais tempo de contrato. Você tem {se.attempts} tentativa(s) nesta temporada. Pedidos exagerados irritam o clube.");
                UIKit.Label(nc, "Salário pedido por semana", Theme.Muted, 22);
                var sr = UIKit.Row(nc, 16);
                var minus = UIKit.Ghost(sr, "-", () => { negSal = Math.Max(k.salary, negSal - step); Render(); });
                UIKit.LE(minus, prefW: 140);
                var val = UIKit.Txt(sr, Fmt.Money(negSal), 46, Theme.Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
                UIKit.LE(val, flexW: 1, minW: 0);
                var plus = UIKit.Ghost(sr, "+", () => { negSal = Math.Min(max, negSal + step); Render(); });
                UIKit.LE(plus, prefW: 140);
                UIKit.Label(nc, "Duração", Theme.Muted, 22);
                var yr = UIKit.Row(nc, 12);
                for (int y = 1; y <= 5; y++)
                {
                    int yy = y;
                    UIKit.LE(Chip(yr, y == 1 ? "1 ano" : $"{y} anos", negYears == y, () => { negYears = yy; Render(); }, 64), flexW: 1, prefW: 0);
                }
                if (!string.IsNullOrEmpty(negMsg)) UIKit.Txt(nc, negMsg, 28, Theme.Ink, FontStyle.Bold);
                var br = UIKit.Row(nc, 16);
                if (game.Counter > 0)
                    UIKit.LE(UIKit.GoldBtn(br, "Aceitar contraproposta", () => { negSal = game.Counter; negMsg = game.Negotiate(game.Counter, negYears); Commit(); }), flexW: 1, prefW: 0);
                UIKit.LE(UIKit.Primary(br, "Enviar proposta", () => { negMsg = game.Negotiate(negSal, negYears); Commit(); }), flexW: 1, prefW: 0);
            }
            else
            {
                var ic = UIKit.Tile(cols, "Negociação", Theme.Gold, 1.6f);
                UIKit.Body(ic, (string.IsNullOrEmpty(negMsg) ? "" : negMsg + " ") +
                    "Novas conversas com a diretoria só na próxima temporada. No fim da temporada chegam propostas de outros clubes.");
            }
        }

        void OfferCard(Transform parent, ContractOffer o, bool window)
        {
            var club = game.ClubById(o.club);
            var sc = SubCard(parent);
            UIKit.LE(sc, flexW: 1, prefW: 0, minW: 0);
            var r = UIKit.Row(sc, 12);
            UIKit.Crest(r, club, 40, 48);
            var name = UIKit.Txt(r, club.name, 30, Theme.Ink, FontStyle.Bold, TextAnchor.MiddleLeft);
            UIKit.LE(name, flexW: 1, minW: 0);
            UIKit.Tag(r, o.loan ? "Empréstimo" : o.clauseMet ? "Multa paga" : o.renewal ? "Renovação" : Game.League(club.league).Name, o.clauseMet ? Theme.Gold : Theme.Card, o.clauseMet ? Theme.GoldInk : Theme.Ink);
            if (o.loan)
            {
                UIKit.KV(sc, "Duração", "até o fim da temporada");
                UIKit.KV(sc, "Salário", "o mesmo, pago por eles");
            }
            else
            {
                UIKit.KV(sc, "Salário", Fmt.Money(o.salary) + " por semana");
                UIKit.KV(sc, "Duração", $"{o.years} temporada(s)");
                UIKit.KV(sc, "Luvas", Fmt.Money(o.signing));
                UIKit.KV(sc, "Bônus por gol", Fmt.Money(o.bonus));
            }
            UIKit.KV(sc, "Força do elenco", club.str.ToString());
            var br = UIKit.Row(sc, 12);
            string id = o.id;
            UIKit.LE(UIKit.Primary(br, o.loan ? "Aceitar" : "Assinar", () => Commit(window ? game.AcceptWindowOffer(id) : game.AcceptOffer(id), true)), flexW: 1, prefW: 0);
            if (!o.loan)
            {
                var hg = UIKit.Btn(br, o.haggled ? "Já negociado" : "Pedir mais", Theme.Card, Theme.Ink, () => Commit(window ? game.HaggleWindow(id) : game.Haggle(id)));
                UIKit.LE(hg, flexW: 1, prefW: 0);
                hg.interactable = !o.haggled;
            }
            if (window) UIKit.LE(UIKit.Btn(br, "Recusar", Theme.Card, Theme.Muted, () => { game.DeclineWindowOffer(id); Commit(); }), flexW: 1, prefW: 0);
        }

        // ---------- patrocínio ----------
        void BuildSponsors()
        {
            var cols = UIKit.Cols(content);
            var a = UIKit.Tile(cols, "Patrocínios ativos", Theme.Turf);
            if (game.S.activeSponsors.Count == 0) UIKit.Muted(a, "Nenhum patrocinador ainda. Aceite uma proposta ao lado.");
            foreach (var d in game.S.activeSponsors)
            {
                var sc = SubCard(a);
                HeadRow(sc, d.brand, GameData.Sponsor(d.cat).Name, Theme.Card, Theme.Ink);
                UIKit.Body(sc, $"{Fmt.Money(d.perSeason)} por temporada, mais {d.seasonsLeft} temporada(s). Bônus de {Fmt.Money(d.bonus)} se bater a meta.");
                UIKit.Muted(sc, game.GoalText(d), 22);
                UIKit.Bar(sc, game.GoalProgress01(d), Theme.Turf);
                UIKit.Muted(sc, game.GoalProgressText(d), 22);
            }

            var o = UIKit.Tile(cols, "Propostas", Theme.Gold);
            if (game.S.sponsorOffers.Count == 0)
                UIKit.Muted(o, "Sem propostas agora. Novas marcas aparecem no início da temporada e na rodada 10. Quanto maior sua fama, maiores os valores.");
            foreach (var d in game.S.sponsorOffers.ToList())
            {
                bool busy = game.S.activeSponsors.Exists(x => x.cat == d.cat);
                var sc = SubCard(o);
                HeadRow(sc, d.brand, GameData.Sponsor(d.cat).Name, Theme.Card, Theme.Ink);
                UIKit.Body(sc, $"{Fmt.Money(d.perSeason)} por temporada durante {d.seasons} temporada(s). Bônus de {Fmt.Money(d.bonus)} se bater a meta.");
                UIKit.Muted(sc, game.GoalText(d), 22);
                if (d.cat == "chuteira") UIKit.Muted(sc, $"Libera as cores da {d.brand} na sua chuteira.", 22);
                var br = UIKit.Row(sc, 12);
                string id = d.id;
                var acc = UIKit.Primary(br, busy ? "Categoria ocupada" : "Aceitar", () => Commit(game.AcceptSponsor(id)));
                UIKit.LE(acc, flexW: 1, prefW: 0);
                acc.interactable = !busy;
                UIKit.LE(UIKit.Btn(br, "Recusar", Theme.Card, Theme.Ink, () => { game.DeclineSponsor(id); Commit(); }), flexW: 1, prefW: 0);
            }
        }

        // ---------- vida ----------
        void BuildLife()
        {
            var p = game.S.player;
            var cols = UIKit.Cols(content);

            var left = UIKit.Stack(cols, .9f);
            var m = UIKit.Tile(left, "Dinheiro em conta", Theme.Gold);
            UIKit.Txt(m, Fmt.Money(p.money), 64, p.money < 0 ? Theme.Red : Theme.Gold, FontStyle.Bold);
            // extrato da última rodada: o que entrou e o que saiu
            var w = game.S.lastWeek;
            UIKit.Label(m, "Extrato da última rodada", Theme.Muted, 20);
            UIKit.KV(m, "Salário", "+" + Fmt.Money(w.salary));
            if (w.bonus > 0) UIKit.KV(m, "Bônus por gols", "+" + Fmt.Money(w.bonus));
            if (w.sponsors > 0) UIKit.KV(m, "Patrocínios", "+" + Fmt.Money(w.sponsors));
            if (w.business != 0) UIKit.KV(m, "Negócios", (w.business > 0 ? "+" : "") + Fmt.Money(w.business));
            UIKit.KV(m, $"Imposto de renda ({w.taxRate * 100:0.#}%)", "-" + Fmt.Money(w.tax));
            UIKit.KV(m, $"Empresário ({Game.AgentFee * 100:0}%)", "-" + Fmt.Money(w.agent));
            UIKit.KV(m, "Custo de vida", "-" + Fmt.Money(w.living));
            if (w.upkeep > 0) UIKit.KV(m, "Manutenção dos bens", "-" + Fmt.Money(w.upkeep));
            if (w.partner > 0) UIKit.KV(m, "Vida a dois", "-" + Fmt.Money(w.partner));
            UIKit.KV(m, "Saldo da rodada", (w.Net >= 0 ? "+" : "") + Fmt.Money(w.Net));
            UIKit.KV(m, "Saldo na temporada", (game.S.seasonNet >= 0 ? "+" : "") + Fmt.Money(game.S.seasonNet));
            UIKit.Ghost(m, "Investir em negócios", () => { tab = "business"; Render(true); });

            // vida pessoal: relacionamento e rival
            var lv = UIKit.Tile(left, "Vida pessoal", Theme.Red);
            var love = game.S.love;
            HeadRow(lv, game.LoveLine(), love.stage == 0 ? "SOLTEIRO" : LifeData.StageName(love.stage).ToUpperInvariant(),
                love.stage == 0 ? Theme.Chip : Theme.Red, love.stage == 0 ? Theme.Ink : Color.white);
            if (love.stage > 0)
            {
                UIKit.KV(lv, "Clima entre vocês", love.affection >= 70 ? "Apaixonados" : love.affection >= 45 ? "Tranquilo" : love.affection >= 25 ? "Esfriando" : "Em crise");
                UIKit.Muted(lv, "Sem atenção a relação esfria. Jantares e escolhas nos eventos aquecem.", 20);
            }
            else UIKit.Muted(lv, "Saia para conhecer gente na Agenda ou responda as mensagens que chegarem.", 20);
            var rv = game.S.rival;
            if (!string.IsNullOrEmpty(rv.name) && game.RivalClub != null)
            {
                UIKit.Space(lv, 8);
                UIKit.Label(lv, "Rival", Theme.Gold, 22);
                UIKit.KV(lv, rv.name, game.RivalClub.name);
                UIKit.KV(lv, "Gols na temporada", $"você {game.S.season.stats.goals} x {rv.goals} ele");
                if (rv.duels > 0) UIKit.KV(lv, "Duelos diretos", $"{rv.won} vitória(s) em {rv.duels}");
                UIKit.KV(lv, "Clima", rv.heat >= 60 ? "Guerra declarada" : rv.heat >= 30 ? "Provocações" : "Respeito");
            }

            var n = UIKit.Tile(left, "Notícias", Theme.Cyan);
            if (game.S.news.Count == 0) UIKit.Muted(n, "Nada por aqui ainda.");
            foreach (var item in game.S.news.Take(12))
            {
                var col = UIKit.Column(n, 2);
                UIKit.Label(col, item.when, Theme.Muted, 20);
                UIKit.Txt(col, item.text, 24, Theme.Ink);
            }

            var right = UIKit.Stack(cols, 1.2f);
            var l = UIKit.Tile(right, "Estilo de vida", Theme.Purple);
            UIKit.Muted(l, "Garagem, casa e acessórios. Cada compra aumenta sua moral e sua fama, mas tem manutenção toda rodada (seguro, IPVA, condomínio).");
            // vitrine em cards de 3 colunas, com ilustração
            RectTransform line = null;
            int count = 0;
            foreach (var it in GameData.Items)
            {
                if (count++ % 3 == 0) line = UIKit.Cols(l, 16);
                bool own = game.S.owned.Contains(it.Id);
                var card = SubCard(line, 0, 0);
                UIKit.LE(card, flexW: 1, prefW: 0, minW: 0);
                // foto real se existir em Resources/Vida; senão, faixa com a categoria
                var photo = Procedural.LifestyleSprite(it.Id);
                var pic = UIKit.Img(card, photo != null ? Color.white : LifeColor(it.Cat), photo == null, "Foto");
                if (photo != null) { pic.sprite = photo; pic.preserveAspect = true; }
                else
                {
                    var cat = UIKit.Txt(pic.transform, it.Cat.ToUpperInvariant(), 44, Theme.Alpha(Color.white, .9f), FontStyle.BoldAndItalic, TextAnchor.MiddleCenter);
                    UIKit.Stretch(cat.rectTransform);
                }
                UIKit.LE(pic, photo != null ? 170 : 92, photo != null ? 170 : 92);
                if (own)
                {
                    var tag = UIKit.Tag(pic.transform, "Seu", Theme.Turf, Theme.TurfInk);
                    tag.anchorMin = tag.anchorMax = tag.pivot = new Vector2(0, 1);
                    tag.anchoredPosition = new Vector2(12, -12);
                    tag.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    tag.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
                var info = UIKit.Column(card, 4);
                info.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(16, 16, 10, 16);
                var name = UIKit.Txt(info, it.Name, 26, Theme.Ink, FontStyle.Bold);
                name.resizeTextForBestFit = true; name.resizeTextMinSize = 18; name.resizeTextMaxSize = 26;
                UIKit.LE(name, 32, 32);
                UIKit.Txt(info, Fmt.Money(it.Price), 26, Theme.Gold, FontStyle.Bold);
                UIKit.Muted(info, $"{it.Cat} · fama +{it.Fame} · moral +{it.Moral}", 20);
                UIKit.Muted(info, $"Manutenção {Fmt.Money(Game.ItemUpkeep(it))} por rodada", 20);
                string id = it.Id;
                // o que é seu pode ser vendido (70% do preço) para cortar a manutenção
                var b = own ? UIKit.Btn(info, $"Vender por {Fmt.Money(Game.R1000(it.Price * .7))}", Theme.Card, Theme.Gold, () => Commit(game.SellItem(id)), 56, 22)
                    : UIKit.Primary(info, "Comprar", () => Commit(game.Buy(id)));
                UIKit.LE(b, 56, 56);
                b.interactable = own || p.money >= it.Price;
            }
            // completa a última linha para os cards não esticarem
            for (; count % 3 != 0; count++) UIKit.LE(UIKit.Rect("Vazio", line), flexW: 1, prefW: 0, minW: 0);

            UIKit.Ghost(right, "Ajustes e recomeçar carreira", () => { tab = "settings"; Render(true); });
        }

        static Color LifeColor(string cat) => cat == "Carro" ? Color.Lerp(Theme.Red, Theme.Card, .35f)
            : cat == "Casa" ? Color.Lerp(Theme.Cyan, Theme.Card, .45f) : Color.Lerp(Theme.Gold, Theme.Card, .4f);

        /// <summary>Campanha da Seleção na Copa do Mundo, com a próxima partida para jogar.</summary>
        void WorldCupTile(Transform cols)
        {
            var wc = game.S.wc;
            var t = UIKit.Tile(cols, $"Copa do Mundo {wc.year}", Theme.Gold);
            HeadRow(t, "Seleção Brasileira", wc.champion ? "CAMPEÃ" : wc.eliminated ? "ELIMINADA" : "EM DISPUTA",
                wc.champion ? Theme.Gold : wc.eliminated ? Theme.Red : Theme.Turf, wc.champion ? Theme.GoldInk : Theme.TurfInk);
            foreach (var w in wc.matches)
            {
                string res = w.played ? $"{w.gf} x {w.ga}" + (w.pens ? (w.won ? " (pên.)" : " (pên.)") : "") : "a jogar";
                string me = w.played && w.myGoals > 0 ? $" · {w.myGoals} gol(s) seu(s)" : "";
                UIKit.KV(t, $"{Game.WcStages[w.stage]} · {w.opp}", res + me);
            }
            if (wc.matches.Count > 0 && wc.matches[0].stage == 0 && game.S.wc.phase != "done")
                UIKit.Muted(t, $"Pontos no grupo: {wc.groupPts}", 22);
            var next = game.WcNext;
            if (next != null)
            {
                UIKit.Primary(t, $"Jogar: Brasil x {next.opp}", StartWorldCupMatch);
                UIKit.Ghost(t, "Simular o resto da Copa", () => { game.SimulateRestOfWorldCup(); Commit(game.S.wc.champion ? "O Brasil é campeão do mundo!" : "Copa simulada.", true); });
            }
        }

        void ResetCareer()
        {
            CloseModal();
            SaveSystem.Delete();
            game = null;
            Array.Clear(alloc, 0, alloc.Length);
            newName = ""; newPos = "ATA"; negSal = -1; negMsg = ""; tab = "home";
            Render(true);
        }

        // ---------- aposentadoria ----------
        void BuildRetired()
        {
            var s = game.S;
            var cols = UIKit.Cols(content);
            var left = UIKit.Stack(cols, .8f, 10);
            left.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            Shirt(left, s.player.name, 400);
            UIKit.Txt(left, "FIM DE CARREIRA", 56, Theme.Ink, FontStyle.Bold, TextAnchor.UpperCenter);

            var a = UIKit.Tile(cols, s.player.name, Theme.Turf);
            UIKit.KV(a, "Temporadas", s.career.Count.ToString());
            UIKit.KV(a, "Jogos", s.career.Sum(r => r.apps).ToString());
            UIKit.KV(a, "Gols", s.career.Sum(r => r.goals).ToString());
            UIKit.KV(a, "Assistências", s.career.Sum(r => r.assists).ToString());
            UIKit.KV(a, "Dinheiro em conta", Fmt.Money(s.player.money));
            UIKit.KV(a, "Ações e empresas", Fmt.Money(game.PortfolioValue + game.VenturesValue));

            var right = UIKit.Stack(cols, 1f);
            var b = UIKit.Tile(right, "Títulos e prêmios", Theme.Gold);
            if (s.titles.Count + s.awards.Count == 0) UIKit.Muted(b, "Nenhum título desta vez. A próxima carreira pode ser diferente.");
            foreach (var t in s.titles.Concat(s.awards)) UIKit.Body(b, "• " + t);
            UIKit.GoldBtn(right, "Começar nova carreira", ResetCareer);
        }
    }
}
