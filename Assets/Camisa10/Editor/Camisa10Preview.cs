using System.IO;
using Camisa10.Core;
using Camisa10.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Camisa10.EditorTools
{
    /// <summary>
    /// Monta o estádio e fotografa alguns lances em PNG (pasta Capturas/, na raiz do projeto), sem entrar em Play.
    /// Serve para conferir o visual rápido e também roda em linha de comando:
    /// Unity -batchmode -projectPath . -executeMethod Camisa10.EditorTools.Camisa10Preview.CaptureBatch -quit
    /// </summary>
    public static class Camisa10Preview
    {
        const int W = 1920, H = 1080;

        [MenuItem("Camisa 10/Capturar imagens do lance")]
        public static void CaptureMenu()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorUtility.RevealInFinder(CaptureAll());
        }

        public static void CaptureBatch()
        {
            CaptureAll();
            EditorApplication.Exit(0);
        }

        /// <summary>Fotografa de dia e de noite numa cena temporária e volta para a cena que estava aberta.</summary>
        static string CaptureAll()
        {
            string previous = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Capture(false);
            string dir = Capture(true);
            var atlas = StadiumArt.BoardAtlas(StadiumStyle.DefaultBoards(new[] { "Aurum" }));
            File.WriteAllBytes(Path.Combine(dir, "placas.png"), atlas.EncodeToPNG());
            ExportSounds(Path.Combine(dir, "sons"));
            CaptureMenus(dir);
            // vitrine dos troféus 3D numa imagem só
            {
                string[] kinds = { "copa_mundo", "brasileirao", "premier", "laliga", "seriea", "ligue1", "mls", "copa", "bola_ouro", "chuteira", "estrela", "ouro_alcas" };
                var sheet = new Texture2D(256 * 6, 256 * 2, TextureFormat.RGBA32, false);
                var bg = new Color32[sheet.width * sheet.height];
                for (int i = 0; i < bg.Length; i++) bg[i] = new Color32(18, 26, 48, 255);
                sheet.SetPixels32(bg);
                for (int i = 0; i < kinds.Length; i++)
                {
                    var sp = TrophyStudio.Get(kinds[i]);
                    if (sp == null) continue;
                    var rt = RenderTexture.GetTemporary(256, 256, 0);
                    Graphics.Blit(sp.texture, rt);
                    var prevA = RenderTexture.active; RenderTexture.active = rt;
                    var cell = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                    cell.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); cell.Apply();
                    RenderTexture.active = prevA; RenderTexture.ReleaseTemporary(rt);
                    var px = cell.GetPixels();
                    int ox = (i % 6) * 256, oy = (1 - i / 6) * 256;
                    for (int y = 0; y < 256; y++)
                        for (int x = 0; x < 256; x++)
                        {
                            var c = px[y * 256 + x];
                            var b0 = sheet.GetPixel(ox + x, oy + y);
                            sheet.SetPixel(ox + x, oy + y, Color.Lerp(b0, new Color(c.r, c.g, c.b, 1), c.a));
                        }
                    Object.DestroyImmediate(cell);
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(dir, "trofeus.png"), sheet.EncodeToPNG());
            }
            foreach (var (nm, st) in new[] { ("volt", new BootStyle { c1 = "#F2C230", c2 = "#111111", sole = "#FFFFFF" }), ("preta", new BootStyle { c1 = "#111111", c2 = "#FFFFFF", sole = "#222222" }), ("kairos", new BootStyle { c1 = "#00ACC1", c2 = "#0D47A1", sole = "#B0BEC5" }) })
            {
                var ph = BootModel.Photo(st);
                if (ph != null) SaveTex(ph.texture, Path.Combine(dir, "chuteira-" + nm + ".png"));
            }
            SaveTex(BootModel.Texture(new BootStyle { c1 = "#F2C230", c2 = "#111111", sole = "#FFFFFF" }), Path.Combine(dir, "chuteira-textura.png"));
            SaveTex(KitArt.Shirt(Kit.For("Palmeiras", "#006437", "#FFFFFF"), 27), Path.Combine(dir, "camisa-palmeiras.png"));
            SaveTex(KitArt.Shirt(Kit.For("Vasco da Gama", "#111111", "#FFFFFF"), 10), Path.Combine(dir, "camisa-vasco.png"));
            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous);
            return dir;
        }

        /// <summary>Fotografa as abas do menu com uma carreira de exemplo (a interface é desenhada numa câmera temporária).</summary>
        static void CaptureMenus(string dir)
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            var go = new GameObject("Camisa10Menus");
            var app = go.AddComponent<GameApp>();
            var t = typeof(GameApp);
            t.GetMethod("BuildShell", flags).Invoke(app, null);
            Rng.Seed(3);
            var game = Camisa10.Core.Game.NewCareer("Gabriel Souza", "ATA", new[] { 4, 4, 3, 3, 3, 3 });
            PlayRounds(game, 9); // tabela, artilharia e caixa de mensagens com cara de meio de temporada
            game.S.owned.Add("carro1"); // mostra um item comprado na vitrine
            game.S.traits.AddRange(new[] { "finalizador", "velocista" }); game.S.captain = true; // selos na carta
            game.StartDating(); game.S.love.stage = 2; game.S.love.affection = 72; // vida pessoal preenchida
            game.S.rival.goals = 3; game.S.season.stats.goals = 4;
            typeof(Camisa10.Core.Game).GetMethod("PayWeek", flags).Invoke(game, new object[] { 1 }); // extrato de uma rodada com gol
            game.S.titles.AddRange(new[] { "Campeão do Brasileirão 2026", "Campeão da Copa do Brasil 2026", "Campeão da Copa do Mundo 2026", "Campeão da Premier League 2028" });
            game.S.awards.AddRange(new[] { "Bola de Ouro 2027", "Artilheiro do Brasileirão 2026 (21 gols)", "Craque do Brasileirão 2026" });
            game.Mail("elenco", "Arrascaeta", "Churrasco no sábado", "Fala, Gabriel! Vai ter churrasco lá em casa depois do jogo. Cola?", "churrasco", null, "Tô dentro!", "Dessa vez não vai dar");
            game.Mail("patrocinador", "Volt", "Campanha nas redes", "Lançamos uma campanha nova e queremos um post seu. Cachê de R$ 900.", "patrocinador_post", "900", "Postar", "Agora não");
            t.GetField("game", flags).SetValue(app, game);

            var cam = new GameObject("CamMenus").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            var rt = new RenderTexture(W, H, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            var canvas = go.GetComponentInChildren<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1;
            var scaler = go.GetComponentInChildren<UnityEngine.UI.CanvasScaler>();

            var scroll = (UnityEngine.UI.ScrollRect)t.GetField("scroll", flags).GetValue(app);
            foreach (var shot in new[] { "home", "home-baixo", "inbox", "player", "life", "life-baixo", "agenda", "settings", "trophies", "posjogo", "coletiva", "fim" })
            {
                string tab = shot.Replace("-baixo", "");
                if (shot == "posjogo" || shot == "coletiva" || shot == "fim") tab = "home";
                if (shot == "fim")
                {
                    // fecha o pós-jogo e joga até o fim da temporada: balanço, objetivos e propostas
                    t.GetMethod("CloseModal", flags).Invoke(app, null);
                    var pr = (GameObject)t.GetField("postRoot", flags).GetValue(app);
                    if (pr != null) Object.DestroyImmediate(pr);
                    PlayRounds(game, 99);
                }
                t.GetField("tab", flags).SetValue(app, tab);
                if (shot != "coletiva") t.GetMethod("Render", flags).Invoke(app, new object[] { true });
                if (shot == "posjogo")
                {
                    var snap = t.GetMethod("TakeSnap", flags).Invoke(app, null);
                    var m = PlayMatch(game);
                    game.FinishMatch(m);
                    t.GetMethod("Render", flags).Invoke(app, new object[] { true });
                    t.GetMethod("ShowPostMatch", flags).Invoke(app, new object[] { m, snap, null });
                }
                if (shot == "coletiva")
                {
                    var m = (MatchEngine)t.GetField("postMatch", flags).GetValue(app);
                    t.GetMethod("PressStep", flags).Invoke(app, new object[] { PressConference.Build(game, m), 0 });
                }
                if (shot.EndsWith("-baixo"))
                {
                    Canvas.ForceUpdateCanvases();
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
                    scroll.verticalNormalizedPosition = 0;
                }
                for (int i = 0; i < 3; i++)
                {
                    typeof(UnityEngine.UI.CanvasScaler).GetMethod("Handle", flags).Invoke(scaler, null);
                    Canvas.ForceUpdateCanvases();
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
                }
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(dir, "menu-" + shot + ".png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(go);
        }

        /// <summary>Joga rodadas com resultados sorteados (treino, lances e eventos), para as fotos terem uma carreira de verdade.</summary>
        static void PlayRounds(Camisa10.Core.Game g, int rounds)
        {
            for (int i = 0; i < rounds && g.S.season.phase != "end"; i++) g.FinishMatch(PlayMatch(g));
        }

        /// <summary>Treina e joga uma partida até o apito final (sem registrar na tabela).</summary>
        static MatchEngine PlayMatch(Camisa10.Core.Game g)
        {
            if (g.S.player.injury > 0) g.Physio(); else g.Train(Camisa10.Core.Game.MainAttr(g.S.player.pos), "normal", out _);
            var m = new MatchEngine(g);
            int guard = 0;
            while (!m.Done && guard++ < 200)
            {
                if (m.Step() != StepResult.AwaitChoice || m.Current == null) continue;
                if (Rng.Chance(.5)) m.Score("Drible", MatchEngine.Pts.Dribble);
                if (Rng.Chance(.3)) m.Score("Passe certo", MatchEngine.Pts.Pass);
                m.ResolveLive(Rng.Chance(.35) ? LiveOutcome.Goal : Rng.Chance(.3) ? LiveOutcome.Assist : LiveOutcome.Saved);
            }
            return m;
        }

        /// <summary>Salva qualquer textura (mesmo sem cópia na CPU) em PNG.</summary>
        static void SaveTex(Texture t, string path)
        {
            var rt = RenderTexture.GetTemporary(t.width, t.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(t, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(t.width, t.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, t.width, t.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>Salva os sons sintetizados em WAV, para ouvir fora do jogo.</summary>
        static void ExportSounds(string dir)
        {
            Directory.CreateDirectory(dir);
            foreach (var name in new[] { "crowd", "roar", "applause", "ooh", "groan", "kick", "post", "net", "whistle" })
            {
                var clip = Sfx.Clip(name);
                var data = new float[clip.samples * clip.channels];
                clip.GetData(data, 0);
                using (var w = new BinaryWriter(File.Create(Path.Combine(dir, name + ".wav"))))
                {
                    int bytes = data.Length * 2;
                    w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
                    w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)clip.channels);
                    w.Write(clip.frequency); w.Write(clip.frequency * clip.channels * 2); w.Write((short)(clip.channels * 2)); w.Write((short)16);
                    w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                    foreach (var v in data) w.Write((short)(Mathf.Clamp(v, -1, 1) * 32767));
                }
            }
        }

        static string Capture(bool night)
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Capturas"));
            Directory.CreateDirectory(dir);

            var root = new GameObject("Preview").transform;
            var a = Arena.Build(root, new StadiumStyle { Night = night, Boards = StadiumStyle.DefaultBoards(new[] { "Aurum" }) });
            a.Ball.isKinematic = true;
            a.Ball.transform.position = new Vector3(4, Arena.BallRadius, -22);

            var keeper = a.Person("Goleiro", Kit.Goalkeeper(), new Vector3(-.6f, 0, -.6f), 180);
            keeper.GetComponent<PersonRig>().Set(PersonRig.Mode.Ready);
            var ball = a.Ball.transform.position;
            Vector3 toGoal = (Vector3.zero - ball).normalized, perp = new Vector3(toGoal.z, 0, -toGoal.x), wallC = ball + toGoal * 9.15f;
            for (int i = 0; i < 4; i++)
                a.Person("Barreira", Kit.For("Vasco da Gama", "#111111", "#FFFFFF"), wallC + perp * ((i - 1.5f) * .55f) + new Vector3(.6f, 0, 0), 180);
            var mate = a.Person("Companheiro", Kit.For("Flamengo", "#C8102E", "#111111"), new Vector3(-6, 0, -15), 20);

            // vitrine de uniformes: de frente e de costas, lado a lado
            string[] clubs = { "Botafogo", "Palmeiras", "São Paulo", "Grêmio", "Fluminense", "Barcelona", "Arsenal", "Real Madrid" };
            for (int i = 0; i < clubs.Length; i++)
            {
                var k = Kit.For(clubs[i], "#888888", "#FFFFFF");
                a.Person(clubs[i], k, new Vector3(-7 + i * 2f, 0, -60), i % 2 == 0 ? 180 : 0);
            }
            // goleiro em pleno mergulho (pose montada à mão, só para a foto)
            var diver = a.Person("Mergulho", Kit.Goalkeeper("#FF7A1A"), new Vector3(10, .35f, -60), 180);
            diver.rotation = Quaternion.LookRotation(Vector3.back) * Quaternion.Euler(0, 0, 70);
            diver.GetComponent<PersonRig>().Set(PersonRig.Mode.Dive);
            mate.GetComponent<PersonRig>().Set(PersonRig.Mode.Run, 6);
            // comemorações lado a lado (de frente para a câmera)
            string[] gestures = { "aviao", "joelhada", "soco", "silencio", "coracao", "danca", "abraco" };
            for (int i = 0; i < gestures.Length; i++)
            {
                var c = a.Person("Comemora-" + gestures[i], Kit.For("Flamengo", "#C8102E", "#111111"), new Vector3(-9 + i * 3f, 0, -75), 180,
                    new BootStyle { c1 = "#F2C230", c2 = "#111111", sole = "#FFFFFF" }, 10);
                var cr = c.GetComponent<PersonRig>();
                cr.Set(PersonRig.Mode.Pose); cr.Gesture = gestures[i];
            }

            foreach (var rig in Object.FindObjectsByType<PersonRig>())
                for (int i = 0; i < 40; i++) rig.Tick(1 / 30f);

            var rt = new RenderTexture(W, H, 24) { antiAliasing = 4 };
            a.Cam.targetTexture = rt;
            a.Cam.rect = new Rect(0, 0, 1, 1);
            a.Cam.aspect = W / (float)H;

            void Shot(string name, Vector3 eye, Vector3 look)
            {
                a.PlaceCamera(eye, look);
                a.Cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(dir, name + (night ? "-noite" : "-dia") + ".png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }

            Shot("1-falta", ball + new Vector3(0, 1.6f, -2.4f), new Vector3(0, .9f, 0));
            Shot("2-area", new Vector3(-3, 1.65f, -12), new Vector3(0, 1f, 0));
            Shot("3-estadio", new Vector3(14, 1.7f, -30), new Vector3(-10, 4f, 0));
            Shot("4-jogador", mate.position + new Vector3(1.6f, 1.4f, 2.2f), mate.position + Vector3.up * 1f);
            Shot("5-uniformes", new Vector3(0, 1.5f, -66.5f), new Vector3(0, 1.1f, -60));
            // goleiro com o código do jogo: posicionado e no meio do mergulho (relógio avançado na mão)
            {
                float clock = 0;
                Goalkeeper.Now = () => clock;
                var gkT = a.Person("GoleiroTeste", Kit.Goalkeeper("#FF7A1A"), new Vector3(20, 0, -40.7f), 180);
                var gkRig = gkT.GetComponent<PersonRig>();
                var gkB = new Goalkeeper(gkT, .55f);
                var fakeBall = new Vector3(20, 0, -58);
                for (int i = 0; i < 30; i++) { clock += 1 / 30f; gkB.Position(fakeBall, 1 / 30f, .7f); gkRig.Tick(1 / 30f); }
                Shot("10-goleiro-pronto", new Vector3(20.6f, 1.3f, -44f), new Vector3(20, 1f, -40.7f));
                gkB.OnShot(fakeBall, new Vector3(2.4f, 1.2f, 22f), 0, false);
                for (int i = 0; i < 21; i++) { clock += 1 / 30f; gkB.Tick(1 / 30f); gkRig.Tick(1 / 30f); }
                Shot("11-goleiro-mergulho", new Vector3(20.5f, 1.3f, -45f), new Vector3(21.2f, 1f, -40.7f));
                Goalkeeper.Now = () => Time.time;
            }
            Shot("9-rosto", new Vector3(-5.05f, 1.68f, -59.35f), new Vector3(-5f, 1.66f, -60f));
            Shot("5b-uniformes-perto", new Vector3(-4, 1.4f, -62.6f), new Vector3(-4, 1.15f, -60));
            Shot("13-comemoracoes", new Vector3(0, 1.3f, -86f), new Vector3(0, .9f, -75f));
            Shot("12-chuteira-pe", new Vector3(-6.45f, .32f, -61.0f), new Vector3(-7f, .07f, -60f));
            Shot("12b-chuteira-lado", new Vector3(-5.9f, .25f, -60.1f), new Vector3(-6.9f, .07f, -60f));
            // rede estufada com a bola lá dentro e torcida comemorando (aplica um quadro da animação na mão)
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            if (a.NearNet != null) { a.NearNet.Hold(new Vector3(1.2f, 1.1f, 2f), .5f); typeof(GoalNet).GetMethod("Update", flags).Invoke(a.NearNet, null); }
            if (a.Crowd != null) { a.Crowd.Excite(1f, 10f); for (int i = 0; i < 40; i++) typeof(CrowdMotion).GetMethod("Update", flags).Invoke(a.Crowd, null); }
            a.Ball.transform.position = new Vector3(1.2f, 1.1f, 2.3f);
            Shot("8-gol-torcida", new Vector3(-4f, 1.7f, -9f), new Vector3(0, 1.8f, 4f));
            a.Ball.transform.position = new Vector3(-1.2f, Arena.BallRadius, -3.2f);
            Shot("7-rede-bola", new Vector3(-1.6f, .55f, -4.4f), new Vector3(.8f, 1.1f, 1.5f));
            a.Ball.transform.position = ball;
            Shot("6-mergulho", new Vector3(10, 1.4f, -64.5f), new Vector3(9.4f, .9f, -60));

            a.Cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(root.gameObject);
            Debug.Log("[Camisa 10] Capturas salvas em " + dir);
            return dir;
        }
    }
}
