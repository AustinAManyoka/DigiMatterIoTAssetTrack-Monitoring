namespace IoTAssetTrack.Application.DTOs;

public class DeviceQueryParameters
{
    public string? Search { get; set; }
    public int? GroupId { get; set; }
    public int? DeviceTypeId { get; set; }
    public string SortBy { get; set; } = "Name";
    public string SortOrder { get; set; } = "ASC";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class DeviceFlatDto
{
    public int DeviceId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public string FirmwareVersion { get; set; } = string.Empty;
    public string DeviceTypeName { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public int TotalCount { get; set; }
}

public class DeviceCreateDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool IsActive { get; set; } = true;
    public int FirmwareId { get; set; }
    public int? GroupId { get; set; }
}

public class DeviceUpdateDto
{
    public string? Name { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool? IsActive { get; set; }
    public int? FirmwareId { get; set; }
}

public class DeviceGroupAssignmentDto
{
    public int? GroupId { get; set; }
}

public class GeoLocationSearchDto
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public double RadiusKm { get; set; } = 10;
    public bool? IsActive { get; set; }
    public int? DeviceTypeId { get; set; }
}
