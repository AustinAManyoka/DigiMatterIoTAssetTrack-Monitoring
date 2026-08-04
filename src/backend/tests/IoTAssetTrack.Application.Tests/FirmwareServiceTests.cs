using IoTAssetTrack.Application.DTOs;
using IoTAssetTrack.Application.Exceptions;
using IoTAssetTrack.Application.Interfaces;
using IoTAssetTrack.Application.Services;
using Moq;
using Xunit;
 
namespace IoTAssetTrack.Application.Tests;
 
public class FirmwareServiceTests
{
    private readonly Mock<IFirmwareRepository> _firmwareRepo = new();
    private readonly FirmwareService _sut;
 
    private static readonly List<DeviceTypeDto> KnownDeviceTypes = new()
    {
        new() { DeviceTypeId = 1, Name = "Griffin Air" },
        new() { DeviceTypeId = 2, Name = "Yabby3 LoRaWAN" },
        new() { DeviceTypeId = 3, Name = "Barra Edge" }
    };
 
    public FirmwareServiceTests()
    {
        _sut = new FirmwareService(_firmwareRepo.Object);
    }
 
    
    // CREATE FIRMWARE


    [Fact]
    public async Task CreateFirmwareAsync_ValidDto_CreatesFirmware()
    {
        var dto = new FirmwareCreateDto { DeviceTypeId = 1, Version = "v1.0.0", ReleaseDate = DateTime.UtcNow };
        _firmwareRepo.Setup(r => r.GetDeviceTypesAsync()).ReturnsAsync(KnownDeviceTypes);
        _firmwareRepo.Setup(r => r.VersionExistsAsync(1, "v1.0.0", null)).ReturnsAsync(false);
        _firmwareRepo.Setup(r => r.CreateAsync(dto)).ReturnsAsync(new FirmwareDto { FirmwareId = 1, DeviceTypeId = 1, Version = "v1.0.0" });
 
        var result = await _sut.CreateFirmwareAsync(dto);
 
        Assert.Equal(1, result.FirmwareId);
        _firmwareRepo.Verify(r => r.CreateAsync(dto), Times.Once);
    }
 
    [Fact]
    public async Task CreateFirmwareAsync_SameVersionDifferentDeviceTypes_BothAllowed()
    {
        // Firmware.Version is only unique per device type (UQ_DeviceType_Version
        // in CreateTables.sql), so the same version string across two different
        // device types must both be creatable.
        var dtoA = new FirmwareCreateDto { DeviceTypeId = 1, Version = "v1.0.0" };
        var dtoB = new FirmwareCreateDto { DeviceTypeId = 2, Version = "v1.0.0" };
        _firmwareRepo.Setup(r => r.GetDeviceTypesAsync()).ReturnsAsync(KnownDeviceTypes);
        _firmwareRepo.Setup(r => r.VersionExistsAsync(1, "v1.0.0", null)).ReturnsAsync(false);
        _firmwareRepo.Setup(r => r.VersionExistsAsync(2, "v1.0.0", null)).ReturnsAsync(false);
        _firmwareRepo.Setup(r => r.CreateAsync(It.IsAny<FirmwareCreateDto>()))
            .ReturnsAsync(new FirmwareDto { FirmwareId = 1 });
 
        await _sut.CreateFirmwareAsync(dtoA);
        await _sut.CreateFirmwareAsync(dtoB);
 
        _firmwareRepo.Verify(r => r.CreateAsync(It.IsAny<FirmwareCreateDto>()), Times.Exactly(2));
    }
 
    // ---- prevent duplicate versions ----
 
    [Fact]
    public async Task CreateFirmwareAsync_DuplicateVersionForSameDeviceType_ThrowsBusinessException()
    {
        var dto = new FirmwareCreateDto { DeviceTypeId = 1, Version = "v1.0.0" };
        _firmwareRepo.Setup(r => r.GetDeviceTypesAsync()).ReturnsAsync(KnownDeviceTypes);
        _firmwareRepo.Setup(r => r.VersionExistsAsync(1, "v1.0.0", null)).ReturnsAsync(true);
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateFirmwareAsync(dto));
 
        Assert.Contains("already exists", ex.Message);
        _firmwareRepo.Verify(r => r.CreateAsync(It.IsAny<FirmwareCreateDto>()), Times.Never);
    }
 
    [Fact]
    public async Task CreateFirmwareAsync_UnknownDeviceType_ThrowsBusinessException()
    {
        var dto = new FirmwareCreateDto { DeviceTypeId = 999, Version = "v1.0.0" };
        _firmwareRepo.Setup(r => r.GetDeviceTypesAsync()).ReturnsAsync(KnownDeviceTypes);
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateFirmwareAsync(dto));
 
        Assert.Contains("Device type does not exist", ex.Message);
        _firmwareRepo.Verify(r => r.VersionExistsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }
 
    // ---- validation rules ----
 
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateFirmwareAsync_MissingVersion_ThrowsBusinessException(string? version)
    {
        var dto = new FirmwareCreateDto { DeviceTypeId = 1, Version = version! };
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateFirmwareAsync(dto));
        Assert.Contains("version", ex.Message, StringComparison.OrdinalIgnoreCase);
        _firmwareRepo.Verify(r => r.GetDeviceTypesAsync(), Times.Never);
    }
 
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateFirmwareAsync_InvalidDeviceTypeId_ThrowsBusinessException(int deviceTypeId)
    {
        var dto = new FirmwareCreateDto { DeviceTypeId = deviceTypeId, Version = "v1.0.0" };
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateFirmwareAsync(dto));
        Assert.Contains("valid device type", ex.Message);
    }
 
    
    // UPDATE FIRMWARE

    [Fact]
    public async Task UpdateFirmwareAsync_ValidChange_UpdatesSuccessfully()
    {
        var dto = new FirmwareUpdateDto { DeviceTypeId = 1, Version = "v1.2.5", ReleaseDate = DateTime.UtcNow };
        _firmwareRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FirmwareDto { FirmwareId = 1, DeviceTypeId = 1, Version = "v1.2.4" });
        _firmwareRepo.Setup(r => r.GetDeviceTypesAsync()).ReturnsAsync(KnownDeviceTypes);
        _firmwareRepo.Setup(r => r.VersionExistsAsync(1, "v1.2.5", 1)).ReturnsAsync(false);
        _firmwareRepo.Setup(r => r.UpdateAsync(1, dto)).ReturnsAsync(true);
 
        await _sut.UpdateFirmwareAsync(1, dto);
 
        _firmwareRepo.Verify(r => r.UpdateAsync(1, dto), Times.Once);
    }
 
    [Fact]
    public async Task UpdateFirmwareAsync_FirmwareNotFound_ThrowsBusinessException()
    {
        var dto = new FirmwareUpdateDto { DeviceTypeId = 1, Version = "v2.0.0" };
        _firmwareRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((FirmwareDto?)null);
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateFirmwareAsync(999, dto));
        Assert.Contains("Firmware not found", ex.Message);
    }
 
    [Fact]
    public async Task UpdateFirmwareAsync_UnknownDeviceType_ThrowsBusinessException()
    {
        var dto = new FirmwareUpdateDto { DeviceTypeId = 999, Version = "v2.0.0" };
        _firmwareRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FirmwareDto { FirmwareId = 1 });
        _firmwareRepo.Setup(r => r.GetDeviceTypesAsync()).ReturnsAsync(KnownDeviceTypes);
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateFirmwareAsync(1, dto));
        Assert.Contains("Device type does not exist", ex.Message);
    }
 
    [Fact]
    public async Task UpdateFirmwareAsync_DuplicateVersionExcludingItself_ThrowsBusinessException()
    {
        // Renaming firmware #1 to a version that ALREADY belongs to a
        // different firmware row (#2) for the same device type should fail...
        var dto = new FirmwareUpdateDto { DeviceTypeId = 1, Version = "v1.0.0" };
        _firmwareRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FirmwareDto { FirmwareId = 1, DeviceTypeId = 1 });
        _firmwareRepo.Setup(r => r.GetDeviceTypesAsync()).ReturnsAsync(KnownDeviceTypes);
        _firmwareRepo.Setup(r => r.VersionExistsAsync(1, "v1.0.0", 1)).ReturnsAsync(true);
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateFirmwareAsync(1, dto));
    }
 
    [Fact]
    public async Task UpdateFirmwareAsync_KeepingSameVersionOnSameRow_ExcludesItselfFromDuplicateCheck()
    {
        // keeping its OWN existing version during an unrelated field
        // update (e.g. just changing notes) must NOT be flagged as a duplicate
        // of itself. VersionExistsAsync is called with firmwareId=1 excluded.
        var dto = new FirmwareUpdateDto { DeviceTypeId = 1, Version = "v1.2.4", Notes = "Updated notes" };
        _firmwareRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FirmwareDto { FirmwareId = 1, DeviceTypeId = 1, Version = "v1.2.4" });
        _firmwareRepo.Setup(r => r.GetDeviceTypesAsync()).ReturnsAsync(KnownDeviceTypes);
        _firmwareRepo.Setup(r => r.VersionExistsAsync(1, "v1.2.4", 1)).ReturnsAsync(false);
        _firmwareRepo.Setup(r => r.UpdateAsync(1, dto)).ReturnsAsync(true);
 
        await _sut.UpdateFirmwareAsync(1, dto);
 
        _firmwareRepo.Verify(r => r.VersionExistsAsync(1, "v1.2.4", 1), Times.Once);
        _firmwareRepo.Verify(r => r.UpdateAsync(1, dto), Times.Once);
    }
 
    [Fact]
    public async Task UpdateFirmwareAsync_MissingVersion_ThrowsBusinessException()
    {
        var dto = new FirmwareUpdateDto { DeviceTypeId = 1, Version = "" };
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateFirmwareAsync(1, dto));
        _firmwareRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }
 
    [Fact]
    public async Task UpdateFirmwareAsync_RepositoryUpdateFails_ThrowsBusinessException()
    {
        var dto = new FirmwareUpdateDto { DeviceTypeId = 1, Version = "v1.2.5" };
        _firmwareRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FirmwareDto { FirmwareId = 1, DeviceTypeId = 1 });
        _firmwareRepo.Setup(r => r.GetDeviceTypesAsync()).ReturnsAsync(KnownDeviceTypes);
        _firmwareRepo.Setup(r => r.VersionExistsAsync(1, "v1.2.5", 1)).ReturnsAsync(false);
        _firmwareRepo.Setup(r => r.UpdateAsync(1, dto)).ReturnsAsync(false);
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateFirmwareAsync(1, dto));
    }
 
    
    // DELETE FIRMWARE
    
 
    [Fact]
    public async Task DeleteFirmwareAsync_NoDevicesUsingIt_DeletesSuccessfully()
    {
        _firmwareRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FirmwareDto { FirmwareId = 1, DeviceCount = 0 });
        _firmwareRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
 
        await _sut.DeleteFirmwareAsync(1);
 
        _firmwareRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }
 
    [Fact]
    public async Task DeleteFirmwareAsync_FirmwareNotFound_ThrowsBusinessException()
    {
        _firmwareRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((FirmwareDto?)null);
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.DeleteFirmwareAsync(1));
        _firmwareRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }
 
    [Fact]
    public async Task DeleteFirmwareAsync_FirmwareInUse_ThrowsBusinessExceptionAndDoesNotDelete()
    {
        _firmwareRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new FirmwareDto { FirmwareId = 1, DeviceCount = 3 });
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.DeleteFirmwareAsync(1));
 
        Assert.Contains("assigned to devices", ex.Message);
        _firmwareRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }
 
    [Fact]
    public async Task DeleteFirmwareAsync_RepositoryDeleteFails_ThrowsBusinessException()
    {
        _firmwareRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new FirmwareDto { FirmwareId = 1, DeviceCount = 0 });
        _firmwareRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(false);
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.DeleteFirmwareAsync(1));
    }
}