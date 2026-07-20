namespace IoTAssetTrack.Domain.Entities;

public class DeviceType
{
    public int DeviceTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Firmware> FirmwareVersions { get; set; } = [];
}
