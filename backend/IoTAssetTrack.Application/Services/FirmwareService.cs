using IoTAssetTrack.Application.DTOs;
using IoTAssetTrack.Application.Exceptions;
using IoTAssetTrack.Application.Interfaces;

namespace IoTAssetTrack.Application.Services;

public class FirmwareService(IFirmwareRepository firmwareRepository) : IFirmwareService
{
    private readonly IFirmwareRepository _firmwareRepository = firmwareRepository;

    public Task<IEnumerable<FirmwareDto>> GetAllFirmwareAsync() =>
        _firmwareRepository.GetAllAsync();

    public Task<FirmwareDto?> GetFirmwareByIdAsync(int firmwareId) =>
        _firmwareRepository.GetByIdAsync(firmwareId);

    public async Task<FirmwareDto> CreateFirmwareAsync(FirmwareCreateDto dto)
    {
        ValidateFirmware(dto.Version, dto.DeviceTypeId);

        var deviceTypes = await _firmwareRepository.GetDeviceTypesAsync();
        if (!deviceTypes.Any(dt => dt.DeviceTypeId == dto.DeviceTypeId))
            throw new BusinessException("Device type does not exist.");

        if (await _firmwareRepository.VersionExistsAsync(dto.DeviceTypeId, dto.Version))
            throw new BusinessException($"Firmware version '{dto.Version}' already exists for this device type.");

        return await _firmwareRepository.CreateAsync(dto);
    }

    public async Task UpdateFirmwareAsync(int firmwareId, FirmwareUpdateDto dto)
    {
        ValidateFirmware(dto.Version, dto.DeviceTypeId);

        var existing = await _firmwareRepository.GetByIdAsync(firmwareId) ?? throw new BusinessException("Firmware not found.");
        var deviceTypes = await _firmwareRepository.GetDeviceTypesAsync();
        if (!deviceTypes.Any(dt => dt.DeviceTypeId == dto.DeviceTypeId))
            throw new BusinessException("Device type does not exist.");

        if (await _firmwareRepository.VersionExistsAsync(dto.DeviceTypeId, dto.Version, firmwareId))
            throw new BusinessException($"Firmware version '{dto.Version}' already exists for this device type.");

        var updated = await _firmwareRepository.UpdateAsync(firmwareId, dto);
        if (!updated)
            throw new BusinessException("Failed to update firmware.");
    }

    public async Task DeleteFirmwareAsync(int firmwareId)
    {
        var existing = await _firmwareRepository.GetByIdAsync(firmwareId) ?? throw new BusinessException("Firmware not found.");
        if (existing.DeviceCount > 0)
            throw new BusinessException("Cannot delete firmware that is assigned to devices.");

        var deleted = await _firmwareRepository.DeleteAsync(firmwareId);
        if (!deleted)
            throw new BusinessException("Failed to delete firmware.");
    }

    public Task<IEnumerable<DeviceTypeDto>> GetDeviceTypesAsync() =>
        _firmwareRepository.GetDeviceTypesAsync();

    private static void ValidateFirmware(string version, int deviceTypeId)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new BusinessException("Firmware version is required.");

        if (deviceTypeId <= 0)
            throw new BusinessException("A valid device type is required.");
    }
}
