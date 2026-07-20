using IoTAssetTrack.Application.DTOs;
using IoTAssetTrack.Application.Interfaces;
using IoTAssetTrack.Domain.Entities;
using IoTAssetTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IoTAssetTrack.Infrastructure.Repositories;

public class FirmwareRepository(AppDbContext context) : IFirmwareRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<FirmwareDto>> GetAllAsync()
    {
        return await _context.Firmware
            .AsNoTracking()
            .Include(f => f.DeviceType)
            .OrderByDescending(f => f.ReleaseDate)
            .Select(f => new FirmwareDto
            {
                FirmwareId = f.FirmwareId,
                DeviceTypeId = f.DeviceTypeId,
                DeviceTypeName = f.DeviceType.Name,
                Version = f.Version,
                ReleaseDate = f.ReleaseDate,
                Notes = f.Notes,
                CreatedDate = f.CreatedDate,
                DeviceCount = f.Devices.Count
            })
            .ToListAsync();
    }

    public async Task<FirmwareDto?> GetByIdAsync(int firmwareId)
    {
        return await _context.Firmware
            .AsNoTracking()
            .Include(f => f.DeviceType)
            .Where(f => f.FirmwareId == firmwareId)
            .Select(f => new FirmwareDto
            {
                FirmwareId = f.FirmwareId,
                DeviceTypeId = f.DeviceTypeId,
                DeviceTypeName = f.DeviceType.Name,
                Version = f.Version,
                ReleaseDate = f.ReleaseDate,
                Notes = f.Notes,
                CreatedDate = f.CreatedDate,
                DeviceCount = f.Devices.Count
            })
            .FirstOrDefaultAsync();
    }

    public async Task<FirmwareDto> CreateAsync(FirmwareCreateDto dto)
    {
        var entity = new Firmware
        {
            DeviceTypeId = dto.DeviceTypeId,
            Version = dto.Version.Trim(),
            ReleaseDate = dto.ReleaseDate.Date,
            Notes = dto.Notes?.Trim(),
            CreatedDate = DateTime.UtcNow
        };

        _context.Firmware.Add(entity);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(entity.FirmwareId))!;
    }

    public async Task<bool> UpdateAsync(int firmwareId, FirmwareUpdateDto dto)
    {
        var entity = await _context.Firmware.FindAsync(firmwareId);
        if (entity is null) return false;

        entity.DeviceTypeId = dto.DeviceTypeId;
        entity.Version = dto.Version.Trim();
        entity.ReleaseDate = dto.ReleaseDate.Date;
        entity.Notes = dto.Notes?.Trim();

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int firmwareId)
    {
        var entity = await _context.Firmware.FindAsync(firmwareId);
        if (entity is null) return false;

        _context.Firmware.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VersionExistsAsync(int deviceTypeId, string version, int? excludeFirmwareId = null)
    {
        var query = _context.Firmware.AsNoTracking()
            .Where(f => f.DeviceTypeId == deviceTypeId && f.Version == version.Trim());

        if (excludeFirmwareId.HasValue)
            query = query.Where(f => f.FirmwareId != excludeFirmwareId.Value);

        return await query.AnyAsync();
    }

    public async Task<IEnumerable<DeviceTypeDto>> GetDeviceTypesAsync()
    {
        return await _context.DeviceTypes
            .AsNoTracking()
            .OrderBy(dt => dt.Name)
            .Select(dt => new DeviceTypeDto
            {
                DeviceTypeId = dt.DeviceTypeId,
                Name = dt.Name,
                Description = dt.Description
            })
            .ToListAsync();
    }
}
