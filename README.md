***** INSTRUCTIONS ***** 

1- To start receiving input, drag the SwipeInput, TouchInput or TouchAndSwipeInput prefab into the scene.

2- Don't use 2 prefabs at once, as you will have 2 instances of the GestureRecognizer doing independant calculations at the same time.

3- To receive the swipe/touch input, subscribe your scripts to:

SwipeInputController.cs (Action<Position, Delta, Swipe Direction>)
public event Action<Vector2, Vector2, Quadrant> OnSingleSwipe;
public event Action<Vector2, Vector2, Quadrant> OnDoubleSwipe;

TouchInputController.cs (Action<Screen Position>)
public event Action<Vector3> OnSingleTap;
public event Action<Vector3> OnDoubleTap;

4- To remove the swipe line, just remove the DragIndicatorScript monobehaviour from the prefabs.

5- To restrict input to a specific location, add the InputValidator Monobehaviour script on to the relevant prefab, and edit the "swipe area" parameter. 
Any input done in a non-red area will be ignored.

***** ERRORS ***** 
If you are NOT using an Orthographic camera, you need to change ScreenToWorldCoordinates to return ScreenToWorldPoint with a Z = to your canvas depth.

> public static Vector2 ScreenToWorldCoordinates(Vector2 coords) => Camera.main.ScreenToWorldPoint(new Vector3(coords.x, coords.y, INSERT Z COORD HERE));

If you don't, you will get a Swipe Direction: None, as delta will always be 0 (due to the z being 0 which is the camera's position).