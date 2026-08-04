using IoTAssetTrack.Application.DTOs;
using IoTAssetTrack.Application.Exceptions;
using IoTAssetTrack.Application.Interfaces;
using IoTAssetTrack.Application.Services;
using Moq;
using Xunit;
 
namespace IoTAssetTrack.Application.Tests;
 
public class GroupServiceTests
{
    private readonly Mock<IGroupRepository> _groupRepo = new();
    private readonly GroupService _sut;
 
    public GroupServiceTests()
    {
        _sut = new GroupService(_groupRepo.Object);
    }
 
    // CREATE GROUP (root-level)
   
 
    [Fact]
    public async Task CreateGroupAsync_ValidRootGroup_CreatesSuccessfully()
    {
        var dto = new GroupCreateDto { Name = "Global Enterprise Fleet", ParentGroupId = null };
        _groupRepo.Setup(r => r.NameExistsAsync("Global Enterprise Fleet", null)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.CreateAsync(dto)).ReturnsAsync(new GroupDto { GroupId = 1, Name = "Global Enterprise Fleet" });
 
        var result = await _sut.CreateGroupAsync(dto);
 
        Assert.Equal(1, result.GroupId);
        Assert.Null(result.ParentGroupId);
        _groupRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never); // no parent to validate
    }
 
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateGroupAsync_MissingName_ThrowsBusinessException(string? name)
    {
        var dto = new GroupCreateDto { Name = name! };
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateGroupAsync(dto));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
        _groupRepo.Verify(r => r.CreateAsync(It.IsAny<GroupCreateDto>()), Times.Never);
    }
 
    [Fact]
    public async Task CreateGroupAsync_DuplicateName_ThrowsBusinessException()
    {
        var dto = new GroupCreateDto { Name = "South Africa Region" };
        _groupRepo.Setup(r => r.NameExistsAsync("South Africa Region", null)).ReturnsAsync(true);
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateGroupAsync(dto));
        Assert.Contains("already exists", ex.Message);
        _groupRepo.Verify(r => r.CreateAsync(It.IsAny<GroupCreateDto>()), Times.Never);
    }
 
   
    // CREATE CHILD GROUP
   
 
    [Fact]
    public async Task CreateGroupAsync_ValidChildGroup_ValidatesParentThenCreates()
    {
        var dto = new GroupCreateDto { Name = "Gauteng Logistics Hub", ParentGroupId = 2 };
        _groupRepo.Setup(r => r.NameExistsAsync("Gauteng Logistics Hub", null)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new GroupDto { GroupId = 2, Name = "South Africa Region" });
        _groupRepo.Setup(r => r.CreateAsync(dto)).ReturnsAsync(new GroupDto { GroupId = 5, Name = "Gauteng Logistics Hub", ParentGroupId = 2 });
 
        var result = await _sut.CreateGroupAsync(dto);
 
        Assert.Equal(2, result.ParentGroupId);
        _groupRepo.Verify(r => r.GetByIdAsync(2), Times.Once);
        _groupRepo.Verify(r => r.CreateAsync(dto), Times.Once);
    }
 
    [Fact]
    public async Task CreateGroupAsync_NestedThreeLevelsDeep_Succeeds()
    {
        // Global Enterprise Fleet -> South Africa Region -> Gauteng Logistics Hub
        // exercises that group-of-groups nesting isn't artificially limited to one level.
        var dto = new GroupCreateDto { Name = "Pretoria Sub-Depot", ParentGroupId = 5 };
        _groupRepo.Setup(r => r.NameExistsAsync("Pretoria Sub-Depot", null)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new GroupDto { GroupId = 5, Name = "Gauteng Logistics Hub", ParentGroupId = 2 });
        _groupRepo.Setup(r => r.CreateAsync(dto)).ReturnsAsync(new GroupDto { GroupId = 8, Name = "Pretoria Sub-Depot", ParentGroupId = 5 });
 
        var result = await _sut.CreateGroupAsync(dto);
 
        Assert.Equal(5, result.ParentGroupId);
    }
 
    [Fact]
    public async Task CreateGroupAsync_NonExistentParent_ThrowsBusinessException()
    {
        var dto = new GroupCreateDto { Name = "Orphan Group", ParentGroupId = 999 };
        _groupRepo.Setup(r => r.NameExistsAsync("Orphan Group", null)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((GroupDto?)null);
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateGroupAsync(dto));
        Assert.Contains("Parent group does not exist", ex.Message);
        _groupRepo.Verify(r => r.CreateAsync(It.IsAny<GroupCreateDto>()), Times.Never);
    }
 
    
    // UPDATE GROUP

 
    [Fact]
    public async Task UpdateGroupAsync_ValidRename_UpdatesSuccessfully()
    {
        _groupRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new GroupDto { GroupId = 1, Name = "Old Name" });
        _groupRepo.Setup(r => r.WouldCreateCycleAsync(1, null)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.NameExistsAsync("New Name", 1)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.UpdateAsync(1, It.IsAny<GroupUpdateDto>())).ReturnsAsync(true);
 
        var dto = new GroupUpdateDto { Name = "New Name", ParentGroupId = null };
        await _sut.UpdateGroupAsync(1, dto);
 
        _groupRepo.Verify(r => r.UpdateAsync(1, dto), Times.Once);
    }
 
    [Fact]
    public async Task UpdateGroupAsync_ReparentToValidGroup_ValidatesNewParentAndUpdates()
    {
        _groupRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new GroupDto { GroupId = 1, Name = "Gauteng Logistics Hub", ParentGroupId = 2 });
        _groupRepo.Setup(r => r.WouldCreateCycleAsync(1, 3)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.NameExistsAsync("Gauteng Logistics Hub", 1)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new GroupDto { GroupId = 3, Name = "Australia Region" });
        _groupRepo.Setup(r => r.UpdateAsync(1, It.IsAny<GroupUpdateDto>())).ReturnsAsync(true);
 
        var dto = new GroupUpdateDto { Name = "Gauteng Logistics Hub", ParentGroupId = 3 };
        await _sut.UpdateGroupAsync(1, dto);
 
        _groupRepo.Verify(r => r.UpdateAsync(1, dto), Times.Once);
    }
 
    [Fact]
    public async Task UpdateGroupAsync_GroupNotFound_ThrowsBusinessException()
    {
        _groupRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((GroupDto?)null);
 
        var dto = new GroupUpdateDto { Name = "Anything" };
        await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateGroupAsync(999, dto));
    }
 
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task UpdateGroupAsync_MissingName_ThrowsBusinessException(string? name)
    {
        var dto = new GroupUpdateDto { Name = name! };
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateGroupAsync(1, dto));
        _groupRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }
 
    [Fact]
    public async Task UpdateGroupAsync_DuplicateName_ThrowsBusinessException()
    {
        _groupRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new GroupDto { GroupId = 1, Name = "Old Name" });
        _groupRepo.Setup(r => r.WouldCreateCycleAsync(1, null)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.NameExistsAsync("Sydney Port Terminals", 1)).ReturnsAsync(true);
 
        var dto = new GroupUpdateDto { Name = "Sydney Port Terminals" };
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateGroupAsync(1, dto));
        Assert.Contains("already exists", ex.Message);
    }
 
    [Fact]
    public async Task UpdateGroupAsync_ReparentToNonExistentGroup_ThrowsBusinessException()
    {
        _groupRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new GroupDto { GroupId = 1, Name = "Gauteng Logistics Hub" });
        _groupRepo.Setup(r => r.WouldCreateCycleAsync(1, 999)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.NameExistsAsync("Gauteng Logistics Hub", 1)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((GroupDto?)null);
 
        var dto = new GroupUpdateDto { Name = "Gauteng Logistics Hub", ParentGroupId = 999 };
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateGroupAsync(1, dto));
        Assert.Contains("Parent group does not exist", ex.Message);
    }
 
    [Fact]
    public async Task UpdateGroupAsync_RepositoryUpdateFails_ThrowsBusinessException()
    {
        _groupRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new GroupDto { GroupId = 1, Name = "Old Name" });
        _groupRepo.Setup(r => r.WouldCreateCycleAsync(1, null)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.NameExistsAsync("New Name", 1)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.UpdateAsync(1, It.IsAny<GroupUpdateDto>())).ReturnsAsync(false);
 
        var dto = new GroupUpdateDto { Name = "New Name" };
        await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateGroupAsync(1, dto));
    }
 
    
    // PREVENT CIRCULAR REFERENCES
    
 
    [Fact]
    public async Task UpdateGroupAsync_ParentIsSelf_ThrowsBusinessExceptionWithoutHittingCycleCheck()
    {
        _groupRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new GroupDto { GroupId = 1, Name = "A" });
 
        var dto = new GroupUpdateDto { Name = "A", ParentGroupId = 1 };
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateGroupAsync(1, dto));
        Assert.Contains("sub-group of itself", ex.Message);
        _groupRepo.Verify(r => r.WouldCreateCycleAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
    }
 
    [Fact]
    public async Task UpdateGroupAsync_ParentIsOwnDescendant_ThrowsBusinessException()
    {
        // e.g. moving "South Africa Region" (id 2) to become a child of
        // "Gauteng Logistics Hub" (id 5), which is currently ITS OWN child —
        // that would create a cycle: 2 -> 5 -> 2.
        _groupRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new GroupDto { GroupId = 2, Name = "South Africa Region" });
        _groupRepo.Setup(r => r.WouldCreateCycleAsync(2, 5)).ReturnsAsync(true);
 
        var dto = new GroupUpdateDto { Name = "South Africa Region", ParentGroupId = 5 };
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateGroupAsync(2, dto));
        Assert.Contains("circular", ex.Message, StringComparison.OrdinalIgnoreCase);
        _groupRepo.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<GroupUpdateDto>()), Times.Never);
    }
 
    [Fact]
    public async Task UpdateGroupAsync_NoParentChange_NeverCallsCycleCheckUnnecessarily()
    {
        // Sanity check: cycle detection still runs when a parent is specified 
        //  but with ParentGroupId=null it should
        // simply report "no cycle" and let the update proceed normally.
        _groupRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new GroupDto { GroupId = 1, Name = "Root" });
        _groupRepo.Setup(r => r.WouldCreateCycleAsync(1, null)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.NameExistsAsync("Root", 1)).ReturnsAsync(false);
        _groupRepo.Setup(r => r.UpdateAsync(1, It.IsAny<GroupUpdateDto>())).ReturnsAsync(true);
 
        var dto = new GroupUpdateDto { Name = "Root", ParentGroupId = null };
        await _sut.UpdateGroupAsync(1, dto);
 
        _groupRepo.Verify(r => r.UpdateAsync(1, dto), Times.Once);
    }
 
    
    // DELETE GROUP
 
    [Fact]
    public async Task DeleteGroupAsync_EmptyLeafGroup_DeletesSuccessfully()
    {
        _groupRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new GroupDto { GroupId = 1, Name = "Leaf", ChildGroupCount = 0, DeviceCount = 0 });
        _groupRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
 
        await _sut.DeleteGroupAsync(1);
 
        _groupRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }
 
    [Fact]
    public async Task DeleteGroupAsync_GroupNotFound_ThrowsBusinessException()
    {
        _groupRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((GroupDto?)null);
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.DeleteGroupAsync(1));
        _groupRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }
 
    [Fact]
    public async Task DeleteGroupAsync_HasChildGroups_ThrowsBusinessExceptionAndDoesNotDelete()
    {
        _groupRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new GroupDto { GroupId = 1, Name = "Parent", ChildGroupCount = 2, DeviceCount = 0 });
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.DeleteGroupAsync(1));
 
        Assert.Contains("child groups", ex.Message);
        _groupRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }
 
    [Fact]
    public async Task DeleteGroupAsync_HasDirectDevices_ThrowsBusinessException()
    {
        _groupRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new GroupDto { GroupId = 1, Name = "Gauteng Logistics Hub", ChildGroupCount = 0, DeviceCount = 22 });
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.DeleteGroupAsync(1));
 
        Assert.Contains("devices", ex.Message);
        _groupRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }
 
    [Fact]
    public async Task DeleteGroupAsync_HasDevicesInDescendantGroupOnly_ThrowsBusinessException()
    {
      
        // once GroupRepository rolls DeviceCount up recursively, a parent with
        // zero direct devices but devices somewhere beneath it must still be
        // blocked from deletion (would otherwise orphan the child's devices).
        _groupRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new GroupDto { GroupId = 1, Name = "South Africa Region", ChildGroupCount = 0, DeviceCount = 22 });
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.DeleteGroupAsync(1));
        _groupRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }
 
    [Fact]
    public async Task DeleteGroupAsync_RepositoryDeleteFails_ThrowsBusinessException()
    {
        _groupRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new GroupDto { GroupId = 1, Name = "Leaf", ChildGroupCount = 0, DeviceCount = 0 });
        _groupRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(false);
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.DeleteGroupAsync(1));
    }
}