using IoTAssetTrack.Application.DTOs;
using IoTAssetTrack.Application.Exceptions;
using IoTAssetTrack.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IoTAssetTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeviceController(IDeviceService deviceService) : ControllerBase
{
    private readonly IDeviceService _deviceService = deviceService;

    [HttpGet]
    public async Task<IActionResult> GetDevices([FromQuery] DeviceQueryParameters queryParams)
    {
        var response = await _deviceService.GetDevicesAsync(queryParams);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDevice(int id)
    {
        var device = await _deviceService.GetDeviceByIdAsync(id);
        if (device is null) return NotFound(new { message = "Device not found." });
        return Ok(device);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDevice([FromBody] DeviceCreateDto deviceDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var createdDevice = await _deviceService.CreateDeviceAsync(deviceDto);
            return CreatedAtAction(nameof(GetDevice), new { id = createdDevice.DeviceId }, createdDevice);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateDevice(int id, [FromBody] DeviceUpdateDto deviceDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var updatedDevice = await _deviceService.UpdateDeviceAsync(id, deviceDto);
            return Ok(updatedDevice);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/group")]
    public async Task<IActionResult> AssignDeviceToGroup(int id, [FromBody] DeviceGroupAssignmentDto groupDto)
    {
        try
        {
            var success = await _deviceService.AssignDeviceToGroupAsync(id, groupDto.GroupId);
            if (!success)
                return NotFound(new { message = "Device not found." });

            return Ok(new { message = "Device group assignment updated successfully." });
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("search/location")]
    public async Task<IActionResult> SearchDevicesByLocation([FromBody] GeoLocationSearchDto searchDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var devices = await _deviceService.SearchDevicesByLocationAsync(searchDto);
            return Ok(devices);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
