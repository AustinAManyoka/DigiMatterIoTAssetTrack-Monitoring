using IoTAssetTrack.Application.DTOs;
using IoTAssetTrack.Application.Interfaces;
using IoTAssetTrack.Domain.Entities;
using IoTAssetTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IoTAssetTrack.Infrastructure.Repositories;

public class GroupRepository(AppDbContext context) : IGroupRepository
{
    private readonly AppDbContext _context = context;

    public async Task<IEnumerable<GroupDto>> GetAllAsync()
    {
        return await _context.DeviceGroups
            .AsNoTracking()
            .Include(g => g.ParentGroup)
            .OrderBy(g => g.Name)
            .Select(g => new GroupDto
            {
                GroupId = g.GroupId,
                Name = g.Name,
                ParentGroupId = g.ParentGroupId,
                ParentGroupName = g.ParentGroup != null ? g.ParentGroup.Name : null,
                Description = g.Description,
                CreatedDate = g.CreatedDate,
                ChildGroupCount = g.ChildGroups.Count,
                DeviceCount = g.Devices.Count
            })
            .ToListAsync();
    }

    public async Task<GroupDto?> GetByIdAsync(int groupId)
    {
        return await _context.DeviceGroups
            .AsNoTracking()
            .Include(g => g.ParentGroup)
            .Where(g => g.GroupId == groupId)
            .Select(g => new GroupDto
            {
                GroupId = g.GroupId,
                Name = g.Name,
                ParentGroupId = g.ParentGroupId,
                ParentGroupName = g.ParentGroup != null ? g.ParentGroup.Name : null,
                Description = g.Description,
                CreatedDate = g.CreatedDate,
                ChildGroupCount = g.ChildGroups.Count,
                DeviceCount = g.Devices.Count
            })
            .FirstOrDefaultAsync();
    }

    public async Task<GroupDto> CreateAsync(GroupCreateDto dto)
    {
        var entity = new DeviceGroup
        {
            Name = dto.Name.Trim(),
            ParentGroupId = dto.ParentGroupId,
            Description = dto.Description?.Trim(),
            CreatedDate = DateTime.UtcNow
        };

        _context.DeviceGroups.Add(entity);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(entity.GroupId))!;
    }

    public async Task<bool> UpdateAsync(int groupId, GroupUpdateDto dto)
    {
        var entity = await _context.DeviceGroups.FindAsync(groupId);
        if (entity is null) return false;

        entity.Name = dto.Name.Trim();
        entity.ParentGroupId = dto.ParentGroupId;
        entity.Description = dto.Description?.Trim();

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int groupId)
    {
        var entity = await _context.DeviceGroups.FindAsync(groupId);
        if (entity is null) return false;

        _context.DeviceGroups.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> WouldCreateCycleAsync(int groupId, int? newParentGroupId)
    {
        if (!newParentGroupId.HasValue) return false;

        var currentId = newParentGroupId;
        var visited = new HashSet<int> { groupId };

        while (currentId.HasValue)
        {
            if (visited.Contains(currentId.Value))
                return true;

            visited.Add(currentId.Value);

            var parent = await _context.DeviceGroups
                .AsNoTracking()
                .Where(g => g.GroupId == currentId.Value)
                .Select(g => g.ParentGroupId)
                .FirstOrDefaultAsync();

            currentId = parent;
        }

        return false;
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeGroupId = null)
    {
        var query = _context.DeviceGroups.AsNoTracking()
            .Where(g => g.Name == name.Trim());

        if (excludeGroupId.HasValue)
            query = query.Where(g => g.GroupId != excludeGroupId.Value);

        return await query.AnyAsync();
    }
}
