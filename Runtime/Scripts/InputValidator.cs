using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Elysium.Input.Mobile
{
    [RequireComponent(typeof(GestureRecognizer))]
    public class InputValidator : MonoBehaviour
    {
        #region SINGLETON
        static InputValidator instance;
        public static InputValidator Instance { get { return instance; } }

        private void Awake()
        {
            if (instance != null)
                throw new Exception("Multiple Singletons in game!");
            instance = this;
        }
        #endregion

        #region VALIDATION_AREA
        [Header("Area Setup")]
        [SerializeField] bool isFullScreen = true;
        [SerializeField] Rect swipeArea;
        [SerializeField]
        Rect SwipeArea
        {
            get
            {
                if (isFullScreen)
                {
                    transform.position = Vector3.zero;
                    Rect rect = new Rect(0, 0, Screen.currentResolution.height, Screen.currentResolution.width);
                    return rect;
                }
                else
                {
                    return swipeArea;
                }
            }
        }

        public bool ValidatePosition(Vector2 P)
        {
            return SwipeArea.Contains(P - (Vector2)transform.position);
        }
        #endregion

        #region GIZMOS
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(255, 0, 0, 0.2f);
            Gizmos.DrawCube(new Vector3(SwipeArea.center.x, SwipeArea.center.y) + transform.position, new Vector3(SwipeArea.width, SwipeArea.height, 1));
        }
        #endregion
    }
}