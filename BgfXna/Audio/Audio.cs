using System;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Audio;

public enum AudioChannels
{
    Mono,
    Stereo
}

public enum AudioCategory
{
    Ambient,
    Communications,
    GameEffects,
    GameMedia,
    SoundEffects
}

public enum SoundState
{
    Playing,
    Paused,
    Stopped
}

public enum AudioStopOptions
{
    AsAuthored,
    Immediate
}

public enum MicrophoneState
{
    Started,
    Stopped
}

public enum RendererDetail
{
    Default,
    Generic,
    HighDefinition
}

public sealed class NoAudioHardwareException : Exception
{
    public NoAudioHardwareException() { }
    public NoAudioHardwareException(string message) : base(message) { }
}

public sealed class InstancePlayLimitException : Exception
{
    public InstancePlayLimitException() { }
    public InstancePlayLimitException(string message) : base(message) { }
}

public sealed class NoMicrophoneConnectedException : Exception
{
    public NoMicrophoneConnectedException() { }
    public NoMicrophoneConnectedException(string message) : base(message) { }
}

public class SoundEffect : IDisposable
{
    public SoundEffect(byte[] buffer, int sampleRate, AudioChannels channels)
    {
        SampleRate = sampleRate;
        Channels = channels;
    }

    public static float MasterVolume { get; set; } = 1f;
    public static float DistanceScale { get; set; } = 1f;
    public static float DopplerScale { get; set; } = 1f;
    public static float SpeedOfSound { get; set; } = 343.5f;
    public int SampleRate { get; }
    public AudioChannels Channels { get; }
    public TimeSpan Duration { get; init; }
    public SoundEffectInstance CreateInstance() => new(this);
    public bool Play() => true;
    public bool Play(float volume, float pitch, float pan) => true;
    public void Dispose() { }
}

public class SoundEffectInstance : IDisposable
{
    internal SoundEffectInstance(SoundEffect soundEffect) => SoundEffect = soundEffect;
    public SoundEffect SoundEffect { get; }
    public bool IsLooped { get; set; }
    public float Pan { get; set; }
    public float Pitch { get; set; }
    public float Volume { get; set; } = 1f;
    public SoundState State { get; private set; } = SoundState.Stopped;
    public void Play() => State = SoundState.Playing;
    public void Pause() => State = SoundState.Paused;
    public void Resume() => State = SoundState.Playing;
    public void Stop() => State = SoundState.Stopped;
    public void Stop(bool immediate) => Stop();
    public void Apply3D(AudioListener listener, AudioEmitter emitter) { }
    public void Dispose() { }
}

public sealed class DynamicSoundEffectInstance : SoundEffectInstance
{
    public DynamicSoundEffectInstance(int sampleRate, AudioChannels channels) : base(new SoundEffect(Array.Empty<byte>(), sampleRate, channels)) { }
    public int PendingBufferCount { get; private set; }
    public event EventHandler<EventArgs>? BufferNeeded;
    public void SubmitBuffer(byte[] buffer) => PendingBufferCount++;
    public void SubmitBuffer(byte[] buffer, int offset, int count) => PendingBufferCount++;
}

public sealed class AudioEngine : IDisposable
{
    public AudioEngine(string settingsFile) => SettingsFile = settingsFile;
    public AudioEngine(string settingsFile, TimeSpan lookAheadTime, string rendererId) => SettingsFile = settingsFile;
    public string SettingsFile { get; }
    public bool IsDisposed { get; private set; }
    public float GetGlobalVariable(string name) => 0f;
    public void SetGlobalVariable(string name, float value) { }
    public void Update() { }
    public void Dispose() => IsDisposed = true;
}

public sealed class WaveBank : IDisposable
{
    public WaveBank(AudioEngine audioEngine, string nonStreamingWaveBankFilename) => AudioEngine = audioEngine;
    public WaveBank(AudioEngine audioEngine, string streamingWaveBankFilename, int offset, short packetsize) => AudioEngine = audioEngine;
    public AudioEngine AudioEngine { get; }
    public bool IsDisposed { get; private set; }
    public bool IsPrepared => true;
    public void Dispose() => IsDisposed = true;
}

public sealed class SoundBank : IDisposable
{
    public SoundBank(AudioEngine audioEngine, string filename) => AudioEngine = audioEngine;
    public AudioEngine AudioEngine { get; }
    public bool IsDisposed { get; private set; }
    public Cue GetCue(string name) => new(name);
    public void PlayCue(string name) => GetCue(name).Play();
    public void Dispose() => IsDisposed = true;
}

public sealed class Cue : IDisposable
{
    internal Cue(string name) => Name = name;
    public string Name { get; }
    public bool IsCreated => true;
    public bool IsDisposed { get; private set; }
    public bool IsPaused => State == SoundState.Paused;
    public bool IsPlaying => State == SoundState.Playing;
    public bool IsPrepared => true;
    public bool IsPreparing => false;
    public bool IsStopped => State == SoundState.Stopped;
    public SoundState State { get; private set; } = SoundState.Stopped;
    public void Apply3D(AudioListener listener, AudioEmitter emitter) { }
    public float GetVariable(string name) => 0f;
    public void Pause() => State = SoundState.Paused;
    public void Play() => State = SoundState.Playing;
    public void Resume() => State = SoundState.Playing;
    public void SetVariable(string name, float value) { }
    public void Stop(AudioStopOptions options) => State = SoundState.Stopped;
    public void Dispose() => IsDisposed = true;
}

public sealed class AudioEmitter
{
    public Vector3 Position { get; set; }
    public Vector3 Forward { get; set; } = Vector3.Forward;
    public Vector3 Up { get; set; } = Vector3.Up;
    public Vector3 Velocity { get; set; }
    public float DopplerScale { get; set; } = 1f;
}

public sealed class AudioListener
{
    public Vector3 Position { get; set; }
    public Vector3 Forward { get; set; } = Vector3.Forward;
    public Vector3 Up { get; set; } = Vector3.Up;
    public Vector3 Velocity { get; set; }
}

public sealed class Microphone
{
    public static Microphone? Default { get; }
    public string Name { get; init; } = string.Empty;
    public MicrophoneState State { get; private set; } = MicrophoneState.Stopped;
    public TimeSpan BufferDuration { get; set; } = TimeSpan.FromMilliseconds(100);
    public void Start() => State = MicrophoneState.Started;
    public void Stop() => State = MicrophoneState.Stopped;
    public int GetData(byte[] buffer) => 0;
}
