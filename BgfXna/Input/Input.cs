using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Input;

public enum ButtonState
{
    Released,
    Pressed
}

public enum KeyState
{
    Up,
    Down
}

[Flags]
public enum Buttons
{
    A = 0x1000,
    B = 0x2000,
    X = 0x4000,
    Y = 0x8000,
    Back = 0x20,
    BigButton = 0x800,
    Start = 0x10,
    LeftShoulder = 0x100,
    RightShoulder = 0x200,
    LeftStick = 0x40,
    RightStick = 0x80,
    DPadUp = 0x1,
    DPadDown = 0x2,
    DPadLeft = 0x4,
    DPadRight = 0x8,
    LeftThumbstickLeft = 0x200000,
    RightTrigger = 0x400000,
    LeftTrigger = 0x800000,
    RightThumbstickUp = 0x1000000,
    RightThumbstickDown = 0x2000000,
    RightThumbstickRight = 0x4000000,
    RightThumbstickLeft = 0x8000000,
    LeftThumbstickUp = 0x10000000,
    LeftThumbstickDown = 0x20000000,
    LeftThumbstickRight = 0x40000000
}

public enum GamePadDeadZone
{
    None,
    IndependentAxes,
    Circular
}

public enum GamePadType
{
    Unknown,
    GamePad,
    Wheel,
    ArcadeStick,
    FlightStick,
    DancePad,
    Guitar,
    AlternateGuitar,
    DrumKit,
    BigButtonPad
}

public enum Keys
{
    None = 0,
    Back = 8,
    Tab = 9,
    Enter = 13,
    Pause = 19,
    CapsLock = 20,
    Escape = 27,
    Space = 32,
    PageUp = 33,
    PageDown = 34,
    End = 35,
    Home = 36,
    Left = 37,
    Up = 38,
    Right = 39,
    Down = 40,
    Select = 41,
    Print = 42,
    Execute = 43,
    PrintScreen = 44,
    Insert = 45,
    Delete = 46,
    Help = 47,
    D0 = 48,
    D1 = 49,
    D2 = 50,
    D3 = 51,
    D4 = 52,
    D5 = 53,
    D6 = 54,
    D7 = 55,
    D8 = 56,
    D9 = 57,
    A = 65,
    B = 66,
    C = 67,
    D = 68,
    E = 69,
    F = 70,
    G = 71,
    H = 72,
    I = 73,
    J = 74,
    K = 75,
    L = 76,
    M = 77,
    N = 78,
    O = 79,
    P = 80,
    Q = 81,
    R = 82,
    S = 83,
    T = 84,
    U = 85,
    V = 86,
    W = 87,
    X = 88,
    Y = 89,
    Z = 90,
    LeftWindows = 91,
    RightWindows = 92,
    Apps = 93,
    Sleep = 95,
    NumPad0 = 96,
    NumPad1 = 97,
    NumPad2 = 98,
    NumPad3 = 99,
    NumPad4 = 100,
    NumPad5 = 101,
    NumPad6 = 102,
    NumPad7 = 103,
    NumPad8 = 104,
    NumPad9 = 105,
    Multiply = 106,
    Add = 107,
    Separator = 108,
    Subtract = 109,
    Decimal = 110,
    Divide = 111,
    F1 = 112,
    F2 = 113,
    F3 = 114,
    F4 = 115,
    F5 = 116,
    F6 = 117,
    F7 = 118,
    F8 = 119,
    F9 = 120,
    F10 = 121,
    F11 = 122,
    F12 = 123,
    NumLock = 144,
    Scroll = 145,
    LeftShift = 160,
    RightShift = 161,
    LeftControl = 162,
    RightControl = 163,
    LeftAlt = 164,
    RightAlt = 165,
    OemSemicolon = 186,
    OemPlus = 187,
    OemComma = 188,
    OemMinus = 189,
    OemPeriod = 190,
    OemQuestion = 191,
    OemTilde = 192,
    OemOpenBrackets = 219,
    OemPipe = 220,
    OemCloseBrackets = 221,
    OemQuotes = 222,
    Oem8 = 223,
    OemBackslash = 226
}

public readonly struct KeyboardState
{
    private readonly HashSet<Keys>? _pressedKeys;

    public KeyboardState(params Keys[] keys)
    {
        _pressedKeys = keys is { Length: > 0 } ? new HashSet<Keys>(keys) : null;
    }

    public bool IsKeyDown(Keys key) => _pressedKeys?.Contains(key) == true;
    public bool IsKeyUp(Keys key) => !IsKeyDown(key);
    public Keys[] GetPressedKeys() => _pressedKeys?.ToArray() ?? Array.Empty<Keys>();
}

public readonly struct MouseState
{
    public MouseState(int x, int y, int scrollWheelValue, ButtonState leftButton, ButtonState middleButton, ButtonState rightButton, ButtonState xButton1, ButtonState xButton2)
    {
        X = x;
        Y = y;
        ScrollWheelValue = scrollWheelValue;
        LeftButton = leftButton;
        MiddleButton = middleButton;
        RightButton = rightButton;
        XButton1 = xButton1;
        XButton2 = xButton2;
    }

    public int X { get; }
    public int Y { get; }
    public int ScrollWheelValue { get; }
    public ButtonState LeftButton { get; }
    public ButtonState MiddleButton { get; }
    public ButtonState RightButton { get; }
    public ButtonState XButton1 { get; }
    public ButtonState XButton2 { get; }
    public Point Position => new(X, Y);
}

public readonly struct GamePadButtons
{
    private readonly Buttons _buttons;

    public GamePadButtons(Buttons buttons) => _buttons = buttons;
    public ButtonState A => Get(Buttons.A);
    public ButtonState B => Get(Buttons.B);
    public ButtonState X => Get(Buttons.X);
    public ButtonState Y => Get(Buttons.Y);
    public ButtonState Back => Get(Buttons.Back);
    public ButtonState Start => Get(Buttons.Start);
    public ButtonState BigButton => Get(Buttons.BigButton);
    public ButtonState LeftShoulder => Get(Buttons.LeftShoulder);
    public ButtonState RightShoulder => Get(Buttons.RightShoulder);
    public ButtonState LeftStick => Get(Buttons.LeftStick);
    public ButtonState RightStick => Get(Buttons.RightStick);
    private ButtonState Get(Buttons button) => (_buttons & button) != 0 ? ButtonState.Pressed : ButtonState.Released;
}

public readonly struct GamePadDPad
{
    public GamePadDPad(ButtonState up, ButtonState down, ButtonState left, ButtonState right)
    {
        Up = up;
        Down = down;
        Left = left;
        Right = right;
    }

    public ButtonState Up { get; }
    public ButtonState Down { get; }
    public ButtonState Left { get; }
    public ButtonState Right { get; }
}

public readonly struct GamePadTriggers
{
    public GamePadTriggers(float left, float right)
    {
        Left = MathHelper.Clamp(left, 0f, 1f);
        Right = MathHelper.Clamp(right, 0f, 1f);
    }

    public float Left { get; }
    public float Right { get; }
}

public readonly struct GamePadThumbSticks
{
    public GamePadThumbSticks(Vector2 left, Vector2 right)
    {
        Left = left;
        Right = right;
    }

    public Vector2 Left { get; }
    public Vector2 Right { get; }
}

public readonly struct GamePadState
{
    private readonly Buttons _buttonsMask;

    public GamePadState(GamePadThumbSticks thumbSticks, GamePadTriggers triggers, GamePadButtons buttons, GamePadDPad dPad)
    {
        ThumbSticks = thumbSticks;
        Triggers = triggers;
        Buttons = buttons;
        DPad = dPad;
        IsConnected = true;
        _buttonsMask = 0;
        foreach (Buttons button in Enum.GetValues(typeof(Buttons)))
        {
            if (button != 0 && GetButton(button, buttons, dPad, triggers, thumbSticks) == ButtonState.Pressed)
            {
                _buttonsMask |= button;
            }
        }
    }

    public GamePadThumbSticks ThumbSticks { get; }
    public GamePadTriggers Triggers { get; }
    public GamePadButtons Buttons { get; }
    public GamePadDPad DPad { get; }
    public bool IsConnected { get; }
    public bool IsButtonDown(Buttons button) => (_buttonsMask & button) != 0;
    public bool IsButtonUp(Buttons button) => !IsButtonDown(button);

    private static ButtonState GetButton(Buttons button, GamePadButtons buttons, GamePadDPad dPad, GamePadTriggers triggers, GamePadThumbSticks thumbSticks) => button switch
    {
        Microsoft.Xna.Framework.Input.Buttons.A => buttons.A,
        Microsoft.Xna.Framework.Input.Buttons.B => buttons.B,
        Microsoft.Xna.Framework.Input.Buttons.X => buttons.X,
        Microsoft.Xna.Framework.Input.Buttons.Y => buttons.Y,
        Microsoft.Xna.Framework.Input.Buttons.Back => buttons.Back,
        Microsoft.Xna.Framework.Input.Buttons.Start => buttons.Start,
        Microsoft.Xna.Framework.Input.Buttons.BigButton => buttons.BigButton,
        Microsoft.Xna.Framework.Input.Buttons.LeftShoulder => buttons.LeftShoulder,
        Microsoft.Xna.Framework.Input.Buttons.RightShoulder => buttons.RightShoulder,
        Microsoft.Xna.Framework.Input.Buttons.LeftStick => buttons.LeftStick,
        Microsoft.Xna.Framework.Input.Buttons.RightStick => buttons.RightStick,
        Microsoft.Xna.Framework.Input.Buttons.DPadUp => dPad.Up,
        Microsoft.Xna.Framework.Input.Buttons.DPadDown => dPad.Down,
        Microsoft.Xna.Framework.Input.Buttons.DPadLeft => dPad.Left,
        Microsoft.Xna.Framework.Input.Buttons.DPadRight => dPad.Right,
        Microsoft.Xna.Framework.Input.Buttons.LeftTrigger => triggers.Left > 0f ? ButtonState.Pressed : ButtonState.Released,
        Microsoft.Xna.Framework.Input.Buttons.RightTrigger => triggers.Right > 0f ? ButtonState.Pressed : ButtonState.Released,
        Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickLeft => thumbSticks.Left.X < 0f ? ButtonState.Pressed : ButtonState.Released,
        Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickRight => thumbSticks.Left.X > 0f ? ButtonState.Pressed : ButtonState.Released,
        Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickUp => thumbSticks.Left.Y > 0f ? ButtonState.Pressed : ButtonState.Released,
        Microsoft.Xna.Framework.Input.Buttons.LeftThumbstickDown => thumbSticks.Left.Y < 0f ? ButtonState.Pressed : ButtonState.Released,
        Microsoft.Xna.Framework.Input.Buttons.RightThumbstickLeft => thumbSticks.Right.X < 0f ? ButtonState.Pressed : ButtonState.Released,
        Microsoft.Xna.Framework.Input.Buttons.RightThumbstickRight => thumbSticks.Right.X > 0f ? ButtonState.Pressed : ButtonState.Released,
        Microsoft.Xna.Framework.Input.Buttons.RightThumbstickUp => thumbSticks.Right.Y > 0f ? ButtonState.Pressed : ButtonState.Released,
        Microsoft.Xna.Framework.Input.Buttons.RightThumbstickDown => thumbSticks.Right.Y < 0f ? ButtonState.Pressed : ButtonState.Released,
        _ => ButtonState.Released,
    };
}

public readonly struct GamePadCapabilities
{
    public bool IsConnected { get; init; }
    public GamePadType GamePadType { get; init; }
    public bool HasAButton { get; init; }
    public bool HasBButton { get; init; }
    public bool HasXButton { get; init; }
    public bool HasYButton { get; init; }
    public bool HasBackButton { get; init; }
    public bool HasStartButton { get; init; }
    public bool HasLeftShoulderButton { get; init; }
    public bool HasRightShoulderButton { get; init; }
    public bool HasLeftTrigger { get; init; }
    public bool HasRightTrigger { get; init; }
    public bool HasLeftXThumbStick { get; init; }
    public bool HasLeftYThumbStick { get; init; }
    public bool HasRightXThumbStick { get; init; }
    public bool HasRightYThumbStick { get; init; }
    public bool HasDPadUpButton { get; init; }
    public bool HasDPadDownButton { get; init; }
    public bool HasDPadLeftButton { get; init; }
    public bool HasDPadRightButton { get; init; }
}

public static class Keyboard
{
    private static KeyboardState _state;
    public static KeyboardState GetState() => _state;
    public static void SetState(KeyboardState state) => _state = state;
}

public static class Mouse
{
    private static MouseState _state;
    public static MouseState GetState() => _state;
    public static void SetPosition(int x, int y) => _state = new MouseState(x, y, _state.ScrollWheelValue, _state.LeftButton, _state.MiddleButton, _state.RightButton, _state.XButton1, _state.XButton2);
    public static void SetState(MouseState state) => _state = state;
}

public static class GamePad
{
    private static readonly GamePadState[] States = new GamePadState[4];

    public static GamePadState GetState(PlayerIndex playerIndex) => States[(int)playerIndex];
    public static GamePadState GetState(PlayerIndex playerIndex, GamePadDeadZone deadZoneMode) => GetState(playerIndex);
    public static GamePadCapabilities GetCapabilities(PlayerIndex playerIndex) => new() { IsConnected = GetState(playerIndex).IsConnected, GamePadType = GamePadType.GamePad };
    public static bool SetVibration(PlayerIndex playerIndex, float leftMotor, float rightMotor) => false;
    public static void SetState(PlayerIndex playerIndex, GamePadState state) => States[(int)playerIndex] = state;
}
