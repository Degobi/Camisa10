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
            string path = System.IO.Path.Combine(Application.persistentDataPath, "camisa10.json");
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
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
