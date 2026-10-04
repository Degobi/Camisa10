using System;
using System.Collections;
using System.Collections.Generic;
using Camisa10.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Camisa10.UI
{
    /// <summary>
    /// Ponto central do jogo: monta a interface, guarda o estado e reage às ações.
    /// Dividido em arquivos parciais: GameApp (estrutura), GameApp.Screens (telas) e GameApp.Match (partida).
    /// </summary>
    public partial class GameApp : MonoBehaviour
    {
        const int HeaderH = 112, NavH = 70;

        Game game;
        MatchEngine match;

        RectTransform safe, header, nav, overlays, topFill;
        Transform canvasRoot;
        Image background, letterbox;
        CanvasScaler scaler;
        ScrollRect scroll;
        RectTransform content;
        GameObject toastGo;
        Text toastText;
        Coroutine toastCo;

        string tab = "home";
        Attr trainAttr = Attr.Fin;
        string intensity = "normal";

        // criação de jogador
        string newName = "";
        string newPos = "ATA";
        readonly int[] alloc = new int[6];
        const int AllocPoints = 20, AllocMax = 10;

        // negociação
        long negSal = -1;
        int negYears = 3;
        string negMsg = "";

        /// <summary>Versão mostrada no topo: confirma que a Unity está rodando o código novo.</summary>
        public const string Version = "0.9";

        // abas do menu principal (ordem da barra)
        static readonly (string id, string label)[] Tabs =
        {
            ("home", "Central"), ("agenda", "Agenda"), ("player", "Jogador"), ("trophies", "Troféus"), ("business", "Negócios"),
            ("contract", "Contrato"), ("sponsors", "Patrocínio"), ("life", "Vida"),
        };

        void Awake()
        {
            Application.targetFrameRate = 60;
            Landscape.LockOrientation();
            EnsureCamera();
            EnsureEventSystem();
            BuildShell();
            var saved = SaveSystem.Load();
            if (saved != null)
            {
                game = new Game(saved);
                trainAttr = Game.MainAttr(saved.player.pos);
            }
            Render(true);
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() { Save(); }

        void Save() { if (game != null) SaveSystem.Save(game.S); }

        // ---------- infraestrutura ----------
        static void EnsureCamera()
        {
            if (Camera.main != null) return;
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            DontDestroyOnLoad(go);
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            // Projetos novos da Unity 6 usam o Input System; projetos antigos usam o módulo clássico.
            var inputSystemModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule != null) go.AddComponent(inputSystemModule);
            else go.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(go);
        }

        void BuildShell()
        {
            var cgo = new GameObject("Canvas", typeof(RectTransform));
            cgo.layer = 5;
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = Landscape.CanvasMatch;
            cgo.AddComponent<GraphicRaycaster>();

            canvasRoot = cgo.transform;
            letterbox = UIKit.Img(cgo.transform, Color.black, false, "Letterbox");
            UIKit.Stretch(letterbox.rectTransform);
            letterbox.raycastTarget = false;
            var bg = UIKit.Img(cgo.transform, Color.white, false, "Background");
            bg.sprite = Procedural.BackdropSprite();
            bg.gameObject.AddComponent<SafeArea>().insets = false;
            background = bg;

            // pinta a área do notch (lateral, com o celular deitado) com a cor da barra
            var tf = UIKit.Img(cgo.transform, Theme.Bar, false, "TopFill");
            topFill = tf.rectTransform;
            topFill.anchorMin = new Vector2(0, 1); topFill.anchorMax = Vector2.one; topFill.pivot = new Vector2(.5f, 1);
            topFill.sizeDelta = new Vector2(0, HeaderH + NavH); topFill.anchoredPosition = Vector2.zero;
            tf.raycastTarget = false;

            safe = UIKit.Rect("SafeArea", cgo.transform);
            UIKit.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            // conteúdo rolável fica abaixo da barra superior e das abas
            var (sr, ct) = UIKit.Scroll(safe, 32, 22);
            scroll = sr; content = ct;
            content.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(44, 44, 28, 28);
            var srt = (RectTransform)sr.transform;
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;

            var hi = UIKit.Img(safe, Theme.Alpha(Theme.Bar, .94f), false, "Header");
            header = hi.rectTransform;
            header.anchorMin = new Vector2(0, 1); header.anchorMax = new Vector2(1, 1);
            header.pivot = new Vector2(.5f, 1);
            header.sizeDelta = new Vector2(0, HeaderH); header.anchoredPosition = Vector2.zero;
            var hl = UIKit.H(hi.gameObject, 22);
            hl.padding = new RectOffset(44, 30, 14, 14);

            var ni = UIKit.Img(safe, Theme.Alpha(Theme.Bar, .78f), false, "Tabs");
            nav = ni.rectTransform;
            nav.anchorMin = new Vector2(0, 1); nav.anchorMax = new Vector2(1, 1);
            nav.pivot = new Vector2(.5f, 1);
            nav.sizeDelta = new Vector2(0, NavH); nav.anchoredPosition = new Vector2(0, -HeaderH);
            var nl = UIKit.H(ni.gameObject, 6, TextAnchor.MiddleLeft);
            nl.childForceExpandHeight = true;
            nl.padding = new RectOffset(30, 30, 0, 0);
            var navLine = UIKit.Img(ni.transform, Theme.Line, false, "Line");
            navLine.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var nlrt = navLine.rectTransform;
            nlrt.anchorMin = Vector2.zero; nlrt.anchorMax = new Vector2(1, 0); nlrt.pivot = new Vector2(.5f, 0);
            nlrt.sizeDelta = new Vector2(0, 2); nlrt.anchoredPosition = Vector2.zero;

            overlays = UIKit.Rect("Overlays", safe);
            UIKit.Stretch(overlays);

            var toast = UIKit.Img(safe, Theme.Alpha(Theme.CardHi, .97f), true, "Toast");
            toastGo = toast.gameObject;
            var trt = toast.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(.5f, 0); trt.pivot = new Vector2(.5f, 0);
            trt.anchoredPosition = new Vector2(0, 40); trt.sizeDelta = new Vector2(1000, 0);
            UIKit.V(toastGo, 26, 0, TextAnchor.MiddleCenter);
            toastGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var toastLine = UIKit.Img(toast.transform, Theme.Turf, false, "Accent");
            toastLine.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var tlrt = toastLine.rectTransform;
            tlrt.anchorMin = Vector2.zero; tlrt.anchorMax = new Vector2(0, 1); tlrt.pivot = new Vector2(0, .5f);
            tlrt.sizeDelta = new Vector2(6, -16); tlrt.anchoredPosition = new Vector2(8, 0);
            toastText = UIKit.Txt(toast.transform, "", 30, Theme.Ink, FontStyle.Normal, TextAnchor.MiddleCenter);
            toast.raycastTarget = false;
            toastGo.SetActive(false);
        }

        void Update()
        {
            float m = Landscape.CanvasMatch;
            if (scaler != null && scaler.matchWidthOrHeight != m) scaler.matchWidthOrHeight = m;
        }

        void SetChrome(bool on)
        {
            header.gameObject.SetActive(on);
            nav.gameObject.SetActive(on);
            topFill.gameObject.SetActive(on && !Landscape.Letterboxed);
            var srt = (RectTransform)scroll.transform;
            srt.offsetMax = new Vector2(0, on ? -(HeaderH + NavH) : 0);
            srt.offsetMin = Vector2.zero;
        }

        // ---------- renderização ----------
        void Render(bool resetScroll = false)
        {
            UIKit.Clear(content);
            if (game == null) { SetChrome(false); BuildCreate(); }
            else if (game.S.retired) { SetChrome(false); BuildRetired(); }
            else
            {
                SetChrome(true);
                BuildHeader();
                BuildNav();
                switch (tab)
                {
                    case "agenda": BuildAgenda(); break;
                    case "player": BuildPlayer(); break;
                    case "trophies": BuildTrophies(); break;
                    case "business": BuildBusiness(); break;
                    case "contract": BuildContract(); break;
                    case "sponsors": BuildSponsors(); break;
                    case "life": BuildLife(); break;
                    default: BuildHome(); break;
                }
            }
            UIKit.Space(content, 30);
            if (resetScroll)
            {
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = 1;
            }
        }

        void Commit(string toastMessage = null, bool resetScroll = false)
        {
            Save();
            Render(resetScroll);
            if (!string.IsNullOrEmpty(toastMessage)) Toast(toastMessage);
        }

        void BuildHeader()
        {
            UIKit.Clear(header);
            var p = game.S.player; var c = game.MyClub; var se = game.S.season;

            UIKit.Crest(header, c, 62, 74);
            var col = UIKit.Column(header, 0);
            UIKit.LE(col, flexW: 1, minW: 0, prefW: 0);
            var tag = UIKit.Label(col, $"Modo carreira · {c.name} · v{Version}", Theme.Turf, 22);
            FitLine(tag, 16, 30);
            var name = UIKit.Txt(col, p.name, 40, Theme.Ink, FontStyle.Bold, TextAnchor.MiddleLeft);
            FitLine(name, 24, 50);

            var when = UIKit.Column(header, 0, TextAnchor.MiddleRight);
            UIKit.LE(when, prefW: 330, minW: 260);
            var l1 = UIKit.Label(when, se.phase == "end" ? $"Temporada {se.year} · encerrada" : $"Temporada {se.year} · rodada {se.week + 1}/{Game.RoundsPerSeason}", Theme.Muted, 22);
            l1.alignment = TextAnchor.UpperRight;
            var l2 = UIKit.Txt(when, Fmt.Money(p.money), 36, Theme.Gold, FontStyle.Bold, TextAnchor.UpperRight);
            FitLine(l2, 20, 46);

            var ovrBox = UIKit.Img(header, Theme.Gold, true, "Ovr");
            UIKit.LE(ovrBox, 84, 84, prefW: 96, minW: 96);
            UIKit.V(ovrBox.gameObject, 4, -6, TextAnchor.MiddleCenter);
            UIKit.Txt(ovrBox.transform, game.Ovr.ToString(), 44, Theme.GoldInk, FontStyle.Bold, TextAnchor.MiddleCenter);
            UIKit.Txt(ovrBox.transform, p.pos, 18, Theme.GoldInk, FontStyle.Bold, TextAnchor.MiddleCenter);

            var (label, action) = ContinueAction();
            var go = UIKit.Btn(header, label + "  ›", Theme.Turf, Theme.TurfInk, action, 84, 30);
            UIKit.LE(go, 84, 84, prefW: 330, minW: 260);
            go.interactable = action != null;
        }

        /// <summary>O botão "Continuar" da barra superior: sempre leva ao próximo passo da carreira.</summary>
        (string label, Action action) ContinueAction()
        {
            var se = game.S.season;
            if (se.phase == "end")
            {
                if (game.MustRetire) return ("Fim de carreira", () => { tab = "home"; Render(true); });
                if (!game.CanStartNextSeason) return ("Escolha um contrato", () => { tab = "contract"; Render(true); });
                return ($"Iniciar {game.S.year + 1}", () => { game.NextSeason(); tab = "home"; Commit(null, true); });
            }
            if (se.phase == "train")
            {
                if (game.S.player.injury > 0) return ("Fisioterapia", () => { game.Physio(); Commit("Sessão de fisioterapia feita."); });
                return ("Treinar", DoTrain);
            }
            return ($"Jogar rodada {se.week + 1}", StartMatch);
        }

        static void FitLine(Text t, int minSize, float height)
        {
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = minSize;
            t.resizeTextMaxSize = t.fontSize;
            UIKit.LE(t, height, height, minW: 0);
        }

        void BuildNav()
        {
            UIKit.Clear(nav);
            foreach (var (id, label) in Tabs)
            {
                bool cur = tab == id;
                var im = UIKit.Img(nav, new Color(0, 0, 0, 0), false, "Tab");
                var b = im.gameObject.AddComponent<Button>();
                b.targetGraphic = im;
                string captured = id;
                b.onClick.AddListener(() => { tab = captured; negMsg = ""; Render(true); });
                UIKit.LE(im, prefW: 210, minW: 150);
                string text = label.ToUpperInvariant();
                if (id == "agenda" && game.S.season.phase != "end" && game.ActionsLeft > 0) text += $" ({game.ActionsLeft})";
                var t = UIKit.Txt(im.transform, text, 26, cur ? Theme.Ink : Theme.Muted, FontStyle.Bold, TextAnchor.MiddleCenter);
                UIKit.Stretch(t.rectTransform);
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 16; t.resizeTextMaxSize = 26;
                if (cur)
                {
                    var bar = UIKit.Img(im.transform, Theme.Turf, false, "Indicator");
                    var rt = bar.rectTransform;
                    rt.anchorMin = new Vector2(.12f, 0); rt.anchorMax = new Vector2(.88f, 0);
                    rt.pivot = new Vector2(.5f, 0);
                    rt.sizeDelta = new Vector2(0, 5); rt.anchoredPosition = new Vector2(0, 2);
                }
            }
        }

        // ---------- avisos e modais ----------
        void Toast(string message)
        {
            toastText.text = message;
            toastGo.SetActive(true);
            toastGo.transform.SetAsLastSibling();
            if (toastCo != null) StopCoroutine(toastCo);
            toastCo = StartCoroutine(HideToast());
        }

        IEnumerator HideToast()
        {
            yield return new WaitForSeconds(3.2f);
            toastGo.SetActive(false);
        }

        GameObject modal;

        void ShowModal(string title, string text, params (string label, Action action, bool primary)[] buttons)
        {
            CloseModal();
            var dim = UIKit.Img(overlays, new Color(0, 0, 0, .7f), false, "Modal");
            UIKit.Stretch(dim.rectTransform);
            modal = dim.gameObject;
            var sheet = UIKit.Img(dim.transform, Theme.CardHi, true, "Dialog");
            var rt = sheet.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(1100, 0);
            UIKit.V(sheet.gameObject, 44, 20);
            sheet.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var strip = UIKit.Img(sheet.transform, Theme.Turf, false, "Strip");
            strip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var srt = strip.rectTransform;
            srt.anchorMin = new Vector2(0, 1); srt.anchorMax = Vector2.one; srt.pivot = new Vector2(.5f, 1);
            srt.sizeDelta = new Vector2(0, 5); srt.anchoredPosition = Vector2.zero;
            UIKit.Title(sheet.transform, title);
            UIKit.Body(sheet.transform, text);
            var row = UIKit.Row(sheet.transform, 18);
            foreach (var (label, action, primary) in buttons)
            {
                var b = primary ? UIKit.Primary(row, label, action) : UIKit.Ghost(row, label, action);
                UIKit.LE(b, flexW: 1, prefW: 0, minW: 0);
            }
        }

        void CloseModal()
        {
            if (modal != null) Destroy(modal);
            modal = null;
        }

        void ShowEvent(GameEvent ev)
        {
            var buttons = new List<(string, Action, bool)>();
            for (int i = 0; i < ev.Options.Length; i++)
            {
                int idx = i;
                buttons.Add((ev.Options[i].Label, () =>
                {
                    string result = GameEvents.Resolve(game, ev, idx);
                    Save();
                    Render();
                    ShowModal(ev.Title, result, ("Continuar", (Action)CloseModal, true));
                }, i == 0));
            }
            ShowModal(ev.Title, ev.Text, buttons.ToArray());
        }
    }
}
