namespace SoundMixer.Api;

public sealed class IpcPresetDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
}

public sealed class IpcGroupDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? ParentId { get; set; }
    public uint LabelColorArgb { get; set; }
    public float Volume { get; set; }
    public float EffectiveVolume { get; set; }
}

/// <summary>UI summary of one IPC caller's active temporary overrides.</summary>
public sealed class IpcOverrideSummary
{
    public string Tag { get; init; } = "";
    public int Priority { get; init; }
    public List<string> DetailLines { get; init; } = new();
    public List<IpcOverrideGroupVolumeLine> GroupVolumeLines { get; init; } = new();
}

public sealed class IpcOverrideGroupVolumeLine
{
    public string GroupName { get; init; } = "";
    public int EffectivePercent { get; init; }
}
