using IoTAssetTrack.Application.DTOs;

namespace IoTAssetTrack.Application.Interfaces;

public interface IGroupRepository
{
    Task<IEnumerable<GroupDto>> GetAllAsync();
    Task<GroupDto?> GetByIdAsync(int groupId);
    Task<GroupDto> CreateAsync(GroupCreateDto dto);
    Task<bool> UpdateAsync(int groupId, GroupUpdateDto dto);
    Task<bool> DeleteAsync(int groupId);
    Task<bool> WouldCreateCycleAsync(int groupId, int? newParentGroupId);
    Task<bool> NameExistsAsync(string name, int? excludeGroupId = null);
}

public interface IFirmwareRepository
{
    Task<IEnumerable<FirmwareDto>> GetAllAsync();
    Task<FirmwareDto?> GetByIdAsync(int firmwareId);
    Task<FirmwareDto> CreateAsync(FirmwareCreateDto dto);
    Task<bool> UpdateAsync(int firmwareId, FirmwareUpdateDto dto);
    Task<bool> DeleteAsync(int firmwareId);
    Task<bool> VersionExistsAsync(int deviceTypeId, string version, int? excludeFirmwareId = null);
    Task<IEnumerable<DeviceTypeDto>> GetDeviceTypesAsync();
}

public interface IDeviceRepository
{
    Task<Common.PagedResponse<DeviceFlatDto>> GetDevicesAsync(DeviceQueryParameters parameters);
    Task<DeviceFlatDto?> GetByIdAsync(int deviceId);
    Task<DeviceFlatDto> CreateAsync(DeviceCreateDto dto);
    Task<bool> UpdateAsync(int deviceId, DeviceUpdateDto dto);
    Task<bool> UpdateDeviceGroupAsync(int deviceId, int? groupId);
    Task<IEnumerable<DeviceFlatDto>> GetDevicesByLocationAsync(decimal latitude, decimal longitude, double radiusKm, bool? isActive = null, int? deviceTypeId = null);
    Task<bool> ExistsAsync(int deviceId);
}
