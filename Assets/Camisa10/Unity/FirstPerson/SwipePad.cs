using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Camisa10.UI
{
    public struct SwipeData
    {
        public Vector2 Start, End;
        public float Duration, Length;
        /// <summary>Quanto o traço se curvou (pixels). Positivo = barriga para a esquerda do traço.</summary>
        public float Bend;
    }

    /// <summary>
    /// Captura o gesto de swipe pela própria UI (EventSystem), então funciona
    /// com o Input Manager antigo e com o Input System novo, no toque e no mouse.
    /// </summary>
    public class SwipePad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Action<SwipeData> OnSwipe;
        readonly List<Vector2> points = new List<Vector2>();
        float startTime;
        bool down;

        public void OnPointerDown(PointerEventData e)
        {
            points.Clear();
            points.Add(e.position);
            startTime = Time.unscaledTime;
            down = true;
        }

        public void OnDrag(PointerEventData e)
        {
            if (down) points.Add(e.position);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!down) return;
            down = false;
            points.Add(e.position);
            Vector2 a = points[0], b = points[points.Count - 1];
            float length = 0;
            for (int i = 1; i < points.Count; i++) length += Vector2.Distance(points[i - 1], points[i]);
            if (length < 25f) return; // toque, não swipe

            Vector2 chord = b - a;
            float chordLen = Mathf.Max(1f, chord.magnitude), bend = 0;
            foreach (var p in points)
            {
                float cross = (chord.x * (p.y - a.y) - chord.y * (p.x - a.x)) / chordLen;
                if (Mathf.Abs(cross) > Mathf.Abs(bend)) bend = cross;
            }
            OnSwipe?.Invoke(new SwipeData { Start = a, End = b, Duration = Mathf.Max(.03f, Time.unscaledTime - startTime), Length = length, Bend = bend });
        }
    }
}
