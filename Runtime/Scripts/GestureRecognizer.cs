using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Elysium.Input.Mobile
{
    public class GestureRecognizer : MonoBehaviour
    {
        public enum ProcessingState { Idle = -1, Unknown, WaitingForSecondTap, SecondTap, Swipe }
        public enum MouseButtonState { Released, Pressed }

        int? _fingerId;
        Vector2 _startPosition;
        ProcessingState _processingState;

        Vector2 _previousPosition;
        MouseButtonState _mouseButtonState;

        float _tapDurationThreshold = 0.2f;
        float _doubleTapTimeThreshold = 0.1f;
        float _swipeMovementThreshold = Screen.height * 40f / 1334f;

        float _thresholdStartTime;
        float _gestureStartTime;
        float _lastMouseTime;

        float _smallSwipeThreshold = 110f * (Screen.height / 1334f);

        int _tapCount = 0;

        public InputThrottle InputThrottle { get; set; } = new InputThrottle();

        public event System.Action<Vector2> Tapped;
        public event System.Action<Vector2> DoubleTapped;

        void Awake()
        {
            _fingerId = null;
            _processingState = ProcessingState.Idle;
            _mouseButtonState = MouseButtonState.Released;
        }

        void Update()
        {
#if !UNITY_EDITOR
        bool isMobile = Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;
        if (isMobile) ProcessTouch();
        else ProcessMouse();
#else
            if (UnityEditor.EditorApplication.isRemoteConnected) ProcessTouch();
            else ProcessMouse();
#endif

            if (_processingState == ProcessingState.WaitingForSecondTap && !WithinDoubleTapThreshold())
            {
                _processingState = ProcessingState.Idle;
                Tapped?.Invoke(_startPosition);
            }
        }

        void ProcessTouch()
        {
            if (UnityEngine.Input.touches.Length > 0)
            {
                Touch mergedTouch = new Touch();
                if (null != _fingerId) mergedTouch.fingerId = _fingerId.Value;
                foreach (Touch touch in UnityEngine.Input.touches)
                {
                    if (null == _fingerId || touch.fingerId == mergedTouch.fingerId) mergedTouch = touch;
                    else
                    {
                        mergedTouch.deltaTime += touch.deltaTime;
                        mergedTouch.deltaPosition += touch.deltaPosition;
                        mergedTouch.position = touch.position;
                        mergedTouch.rawPosition = touch.rawPosition;
                        mergedTouch.tapCount = UnityEngine.Input.touches.Length;
                    }
                }

                ProcessTouch(mergedTouch);
            }
        }

        void ProcessTouch(Touch touch)
        {
            if (touch.phase == TouchPhase.Began && null == _fingerId)
            {
                _fingerId = touch.fingerId;
                ProcessTouchBegan(touch.position, touch.tapCount);
            }

            if (touch.fingerId == _fingerId)
            {
                if (TouchPhase.Moved == touch.phase) ProcessTouchMoved(touch.position, touch.deltaTime, touch.tapCount);
                else if (TouchPhase.Ended == touch.phase) ProcessTouchEnded(touch.position, touch.deltaTime, touch.tapCount);
                else if (TouchPhase.Stationary == touch.phase) ProcessTouchStationary(touch.position, touch.deltaTime, touch.tapCount);
            }
        }

        void ProcessMouse()
        {
            Vector2 mousePosition = new Vector2(UnityEngine.Input.mousePosition.x, UnityEngine.Input.mousePosition.y);
            if (UnityEngine.Input.GetMouseButton(0))
            {
                if (_mouseButtonState == MouseButtonState.Released) ProcessTouchBegan(mousePosition, 1);
                else if (_previousPosition != mousePosition) ProcessTouchMoved(mousePosition, Time.time - _lastMouseTime, 1);
                else ProcessTouchStationary(mousePosition, Time.time - _lastMouseTime, 1);
                _mouseButtonState = MouseButtonState.Pressed;
                _previousPosition = mousePosition;
                _lastMouseTime = Time.time;
            }
            else if (_mouseButtonState == MouseButtonState.Pressed)
            {
                _mouseButtonState = MouseButtonState.Released;
                ProcessTouchEnded(mousePosition, Time.time - _lastMouseTime, 1);
                _lastMouseTime = Time.time;
            }
        }

        bool WithinTapDurationThreshold()
        {
            return _gestureStartTime + _tapDurationThreshold > Time.time;
        }

        bool WithinDoubleTapThreshold()
        {
            return _thresholdStartTime + _doubleTapTimeThreshold > Time.time;
        }

        bool PastSwipeThreshold(Vector2 position, float scale)
        {
            Vector2 swipeVector = position - _startPosition;
            float swipeAngle = Mathf.Atan2(swipeVector.y, swipeVector.x);
            return swipeVector.magnitude > _smallSwipeThreshold * scale * (1f - .5f * Mathf.Abs(Mathf.Sin(swipeAngle)));
        }

        void ProcessTouchBegan(Vector2 position, int tapCount)
        {
            if (!UIClick(position))
            {
                _gestureStartTime = Time.time;
                _startPosition = position;
                _tapCount += 1;
                if (_processingState == ProcessingState.WaitingForSecondTap && WithinDoubleTapThreshold())
                {
                    _processingState = ProcessingState.SecondTap;
                }
                else _processingState = ProcessingState.Unknown;
            }
            else
            {
                _fingerId = null;
                _mouseButtonState = MouseButtonState.Released;
            }
        }

        void ProcessTouchMoved(Vector2 position, float deltaTime, int tapCount)
        {
            if (_processingState != ProcessingState.Idle)
            {
                UpdateStateOnTouchMoved(position, tapCount);
                if (_processingState == ProcessingState.Swipe)
                {
                    // InputMoved?.Invoke(_startPosition, position, deltaTime);
                    InputThrottle.Enqueue(new RawInput(TouchPhase.Moved, _startPosition, position, deltaTime, Time.time, tapCount));
                }
            }
        }

        void ProcessTouchStationary(Vector2 position, float deltaTime, int tapCount)
        {
            if (_processingState == ProcessingState.Swipe)
            {
                // InputHold?.Invoke(_startPosition, position, deltaTime);
                InputThrottle.Enqueue(new RawInput(TouchPhase.Stationary, _startPosition, position, deltaTime, Time.time, tapCount));
            }

            if (CanStartSwipe(position) && !WithinTapDurationThreshold())
            {
                if (_processingState == ProcessingState.SecondTap) Tapped?.Invoke(_startPosition);
                // InputBegan?.Invoke(_startPosition);
                InputThrottle.Enqueue(new RawInput(TouchPhase.Began, _startPosition, _startPosition, 0f, Time.time, tapCount));
                _processingState = ProcessingState.Swipe;
            }
        }

        void ProcessTouchEnded(Vector2 position, float deltaTime, int tapCount)
        {
            if (_processingState != ProcessingState.Idle)
            {
                UpdateStateOnTouchMoved(position, tapCount);
                _fingerId = null;
                _tapCount -= 1;
                if (_processingState == ProcessingState.Unknown)
                {
                    _processingState = ProcessingState.WaitingForSecondTap;
                    _thresholdStartTime = Time.time;
                }
                else if (_processingState == ProcessingState.SecondTap)
                {
                    DoubleTapped?.Invoke(_startPosition);
                    _processingState = ProcessingState.Idle;
                }
                else if (_processingState == ProcessingState.Swipe)
                {
                    // InputEnded?.Invoke(_startPosition, position, deltaTime);
                    InputThrottle.Enqueue(new RawInput(TouchPhase.Ended, _startPosition, position, deltaTime, Time.time, tapCount));
                    _processingState = ProcessingState.Idle;
                }
            }
        }

        void UpdateStateOnTouchMoved(Vector2 position, int tapCount)
        {
            if (CanStartSwipe(position))
            {
                if (_processingState == ProcessingState.SecondTap) Tapped?.Invoke(_startPosition);
                // InputBegan?.Invoke(_startPosition);
                InputThrottle.Enqueue(new RawInput(TouchPhase.Began, _startPosition, _startPosition, 0f, Time.time, tapCount));
                _processingState = ProcessingState.Swipe;
            }
        }

        bool UIClick(Vector2 position)
        {
            if (EventSystem.current == null)
                return false;

            PointerEventData pe = new PointerEventData(EventSystem.current);
            pe.position = position;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pe, results);
            return results.FindAll(r => r.gameObject && (r.gameObject.layer == LayerMask.NameToLayer("UI") || r.gameObject.layer == LayerMask.NameToLayer("Emoji"))).Count > 0;
        }

        bool CanStartSwipe(Vector2 position)
        {
            return (_processingState == ProcessingState.Unknown || _processingState == ProcessingState.SecondTap)
                   && PastSwipeThreshold(position, 1f);
        }
    }
}