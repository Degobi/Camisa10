using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Camisa10.EditorTools
{
    /// <summary>Menu "Camisa 10" no editor com a configuração de build para celular.</summary>
    public static class Camisa10Setup
    {
        const string BundleId = "com.camisa10.carreira";

        [MenuItem("Camisa 10/Configurar build Android")]
        public static void ConfigureAndroid()
        {
            ApplyCommon();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64; // exigido pela Play Store
            bool ok = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUtility.DisplayDialog("Camisa 10", ok
                ? "Android configurado: paisagem, IL2CPP e ARM64. Use File > Build Profiles (ou Build Settings) para gerar o APK/AAB."
                : "Configurações aplicadas, mas não foi possível trocar a plataforma. Instale o módulo Android Build Support pelo Unity Hub.", "OK");
        }

        /// <summary>
        /// Gera Builds/Camisa10.apk pronto para instalar no celular (build de desenvolvimento: mostra erros no logcat).
        /// Também roda em linha de comando: Unity -batchmode -projectPath . -executeMethod Camisa10.EditorTools.Camisa10Setup.BuildApkBatch -quit
        /// </summary>
        [MenuItem("Camisa 10/Gerar APK de teste")]
        public static void BuildApkMenu()
        {
            string result = BuildApk();
            if (result == null) EditorUtility.RevealInFinder(ApkPath);
            else EditorUtility.DisplayDialog("Camisa 10", result, "OK");
        }

        public static void BuildApkBatch()
        {
            string result = BuildApk();
            if (result != null) Debug.LogError("[Camisa 10] " + result);
            EditorApplication.Exit(result == null ? 0 : 1);
        }

        static string ApkPath => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Builds", "Camisa10.apk"));

        /// <summary>Devolve null se deu certo, ou a mensagem do problema.</summary>
        static string BuildApk()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                return "O módulo Android não está instalado. No Unity Hub: Installs > (sua versão) > Add modules > Android Build Support (com OpenJDK e Android SDK & NDK Tools).";
            ApplyCommon();
            Camisa10BuildPrep.Prepare();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false; // APK para instalar direto (AAB é só para a Play Store)
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                return "Não foi possível trocar a plataforma para Android.";

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ApkPath));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Camisa10/Scenes/Main.unity" },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development,
            });
            var s = report.summary;
            if (s.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[Camisa 10] APK gerado: {ApkPath} ({s.totalSize / 1048576f:0.0} MB)");
                return null;
            }
            return $"O build falhou ({s.totalErrors} erro(s)). Veja o Console para os detalhes.";
        }

        [MenuItem("Camisa 10/Configurar build iOS")]
        public static void ConfigureIOS()
        {
            ApplyCommon();
            bool ok = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            EditorUtility.DisplayDialog("Camisa 10", ok
                ? "iOS configurado. O build gera um projeto Xcode; abra no Mac para assinar e instalar."
                : "Configurações aplicadas, mas não foi possível trocar a plataforma. Instale o módulo iOS Build Support pelo Unity Hub.", "OK");
        }

        [MenuItem("Camisa 10/Apagar carreira salva")]
        public static void DeleteSave()
        {
            Camisa10.UI.SaveSystem.Delete(); // apaga também a cópia de segurança
            EditorUtility.DisplayDialog("Camisa 10", "Carreira salva apagada. O próximo Play começa do zero.", "OK");
        }

        static void ApplyCommon()
        {
            PlayerSettings.productName = "Camisa 10";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            Camisa10AutoSetup.ApplyLandscape();
        }
    }
}
