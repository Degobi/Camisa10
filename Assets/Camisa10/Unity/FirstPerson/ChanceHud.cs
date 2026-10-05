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

        public void SetView(Texture t)
        {
            view.texture = t; view.enabled = t != null;
            foreach (var b in blur) { b.texture = t; }
        }

        // ---------- efeito de velocidade: borrão de zoom (cópias da imagem ampliadas) e linhas ----------
        readonly RawImage[] blur = new RawImage[2];
        Image speedLines;
        float speedNow;

        /// <summary>0 = parado, 1 = arrancada máxima. Borrão radial e linhas de velocidade, como na corrida do I Am Playr.</summary>
        public void SetSpeed(float v01)
        {
            speedNow = Mathf.Lerp(speedNow, Mathf.Clamp01(v01), 1f - Mathf.Exp(-8f * Time.deltaTime));
            for (int i = 0; i < blur.Length; i++)
            {
                float k = speedNow * (i + 1);
                blur[i].enabled = view.enabled && speedNow > .02f;
                blur[i].color = new Color(1, 1, 1, .22f * speedNow);
                blur[i].rectTransform.localScale = Vector3.one * (1f + .025f * k);
            }
            speedLines.enabled = speedNow > .05f;
            speedLines.color = new Color(1, 1, 1, speedNow * .85f);
            // as linhas "correm" para fora trocando de escala e girando um pouco a cada quadro
            speedLines.rectTransform.localScale = Vector3.one * (1.05f + Mathf.Repeat(Time.time * 3.1f, .18f));
            speedLines.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Floor(Time.time * 20f) * 37f % 6f - 3f);
        }

        // ---------- mira, força, efeito e tempo ----------
        RectTransform aimRoot, reticle, timingRing;
        Image powerFill, powerSweet;
        GameObject powerGo, curveGo;
        Text curveText;
        public TouchButton CurveL, CurveR;

        /// <summary>Mostra a mira na posição (0..1 da tela do lance).</summary>
        public void Reticle(bool on, Vector2 viewport01 = default, Color? color = null)
        {
            reticle.gameObject.SetActive(on);
            if (!on) return;
            reticle.anchorMin = reticle.anchorMax = viewport01;
            reticle.anchoredPosition = Vector2.zero;
            var c = color ?? Color.white;
            foreach (var g in reticle.GetComponentsInChildren<Graphic>()) g.color = new Color(c.r, c.g, c.b, g.color.a);
            reticle.localScale = Vector3.one * (1f + .06f * Mathf.Sin(Time.time * 6f));
        }

        /// <summary>Anel do cabeceio: encolhe até o tamanho da mira; aperte quando os dois coincidirem.</summary>
        public void Timing(bool on, Vector2 viewport01 = default, float scale = 1, bool perfect = false)
        {
            timingRing.gameObject.SetActive(on);
            if (!on) return;
            timingRing.anchorMin = timingRing.anchorMax = viewport01;
            timingRing.anchoredPosition = Vector2.zero;
            timingRing.localScale = Vector3.one * Mathf.Max(.2f, scale);
            timingRing.GetComponent<Image>().color = perfect ? Theme.Turf : Theme.FeedGold;
        }

        /// <summary>Barra de força ao lado do botão de chute. sweet = faixa ideal (verde).</summary>
        public void Power(bool on, float v01 = 0, float sweetMin = .55f, float sweetMax = .8f)
        {
            powerGo.SetActive(on);
            if (!on) return;
            powerFill.rectTransform.anchorMax = new Vector2(1, Mathf.Clamp01(v01));
            powerFill.color = v01 > sweetMax ? Theme.Red : v01 >= sweetMin ? Theme.Turf : Theme.FeedGold;
            powerSweet.rectTransform.anchorMin = new Vector2(0, sweetMin);
            powerSweet.rectTransform.anchorMax = new Vector2(1, sweetMax);
        }

        /// <summary>Controle de efeito da falta (-3 a 3; negativo curva para a esquerda).</summary>
        public void Curve(bool on, int value = 0)
        {
            curveGo.SetActive(on);
            if (!on) return;
            string arrows = value == 0 ? "SEM EFEITO" : (value < 0 ? new string('‹', -value) + "  ESQUERDA" : "DIREITA  " + new string('›', value));
            curveText.text = arrows;
        }

        void BuildAim(Transform safe, RectTransform root)
        {
            // mira (fica no espaço da imagem do lance, não da área segura)
            aimRoot = UIKit.Rect("Mira", root);
            UIKit.Stretch(aimRoot);
            reticle = UIKit.Rect("Alvo", aimRoot);
            reticle.sizeDelta = new Vector2(96, 96);
            var ring = UIKit.Img(reticle, Color.white, false, "Anel"); ring.sprite = Procedural.RingSprite(); UIKit.Stretch(ring.rectTransform); ring.raycastTarget = false;
            var dot = UIKit.Img(reticle, Color.white, false, "Ponto"); dot.sprite = UIKit.Circle; dot.rectTransform.sizeDelta = new Vector2(14, 14); dot.raycastTarget = false;
            reticle.gameObject.SetActive(false);
            var tr = UIKit.Img(aimRoot, Theme.FeedGold, false, "Tempo");
            tr.sprite = Procedural.RingSprite(); tr.raycastTarget = false;
            timingRing = tr.rectTransform; timingRing.sizeDelta = new Vector2(110, 110);
            timingRing.gameObject.SetActive(false);

            // barra de força (à esquerda do botão de chute)
            var pb = UIKit.Img(safe, new Color(0, 0, 0, .55f), true, "Forca");
            powerGo = pb.gameObject;
            var prt = pb.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(1, 0);
            prt.sizeDelta = new Vector2(40, 300); prt.anchoredPosition = new Vector2(-362, 300);
            pb.raycastTarget = false;
            powerSweet = UIKit.Img(pb.transform, Theme.Alpha(Theme.Turf, .28f), false, "Ideal");
            powerSweet.raycastTarget = false;
            powerSweet.rectTransform.offsetMin = powerSweet.rectTransform.offsetMax = Vector2.zero;
            powerFill = UIKit.Img(pb.transform, Theme.Turf, true, "Nivel");
            powerFill.raycastTarget = false;
            var frt = powerFill.rectTransform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(1, 0); frt.offsetMin = new Vector2(5, 5); frt.offsetMax = new Vector2(-5, -5);
            var pl = UIKit.Txt(pb.transform, "FORÇA", 22, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            pl.rectTransform.anchorMin = new Vector2(.5f, 1); pl.rectTransform.anchorMax = new Vector2(.5f, 1);
            pl.rectTransform.sizeDelta = new Vector2(120, 30); pl.rectTransform.anchoredPosition = new Vector2(0, 22);
            powerGo.SetActive(false);

            // efeito da falta (canto inferior esquerdo, no lugar do joystick)
            var cg = UIKit.Rect("Efeito", safe);
            curveGo = cg.gameObject;
            cg.anchorMin = cg.anchorMax = Vector2.zero; cg.sizeDelta = new Vector2(520, 170); cg.anchoredPosition = new Vector2(320, 150);
            CurveL = RoundButton(cg, "‹", Theme.Chip, Color.white, new Vector2(0, .5f), new Vector2(70, 0), 130, 64);
            CurveR = RoundButton(cg, "›", Theme.Chip, Color.white, new Vector2(1, .5f), new Vector2(-70, 0), 130, 64);
            var lab = UIKit.Txt(cg, "EFEITO", 22, Theme.Muted, FontStyle.Bold, TextAnchor.MiddleCenter);
            lab.rectTransform.anchorMin = new Vector2(0, .5f); lab.rectTransform.anchorMax = new Vector2(1, .5f);
            lab.rectTransform.sizeDelta = new Vector2(-260, 30); lab.rectTransform.anchoredPosition = new Vector2(0, 26);
            curveText = UIKit.Txt(cg, "", 28, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            curveText.rectTransform.anchorMin = new Vector2(0, .5f); curveText.rectTransform.anchorMax = new Vector2(1, .5f);
            curveText.rectTransform.sizeDelta = new Vector2(-260, 40); curveText.rectTransform.anchoredPosition = new Vector2(0, -12);
            curveGo.SetActive(false);
        }

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

            // borrão de velocidade: cópias da imagem do campo, ampliadas e translúcidas
            for (int i = 0; i < h.blur.Length; i++)
            {
                var bgo = UIKit.Rect("Borrao" + i, root);
                UIKit.Stretch(bgo);
                h.blur[i] = bgo.gameObject.AddComponent<RawImage>();
                h.blur[i].raycastTarget = false;
                h.blur[i].enabled = false;
            }
            h.speedLines = UIKit.Img(root, Color.white, false, "LinhasVelocidade");
            h.speedLines.sprite = Procedural.SpeedLinesSprite();
            UIKit.Stretch(h.speedLines.rectTransform);
            h.speedLines.raycastTarget = false;
            h.speedLines.enabled = false;

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

            // selo de replay no canto, como na transmissão
            h.replayTag = UIKit.Img(safe, Theme.Red, true, "Replay");
            var rrt = h.replayTag.rectTransform;
            rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(1, 1);
            rrt.anchoredPosition = new Vector2(-36, -30); rrt.sizeDelta = new Vector2(230, 66);
            h.replayTag.raycastTarget = false;
            var rtx = UIKit.Txt(h.replayTag.transform, "REPLAY", 40, Color.white, FontStyle.BoldAndItalic, TextAnchor.MiddleCenter);
            UIKit.Stretch(rtx.rectTransform);
            h.replayTag.gameObject.SetActive(false);

            h.now = UIKit.Txt(safe, "AGORA!", 120, Theme.FeedGold, FontStyle.Bold, TextAnchor.MiddleCenter);
            var nrt = h.now.rectTransform;
            nrt.anchorMin = new Vector2(0, .5f); nrt.anchorMax = new Vector2(1, .5f);
            nrt.sizeDelta = new Vector2(0, 160); nrt.anchoredPosition = new Vector2(0, -160);
            var outline = h.now.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, .8f); outline.effectDistance = new Vector2(4, -4);
            h.now.gameObject.SetActive(false);
            h.BuildControls(safe);
            h.BuildAim(safe, root);
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
        Image replayTag;
        public void ReplayTag(bool on) { if (replayTag != null) replayTag.gameObject.SetActive(on); }
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
        public TouchButton Shoot, PassBtn, Dribble, Sprint, TackleL, TackleR, Skill, Finesse, SkipBtn;

        /// <summary>Mostra o botão de pular (comemoração) e chama onSkip quando tocado.</summary>
        public void ShowSkip(bool on, System.Action onSkip = null)
        {
            SkipBtn.gameObject.SetActive(on);
            SkipBtn.OnPress = on ? onSkip : null;
        }
        Text skillHint;
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
            Finesse = RoundButton(safe, "COLOCADO", Theme.Hex("#2E7DFF"), Color.white, br, new Vector2(-490, 160), 160, 26);
            PassBtn = RoundButton(safe, "PASSE", Theme.Cyan, Theme.TurfInk, br, new Vector2(-725, 130), 140, 30);
            Dribble = RoundButton(safe, "DRIBLE", Theme.Gold, Theme.GoldInk, br, new Vector2(-470, 395), 145, 30);
            Sprint = RoundButton(safe, "CORRER", Theme.Purple, Color.white, br, new Vector2(-210, 480), 140, 28);
            Sprint.Sticky = true;
            Skill = RoundButton(safe, "FIRULA", Theme.Hex("#FF7A1A"), Color.white, br, new Vector2(-715, 330), 145, 28);
            skillHint = UIKit.Txt(Skill.transform, "", 20, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            skillHint.rectTransform.anchorMin = new Vector2(.5f, 0); skillHint.rectTransform.anchorMax = new Vector2(.5f, 0);
            skillHint.rectTransform.sizeDelta = new Vector2(220, 28); skillHint.rectTransform.anchoredPosition = new Vector2(0, -22);
            skillHint.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .7f);

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

            // pular a comemoração
            SkipBtn = WideButton(safe, "PULAR ›", br, new Vector2(-190, 110));
            SkipBtn.GetComponent<Image>().color = new Color(0, 0, 0, .55f);
            SkipBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(260, 100);
            SkipBtn.gameObject.SetActive(false);
            Controls(false, false, false, false, false, false);
        }

        /// <summary>Liga só os controles que o tipo de lance usa.</summary>
        public void Controls(bool stick, bool shoot, bool pass, bool dribble, bool sprint, bool tackle)
        {
            stickGo.SetActive(stick);
            Skill.gameObject.SetActive(stick); // firulas só com a bola nos pés
            Shoot.gameObject.SetActive(shoot);
            Finesse.gameObject.SetActive(shoot && stick);
            PassBtn.gameObject.SetActive(pass);
            Dribble.gameObject.SetActive(dribble);
            Sprint.gameObject.SetActive(sprint);
            TackleL.gameObject.SetActive(tackle);
            TackleR.gameObject.SetActive(tackle);
        }

        public void HideControls() => Controls(false, false, false, false, false, false);

        /// <summary>Nome da firula que sai agora (muda com a direção do joystick).</summary>
        public void SkillName(string s) { if (skillHint.text != s) skillHint.text = s; }

        // ---------- pontos da nota subindo na tela ----------
        int popups;

        /// <summary>"+3 DRIBLE": texto que sobe e some (pontos da nota da partida).</summary>
        public void Popup(string text, Color color)
        {
            var t = UIKit.Txt(Root.transform, text, 54, color, FontStyle.BoldAndItalic, TextAnchor.MiddleCenter);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .62f);
            rt.sizeDelta = new Vector2(900, 80);
            rt.anchoredPosition = new Vector2(0, -(popups++ % 3) * 70);
            var o = t.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, .75f); o.effectDistance = new Vector2(3, -3);
            t.gameObject.AddComponent<FloatUp>();
        }

        /// <summary>Acende o botão CORRER enquanto o jogador está em arrancada.</summary>
        public void SprintOn(bool on)
        {
            var g = Sprint.GetComponent<Image>();
            if (g != null) Sprint.transform.localScale = Vector3.one * (on ? 1.12f : 1f);
        }

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

    /// <summary>Sobe devagar e desaparece.</summary>
    public class FloatUp : MonoBehaviour
    {
        float t;
        Text txt;
        void Awake() => txt = GetComponent<Text>();
        void Update()
        {
            t += Time.deltaTime;
            ((RectTransform)transform).anchoredPosition += new Vector2(0, 70 * Time.deltaTime);
            float a = t < .15f ? t / .15f : Mathf.Clamp01(1.6f - t);
            transform.localScale = Vector3.one * (t < .15f ? Mathf.Lerp(1.4f, 1f, t / .15f) : 1f);
            if (txt != null) { var c = txt.color; c.a = a; txt.color = c; }
            if (t > 1.6f) Destroy(gameObject);
        }
    }
}
