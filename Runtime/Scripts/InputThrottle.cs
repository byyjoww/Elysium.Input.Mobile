using UnityEngine;
using System.Collections.Generic;

namespace Elysium.Input.Mobile
{
    public class InputThrottle
    {
        List<RawInput> _queue;
        RawInput _currentBegin;
        float _consumedSwipeTime;

        public InputThrottle()
        {
            _queue = new List<RawInput>();
        }

        public bool HasData()
        {
            return _queue.Count > 0;
        }

        public float GetThrottledSwipeTime()
        {
            float swipeTime = 10000f;
            if (_queue.Count > 0 && _queue[0].Phase != TouchPhase.Began)
            {
                swipeTime = _consumedSwipeTime + GetNotConsumedTime();
            }
            return swipeTime;
        }

        public void Enqueue(RawInput raw)
        {
            _queue.Add(raw);
        }

        public Vector2 GetThrottledFirst()
        {
            if (_queue.Count > 0 && _queue[0].Phase == TouchPhase.Began)
            {
                _currentBegin = _queue[0];
            }
            return _currentBegin.Position;
        }

        public Vector2 GetThrottledLast()
        {
            return GetThrottledRawInput().Position;
        }

        public List<RawInput> Consume()
        {
            List<RawInput> inputs = null;

            (int begin, int end) = GetCurrentSwipeQueueSlice();
            if (begin >= 0 && end >= 0)
            {
                inputs = new List<RawInput>(end - begin + 1);
                int touchEndedIndex = _queue.FindIndex(begin, end - begin + 1, raw => raw.Phase == TouchPhase.Ended);

                for (int i = end; i >= begin; --i)
                {
                    inputs.Add(_queue[i]);
                    _consumedSwipeTime += _queue[i].DeltaTime;
                    _queue.RemoveAt(i);
                }

                if (touchEndedIndex >= 0)
                {
                    _consumedSwipeTime = 0f;
                }
            }
            return inputs;
        }

        float GetNotConsumedTime()
        {
            float notConsumedTime = 0f;
            (int begin, int end) = GetCurrentSwipeQueueSlice();
            if (begin >= 0 && end >= 0)
            {
                for (int i = begin; i <= end; ++i)
                {
                    notConsumedTime += _queue[i].DeltaTime;
                }
            }
            return notConsumedTime;
        }

        (int first, int last) GetCurrentSwipeQueueSlice()
        {
            int begin = -1;
            int end = -1;

            if (_queue.Count > 0)
            {
                begin = _queue.FindIndex(r => r.Phase == TouchPhase.Began);
                end = _queue.FindIndex(r => r.Phase == TouchPhase.Ended);

                // If we have TouchBegan and TouchEnded, with begin coming first,
                // consume till TouchEnded exclusive
                if (begin >= 0 && end >= 0 && begin < end)
                {
                    begin = 0;
                    end = end - 1;
                }
                // If we have TouchEnded, but not TouchBegan,
                // Or if TouchEnded came first than TouchBegan, 
                // consume till TouchEnded inclusive
                else if (end >= 0)
                {
                    begin = 0;
                }
                // Else, consume whole queue
                else
                {
                    begin = 0;
                    end = _queue.Count - 1;
                }
            }
            return (begin, end);
        }

        RawInput GetThrottledRawInput()
        {
            RawInput last = default;
            if (_queue.Count > 0)
            {
                int end = _queue.FindIndex(r => r.Phase == TouchPhase.Ended);
                if (end >= 0)
                {
                    last = _queue[end];
                }
                else
                {
                    last = _queue[_queue.Count - 1];
                }
            }
            return last;
        }
    }
}