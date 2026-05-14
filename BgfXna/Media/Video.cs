using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Media;

public sealed class Video
{
    public string Name { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public float FramesPerSecond { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public VideoSoundtrackType VideoSoundtrackType { get; init; }
}

public sealed class VideoPlayer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;

    public VideoPlayer(GraphicsDevice graphicsDevice) => _graphicsDevice = graphicsDevice;
    public bool IsDisposed { get; private set; }
    public bool IsLooped { get; set; }
    public bool IsMuted { get; set; }
    public TimeSpan PlayPosition { get; private set; }
    public SoundState State { get; private set; } = SoundState.Stopped;
    public Video? Video { get; private set; }
    public float Volume { get; set; } = 1f;

    public Texture2D GetTexture() => new(_graphicsDevice, Math.Max(Video?.Width ?? 1, 1), Math.Max(Video?.Height ?? 1, 1));
    public void Pause() => State = SoundState.Paused;
    public void Play(Video video)
    {
        Video = video;
        PlayPosition = TimeSpan.Zero;
        State = SoundState.Playing;
    }

    public void Resume() => State = SoundState.Playing;
    public void Stop()
    {
        PlayPosition = TimeSpan.Zero;
        State = SoundState.Stopped;
    }

    public void Dispose() => IsDisposed = true;
}
