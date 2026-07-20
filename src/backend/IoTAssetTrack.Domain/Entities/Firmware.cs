namespace IoTAssetTrack.Domain.Entities;

public class Firmware
{
    public int FirmwareId { get; set; }
    public int DeviceTypeId { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }

    public DeviceType DeviceType { get; set; } = null!;
    public ICollection<Device> Devices { get; set; } = [];
}
