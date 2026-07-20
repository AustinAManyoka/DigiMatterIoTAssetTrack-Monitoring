using IoTAssetTrack.Application.DTOs;
using IoTAssetTrack.Application.Exceptions;
using IoTAssetTrack.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IoTAssetTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupController(IGroupService groupService) : ControllerBase
{
    private readonly IGroupService _groupService = groupService;

    [HttpGet]
    public async Task<IActionResult> GetGroups()
    {
        var groups = await _groupService.GetAllGroupsAsync();
        return Ok(groups);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetGroup(int id)
    {
        var group = await _groupService.GetGroupByIdAsync(id);
        if (group is null) return NotFound(new { message = "Group not found." });
        return Ok(group);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] GroupCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var created = await _groupService.CreateGroupAsync(dto);
            return CreatedAtAction(nameof(GetGroup), new { id = created.GroupId }, created);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateGroup(int id, [FromBody] GroupUpdateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await _groupService.UpdateGroupAsync(id, dto);
            var updated = await _groupService.GetGroupByIdAsync(id);
            return Ok(updated);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        try
        {
            await _groupService.DeleteGroupAsync(id);
            return NoContent();
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
