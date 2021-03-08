using UnityEngine;

namespace Elysium.Input.Mobile
{
    public struct RawInput
    {
        public RawInput(TouchPhase phase, Vector3 startPosition, Vector3 position, float deltaTime, float time, int touches)
        {
            Phase = phase;
            StartPosition = startPosition;
            Position = position;
            DeltaTime = deltaTime;
            Time = time;
            Touches = touches;
        }

        public TouchPhase Phase;
        public Vector3 StartPosition;
        public Vector3 Position;
        public float DeltaTime;
        public float Time;
        public int Touches;
    }
}