namespace IoTAssetTrack.Application.DTOs;

public class FirmwareDto
{
    public int FirmwareId { get; set; }
    public int DeviceTypeId { get; set; }
    public string DeviceTypeName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public int DeviceCount { get; set; }
}

public class FirmwareCreateDto
{
    public int DeviceTypeId { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string? Notes { get; set; }
}

public class FirmwareUpdateDto
{
    public int DeviceTypeId { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string? Notes { get; set; }
}

public class DeviceTypeDto
{
    public int DeviceTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
