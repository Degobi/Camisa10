using System.IO;
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
            var game = Camisa10.Core.Game.NewCareer("Gabriel Souza", "ATA", new[] { 4, 4, 3, 3, 3, 3 });
            game.S.owned.Add("carro1"); // mostra um item comprado na vitrine
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

            foreach (var tab in new[] { "home", "player", "life", "settings" })
            {
                t.GetField("tab", flags).SetValue(app, tab);
                t.GetMethod("Render", flags).Invoke(app, new object[] { true });
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
                File.WriteAllBytes(Path.Combine(dir, "menu-" + tab + ".png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(go);
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

            Color red = Theme.Hex("#C8102E"), white = Color.white, navy = Theme.Hex("#0B1F4B");
            var keeper = a.Person("Goleiro", Theme.Hex("#C6E03A"), Theme.Hex("#222222"), new Vector3(-.6f, 0, -.6f), 180, true);
            keeper.GetComponent<PersonRig>().Set(PersonRig.Mode.Ready);
            var ball = a.Ball.transform.position;
            Vector3 toGoal = (Vector3.zero - ball).normalized, perp = new Vector3(toGoal.z, 0, -toGoal.x), wallC = ball + toGoal * 9.15f;
            for (int i = 0; i < 4; i++)
                a.Person("Barreira", navy, white, wallC + perp * ((i - 1.5f) * .55f) + new Vector3(.6f, 0, 0), 180);
            var mate = a.Person("Companheiro", red, white, new Vector3(-6, 0, -15), 20);
            mate.GetComponent<PersonRig>().Set(PersonRig.Mode.Run, 6);

            foreach (var rig in Object.FindObjectsByType<PersonRig>())
                for (int i = 0; i < 12; i++) rig.Tick(1 / 30f);

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

            a.Cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(root.gameObject);
            Debug.Log("[Camisa 10] Capturas salvas em " + dir);
            return dir;
        }
    }
}
