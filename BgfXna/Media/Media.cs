using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Media;

public enum MediaState { Stopped, Playing, Paused }
public enum MediaSourceType { LocalDevice, WindowsMediaConnect, MediaCenter }
public enum VideoSoundtrackType { Music, Dialog, MusicAndDialog }

public sealed class MediaSource
{
    public MediaSourceType MediaSourceType { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class VisualizationData
{
    public float[] Frequencies { get; } = new float[256];
    public float[] Samples { get; } = new float[256];
}

public sealed class Song
{
    public string Name { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public Artist Artist { get; init; } = new();
    public Album Album { get; init; } = new();
    public Genre Genre { get; init; } = new();
    public bool IsProtected { get; init; }
    public bool IsRated { get; init; }
    public int PlayCount { get; init; }
    public int Rating { get; init; }
    public int TrackNumber { get; init; }
}

public sealed class Album
{
    public string Name { get; init; } = string.Empty;
    public Artist Artist { get; init; } = new();
    public Genre Genre { get; init; } = new();
    public TimeSpan Duration { get; init; }
    public SongCollection Songs { get; init; } = new(Array.Empty<Song>());
    public bool HasArt { get; init; }
}

public sealed class Artist
{
    public string Name { get; init; } = string.Empty;
    public AlbumCollection Albums { get; init; } = new(Array.Empty<Album>());
    public SongCollection Songs { get; init; } = new(Array.Empty<Song>());
}

public sealed class Genre
{
    public string Name { get; init; } = string.Empty;
    public AlbumCollection Albums { get; init; } = new(Array.Empty<Album>());
    public SongCollection Songs { get; init; } = new(Array.Empty<Song>());
}

public sealed class Playlist
{
    public string Name { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public SongCollection Songs { get; init; } = new(Array.Empty<Song>());
}

public sealed class Picture : IDisposable
{
    public string Name { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public void Dispose() { }
}

public sealed class PictureAlbum
{
    public string Name { get; init; } = string.Empty;
    public PictureCollection Pictures { get; init; } = new(Array.Empty<Picture>());
    public PictureAlbumCollection Albums { get; init; } = new(Array.Empty<PictureAlbum>());
}

public abstract class MediaCollection<T> : IEnumerable<T>
{
    private readonly IReadOnlyList<T> _items;
    protected MediaCollection(IReadOnlyList<T> items) => _items = items;
    public int Count => _items.Count;
    public T this[int index] => _items[index];
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class SongCollection : MediaCollection<Song> { public SongCollection(IReadOnlyList<Song> items) : base(items) { } }
public sealed class AlbumCollection : MediaCollection<Album> { public AlbumCollection(IReadOnlyList<Album> items) : base(items) { } }
public sealed class ArtistCollection : MediaCollection<Artist> { public ArtistCollection(IReadOnlyList<Artist> items) : base(items) { } }
public sealed class GenreCollection : MediaCollection<Genre> { public GenreCollection(IReadOnlyList<Genre> items) : base(items) { } }
public sealed class PlaylistCollection : MediaCollection<Playlist> { public PlaylistCollection(IReadOnlyList<Playlist> items) : base(items) { } }
public sealed class PictureCollection : MediaCollection<Picture> { public PictureCollection(IReadOnlyList<Picture> items) : base(items) { } }
public sealed class PictureAlbumCollection : MediaCollection<PictureAlbum> { public PictureAlbumCollection(IReadOnlyList<PictureAlbum> items) : base(items) { } }

public sealed class MediaLibrary : IDisposable
{
    public MediaLibrary() { }
    public MediaLibrary(MediaSource mediaSource) => MediaSource = mediaSource;
    public MediaSource? MediaSource { get; }
    public AlbumCollection Albums { get; } = new(Array.Empty<Album>());
    public ArtistCollection Artists { get; } = new(Array.Empty<Artist>());
    public GenreCollection Genres { get; } = new(Array.Empty<Genre>());
    public PlaylistCollection Playlists { get; } = new(Array.Empty<Playlist>());
    public SongCollection Songs { get; } = new(Array.Empty<Song>());
    public PictureAlbum RootPictureAlbum { get; } = new();
    public void Dispose() { }
}

public sealed class MediaQueue
{
    private readonly List<Song> _songs = new();
    internal void Set(IEnumerable<Song> songs)
    {
        _songs.Clear();
        _songs.AddRange(songs);
        ActiveSong = _songs.Count == 0 ? null : _songs[0];
    }

    public Song? ActiveSong { get; private set; }
    public int ActiveSongIndex => ActiveSong is null ? -1 : _songs.IndexOf(ActiveSong);
    public int Count => _songs.Count;
    public Song this[int index] => _songs[index];
}

public static class MediaPlayer
{
    public static bool GameHasControl { get; private set; } = true;
    public static bool IsMuted { get; set; }
    public static bool IsRepeating { get; set; }
    public static bool IsShuffled { get; set; }
    public static TimeSpan PlayPosition { get; private set; }
    public static MediaQueue Queue { get; } = new();
    public static MediaState State { get; private set; } = MediaState.Stopped;
    public static float Volume { get; set; } = 1f;
    public static event EventHandler<EventArgs>? ActiveSongChanged;
    public static event EventHandler<EventArgs>? MediaStateChanged;

    public static void Play(Song song)
    {
        Queue.Set(new[] { song });
        PlayPosition = TimeSpan.Zero;
        State = MediaState.Playing;
        ActiveSongChanged?.Invoke(null, EventArgs.Empty);
        MediaStateChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void Play(SongCollection songs) => Play((IEnumerable<Song>)songs);
    public static void Play(SongCollection songs, int startIndex) => Play(songs[startIndex]);
    public static void Play(Playlist playlist) => Play(playlist.Songs);
    public static void Play(IEnumerable<Song> songs)
    {
        Queue.Set(songs);
        PlayPosition = TimeSpan.Zero;
        State = Queue.Count == 0 ? MediaState.Stopped : MediaState.Playing;
        ActiveSongChanged?.Invoke(null, EventArgs.Empty);
        MediaStateChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void Pause() { State = MediaState.Paused; MediaStateChanged?.Invoke(null, EventArgs.Empty); }
    public static void Resume() { State = MediaState.Playing; MediaStateChanged?.Invoke(null, EventArgs.Empty); }
    public static void Stop() { State = MediaState.Stopped; PlayPosition = TimeSpan.Zero; MediaStateChanged?.Invoke(null, EventArgs.Empty); }
    public static void MoveNext() { }
    public static void MovePrevious() { }
    public static void GetVisualizationData(VisualizationData visualizationData) { }
}
