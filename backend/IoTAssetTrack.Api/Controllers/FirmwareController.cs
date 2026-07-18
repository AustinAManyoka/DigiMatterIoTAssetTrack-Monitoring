using IoTAssetTrack.Application.DTOs;
using IoTAssetTrack.Application.Exceptions;
using IoTAssetTrack.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IoTAssetTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FirmwareController(IFirmwareService firmwareService) : ControllerBase
{
    private readonly IFirmwareService _firmwareService = firmwareService;

    [HttpGet]
    public async Task<IActionResult> GetFirmware()
    {
        var firmware = await _firmwareService.GetAllFirmwareAsync();
        return Ok(firmware);
    }

    [HttpGet("device-types")]
    public async Task<IActionResult> GetDeviceTypes()
    {
        var deviceTypes = await _firmwareService.GetDeviceTypesAsync();
        return Ok(deviceTypes);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetFirmwareById(int id)
    {
        var firmware = await _firmwareService.GetFirmwareByIdAsync(id);
        if (firmware is null) return NotFound(new { message = "Firmware not found." });
        return Ok(firmware);
    }

    [HttpPost]
    public async Task<IActionResult> CreateFirmware([FromBody] FirmwareCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var created = await _firmwareService.CreateFirmwareAsync(dto);
            return CreatedAtAction(nameof(GetFirmwareById), new { id = created.FirmwareId }, created);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateFirmware(int id, [FromBody] FirmwareUpdateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await _firmwareService.UpdateFirmwareAsync(id, dto);
            var updated = await _firmwareService.GetFirmwareByIdAsync(id);
            return Ok(updated);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteFirmware(int id)
    {
        try
        {
            await _firmwareService.DeleteFirmwareAsync(id);
            return NoContent();
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
