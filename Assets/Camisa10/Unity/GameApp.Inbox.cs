using System.Linq;
using Camisa10.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Camisa10.UI
{
    // Caixa de mensagens (o canal da história da carreira) e o bloco de mensagens da Central.
    public partial class GameApp
    {
        string inboxSel; // mensagem aberta na aba Mensagens

        static Color RoleColor(string role)
        {
            switch (role)
            {
                case "tecnico": case "elenco": return Theme.Turf;
                case "empresario": case "selecao": return Theme.Gold;
                case "diretoria": case "imprensa": return Theme.Cyan;
                case "patrocinador": return Theme.Purple;
                default: return Theme.Red; // namorada, torcida, médico, rival
            }
        }

        /// <summary>Avatar de quem escreve: escudo do clube quando houver, senão as iniciais na cor do papel.</summary>
        void Avatar(Transform parent, InboxMsg m, float size)
        {
            var club = string.IsNullOrEmpty(m.club) ? null : game.ClubById(m.club);
            if (m.role == "selecao") club = new Club { name = "Seleção", c1 = "#FFDF00", c2 = "#009C3B" };
            if (club != null)
            {
                var holder = UIKit.Img(parent, Theme.Alpha(RoleColor(m.role), .16f), true, "Avatar");
                UIKit.LE(holder, size, size, prefW: size, minW: size);
                var cr = UIKit.Crest(holder.transform, club, size * .62f, size * .74f);
                cr.GetComponent<LayoutElement>().ignoreLayout = true;
                var rt = cr.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
                rt.sizeDelta = new Vector2(size * .62f, size * .74f); rt.anchoredPosition = Vector2.zero;
                return;
            }
            var im = UIKit.Img(parent, RoleColor(m.role), true, "Avatar");
            UIKit.LE(im, size, size, prefW: size, minW: size);
            var t = UIKit.Txt(im.transform, Initials(m), Mathf.RoundToInt(size * .36f), m.role == "empresario" || m.role == "selecao" ? Theme.GoldInk : m.role == "tecnico" || m.role == "elenco" ? Theme.TurfInk : Color.white,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.Stretch(t.rectTransform);
        }

        /// <summary>Texto de uma linha só: o que não couber é cortado (lista compacta).</summary>
        static void OneLine(Text t, float h)
        {
            t.verticalOverflow = VerticalWrapMode.Truncate;
            UIKit.LE(t, h, h, minW: 0);
        }

        static string Initials(InboxMsg m)
        {
            string name = (m.from ?? "?").Split('·')[0].Trim();
            var parts = name.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            string s = parts[0].Substring(0, 1);
            if (parts.Length > 1 && char.IsLetter(parts[parts.Length - 1][0])) s += parts[parts.Length - 1].Substring(0, 1);
            return s.ToUpperInvariant();
        }

        /// <summary>Escolhe a mensagem aberta (a mais nova não lida) e marca como lida antes de desenhar a barra de abas.</summary>
        void PrepareInbox()
        {
            var box = game.S.inbox;
            var sel = box.Find(x => x.id == inboxSel);
            if (sel == null) sel = box.FirstOrDefault(x => !x.read) ?? box.FirstOrDefault();
            inboxSel = sel?.id;
            if (sel != null && !sel.read) { sel.read = true; Save(); }
        }

        void BuildInbox()
        {
            var box = game.S.inbox;
            var cols = UIKit.Cols(content);
            var list = UIKit.Tile(cols, "Caixa de entrada", Theme.Cyan, .95f, 22, 8);
            int unread = game.UnreadCount, pending = game.PendingReplies;
            HeadRow(list, unread > 0 ? $"{unread} não lida(s)" : "Tudo em dia", pending > 0 ? $"{pending} para responder" : null, Theme.Gold, Theme.GoldInk);
            if (box.Count == 0) UIKit.Muted(list, "Nenhuma mensagem ainda. Técnico, empresário, diretoria e patrocinadores falam com você por aqui.");
            foreach (var m in box.Take(30)) MailRow(list, m, m.id == inboxSel);
            if (unread > 0) UIKit.Ghost(list, "Marcar tudo como lido", () => { game.MarkAllRead(); Commit(); });

            var sel = box.Find(x => x.id == inboxSel);
            var view = UIKit.Tile(cols, sel == null ? "Mensagem" : InboxData.RoleName(sel.role), sel == null ? Theme.Line : RoleColor(sel.role), 1.35f, 34, 18);
            if (sel == null) { UIKit.Muted(view, "Selecione uma mensagem."); return; }
            var head = UIKit.Row(view, 20);
            Avatar(head, sel, 96);
            var hc = UIKit.Column(head, 2);
            UIKit.LE(hc, flexW: 1, minW: 0);
            UIKit.Txt(hc, sel.from, 32, Theme.Ink, FontStyle.Bold);
            UIKit.Muted(hc, $"Temporada {sel.year} · rodada {sel.week}", 22);
            UIKit.Txt(view, sel.subject, 44, Theme.Ink, FontStyle.Bold);
            UIKit.Txt(view, sel.body, 30, Theme.Ink);
            if (sel.NeedsReply)
            {
                UIKit.Space(view, 6);
                UIKit.Label(view, "Sua resposta", Theme.Gold, 22);
                for (int i = 0; i < sel.options.Count; i++)
                {
                    int idx = i; string id = sel.id;
                    var b = i == 0 ? UIKit.Primary(view, sel.options[i], () => Commit(game.Reply(id, idx))) : UIKit.Ghost(view, sel.options[i], () => Commit(game.Reply(id, idx)));
                    UIKit.LE(b, 80, 80);
                }
            }
            else if (!string.IsNullOrEmpty(sel.answer))
            {
                var sc = SubCard(view, 24, 6);
                UIKit.Label(sc, "Respondida", Theme.Turf, 20);
                UIKit.Txt(sc, sel.answer, 26, Theme.Ink);
            }
        }

        void MailRow(Transform parent, InboxMsg m, bool selected)
        {
            var im = UIKit.Img(parent, selected ? Theme.Alpha(Theme.Turf, .16f) : Theme.Alpha(Theme.Chip, m.read ? .45f : .9f), true, "Mail");
            var b = im.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            string id = m.id;
            b.onClick.AddListener(() => { inboxSel = id; game.MarkRead(id); Save(); Render(); });
            var h = UIKit.H(im.gameObject, 14);
            h.padding = new RectOffset(14, 16, 10, 10);
            Avatar(im.transform, m, 60);
            var col = UIKit.Column(im.transform, 0);
            UIKit.LE(col, flexW: 1, minW: 0, prefW: 0);
            OneLine(UIKit.Txt(col, m.from, 23, m.read ? Theme.Muted : Theme.Ink, FontStyle.Bold), 28);
            OneLine(UIKit.Txt(col, m.subject, 22, m.read ? Theme.Muted : Theme.Ink), 28);
            if (m.NeedsReply) UIKit.Tag(im.transform, "Responder", Theme.Gold, Theme.GoldInk);
            else if (!m.read)
            {
                var dot = UIKit.Img(im.transform, Theme.Turf, false, "Dot");
                dot.sprite = UIKit.Circle;
                UIKit.LE(dot, 18, 18, prefW: 18, minW: 18);
            }
        }

        /// <summary>Bloco "Mensagens" da Central: as últimas que chegaram e um atalho para a caixa.</summary>
        void HomeInbox(Transform parent)
        {
            int unread = game.UnreadCount, pending = game.PendingReplies;
            var t = UIKit.Tile(parent, "Mensagens", Theme.Cyan, 1, 24, 8);
            HeadRow(t, unread > 0 ? $"{unread} nova(s)" : "Nada novo", pending > 0 ? $"{pending} para responder" : null, Theme.Gold, Theme.GoldInk);
            if (game.S.inbox.Count == 0) UIKit.Muted(t, "Nenhuma mensagem ainda.", 22);
            foreach (var m in game.S.inbox.Take(3))
            {
                var r = UIKit.Row(t, 12);
                Avatar(r, m, 46);
                var col = UIKit.Column(r, 0);
                UIKit.LE(col, flexW: 1, minW: 0, prefW: 0);
                OneLine(UIKit.Txt(col, m.from, 21, m.read ? Theme.Muted : Theme.Ink, FontStyle.Bold), 26);
                OneLine(UIKit.Txt(col, m.subject, 20, m.read ? Theme.Muted : Theme.Ink), 25);
                if (m.NeedsReply) UIKit.Tag(r, "!", Theme.Gold, Theme.GoldInk);
            }
            UIKit.Ghost(t, "Abrir mensagens", () => { tab = "inbox"; inboxSel = null; Render(true); });
        }
    }
}
