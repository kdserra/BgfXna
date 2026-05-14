using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.GamerServices;

public enum GamerPrivilegeSetting { Everyone, FriendsOnly, Blocked }
public enum GamerZone { Family, Pro, Recreation, Underground, Unknown }
public enum LeaderboardOutcome { All, Win, Loss }
public enum MessageBoxIcon { None, Alert, Error, Warning }
public enum NotificationPosition { TopLeft, TopCenter, TopRight, CenterLeft, Center, CenterRight, BottomLeft, BottomCenter, BottomRight }

public sealed class GamerServicesNotAvailableException : Exception
{
    public GamerServicesNotAvailableException() { }
    public GamerServicesNotAvailableException(string message) : base(message) { }
}

public class Gamer
{
    public string DisplayName { get; init; } = string.Empty;
    public string Gamertag { get; init; } = string.Empty;
    public bool IsDisposed { get; protected set; }
    public object? Tag { get; set; }
    public override string ToString() => Gamertag;
}

public sealed class SignedInGamer : Gamer
{
    public PlayerIndex PlayerIndex { get; init; }
    public GamerPrivileges Privileges { get; } = new();
    public GamerZone GamerZone { get; init; } = GamerZone.Unknown;
    public bool IsGuest { get; init; }
}

public sealed class GamerPrivileges
{
    public GamerPrivilegeSetting AllowCommunication { get; init; } = GamerPrivilegeSetting.Everyone;
    public bool AllowOnlineSessions { get; init; } = true;
    public bool AllowPremiumContent { get; init; } = true;
    public bool AllowProfileViewing { get; init; } = true;
    public GamerPrivilegeSetting AllowTradeContent { get; init; } = GamerPrivilegeSetting.Everyone;
    public bool AllowUserCreatedContent { get; init; } = true;
}

public sealed class GamerCollection<T> : IEnumerable<T> where T : Gamer
{
    private readonly IReadOnlyList<T> _gamers;
    public GamerCollection(IReadOnlyList<T> gamers) => _gamers = gamers;
    public int Count => _gamers.Count;
    public T this[int index] => _gamers[index];
    public IEnumerator<T> GetEnumerator() => _gamers.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public static class Guide
{
    public static bool IsScreenSaverEnabled { get; set; }
    public static bool IsTrialMode { get; set; }
    public static NotificationPosition NotificationPosition { get; set; } = NotificationPosition.TopRight;
    public static bool IsVisible { get; private set; }
    public static event EventHandler<EventArgs>? GuideVisibilityChanged;

    public static void ShowSignIn(int paneCount, bool onlineOnly) { IsVisible = true; GuideVisibilityChanged?.Invoke(null, EventArgs.Empty); }
    public static IAsyncResult BeginShowMessageBox(string title, string text, IEnumerable<string> buttons, int focusButton, MessageBoxIcon icon, AsyncCallback? callback, object? state) => Complete(0, callback, state);
    public static int? EndShowMessageBox(IAsyncResult result) => ((CompletedAsyncResult<int>)result).Value;
    public static IAsyncResult BeginShowKeyboardInput(PlayerIndex player, string title, string description, string defaultText, AsyncCallback? callback, object? state) => Complete(defaultText, callback, state);
    public static string EndShowKeyboardInput(IAsyncResult result) => ((CompletedAsyncResult<string>)result).Value;

    private static CompletedAsyncResult<T> Complete<T>(T value, AsyncCallback? callback, object? state)
    {
        CompletedAsyncResult<T> result = new(value, state);
        callback?.Invoke(result);
        return result;
    }

    private sealed class CompletedAsyncResult<T> : IAsyncResult
    {
        public CompletedAsyncResult(T value, object? state) { Value = value; AsyncState = state; }
        public T Value { get; }
        public object? AsyncState { get; }
        public System.Threading.WaitHandle AsyncWaitHandle => throw new NotSupportedException();
        public bool CompletedSynchronously => true;
        public bool IsCompleted => true;
    }
}
