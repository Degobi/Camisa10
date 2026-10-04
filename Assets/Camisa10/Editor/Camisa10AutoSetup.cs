using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Camisa10.EditorTools
{
    /// <summary>
    /// Na primeira abertura: cria a cena principal, coloca na lista de build, trava o app em paisagem
    /// e deixa a aba Game em 1920x1080. Nas próximas aberturas só confere se está tudo no lugar.
    /// </summary>
    [InitializeOnLoad]
    public static class Camisa10AutoSetup
    {
        const string ScenePath = "Assets/Camisa10/Scenes/Main.unity";
        const string GameViewFlag = "Camisa10.GameViewPaisagem";

        static Camisa10AutoSetup() { EditorApplication.delayCall += Run; }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
            {
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                    .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            }

            if (string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene(ScenePath);

            // o jogo é só em paisagem (celular deitado, nos dois sentidos)
            ApplyLandscape();
            Camisa10BuildPrep.Prepare(); // evita tela rosa e componentes cortados no build

            if (!EditorPrefs.GetBool(GameViewFlag, false))
            {
                if (TrySetLandscapeGameView()) EditorPrefs.SetBool(GameViewFlag, true);
            }
        }

        public static void ApplyLandscape()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }

        /// <summary>Cria e seleciona a resolução 1920x1080 na aba Game (API interna da Unity, por isso protegida).</summary>
        [MenuItem("Camisa 10/Aba Game em paisagem (1920x1080)")]
        public static void SetLandscapeGameViewMenu()
        {
            if (!TrySetLandscapeGameView())
                EditorUtility.DisplayDialog("Camisa 10", "Não consegui ajustar automaticamente. Na aba Game, clique no seletor de resolução, use + e crie 1920x1080.", "OK");
        }

        static bool TrySetLandscapeGameView()
        {
            try
            {
                var asm = typeof(Editor).Assembly;
                var sizesType = asm.GetType("UnityEditor.GameViewSizes");
                var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instance = singleton.GetProperty("instance").GetValue(null, null);
                var groupType = sizesType.GetProperty("currentGroupType").GetValue(instance, null);
                var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { groupType });
                var gt = group.GetType();
                var texts = (string[])gt.GetMethod("GetDisplayTexts").Invoke(group, null);
                int idx = Array.FindIndex(texts, t => t.StartsWith("Camisa 10 paisagem"));
                if (idx < 0)
                {
                    var sizeType = asm.GetType("UnityEditor.GameViewSize");
                    var kindType = asm.GetType("UnityEditor.GameViewSizeType");
                    var ctor = sizeType.GetConstructor(new[] { kindType, typeof(int), typeof(int), typeof(string) });
                    var size = ctor.Invoke(new object[] { Enum.ToObject(kindType, 1), 1920, 1080, "Camisa 10 paisagem" });
                    gt.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                    idx = (int)gt.GetMethod("GetTotalCount").Invoke(group, null) - 1;
                }
                var gameViewType = asm.GetType("UnityEditor.GameView");
                var window = EditorWindow.GetWindow(gameViewType);
                var prop = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop == null) return false;
                prop.SetValue(window, idx, null);
                window.Repaint();
                return true;
            }
            catch (Exception e)
            {
                Debug.Log("Camisa 10: ajuste a aba Game para 1920x1080 manualmente. (" + e.Message + ")");
                return false;
            }
        }
    }
}
