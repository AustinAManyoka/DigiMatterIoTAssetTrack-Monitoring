using IoTAssetTrack.Application.Common;
using IoTAssetTrack.Application.DTOs;

namespace IoTAssetTrack.Application.Interfaces;

public interface IGroupService
{
    Task<IEnumerable<GroupDto>> GetAllGroupsAsync();
    Task<GroupDto?> GetGroupByIdAsync(int groupId);
    Task<GroupDto> CreateGroupAsync(GroupCreateDto dto);
    Task UpdateGroupAsync(int groupId, GroupUpdateDto dto);
    Task DeleteGroupAsync(int groupId);
}

public interface IFirmwareService
{
    Task<IEnumerable<FirmwareDto>> GetAllFirmwareAsync();
    Task<FirmwareDto?> GetFirmwareByIdAsync(int firmwareId);
    Task<FirmwareDto> CreateFirmwareAsync(FirmwareCreateDto dto);
    Task UpdateFirmwareAsync(int firmwareId, FirmwareUpdateDto dto);
    Task DeleteFirmwareAsync(int firmwareId);
    Task<IEnumerable<DeviceTypeDto>> GetDeviceTypesAsync();
}

public interface IDeviceService
{
    Task<PagedResponse<DeviceFlatDto>> GetDevicesAsync(DeviceQueryParameters parameters);
    Task<DeviceFlatDto?> GetDeviceByIdAsync(int deviceId);
    Task<DeviceFlatDto> CreateDeviceAsync(DeviceCreateDto dto);
    Task<DeviceFlatDto> UpdateDeviceAsync(int deviceId, DeviceUpdateDto dto);
    Task<bool> AssignDeviceToGroupAsync(int deviceId, int? groupId);
    Task DeviceDeleteAsync(int deviceId);
    Task<IEnumerable<DeviceFlatDto>> SearchDevicesByLocationAsync(GeoLocationSearchDto searchDto);
    Task DeleteDeviceAsync(int id);
}
