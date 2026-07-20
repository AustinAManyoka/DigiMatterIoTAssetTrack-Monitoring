namespace IoTAssetTrack.Domain.Entities;

public class DeviceGroup
{
    public int GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentGroupId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }

    public DeviceGroup? ParentGroup { get; set; }
    public ICollection<DeviceGroup> ChildGroups { get; set; } = [];
    public ICollection<Device> Devices { get; set; } = [];
}
