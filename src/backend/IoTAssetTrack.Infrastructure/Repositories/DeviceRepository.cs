using Dapper;
using IoTAssetTrack.Application.Common;
using IoTAssetTrack.Application.DTOs;
using IoTAssetTrack.Application.Interfaces;
using IoTAssetTrack.Domain.Entities;
using IoTAssetTrack.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IoTAssetTrack.Infrastructure.Repositories;

public class DeviceRepository(IConfiguration configuration, AppDbContext context) : IDeviceRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    private readonly AppDbContext _context = context;

    public async Task<PagedResponse<DeviceFlatDto>> GetDevicesAsync(DeviceQueryParameters parameters)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = """
            SELECT 
                d.DeviceId, d.SerialNumber, d.Name, d.Latitude, d.Longitude, d.IsActive, d.CreatedDate,
                f.Version AS FirmwareVersion,
                dt.Name AS DeviceTypeName,
                dg.Name AS GroupName,
                COUNT(*) OVER() AS TotalCount
            FROM Device d
            JOIN Firmware f ON d.FirmwareId = f.FirmwareId
            JOIN DeviceType dt ON f.DeviceTypeId = dt.DeviceTypeId
            LEFT JOIN DeviceGroup dg ON d.GroupId = dg.GroupId
            WHERE 
                (@Search IS NULL OR d.SerialNumber LIKE '%' + @Search + '%' OR d.Name LIKE '%' + @Search + '%')
                AND (@GroupId IS NULL OR d.GroupId = @GroupId)
                AND (@DeviceTypeId IS NULL OR f.DeviceTypeId = @DeviceTypeId)
            ORDER BY 
                CASE WHEN @SortBy = 'Name' AND @SortOrder = 'ASC' THEN d.Name END ASC,
                CASE WHEN @SortBy = 'Name' AND @SortOrder = 'DESC' THEN d.Name END DESC,
                CASE WHEN @SortBy = 'SerialNumber' AND @SortOrder = 'ASC' THEN d.SerialNumber END ASC,
                CASE WHEN @SortBy = 'SerialNumber' AND @SortOrder = 'DESC' THEN d.SerialNumber END DESC,
                CASE WHEN @SortBy = 'CreatedDate' AND @SortOrder = 'ASC' THEN d.CreatedDate END ASC,
                CASE WHEN @SortBy = 'CreatedDate' AND @SortOrder = 'DESC' THEN d.CreatedDate END DESC
            OFFSET (@Page - 1) * @PageSize ROWS
            FETCH NEXT @PageSize ROWS ONLY;
            """;

        var results = (await connection.QueryAsync<DeviceFlatDto>(sql, new
        {
            Search = string.IsNullOrWhiteSpace(parameters.Search) ? null : parameters.Search,
            parameters.GroupId,
            parameters.DeviceTypeId,
            parameters.SortBy,
            parameters.SortOrder,
            parameters.Page,
            parameters.PageSize
        })).ToList();

        var totalRecords = results.FirstOrDefault()?.TotalCount ?? 0;
        return new PagedResponse<DeviceFlatDto>(results, totalRecords, parameters.Page, parameters.PageSize);
    }

    public async Task<DeviceFlatDto?> GetByIdAsync(int deviceId)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = """
            SELECT 
                d.DeviceId, d.SerialNumber, d.Name, d.Latitude, d.Longitude, d.IsActive, d.CreatedDate,
                f.Version AS FirmwareVersion,
                dt.Name AS DeviceTypeName,
                dg.Name AS GroupName,
                1 AS TotalCount
            FROM Device d
            JOIN Firmware f ON d.FirmwareId = f.FirmwareId
            JOIN DeviceType dt ON f.DeviceTypeId = dt.DeviceTypeId
            LEFT JOIN DeviceGroup dg ON d.GroupId = dg.GroupId
            WHERE d.DeviceId = @DeviceId;
            """;

        return await connection.QueryFirstOrDefaultAsync<DeviceFlatDto>(sql, new { DeviceId = deviceId });
    }

    public async Task<DeviceFlatDto> CreateAsync(DeviceCreateDto dto)
    {
        var entity = new Device
        {
            SerialNumber = dto.SerialNumber.Trim(),
            Name = dto.Name.Trim(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            IsActive = dto.IsActive,
            FirmwareId = dto.FirmwareId,
            GroupId = dto.GroupId,
            CreatedDate = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };

        _context.Devices.Add(entity);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(entity.DeviceId))!;
    }

    public async Task<bool> UpdateAsync(int deviceId, DeviceUpdateDto dto)
    {
        var device = await _context.Devices.FindAsync(deviceId);
        if (device is null) return false;

        if (!string.IsNullOrWhiteSpace(dto.Name))
            device.Name = dto.Name.Trim();

        if (dto.Latitude.HasValue)
            device.Latitude = dto.Latitude.Value;

        if (dto.Longitude.HasValue)
            device.Longitude = dto.Longitude.Value;

        if (dto.IsActive.HasValue)
            device.IsActive = dto.IsActive.Value;

        if (dto.FirmwareId.HasValue)
            device.FirmwareId = dto.FirmwareId.Value;

        device.LastModified = DateTime.UtcNow;

        _context.Devices.Update(device);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateDeviceGroupAsync(int deviceId, int? groupId)
    {
        var device = await _context.Devices.FindAsync(deviceId);
        if (device is null) return false;

        // Validate group exists if not null
        if (groupId.HasValue)
        {
            var groupExists = await _context.DeviceGroups.AnyAsync(g => g.GroupId == groupId.Value);
            if (!groupExists) return false;
        }

        device.GroupId = groupId;
        device.LastModified = DateTime.UtcNow;

        _context.Devices.Update(device);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<DeviceFlatDto>> GetDevicesByLocationAsync(
        decimal latitude, decimal longitude, double radiusKm, bool? isActive = null, int? deviceTypeId = null)
    {
        using var connection = new SqlConnection(_connectionString);

        // Haversine formula for distance calculation in SQL
        const string sql = """
            SELECT 
                d.DeviceId, d.SerialNumber, d.Name, d.Latitude, d.Longitude, d.IsActive, d.CreatedDate,
                f.Version AS FirmwareVersion,
                dt.Name AS DeviceTypeName,
                dg.Name AS GroupName,
                1 AS TotalCount
            FROM Device d
            JOIN Firmware f ON d.FirmwareId = f.FirmwareId
            JOIN DeviceType dt ON f.DeviceTypeId = dt.DeviceTypeId
            LEFT JOIN DeviceGroup dg ON d.GroupId = dg.GroupId
            WHERE 
                (3959 * ACOS(
                    COS(RADIANS(90 - @Latitude)) * COS(RADIANS(90 - d.Latitude)) +
                    SIN(RADIANS(90 - @Latitude)) * SIN(RADIANS(90 - d.Latitude)) * 
                    COS(RADIANS(@Longitude - d.Longitude))
                )) <= @RadiusKm
                AND (@IsActive IS NULL OR d.IsActive = @IsActive)
                AND (@DeviceTypeId IS NULL OR dt.DeviceTypeId = @DeviceTypeId)
            ORDER BY d.Name;
            """;

        return await connection.QueryAsync<DeviceFlatDto>(sql, new
        {
            Latitude = latitude,
            Longitude = longitude,
            RadiusKm = radiusKm,
            IsActive = isActive,
            DeviceTypeId = deviceTypeId
        });
    }

    public async Task DeleteAsync(DeviceDeleteDto device)
    {
        device.IsActive = false;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int deviceId)
    {
        return await _context.Devices.AnyAsync(d => d.DeviceId == deviceId);
    }

    Task<bool> IDeviceRepository.DeleteAsync(int deviceId, DeviceDeleteDto dto)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(DeviceFlatDto device)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }
}

