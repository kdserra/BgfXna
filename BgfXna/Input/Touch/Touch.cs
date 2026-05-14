using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Input.Touch;

[Flags]
public enum GestureType
{
    None = 0,
    Tap = 1,
    DoubleTap = 2,
    Hold = 4,
    HorizontalDrag = 8,
    VerticalDrag = 16,
    FreeDrag = 32,
    Pinch = 64,
    Flick = 128,
    DragComplete = 256,
    PinchComplete = 512
}

public enum TouchLocationState
{
    Invalid,
    Moved,
    Pressed,
    Released
}

public readonly struct TouchLocation
{
    public TouchLocation(int id, TouchLocationState state, Vector2 position)
        : this(id, state, position, TouchLocationState.Invalid, Vector2.Zero)
    {
    }

    public TouchLocation(int id, TouchLocationState state, Vector2 position, TouchLocationState previousState, Vector2 previousPosition)
    {
        Id = id;
        State = state;
        Position = position;
        _previousState = previousState;
        _previousPosition = previousPosition;
    }

    private readonly TouchLocationState _previousState;
    private readonly Vector2 _previousPosition;
    public int Id { get; }
    public TouchLocationState State { get; }
    public Vector2 Position { get; }
    public bool TryGetPreviousLocation(out TouchLocation previousLocation)
    {
        previousLocation = new TouchLocation(Id, _previousState, _previousPosition);
        return _previousState != TouchLocationState.Invalid;
    }
}

public readonly struct TouchCollection : IEnumerable<TouchLocation>
{
    private readonly TouchLocation[]? _touches;
    public TouchCollection(TouchLocation[] touches) => _touches = touches;
    public int Count => _touches?.Length ?? 0;
    public bool IsConnected => true;
    public TouchLocation this[int index] => (_touches ?? Array.Empty<TouchLocation>())[index];
    public IEnumerator<TouchLocation> GetEnumerator() => ((IEnumerable<TouchLocation>)(_touches ?? Array.Empty<TouchLocation>())).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public readonly struct TouchPanelCapabilities
{
    public TouchPanelCapabilities(bool isConnected, int maximumTouchCount)
    {
        IsConnected = isConnected;
        MaximumTouchCount = maximumTouchCount;
    }

    public bool IsConnected { get; }
    public int MaximumTouchCount { get; }
}

public readonly struct GestureSample
{
    public GestureSample(GestureType gestureType, TimeSpan timestamp, Vector2 position, Vector2 position2, Vector2 delta, Vector2 delta2)
    {
        GestureType = gestureType;
        Timestamp = timestamp;
        Position = position;
        Position2 = position2;
        Delta = delta;
        Delta2 = delta2;
    }

    public GestureType GestureType { get; }
    public TimeSpan Timestamp { get; }
    public Vector2 Position { get; }
    public Vector2 Position2 { get; }
    public Vector2 Delta { get; }
    public Vector2 Delta2 { get; }
}

public static class TouchPanel
{
    private static readonly Queue<GestureSample> Gestures = new();
    public static DisplayOrientation DisplayOrientation { get; set; } = DisplayOrientation.Default;
    public static GestureType EnabledGestures { get; set; }
    public static int DisplayWidth { get; set; }
    public static int DisplayHeight { get; set; }
    public static bool IsGestureAvailable => Gestures.Count > 0;
    public static TouchPanelCapabilities GetCapabilities() => new(true, 8);
    public static TouchCollection GetState() => new(Array.Empty<TouchLocation>());
    public static GestureSample ReadGesture() => Gestures.Count == 0 ? default : Gestures.Dequeue();
    public static void AddGesture(GestureSample gestureSample) => Gestures.Enqueue(gestureSample);
}
