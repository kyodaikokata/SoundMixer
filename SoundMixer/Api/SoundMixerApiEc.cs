namespace SoundMixer.Api;

/// <summary>IPC return codes (Penumbra-style).</summary>
public enum SoundMixerApiEc : int
{
    Success = 0,
    InvalidArgument = 1,
    NotFound = 2,
    InvalidTag = 3,
    Unknown = 4,
}
