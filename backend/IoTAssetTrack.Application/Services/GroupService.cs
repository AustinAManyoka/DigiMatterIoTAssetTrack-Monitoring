using IoTAssetTrack.Application.DTOs;
using IoTAssetTrack.Application.Exceptions;
using IoTAssetTrack.Application.Interfaces;

namespace IoTAssetTrack.Application.Services;

public class GroupService(IGroupRepository groupRepository) : IGroupService
{
    private readonly IGroupRepository _groupRepository = groupRepository;

    public Task<IEnumerable<GroupDto>> GetAllGroupsAsync() =>
        _groupRepository.GetAllAsync();

    public Task<GroupDto?> GetGroupByIdAsync(int groupId) =>
        _groupRepository.GetByIdAsync(groupId);

    public async Task<GroupDto> CreateGroupAsync(GroupCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BusinessException("Group name is required.");

        if (await _groupRepository.NameExistsAsync(dto.Name))
            throw new BusinessException($"A group named '{dto.Name}' already exists.");

        if (dto.ParentGroupId.HasValue)
        {
            _ = await _groupRepository.GetByIdAsync(dto.ParentGroupId.Value) ?? throw new BusinessException("Parent group does not exist.");
        }

        return await _groupRepository.CreateAsync(dto);
    }

    public async Task UpdateGroupAsync(int groupId, GroupUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BusinessException("Group name is required.");
        _ = await _groupRepository.GetByIdAsync(groupId) ?? throw new BusinessException("Group not found.");
        if (groupId == dto.ParentGroupId)
            throw new BusinessException("A device group cannot be assigned as a sub-group of itself.");

        if (await _groupRepository.WouldCreateCycleAsync(groupId, dto.ParentGroupId))
            throw new BusinessException("Assigning this parent would create a circular group hierarchy.");

        if (await _groupRepository.NameExistsAsync(dto.Name, groupId))
            throw new BusinessException($"A group named '{dto.Name}' already exists.");

        if (dto.ParentGroupId.HasValue)
        {
            _ = await _groupRepository.GetByIdAsync(dto.ParentGroupId.Value) ?? throw new BusinessException("Parent group does not exist.");
        }

        var updated = await _groupRepository.UpdateAsync(groupId, dto);
        if (!updated)
            throw new BusinessException("Failed to update group.");
    }

    public async Task DeleteGroupAsync(int groupId)
    {
        var existing = await _groupRepository.GetByIdAsync(groupId) ?? throw new BusinessException("Group not found.");
        if (existing.ChildGroupCount > 0)
            throw new BusinessException("Cannot delete a group that contains child groups. Remove or reassign child groups first.");

        if (existing.DeviceCount > 0)
            throw new BusinessException("Cannot delete a group that contains devices. Reassign devices first.");

        var deleted = await _groupRepository.DeleteAsync(groupId);
        if (!deleted)
            throw new BusinessException("Failed to delete group.");
    }
}
