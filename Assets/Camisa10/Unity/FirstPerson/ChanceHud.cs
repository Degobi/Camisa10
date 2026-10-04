using UnityEngine;
using UnityEngine.UI;

namespace Camisa10.UI
{
    /// <summary>Interface por cima da cena 3D: placar, instrução, aviso de tempo e resultado do lance.</summary>
    public class ChanceHud
    {
        public GameObject Root;
        public SwipePad Pad;
        Text top, hint, banner, now;
        Image bannerBg;

        public static ChanceHud Build(Transform canvas)
        {
            var h = new ChanceHud();
            var root = UIKit.Rect("ChanceHud", canvas);
            UIKit.Stretch(root);
            h.Root = root.gameObject;

            // área de toque em tela cheia (invisível)
            var padImg = UIKit.Img(root, new Color(0, 0, 0, 0), false, "SwipePad");
            UIKit.Stretch(padImg.rectTransform);
            h.Pad = padImg.gameObject.AddComponent<SwipePad>();

            var safe = UIKit.Rect("Safe", root);
            UIKit.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            var topBg = UIKit.Img(safe, new Color(.04f, .12f, .07f, .78f), true, "Top");
            var trt = topBg.rectTransform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(.5f, 1);
            trt.anchoredPosition = new Vector2(0, -24); trt.sizeDelta = new Vector2(-48, 110);
            topBg.raycastTarget = false;
            h.top = UIKit.Txt(topBg.transform, "", 44, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.Stretch(h.top.rectTransform);

            var hintBg = UIKit.Img(safe, new Color(.04f, .12f, .07f, .72f), true, "Hint");
            var hrt = hintBg.rectTransform;
            hrt.anchorMin = new Vector2(0, 0); hrt.anchorMax = new Vector2(1, 0); hrt.pivot = new Vector2(.5f, 0);
            hrt.anchoredPosition = new Vector2(0, 40); hrt.sizeDelta = new Vector2(-48, 0);
            hintBg.raycastTarget = false;
            UIKit.V(hintBg.gameObject, 30, 0, TextAnchor.MiddleCenter);
            hintBg.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            h.hint = UIKit.Txt(hintBg.transform, "", 36, Color.white, FontStyle.Normal, TextAnchor.MiddleCenter);

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
            return h;
        }

        public void SetTop(string s) => top.text = s;
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

        public void Banner(string text, Color color)
        {
            banner.text = text;
            banner.color = color;
            bannerBg.gameObject.SetActive(true);
        }

        public void Destroy() { if (Root != null) Object.Destroy(Root); }
    }
}
