using IoTAssetTrack.Application.Common;
using IoTAssetTrack.Application.DTOs;
using IoTAssetTrack.Application.Exceptions;
using IoTAssetTrack.Application.Interfaces;

namespace IoTAssetTrack.Application.Services;

public class DeviceService(
    IDeviceRepository deviceRepository,
    IFirmwareRepository firmwareRepository,
    IGroupRepository groupRepository) : IDeviceService
{
    private readonly IDeviceRepository _deviceRepository = deviceRepository;
    private readonly IFirmwareRepository _firmwareRepository = firmwareRepository;
    private readonly IGroupRepository _groupRepository = groupRepository;

    public Task<PagedResponse<DeviceFlatDto>> GetDevicesAsync(DeviceQueryParameters parameters)
    {
        if (parameters.Page < 1) parameters.Page = 1;
        if (parameters.PageSize < 1 || parameters.PageSize > 100) parameters.PageSize = 25;

        var allowedSortColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Name", "SerialNumber", "CreatedDate"
        };

        if (!allowedSortColumns.Contains(parameters.SortBy))
            parameters.SortBy = "Name";

        parameters.SortOrder = parameters.SortOrder.Equals("DESC", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

        return _deviceRepository.GetDevicesAsync(parameters);
    }

    public Task<DeviceFlatDto?> GetDeviceByIdAsync(int deviceId) =>
        _deviceRepository.GetByIdAsync(deviceId);

    public async Task<DeviceFlatDto> CreateDeviceAsync(DeviceCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SerialNumber))
            throw new BusinessException("Serial number is required.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BusinessException("Device name is required.");

        if (dto.Latitude is < -90 or > 90)
            throw new BusinessException("Latitude must be between -90 and 90.");

        if (dto.Longitude is < -180 or > 180)
            throw new BusinessException("Longitude must be between -180 and 180.");
        _ = await _firmwareRepository.GetByIdAsync(dto.FirmwareId) ?? throw new BusinessException("Firmware not found.");
        if (dto.GroupId.HasValue)
        {
            _ = await _groupRepository.GetByIdAsync(dto.GroupId.Value) ?? throw new BusinessException("Group not found.");
        }

        return await _deviceRepository.CreateAsync(dto);
    }

    public async Task<DeviceFlatDto> UpdateDeviceAsync(int deviceId, DeviceUpdateDto dto)
    {
        if (!await _deviceRepository.ExistsAsync(deviceId))
            throw new BusinessException("Device not found.");

        // Validate latitude if provided
        if (dto.Latitude.HasValue && (dto.Latitude.Value < -90 || dto.Latitude.Value > 90))
            throw new BusinessException("Latitude must be between -90 and 90.");

        // Validate longitude if provided
        if (dto.Longitude.HasValue && (dto.Longitude.Value < -180 || dto.Longitude.Value > 180))
            throw new BusinessException("Longitude must be between -180 and 180.");

        // Validate firmware if provided
        if (dto.FirmwareId.HasValue)
        {
            _ = await _firmwareRepository.GetByIdAsync(dto.FirmwareId.Value) ?? throw new BusinessException("Firmware not found.");
        }

        var success = await _deviceRepository.UpdateAsync(deviceId, dto);
        if (!success)
            throw new BusinessException("Failed to update device.");

        return (await _deviceRepository.GetByIdAsync(deviceId))!;
    }

    public async Task<bool> AssignDeviceToGroupAsync(int deviceId, int? groupId)
    {
        if (!await _deviceRepository.ExistsAsync(deviceId))
            throw new BusinessException("Device not found.");

        if (groupId.HasValue)
        {
            _ = await _groupRepository.GetByIdAsync(groupId.Value) ?? throw new BusinessException("Group not found.");
        }

        return await _deviceRepository.UpdateDeviceGroupAsync(deviceId, groupId);
    }

    public async Task<IEnumerable<DeviceFlatDto>> SearchDevicesByLocationAsync(GeoLocationSearchDto searchDto)
    {
        if (searchDto.Latitude < -90 || searchDto.Latitude > 90)
            throw new BusinessException("Latitude must be between -90 and 90.");

        if (searchDto.Longitude < -180 || searchDto.Longitude > 180)
            throw new BusinessException("Longitude must be between -180 and 180.");

        if (searchDto.RadiusKm <= 0)
            throw new BusinessException("Radius must be greater than 0.");

        return await _deviceRepository.GetDevicesByLocationAsync(
            searchDto.Latitude,
            searchDto.Longitude,
            searchDto.RadiusKm,
            searchDto.IsActive,
            searchDto.DeviceTypeId);
    }
}
