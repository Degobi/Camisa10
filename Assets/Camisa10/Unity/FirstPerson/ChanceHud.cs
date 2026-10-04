using UnityEngine;
using UnityEngine.UI;

namespace Camisa10.UI
{
    /// <summary>Interface por cima da cena 3D: placar, instrução, aviso de tempo e resultado do lance.</summary>
    public class ChanceHud
    {
        public GameObject Root;
        public SwipePad Pad;
        Text clock, homeName, awayName, score, hint, banner, now;
        Image homeChip, awayChip;
        Image bannerBg;
        RawImage view;

        public void SetView(Texture t) { view.texture = t; view.enabled = t != null; }

        public static ChanceHud Build(Transform canvas)
        {
            var h = new ChanceHud();
            var root = UIKit.Rect("ChanceHud", canvas);
            UIKit.Stretch(root);
            h.Root = root.gameObject;

            // imagem do campo (a câmera do lance desenha nesta textura)
            h.view = root.gameObject.AddComponent<RawImage>();
            h.view.color = Color.white;
            h.view.raycastTarget = false;
            h.view.enabled = false;

            // vinheta por cima da imagem do campo (bordas mais escuras, como lente de transmissão)
            var vig = UIKit.Img(root, Color.white, false, "Vinheta");
            vig.sprite = Procedural.VignetteSprite();
            UIKit.Stretch(vig.rectTransform);
            vig.raycastTarget = false;

            // área de toque em tela cheia (invisível)
            var padImg = UIKit.Img(root, new Color(0, 0, 0, 0), false, "SwipePad");
            UIKit.Stretch(padImg.rectTransform);
            h.Pad = padImg.gameObject.AddComponent<SwipePad>();

            var safe = UIKit.Rect("Safe", root);
            UIKit.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            // placar no canto, no estilo das transmissões de TV
            var bug = UIKit.Rect("Placar", safe);
            bug.anchorMin = bug.anchorMax = new Vector2(0, 1); bug.pivot = new Vector2(0, 1);
            bug.anchoredPosition = new Vector2(36, -30);
            var bl = UIKit.H(bug.gameObject, 0);
            bl.childForceExpandHeight = true;
            bug.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            bug.sizeDelta = new Vector2(0, 66);
            Text Cell(Color bg, int minW, int size, Color fg, out Image img)
            {
                img = UIKit.Img(bug, bg, false, "Cel");
                img.raycastTarget = false;
                UIKit.LE(img, prefW: minW, minW: minW);
                var t = UIKit.Txt(img.transform, "", size, fg, FontStyle.Bold, TextAnchor.MiddleCenter);
                UIKit.Stretch(t.rectTransform);
                return t;
            }
            h.clock = Cell(Theme.Hex("#0B0F1A"), 104, 36, Theme.Turf, out _);
            h.homeChip = UIKit.Img(bug, Color.white, false, "CorCasa"); UIKit.LE(h.homeChip, prefW: 10, minW: 10); h.homeChip.raycastTarget = false;
            h.homeName = Cell(Theme.Alpha(Theme.Hex("#141B2D"), .94f), 116, 36, Color.white, out _);
            h.score = Cell(Color.white, 120, 40, Theme.Hex("#0B0F1A"), out _);
            h.awayName = Cell(Theme.Alpha(Theme.Hex("#141B2D"), .94f), 116, 36, Color.white, out _);
            h.awayChip = UIKit.Img(bug, Color.white, false, "CorFora"); UIKit.LE(h.awayChip, prefW: 10, minW: 10); h.awayChip.raycastTarget = false;
            UIKit.Depth(h.clock.transform.parent.GetComponent<Image>(), 4, .4f);

            var hintBg = UIKit.Img(safe, new Color(.04f, .12f, .07f, .72f), true, "Hint");
            var hrt = hintBg.rectTransform;
            hrt.anchorMin = new Vector2(0, 0); hrt.anchorMax = new Vector2(1, 0); hrt.pivot = new Vector2(.5f, 0);
            hrt.anchoredPosition = new Vector2(0, 40); hrt.sizeDelta = new Vector2(-1150, 0); // entre o joystick e os botões
            hintBg.raycastTarget = false;
            UIKit.V(hintBg.gameObject, 30, 0, TextAnchor.MiddleCenter);
            hintBg.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            h.hint = UIKit.Txt(hintBg.transform, "", 30, Color.white, FontStyle.Normal, TextAnchor.MiddleCenter);

            h.bannerBg = UIKit.Img(safe, new Color(0, 0, 0, .55f), true, "Banner");
            var brt = h.bannerBg.rectTransform;
            brt.anchorMin = new Vector2(0, .5f); brt.anchorMax = new Vector2(1, .5f);
            brt.sizeDelta = new Vector2(-80, 230); brt.anchoredPosition = new Vector2(0, 120);
            h.bannerBg.raycastTarget = false;
            h.banner = UIKit.Txt(h.bannerBg.transform, "", 110, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.Stretch(h.banner.rectTransform);
            h.banner.resizeTextForBestFit = true; h.banner.resizeTextMinSize = 40; h.banner.resizeTextMaxSize = 110;
            h.bannerBg.gameObject.SetActive(false);

            h.now = UIKit.Txt(safe, "AGORA!", 120, Theme.FeedGold, FontStyle.Bold, TextAnchor.MiddleCenter);
            var nrt = h.now.rectTransform;
            nrt.anchorMin = new Vector2(0, .5f); nrt.anchorMax = new Vector2(1, .5f);
            nrt.sizeDelta = new Vector2(0, 160); nrt.anchoredPosition = new Vector2(0, -160);
            var outline = h.now.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, .8f); outline.effectDistance = new Vector2(4, -4);
            h.now.gameObject.SetActive(false);
            h.BuildControls(safe);
            return h;
        }

        /// <summary>Atualiza o placar (mandante sempre à esquerda, como na TV).</summary>
        public void SetScore(int minute, string home, Color homeColor, int homeGoals, int awayGoals, string away, Color awayColor)
        {
            clock.text = minute + "'";
            homeName.text = Abbrev(home); awayName.text = Abbrev(away);
            homeChip.color = homeColor; awayChip.color = awayColor;
            score.text = $"{homeGoals} - {awayGoals}";
        }

        /// <summary>Sigla de três letras do clube ("São Paulo" → "SAO").</summary>
        public static string Abbrev(string name)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char ch in (name ?? "").Normalize(System.Text.NormalizationForm.FormD))
                if (char.IsLetter(ch) && System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(char.ToUpperInvariant(ch));
            var s = sb.ToString();
            return s.Length > 3 ? s.Substring(0, 3) : s;
        }
        public void SetHint(string s) => hint.text = s;
        public void ShowNow(bool on) { if (now.gameObject.activeSelf != on) now.gameObject.SetActive(on); }

        bool intro;

        /// <summary>Mostra a situação do lance por alguns instantes antes de liberar o controle.</summary>
        public void Intro(string text)
        {
            intro = true;
            banner.resizeTextMaxSize = 54;
            Banner(text, Color.white);
        }

        public void EndIntro()
        {
            if (!intro) return;
            intro = false;
            banner.resizeTextMaxSize = 110;
            bannerBg.gameObject.SetActive(false);
        }

        public void EndBanner() => bannerBg.gameObject.SetActive(false);

        public void Banner(string text, Color color)
        {
            banner.text = text;
            banner.color = color;
            bannerBg.gameObject.SetActive(true);
        }

        // ---------- controles na tela ----------
        public VirtualStick Stick;
        public TouchButton Shoot, PassBtn, Dribble, Sprint, TackleL, TackleR;
        Image staminaFill, dribbleRing;
        GameObject stickGo;

        static TouchButton RoundButton(Transform parent, string label, Color bg, Color fg, Vector2 anchor, Vector2 pos, float size, int font)
        {
            var im = UIKit.Img(parent, Theme.Alpha(bg, .9f), false, label);
            im.sprite = UIKit.Circle;
            var rt = im.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;
            UIKit.Depth(im, 6, .45f);
            var t = UIKit.Txt(im.transform, label, font, fg, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.Stretch(t.rectTransform);
            return im.gameObject.AddComponent<TouchButton>();
        }

        static TouchButton WideButton(Transform parent, string label, Vector2 anchor, Vector2 pos)
        {
            var im = UIKit.Img(parent, Theme.Alpha(Theme.Red, .9f), true, label);
            var rt = im.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(330, 150);
            rt.anchoredPosition = pos;
            UIKit.Depth(im, 6, .45f);
            var t = UIKit.Txt(im.transform, label, 40, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.Stretch(t.rectTransform);
            return im.gameObject.AddComponent<TouchButton>();
        }

        void BuildControls(Transform safe)
        {
            Vector2 bl = Vector2.zero, br = new Vector2(1, 0);

            // joystick (canto inferior esquerdo)
            var baseIm = UIKit.Img(safe, new Color(1, 1, 1, .12f), false, "Joystick");
            baseIm.sprite = UIKit.Circle;
            var brt = baseIm.rectTransform;
            brt.anchorMin = brt.anchorMax = bl;
            brt.sizeDelta = new Vector2(320, 320);
            brt.anchoredPosition = new Vector2(250, 250);
            var ring = UIKit.Img(baseIm.transform, new Color(1, 1, 1, .25f), false, "Aro");
            ring.sprite = UIKit.Circle; ring.raycastTarget = false;
            UIKit.Stretch(ring.rectTransform);
            ring.rectTransform.offsetMin = new Vector2(6, 6); ring.rectTransform.offsetMax = new Vector2(-6, -6);
            ring.color = new Color(0, 0, 0, .25f);
            var knob = UIKit.Img(baseIm.transform, new Color(1, 1, 1, .75f), false, "Botao");
            knob.sprite = UIKit.Circle; knob.raycastTarget = false;
            knob.rectTransform.sizeDelta = new Vector2(140, 140);
            UIKit.Depth(knob, 4, .4f);
            var lbl = UIKit.Txt(baseIm.transform, "CONDUZIR", 24, new Color(1, 1, 1, .8f), FontStyle.Bold, TextAnchor.MiddleCenter);
            lbl.rectTransform.anchorMin = new Vector2(0, 0); lbl.rectTransform.anchorMax = new Vector2(1, 0);
            lbl.rectTransform.sizeDelta = new Vector2(0, 40); lbl.rectTransform.anchoredPosition = new Vector2(0, -26);
            Stick = baseIm.gameObject.AddComponent<VirtualStick>();
            Stick.Knob = knob.rectTransform;
            stickGo = baseIm.gameObject;

            // botões (canto inferior direito)
            Shoot = RoundButton(safe, "CHUTAR", Theme.Turf, Theme.TurfInk, br, new Vector2(-220, 230), 230, 40);
            PassBtn = RoundButton(safe, "PASSE", Theme.Cyan, Theme.TurfInk, br, new Vector2(-480, 150), 150, 30);
            Dribble = RoundButton(safe, "DRIBLE", Theme.Gold, Theme.GoldInk, br, new Vector2(-450, 380), 150, 30);
            Sprint = RoundButton(safe, "CORRER", Theme.Purple, Color.white, br, new Vector2(-210, 480), 140, 28);

            dribbleRing = UIKit.Img(Dribble.transform, Theme.Gold, false, "Pronto");
            dribbleRing.sprite = UIKit.Circle; dribbleRing.raycastTarget = false;
            UIKit.Stretch(dribbleRing.rectTransform);
            dribbleRing.rectTransform.offsetMin = new Vector2(-14, -14); dribbleRing.rectTransform.offsetMax = new Vector2(14, 14);
            dribbleRing.transform.SetAsFirstSibling();
            dribbleRing.enabled = false;

            var bar = UIKit.Img(Sprint.transform, new Color(0, 0, 0, .55f), true, "Folego");
            var bart = bar.rectTransform;
            bart.anchorMin = new Vector2(.1f, 0); bart.anchorMax = new Vector2(.9f, 0);
            bart.sizeDelta = new Vector2(0, 12); bart.anchoredPosition = new Vector2(0, -16);
            bar.raycastTarget = false;
            staminaFill = UIKit.Img(bar.transform, Theme.Turf, true, "Nivel");
            staminaFill.raycastTarget = false;
            UIKit.Stretch(staminaFill.rectTransform);

            // defesa: desarmar para um lado ou para o outro
            TackleL = WideButton(safe, "‹ DESARME", bl, new Vector2(250, 200));
            TackleR = WideButton(safe, "DESARME ›", br, new Vector2(-250, 200));
            Controls(false, false, false, false, false, false);
        }

        /// <summary>Liga só os controles que o tipo de lance usa.</summary>
        public void Controls(bool stick, bool shoot, bool pass, bool dribble, bool sprint, bool tackle)
        {
            stickGo.SetActive(stick);
            Shoot.gameObject.SetActive(shoot);
            PassBtn.gameObject.SetActive(pass);
            Dribble.gameObject.SetActive(dribble);
            Sprint.gameObject.SetActive(sprint);
            TackleL.gameObject.SetActive(tackle);
            TackleR.gameObject.SetActive(tackle);
        }

        public void HideControls() => Controls(false, false, false, false, false, false);

        public void SetStamina(float v01)
        {
            staminaFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(v01), 1);
            staminaFill.color = v01 < .25f ? Theme.Red : Theme.Turf;
        }

        /// <summary>Acende o botão de drible quando há marcador perto.</summary>
        public void DribbleReady(bool on)
        {
            if (dribbleRing.enabled != on) dribbleRing.enabled = on;
            if (on) dribbleRing.color = Theme.Alpha(Theme.Gold, .45f + .35f * Mathf.Abs(Mathf.Sin(Time.time * 8f)));
        }

        public void Destroy() { if (Root != null) Object.Destroy(Root); }
    }
}
