using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>Encaixa a interface na faixa em paisagem e, opcionalmente, fora do notch e da barra de gestos.</summary>
    public class SafeArea : MonoBehaviour
    {
        public bool insets = true;
        RectTransform rt;
        Rect lastSafe;
        Vector2Int lastScreen;

        void Awake() { rt = (RectTransform)transform; Apply(); }

        void Update()
        {
            if (Screen.safeArea != lastSafe || Screen.width != lastScreen.x || Screen.height != lastScreen.y) Apply();
        }

        public void Apply()
        {
            if (rt == null) rt = (RectTransform)transform;
            lastSafe = Screen.safeArea;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var v = Landscape.Viewport01;
            Vector2 min = new Vector2(v.xMin, v.yMin), max = new Vector2(v.xMax, v.yMax);
            if (insets)
            {
                var sa = lastSafe;
                min = Vector2.Max(min, new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height));
                max = Vector2.Min(max, new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height));
            }
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
