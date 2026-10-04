using System;
using Camisa10.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Camisa10.UI
{
    /// <summary>
    /// Fábrica de componentes uGUI. Toda a interface é criada por código,
    /// então o projeto não depende de cenas ou prefabs montados à mão.
    /// Unidades em pixels de referência (tela deitada de 1920x1080).
    /// </summary>
    public static class UIKit
    {
        static Font font, bold, boldItalic;

        // Fontes Barlow (licença OFL, em Resources/Fonts): texto corrido em Barlow, títulos e números em Barlow Condensed,
        // no estilo das telas de jogos de futebol. Sem os arquivos, cai na fonte padrão da Unity.
        static Font Load(string name) => Resources.Load<Font>("Fonts/" + name) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font Font => font != null ? font : (font = Load("Barlow-Medium"));
        public static Font Bold => bold != null ? bold : (bold = Load("BarlowCondensed-Bold"));
        public static Font BoldItalic => boldItalic != null ? boldItalic : (boldItalic = Load("BarlowCondensed-BoldItalic"));

        /// <summary>Aplica a fonte certa para o estilo (negrito usa o arquivo condensado em vez de negrito sintético).</summary>
        public static void Style(Text t, FontStyle style)
        {
            switch (style)
            {
                case FontStyle.Bold: t.font = Bold; t.fontStyle = Bold.name.Contains("Barlow") ? FontStyle.Normal : FontStyle.Bold; break;
                case FontStyle.BoldAndItalic: t.font = BoldItalic; t.fontStyle = BoldItalic.name.Contains("Barlow") ? FontStyle.Normal : FontStyle.BoldAndItalic; break;
                default: t.font = Font; t.fontStyle = style; break;
            }
        }

        /// <summary>Sombra suave embaixo de painéis e botões, para dar profundidade.</summary>
        public static void Depth(Graphic g, float dist = 6, float alpha = .45f)
        {
            var sh = g.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, alpha);
            sh.effectDistance = new Vector2(0, -dist);
        }

        static Sprite round, circle;
        public static Sprite Round => round != null ? round : (round = Procedural.PanelSprite(96, 14));
        public static Sprite Circle => circle != null ? circle : (circle = Procedural.RoundedSprite(64, 32));

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                UnityEngine.Object.Destroy(c);
            }
        }

        public static Image Img(Transform parent, Color color, bool rounded = false, string name = "Image")
        {
            var rt = Rect(name, parent);
            var im = rt.gameObject.AddComponent<Image>();
            im.color = color;
            if (rounded)
            {
                im.sprite = Round;
                im.type = Image.Type.Sliced;
                im.pixelsPerUnitMultiplier = 1f;
            }
            return im;
        }

        public static Text Txt(Transform parent, string text, int size, Color color, FontStyle style = FontStyle.Normal, TextAnchor align = TextAnchor.UpperLeft)
        {
            var rt = Rect("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            Style(t, style);
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.lineSpacing = 1.05f;
            return t;
        }

        public static VerticalLayoutGroup V(GameObject go, int pad, int spacing, TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.spacing = spacing;
            v.childAlignment = align;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup H(GameObject go, int spacing, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            return h;
        }

        public static LayoutElement LE(Component c, float prefH = -1, float minH = -1, float flexW = -1, float prefW = -1, float flexH = -1, float minW = -1)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (prefH >= 0) le.preferredHeight = prefH;
            if (minH >= 0) le.minHeight = minH;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (prefW >= 0) le.preferredWidth = prefW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            if (minW >= 0) le.minWidth = minW;
            return le;
        }

        public static Button Btn(Transform parent, string label, Color bg, Color fg, Action onClick, int height = 92, int size = 32)
        {
            var im = Img(parent, bg, true, "Button");
            Depth(im, 5, .5f);
            var b = im.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            var cb = b.colors;
            cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            cb.pressedColor = new Color(.8f, .8f, .8f, 1f);
            cb.disabledColor = new Color(.55f, .55f, .55f, .55f);
            b.colors = cb;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            var t = Txt(im.transform, (label ?? "").ToUpperInvariant(), size, fg, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(20, 0);
            t.rectTransform.offsetMax = new Vector2(-20, 0);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 18;
            t.resizeTextMaxSize = size;
            LE(im, height, height);
            return b;
        }

        public static Button Primary(Transform p, string label, Action a) => Btn(p, label, Theme.Turf, Theme.TurfInk, a);
        public static Button GoldBtn(Transform p, string label, Action a) => Btn(p, label, Theme.Gold, Theme.GoldInk, a);
        public static Button Ghost(Transform p, string label, Action a) => Btn(p, label, Theme.Chip, Theme.Ink, a);

        public static RectTransform Card(Transform parent, int pad = 30, int spacing = 14)
        {
            var im = Img(parent, Theme.Alpha(Theme.Card, .92f), true, "Card");
            V(im.gameObject, pad, spacing);
            return im.rectTransform;
        }

        /// <summary>
        /// Bloco do menu no estilo modo carreira: painel escuro, filete colorido no topo e rótulo em caixa alta.
        /// Use dentro de <see cref="Cols"/>; flex define a proporção da largura.
        /// </summary>
        public static RectTransform Tile(Transform parent, string label, Color? accent = null, float flex = 1, int pad = 28, int spacing = 14)
        {
            var im = Img(parent, Theme.Alpha(Theme.CardHi, .94f), true, "Tile");
            Depth(im, 8, .5f);
            var v = V(im.gameObject, pad, spacing);
            v.padding = new RectOffset(pad, pad, pad + 6, pad);
            LE(im, flexW: flex, prefW: 0, minW: 0);
            var strip = Img(im.transform, accent ?? Theme.Turf, false, "Strip");
            strip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var srt = strip.rectTransform;
            srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1); srt.pivot = new Vector2(.5f, 1);
            srt.sizeDelta = new Vector2(-20, 4); srt.anchoredPosition = Vector2.zero;
            strip.raycastTarget = false;
            if (!string.IsNullOrEmpty(label)) Label(im.transform, label, accent ?? Theme.Muted);
            return im.rectTransform;
        }

        /// <summary>Rótulo pequeno em caixa alta, como os títulos dos blocos de menu.</summary>
        public static Text Label(Transform parent, string text, Color? color = null, int size = 24)
            => Txt(parent, (text ?? "").ToUpperInvariant(), size, color ?? Theme.Muted, FontStyle.Bold);

        /// <summary>Linha de colunas com a mesma altura (blocos lado a lado).</summary>
        public static RectTransform Cols(Transform parent, int spacing = 22)
        {
            var rt = Rect("Cols", parent);
            var h = H(rt.gameObject, spacing, TextAnchor.UpperLeft);
            h.childForceExpandHeight = true;
            h.childForceExpandWidth = false;
            return rt;
        }

        /// <summary>Coluna vertical dentro de <see cref="Cols"/> para empilhar blocos.</summary>
        public static RectTransform Stack(Transform parent, float flex = 1, int spacing = 22)
        {
            var rt = Column(parent, spacing);
            LE(rt, flexW: flex, prefW: 0, minW: 0);
            return rt;
        }

        public static RectTransform Row(Transform parent, int spacing = 16, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rt = Rect("Row", parent);
            H(rt.gameObject, spacing, align);
            return rt;
        }

        public static RectTransform Column(Transform parent, int spacing = 6, TextAnchor align = TextAnchor.UpperLeft)
        {
            var rt = Rect("Column", parent);
            var v = V(rt.gameObject, 0, spacing, align);
            return rt;
        }

        public static Text Title(Transform p, string s) => Txt(p, s, 40, Theme.Ink, FontStyle.Bold);
        public static Text Body(Transform p, string s) => Txt(p, s, 30, Theme.Ink);
        public static Text Muted(Transform p, string s, int size = 26) => Txt(p, s, size, Theme.Muted);

        /// <summary>Linha "rótulo ........ valor".</summary>
        public static void KV(Transform parent, string label, string value)
        {
            var r = Row(parent, 20);
            var l = Txt(r, label, 28, Theme.Muted);
            LE(l, flexW: 1, minW: 0);
            var v = Txt(r, value, 28, Theme.Ink, FontStyle.Bold, TextAnchor.UpperRight);
            LE(v, minW: 0);
        }

        public static RectTransform Tag(Transform parent, string text, Color bg, Color fg)
        {
            var im = Img(parent, bg, true, "Tag");
            var h = H(im.gameObject, 0, TextAnchor.MiddleCenter);
            h.padding = new RectOffset(16, 16, 6, 6);
            Txt(im.transform, (text ?? "").ToUpperInvariant(), 22, fg, FontStyle.Bold);
            return im.rectTransform;
        }

        public static void Bar(Transform parent, float v01, Color fill, float height = 12)
        {
            var bg = Img(parent, Theme.Chip, true, "Bar");
            LE(bg, height, height);
            var f = Img(bg.transform, fill, true, "Fill");
            var rt = f.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(Mathf.Clamp01(v01), 1);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            if (v01 <= 0.02f) f.enabled = false;
        }

        public static Image Crest(Transform parent, Club club, float w = 54, float h = 64)
        {
            var im = Img(parent, Color.white, false, "Crest");
            im.sprite = Procedural.CrestSprite(club);
            im.preserveAspect = true;
            LE(im, h, h, prefW: w, minW: w);
            // iniciais do clube por cima do escudo desenhado (só onde o escudo é grande o bastante para ler)
            var d = Art.CrestArt.For(club.name, club.c1, club.c2);
            if (h >= 44 && !string.IsNullOrEmpty(d.Text) && !Procedural.HasOfficialCrest(club))
            {
                var t = Txt(im.transform, d.Text, Mathf.RoundToInt(h * d.TextSize), Theme.Hex(d.TextCol), FontStyle.Bold, TextAnchor.MiddleCenter);
                var rt = t.rectTransform;
                rt.anchorMin = new Vector2(.12f, 1 - d.TextY - d.TextSize * .7f);
                rt.anchorMax = new Vector2(.88f, 1 - d.TextY + d.TextSize * .7f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 6; t.resizeTextMaxSize = Mathf.RoundToInt(h * d.TextSize);
                t.raycastTarget = false;
                var sh = t.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0, 0, 0, .45f); sh.effectDistance = new Vector2(1, -1);
            }
            return im;
        }

        public static void Space(Transform parent, float h)
        {
            var rt = Rect("Space", parent);
            LE(rt, h, h);
        }

        public static (ScrollRect scroll, RectTransform content) Scroll(Transform parent, int pad = 36, int spacing = 28)
        {
            var root = Rect("Scroll", parent);
            var sr = root.gameObject.AddComponent<ScrollRect>();
            var vp = Rect("Viewport", root);
            Stretch(vp);
            var vpi = vp.gameObject.AddComponent<Image>();
            vpi.color = new Color(0, 0, 0, 0);
            vp.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", vp);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(.5f, 1);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            V(content.gameObject, pad, spacing);
            var csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vp;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40;
            return (sr, content);
        }

        public static GridLayoutGroup Grid(Transform parent, Vector2 cell, int columns, float spacing = 18)
        {
            var rt = Rect("Grid", parent);
            var g = rt.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cell;
            g.spacing = new Vector2(spacing, spacing);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columns;
            g.childAlignment = TextAnchor.UpperLeft;
            return g;
        }

        public static InputField Input(Transform parent, string value, string placeholder, int limit, Action<string> onChange)
        {
            var im = Img(parent, Theme.Chip, true, "Input");
            LE(im, 96, 96);
            var field = im.gameObject.AddComponent<InputField>();
            field.targetGraphic = im;
            var txt = Txt(im.transform, "", 36, Theme.Ink, FontStyle.Bold, TextAnchor.MiddleLeft);
            txt.supportRichText = false;
            Stretch(txt.rectTransform);
            txt.rectTransform.offsetMin = new Vector2(30, 0); txt.rectTransform.offsetMax = new Vector2(-30, 0);
            var ph = Txt(im.transform, placeholder, 32, Theme.Muted, FontStyle.Italic, TextAnchor.MiddleLeft);
            Stretch(ph.rectTransform);
            ph.rectTransform.offsetMin = new Vector2(30, 0); ph.rectTransform.offsetMax = new Vector2(-30, 0);
            field.textComponent = txt;
            field.placeholder = ph;
            field.characterLimit = limit;
            field.lineType = InputField.LineType.SingleLine;
            field.text = value ?? "";
            if (onChange != null) field.onValueChanged.AddListener(s => onChange(s));
            return field;
        }

        public static void Selected(Graphic g, bool on)
        {
            var o = g.GetComponent<Outline>();
            if (on)
            {
                if (o == null) o = g.gameObject.AddComponent<Outline>();
                o.effectColor = Theme.Turf;
                o.effectDistance = new Vector2(4, -4);
            }
            else if (o != null) UnityEngine.Object.Destroy(o);
        }
    }
}
