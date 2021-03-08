using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Elysium.Input.Mobile
{
    [RequireComponent(typeof(GestureRecognizer))]
    public class SwipeInputController : MonoBehaviour
    {
        // Output Events
        public event Action<Vector2, Vector2, Quadrant> OnSingleSwipe;
        public event Action<Vector2, Vector2, Quadrant> OnDoubleSwipe;

        // Touch Phase Events
        public event Action<Vector2, Vector2, TouchPhase> OnSingleSwipeDetails;
        public event Action<Vector2, Vector2, TouchPhase> OnDoubleSwipeDetails;
        private event Action OnAnyInput;

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
            Debug.Log("SwipeInputController Initialized");
        }

        private void OnValidate()
        {
            gestureRecognizer = gestureRecognizer ?? GetComponent<GestureRecognizer>();
        }
        #endregion

        #region SWIPE_STATES
        enum SwipeType { None = 0, Single = 1, Double = 2 }
        SwipeType currentState;

        bool ChangeState(int touchCount, TouchPhase phase, Vector2 delta)
        {
            if (phase == TouchPhase.Ended || touchCount == 0 && swipeTime >= minSwipeTime && delta.magnitude < cancelRadius) // Any -> 0
            {
                // End State
                currentState = SwipeType.None;
            }
            else if ((int)currentState == touchCount) // X -> X (X!=0)
            {
                // Keep State
                swipeTime += Time.deltaTime;
                cancelTimeout = 0;
            }
            else if (phase == TouchPhase.Began && currentState == SwipeType.None && touchCount > 0) //  0 -> X (X!=0)
            {
                // Begin State
                currentState = (SwipeType)touchCount;
                swipeTime = 0;
            }
            else // X -> Y (X!=0, Y!=0)
            {
                // Cancel State
                if (cancelTimeout > timeToCancelTimeout)
                {
                    currentState = SwipeType.None;
                    return true;
                }
                cancelTimeout += Time.deltaTime;
            }
            return false;
        }
        #endregion

        #region INPUT
        [Header("Swipe Details")]
        [SerializeField, Range(0, 90)] float activateAngle = 60f;
        [SerializeField] float minSwipeTime = 0.05f;
        [SerializeField] float timeToCancelTimeout = 0.1f;
        [SerializeField] public float cancelRadius = 0.05f;

        private int _inputTouches = 0;
        float swipeTime = int.MaxValue;
        float cancelTimeout = 0f;

        private void Update()
        {
            List<RawInput> Swipes = gestureRecognizer.InputThrottle.Consume();

            if (Swipes != null)
            {
                //print("Swipe Count: " + Swipes.Count + " Touch Count: " + Input.touchCount + " / prev: " + _inputTouches);
                OnAnyInput?.Invoke();
                foreach (RawInput s in Swipes)
                {
                    Vector2 startPosition = ScreenToWorldCoordinates(s.StartPosition);
                    Vector2 delta = ScreenToWorldCoordinates(s.Position) - startPosition;

                    SwipeType previousState = currentState;

                    int touchCount = UnityEngine.Input.touchCount;
#if UNITY_EDITOR
                    touchCount = Convert.ToInt32(UnityEngine.Input.GetMouseButton(0)) + Convert.ToInt32(UnityEngine.Input.GetMouseButton(1));
#endif
                    bool cancel = ChangeState(touchCount, s.Phase, delta);

                    //print(s.Phase +" | "+ previousState +" -> "+ currentState +" (" +cancel+")");
                    //if (previousState != currentState)
                    //    print(currentState);

                    if (cancel)
                    {
                        InvokeSwipeEvents(previousState, startPosition, delta, TouchPhase.Canceled);
                    }
                    else if (previousState != SwipeType.None && currentState == SwipeType.None)
                    {
                        InvokeSwipeEvents(previousState, startPosition, delta, TouchPhase.Ended);
                    }
                    else if (previousState == SwipeType.None && currentState != SwipeType.None)
                    {
                        InvokeSwipeEvents(currentState, startPosition, delta, TouchPhase.Began);
                    }
                    else if (previousState == currentState)
                    {
                        InvokeSwipeEvents(currentState, startPosition, delta, s.Phase);
                    }
                }
                _inputTouches = UnityEngine.Input.touchCount;
            }
        }

        // CHANGE THIS IF YOU ARE NOT USING AN ORTHOGRAPHIC CAMERA
        public static Vector2 ScreenToWorldCoordinates(Vector2 coords) => Camera.main.ScreenToWorldPoint(new Vector3(coords.x, coords.y));
        #endregion

        #region OUTPUT
        void InvokeSwipeEvents(SwipeType type, Vector2 pos, Vector2 delta, TouchPhase phase)
        {
            if (type == SwipeType.Single)
            {
                OnSingleSwipeDetails?.Invoke(pos, delta, phase);

                if (phase == TouchPhase.Ended)
                {
                    print($"Single Swipe Direction: {InQuadrant(delta)}");
                    OnSingleSwipe?.Invoke(pos, delta, InQuadrant(delta));
                }
            }

            if (type == SwipeType.Double)
            {
                OnDoubleSwipeDetails?.Invoke(pos, delta, phase);

                if (phase == TouchPhase.Ended)
                {
                    print($"Double Swipe Direction: {InQuadrant(delta)}.");
                    OnDoubleSwipe?.Invoke(pos, delta, InQuadrant(delta));
                }
            }
        }

        public enum Quadrant { None = 0, Up = 1, Down = 2, Left = 3, Right = 4 }

        public Quadrant InQuadrant(Vector2 v)
        {
            v.Normalize();
            float dot = Mathf.Cos(activateAngle / 2 * Mathf.Deg2Rad);

            if (v.x < -dot) return Quadrant.Left;
            if (v.x > dot) return Quadrant.Right;
            if (v.y > dot) return Quadrant.Up;
            if (v.y < -dot) return Quadrant.Down;
            return Quadrant.None;
        }
        #endregion
    }
}