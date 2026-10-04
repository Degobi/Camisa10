using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// O jogo é sempre em paisagem (celular deitado). Em telas mais altas que 4:3
    /// (editor em pé, celular em retrato) o jogo é desenhado numa faixa 16:9 centralizada.
    /// </summary>
    public static class Landscape
    {
        public const float Aspect = 16f / 9f;
        public const float MinAspect = 4f / 3f;

        public static float ScreenAspect => Screen.height > 0 ? (float)Screen.width / Screen.height : Aspect;

        public static bool Letterboxed => ScreenAspect < MinAspect - .01f;

        /// <summary>Área do jogo em coordenadas normalizadas da tela (0..1).</summary>
        public static Rect Viewport01
        {
            get
            {
                if (!Letterboxed) return new Rect(0, 0, 1, 1);
                float h = Screen.width / Aspect / Screen.height;
                return new Rect(0, (1 - h) / 2f, 1, h);
            }
        }

        public static float GameAspect => Letterboxed ? Aspect : ScreenAspect;

        /// <summary>Telas largas casam pela altura (sobra espaço dos lados); tablets 4:3 casam pela largura.</summary>
        public static float CanvasMatch => GameAspect >= Aspect - .01f ? 1f : 0f;

        /// <summary>Trava o aparelho deitado, nos dois sentidos.</summary>
        public static void LockOrientation()
        {
            if (!Application.isMobilePlatform) return;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}
