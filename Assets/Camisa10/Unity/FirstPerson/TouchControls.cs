using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Camisa10.UI
{
    /// <summary>Joystick virtual: arraste o dedo dentro do círculo. Value vai de (-1,-1) a (1,1).</summary>
    public class VirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Value;
        public RectTransform Knob;
        RectTransform rt;
        Camera eventCam;

        void Awake() => rt = (RectTransform)transform;

        public void OnPointerDown(PointerEventData e) { eventCam = e.pressEventCamera; OnDrag(e); }

        public void OnDrag(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, eventCam, out var local)) return;
            float radius = rt.rect.width * .5f;
            var v = Vector2.ClampMagnitude(local / radius, 1f);
            Value = v.magnitude < .12f ? Vector2.zero : v;
            if (Knob != null) Knob.anchoredPosition = v * radius * .55f;
        }

        public void OnPointerUp(PointerEventData e)
        {
            Value = Vector2.zero;
            if (Knob != null) Knob.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>Botão de toque: dispara ao encostar (sem esperar soltar) e informa se está pressionado.</summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action OnPress;
        public bool Held;
        Graphic g;
        Color baseColor;

        void Awake() { g = GetComponent<Graphic>(); if (g != null) baseColor = g.color; }

        public void OnPointerDown(PointerEventData e)
        {
            Held = true;
            if (g != null) g.color = Color.Lerp(baseColor, Color.white, .35f);
            OnPress?.Invoke();
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        void Release()
        {
            Held = false;
            if (g != null) g.color = baseColor;
        }
    }
}
