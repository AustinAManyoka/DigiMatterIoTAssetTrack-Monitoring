namespace IoTAssetTrack.Application.DTOs;

public class GroupDto
{
    public int GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentGroupId { get; set; }
    public string? ParentGroupName { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public int ChildGroupCount { get; set; }
    public int DeviceCount { get; set; }
}

public class GroupCreateDto
{
    public string Name { get; set; } = string.Empty;
    public int? ParentGroupId { get; set; }
    public string? Description { get; set; }
}

public class GroupUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public int? ParentGroupId { get; set; }
    public string? Description { get; set; }
}
