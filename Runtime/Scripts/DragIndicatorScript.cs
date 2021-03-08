using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Elysium.Input.Mobile
{
    [RequireComponent(typeof(SwipeInputController))]
    public class DragIndicatorScript : MonoBehaviour
    {
        [Header("Set These")]
        [SerializeField] AnimationCurve lineWidth;
        [SerializeField] Material rendererMaterial;

        [Header("Indicator Colors")]
        [SerializeField] private Color singleSwipeColor = new Color32(200, 200, 200, 50);
        [SerializeField] private Color doubleSwipeColorLeft = new Color32(0, 0, 255, 50);
        [SerializeField] private Color doubleSwipeColorRight = new Color32(255, 0, 0, 50);
        [SerializeField] private Color doubleSwipeColorUp = new Color32(0, 255, 0, 50);
        [SerializeField] private Color doubleSwipeDown = new Color32(255, 255, 0, 50);
        [SerializeField] private Color doubleSwipeColorUnavailable = new Color32(0, 0, 0, 50);

        [Header("Details")]
        [SerializeField] Vector3 camOffset = new Vector3(0, 0, 1);
        [SerializeField] float swipeAnimDelay = 0.02f;

        [Header("Input")]
        [SerializeField] SwipeInputController input;

        #region INITIALIZE
        bool isInitialized;

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

            input.OnSingleSwipeDetails += OnSwipeUpdate;
            input.OnDoubleSwipeDetails += OnDoubleSwipeUpdate;

            GameObject obj1 = new GameObject("singleSwipeRenderer");
            obj1.transform.parent = transform;
            GameObject obj2 = new GameObject("doubleSwipeRenderer");
            obj2.transform.parent = transform;

            singleSwipeRenderer = obj1.AddComponent<LineRenderer>();
            doubleSwipeRenderer = obj2.AddComponent<LineRenderer>();

            LineRendererSetup(singleSwipeRenderer);
            LineRendererSetup(doubleSwipeRenderer);

            isInitialized = true;
            Debug.Log("DragIndicatorScript is initialized.");
        }

        private void OnValidate()
        {
            input = input ?? GetComponent<SwipeInputController>();
        }

        void LineRendererSetup(LineRenderer lr)
        {
            lr.material = rendererMaterial;
            lr.startColor = Color.white;
            lr.positionCount = 2;
            lr.SetPosition(0, startPos);
            lr.useWorldSpace = true;
            lr.widthCurve = lineWidth;
            lr.numCapVertices = 10;
            lr.enabled = false;
        }
        #endregion

        #region BEHAVIOR
        Vector3 startPos;
        Vector3 endPos;
        LineRenderer singleSwipeRenderer, doubleSwipeRenderer;
        float swipeAnimTimer = 0f;

        void OnDoubleSwipeUpdate(Vector2 pos, Vector2 delta, TouchPhase phase)
        {
            //Debug.Log($"Double Swipe Pos: {pos}.");
            switch (input.InQuadrant(delta))
            {
                case SwipeInputController.Quadrant.Left:
                    doubleSwipeRenderer.endColor = doubleSwipeColorLeft;
                    break;
                case SwipeInputController.Quadrant.Right:
                    doubleSwipeRenderer.endColor = doubleSwipeColorRight;
                    break;
                case SwipeInputController.Quadrant.Up:
                    doubleSwipeRenderer.endColor = doubleSwipeColorUp;
                    break;
                case SwipeInputController.Quadrant.Down:
                    doubleSwipeRenderer.endColor = doubleSwipeDown;
                    break;
                default:
                    doubleSwipeRenderer.endColor = doubleSwipeColorUnavailable;
                    break;
            }

            OnSwipeUpdateWithColor(pos, delta, phase, doubleSwipeRenderer);
        }

        void OnSwipeUpdate(Vector2 pos, Vector2 delta, TouchPhase phase)
        {
            //Debug.Log($"Single Swipe Pos: {pos}.");
            singleSwipeRenderer.endColor = singleSwipeColor;
            OnSwipeUpdateWithColor(pos, delta, phase, singleSwipeRenderer);
        }

        void OnSwipeUpdateWithColor(Vector2 pos, Vector2 delta, TouchPhase phase, LineRenderer lr)
        {
            //Debug.Log($"Swipe Pos: {pos}. Phase: {phase}.");
            bool isValid = delta.magnitude > input.cancelRadius;

            if (phase == TouchPhase.Began)
            {
                startPos = (Vector3)(pos + delta) + camOffset;
                lr.SetPosition(0, startPos);
            }
            else if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled || !isValid)
            {
                swipeAnimTimer = 0;
                lr.enabled = false;
            }
            else if (phase == TouchPhase.Moved || phase == TouchPhase.Stationary)
            {
                swipeAnimTimer += Time.deltaTime;
                if (swipeAnimTimer >= swipeAnimDelay)
                {
                    lr.enabled = true;
                    endPos = (Vector3)(pos + delta) + camOffset;
                    lr.SetPosition(1, endPos);
                }
            }
        }
        #endregion

        #region ONDESTROY
        private void OnDestroy()
        {
            input.OnSingleSwipeDetails -= OnSwipeUpdate;
            input.OnDoubleSwipeDetails -= OnDoubleSwipeUpdate;
        }
        #endregion
    }
}