using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>Sobe o jogo automaticamente em qualquer cena: não precisa montar nada no editor.</summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindFirstObjectByType<GameApp>() != null) return;
            var go = new GameObject("Camisa10");
            go.AddComponent<GameApp>();
            Object.DontDestroyOnLoad(go);
        }
    }
}
