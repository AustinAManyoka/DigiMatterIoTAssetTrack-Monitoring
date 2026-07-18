namespace IoTAssetTrack.Domain.Entities;

public class Device
{
    public int DeviceId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool IsActive { get; set; } = true;
    public int FirmwareId { get; set; }
    public int? GroupId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModified { get; set; }

    public Firmware Firmware { get; set; } = null!;
    public DeviceGroup? Group { get; set; }
}
