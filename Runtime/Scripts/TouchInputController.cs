using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Elysium.Input.Mobile
{
    [RequireComponent(typeof(GestureRecognizer))]
    public class TouchInputController : MonoBehaviour
    {
        // Output Events
        public event Action<Vector3> OnSingleTap;
        public event Action<Vector3> OnDoubleTap;

        #region INITIALIZE
        [Header("Raw Input")]
        [SerializeField] public GestureRecognizer gestureRecognizer;
        private bool isInitialized = false;

        private void Start()
        {
            Init();
        }

        private void Init()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;

            gestureRecognizer.Tapped += SingleTap;
            gestureRecognizer.DoubleTapped += DoubleTap;

            Debug.Log("TouchInputController Initialized");
        }

        private void OnValidate()
        {
            gestureRecognizer = gestureRecognizer ?? GetComponent<GestureRecognizer>();
        }
        #endregion

        #region SINGLE_TAP
        void SingleTap(Vector2 pos)
        {
            Debug.Log($"Single Tap: {pos}");
            OnSingleTap?.Invoke(pos);
        }
        #endregion

        #region DOUBLE_TAP
        void DoubleTap(Vector2 pos)
        {
            Debug.Log($"Double Tap: {pos}");
            OnDoubleTap?.Invoke(pos);
        }
        #endregion

    }
}