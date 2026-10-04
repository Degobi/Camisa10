using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Preferências do aparelho (não fazem parte da carreira): qualidade gráfica, som e vibração.
    /// Ficam no PlayerPrefs, então continuam valendo mesmo se a carreira for apagada ou, no futuro, vier do servidor.
    /// </summary>
    public static class GameSettings
    {
        public enum Level { Auto = 0, Low = 1, Medium = 2, High = 3 }

        const string KQuality = "c10.quality", KSound = "c10.sound", KVibration = "c10.vibration";

        public static Level Quality
        {
            get => (Level)PlayerPrefs.GetInt(KQuality, 0);
            set { PlayerPrefs.SetInt(KQuality, (int)value); PlayerPrefs.Save(); Apply(); }
        }

        public static bool Sound
        {
            get => PlayerPrefs.GetInt(KSound, 1) == 1;
            set { PlayerPrefs.SetInt(KSound, value ? 1 : 0); PlayerPrefs.Save(); AudioListener.volume = value ? 1 : 0; }
        }

        public static bool Vibration
        {
            get => PlayerPrefs.GetInt(KVibration, 1) == 1;
            set { PlayerPrefs.SetInt(KVibration, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Qualidade em uso: no automático, decide pela memória do aparelho.</summary>
        public static Level Effective
        {
            get
            {
                var q = Quality;
                if (q != Level.Auto) return q;
                if (!Application.isMobilePlatform) return Level.High;
                int ram = SystemInfo.systemMemorySize, cores = SystemInfo.processorCount;
                if (ram >= 6000 && cores >= 8) return Level.High;
                if (ram >= 3500) return Level.Medium;
                return Level.Low;
            }
        }

        public static string Label(Level l)
        {
            switch (l)
            {
                case Level.Low: return "Leve";
                case Level.Medium: return "Equilibrada";
                case Level.High: return "Máxima";
                default: return "Automática";
            }
        }

        public static bool Shadows => Effective != Level.Low;
        public static int AntiAliasing => Effective == Level.High ? 4 : Effective == Level.Medium ? 2 : 0;
        /// <summary>Fração da resolução da tela usada no lance 3D (a interface continua nítida).</summary>
        public static float RenderScale => Effective == Level.High ? 1f : Effective == Level.Medium ? .85f : .7f;

        /// <summary>Aplica o nível de qualidade da Unity correspondente (definidos em ProjectSettings/QualitySettings).</summary>
        public static void Apply()
        {
            int index = Effective == Level.High ? 4 : Effective == Level.Medium ? 3 : 1; // Very High, High, Low
            // no editor não troca: mudaria o nível salvo nas configurações do projeto
            if (!Application.isEditor && index < QualitySettings.names.Length) QualitySettings.SetQualityLevel(index, true);
            AudioListener.volume = Sound ? 1 : 0;
        }

        /// <summary>Vibração curta (gol, trave), se o jogador deixou ligada.</summary>
        public static void Buzz()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Vibration) Handheld.Vibrate();
#endif
        }
    }
}
