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
        // Retrieve only the fields needed to build the hierarchy.
        var groups = await _context.DeviceGroups
            .AsNoTracking()
            .Select(g => new
            {
                g.GroupId,
                g.Name,
                g.ParentGroupId,
                g.Description,
                g.CreatedDate
            })
            .ToListAsync();

        // Lookup used to resolve parent group names efficiently.
        var groupLookup = groups.ToDictionary(g => g.GroupId);

        // Count devices assigned directly to each group.
        var directDeviceCounts = await _context.Devices
            .AsNoTracking()
            .Where(d => d.GroupId != null)
            .GroupBy(d => d.GroupId!.Value)
            .Select(g => new
            {
                GroupId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count);

        // Count immediate child groups.
        var childCounts = groups
            .Where(g => g.ParentGroupId.HasValue)
            .GroupBy(g => g.ParentGroupId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // Compute total devices for each group, including all descendant groups.
        var recursiveDeviceCounts = ComputeRecursiveDeviceCounts(
            groups.Select(g => (g.GroupId, g.ParentGroupId)),
            directDeviceCounts);

        return groups.Select(g => new GroupDto
        {
            GroupId = g.GroupId,
            Name = g.Name,
            ParentGroupId = g.ParentGroupId,
            ParentGroupName = g.ParentGroupId.HasValue &&
                              groupLookup.TryGetValue(g.ParentGroupId.Value, out var parent)
                ? parent.Name
                : null,
            Description = g.Description,
            CreatedDate = g.CreatedDate,
            ChildGroupCount = childCounts.GetValueOrDefault(g.GroupId, 0),
            DeviceCount = recursiveDeviceCounts.GetValueOrDefault(g.GroupId, 0)
        });
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

    private static Dictionary<int, int> ComputeRecursiveDeviceCounts(
    IEnumerable<(int GroupId, int? ParentGroupId)> groups,
    Dictionary<int, int> directCounts)
    {
        var children = groups
            .Where(g => g.ParentGroupId.HasValue)
            .GroupBy(g => g.ParentGroupId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.GroupId).ToList());

        var totals = new Dictionary<int, int>();

        int Count(int groupId)
        {
            if (totals.TryGetValue(groupId, out var cached))
                return cached;

            var total = directCounts.GetValueOrDefault(groupId, 0);

            if (children.TryGetValue(groupId, out var childGroups))
            {
                foreach (var child in childGroups)
                {
                    total += Count(child);
                }
            }

            totals[groupId] = total;
            return total;
        }

        foreach (var (GroupId, ParentGroupId) in groups)
        {
            Count(GroupId);
        }

        return totals;
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
