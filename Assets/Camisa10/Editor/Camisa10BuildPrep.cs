using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Camisa10.EditorTools
{
    /// <summary>
    /// Deixa o projeto pronto para build de celular. Como tudo é criado por código e a cena é vazia, a Unity
    /// não "vê" o que o jogo usa e corta demais no build: componentes somem ("Can't add component CapsuleCollider")
    /// e o shader dos materiais some (tela rosa). Roda sozinho antes de qualquer build e ao abrir o projeto.
    /// </summary>
    public class Camisa10BuildPrep : IPreprocessBuildWithReport
    {
        public const string MaterialPath = "Assets/Camisa10/Resources/Materiais/Padrao.mat";

        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => Prepare();

        public static void Prepare()
        {
            // 1) não remover código da engine (os colisores das primitivas vêm de CreatePrimitive, invisível para o corte)
            PlayerSettings.stripEngineCode = false;
            foreach (var t in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS })
                PlayerSettings.SetManagedStrippingLevel(t, ManagedStrippingLevel.Minimal);

            // 2) material âncora em Resources: obriga o shader Standard a entrar no build; Arena.Mat copia dele
            if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) == null)
            {
                var std = Shader.Find("Standard");
                if (std != null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
                    var m = new Material(std) { name = "Padrao" };
                    m.SetFloat("_Glossiness", .15f);
                    AssetDatabase.CreateAsset(m, MaterialPath);
                    AssetDatabase.SaveAssets();
                }
            }

            // 3) neblina: a cena não tem neblina, então o modo automático cortaria as variações que o estádio usa
            var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (gs != null)
            {
                var so = new SerializedObject(gs);
                bool changed = false;
                void Set(string prop, int v)
                {
                    var p = so.FindProperty(prop);
                    if (p != null && p.intValue != v) { p.intValue = v; changed = true; }
                }
                Set("m_FogStripping", 1); // personalizado
                Set("m_FogKeepLinear", 1);
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
