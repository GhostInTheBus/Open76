using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Menus
{
    // Lets a menu button be lowered with a right-click or raised/lowered with the arrow keys.
    public class MenuButtonAdjust : MonoBehaviour, IPointerClickHandler
    {
        private Action<int> _onAdjust;

        public void Setup(Action<int> onAdjust)
        {
            _onAdjust = onAdjust;
        }

        public void Invoke(int delta)
        {
            if (_onAdjust != null)
            {
                _onAdjust(delta);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                Invoke(-1);
            }
        }
    }
}
