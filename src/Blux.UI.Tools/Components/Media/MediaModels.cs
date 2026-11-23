namespace Blux.UI.Tools;

/// <summary>Controls that can be selectively hidden on <see cref="BxAudioPlayer"/> and <see cref="BxVideoPlayer"/>.</summary>
public enum MediaPlayerControl
{
    Time,
    Volume,
    Rate,
    Captions,
    PictureInPicture,
    Fullscreen,
    Download,
}

/// <summary>A subtitle or caption track for <see cref="BxVideoPlayer"/>.</summary>
public sealed record VideoPlayerTrack(
    string Src,
    string SrcLang,
    string Label,
    string Kind = "subtitles",
    bool Default = false);

/// <summary>Playback progress snapshot reported by <see cref="BxAudioPlayer"/> and <see cref="BxVideoPlayer"/>.</summary>
/// <param name="Position">Seconds elapsed relative to the clip start.</param>
/// <param name="Length">Total clip duration in seconds.</param>
/// <param name="CurrentTime">Raw media element time in seconds.</param>
public sealed record MediaPlayerProgress(
    double Position,
    double Length,
    double CurrentTime);
