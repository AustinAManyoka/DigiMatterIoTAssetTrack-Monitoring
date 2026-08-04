using IoTAssetTrack.Application.Common;
using IoTAssetTrack.Application.DTOs;
using IoTAssetTrack.Application.Exceptions;
using IoTAssetTrack.Application.Interfaces;
using IoTAssetTrack.Application.Services;
using Moq;
using Xunit;
 
namespace IoTAssetTrack.Application.Tests;
 
public class DeviceServiceTests
{
    private readonly Mock<IDeviceRepository> _deviceRepo = new();
    private readonly Mock<IFirmwareRepository> _firmwareRepo = new();
    private readonly Mock<IGroupRepository> _groupRepo = new();
    private readonly DeviceService _sut;
 
    public DeviceServiceTests()
    {
        _sut = new DeviceService(_deviceRepo.Object, _firmwareRepo.Object, _groupRepo.Object);
    }
 
    
    // CREATE DEVICE UNIT TESTS
 
    [Fact]
    public async Task CreateDeviceAsync_ValidDto_CreatesDevice()
    {
        var dto = new DeviceCreateDto { SerialNumber = "SN-1", Name = "Tracker 1", Latitude = 10, Longitude = 20, FirmwareId = 1 };
        _firmwareRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FirmwareDto { FirmwareId = 1 });
        _deviceRepo.Setup(r => r.CreateAsync(dto)).ReturnsAsync(new DeviceFlatDto { DeviceId = 1, Name = "Tracker 1" });
 
        var result = await _sut.CreateDeviceAsync(dto);
 
        Assert.Equal(1, result.DeviceId);
        _deviceRepo.Verify(r => r.CreateAsync(dto), Times.Once);
    }
 
    [Fact]
    public async Task CreateDeviceAsync_WithGroupAndFirmware_ValidatesBothBeforeCreating()
    {
        var dto = new DeviceCreateDto { SerialNumber = "SN-1", Name = "Tracker 1", Latitude = 10, Longitude = 20, FirmwareId = 1, GroupId = 5 };
        _firmwareRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FirmwareDto { FirmwareId = 1 });
        _groupRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new GroupDto { GroupId = 5, Name = "Gauteng Logistics Hub" });
        _deviceRepo.Setup(r => r.CreateAsync(dto)).ReturnsAsync(new DeviceFlatDto { DeviceId = 1, GroupId = 5 });
 
        var result = await _sut.CreateDeviceAsync(dto);
 
        Assert.Equal(5, result.GroupId);
        _firmwareRepo.Verify(r => r.GetByIdAsync(1), Times.Once);
        _groupRepo.Verify(r => r.GetByIdAsync(5), Times.Once);
    }
 
    [Fact]
    public async Task CreateDeviceAsync_WithoutGroup_SkipsGroupValidation()
    {
        var dto = new DeviceCreateDto { SerialNumber = "SN-1", Name = "Tracker 1", Latitude = 10, Longitude = 20, FirmwareId = 1, GroupId = null };
        _firmwareRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FirmwareDto { FirmwareId = 1 });
        _deviceRepo.Setup(r => r.CreateAsync(dto)).ReturnsAsync(new DeviceFlatDto { DeviceId = 1 });
 
        await _sut.CreateDeviceAsync(dto);
 
        _groupRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }
 
  
 
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateDeviceAsync_MissingSerialNumber_ThrowsBusinessException(string? serial)
    {
        var dto = new DeviceCreateDto { SerialNumber = serial!, Name = "Test", Latitude = 0, Longitude = 0, FirmwareId = 1 };
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateDeviceAsync(dto));
        Assert.Contains("Serial number", ex.Message);
        _deviceRepo.Verify(r => r.CreateAsync(It.IsAny<DeviceCreateDto>()), Times.Never);
    }
 
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateDeviceAsync_MissingName_ThrowsBusinessException(string? name)
    {
        var dto = new DeviceCreateDto { SerialNumber = "SN-1", Name = name!, Latitude = 0, Longitude = 0, FirmwareId = 1 };
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateDeviceAsync(dto));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
 
    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    [InlineData(-90.001)]
    public async Task CreateDeviceAsync_LatitudeOutOfRange_ThrowsBusinessException(double lat)
    {
        var dto = new DeviceCreateDto { SerialNumber = "SN-1", Name = "Test", Latitude = lat, Longitude = 0, FirmwareId = 1 };
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateDeviceAsync(dto));
    }
 
    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public async Task CreateDeviceAsync_LongitudeOutOfRange_ThrowsBusinessException(double lon)
    {
        var dto = new DeviceCreateDto { SerialNumber = "SN-1", Name = "Test", Latitude = 0, Longitude = lon, FirmwareId = 1 };
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateDeviceAsync(dto));
    }
 
    [Theory]
    [InlineData(-90)]
    [InlineData(90)]
    [InlineData(-180)]
    [InlineData(180)]
    [InlineData(0)]
    public async Task CreateDeviceAsync_BoundaryLatLon_IsValid(double boundary)
    {
        var dto = new DeviceCreateDto { SerialNumber = "SN-1", Name = "Test", Latitude = boundary, Longitude = boundary, FirmwareId = 1 };
        _firmwareRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FirmwareDto { FirmwareId = 1 });
        _deviceRepo.Setup(r => r.CreateAsync(dto)).ReturnsAsync(new DeviceFlatDto { DeviceId = 1 });
 
        var result = await _sut.CreateDeviceAsync(dto);
 
        Assert.NotNull(result);
    }
 
    [Fact]
    public async Task CreateDeviceAsync_UnknownFirmware_ThrowsBusinessException()
    {
        var dto = new DeviceCreateDto { SerialNumber = "SN-1", Name = "Test", Latitude = 0, Longitude = 0, FirmwareId = 999 };
        _firmwareRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((FirmwareDto?)null);
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateDeviceAsync(dto));
        Assert.Contains("Firmware not found", ex.Message);
    }
 
    [Fact]
    public async Task CreateDeviceAsync_UnknownGroup_ThrowsBusinessException()
    {
        var dto = new DeviceCreateDto { SerialNumber = "SN-1", Name = "Test", Latitude = 0, Longitude = 0, FirmwareId = 1, GroupId = 999 };
        _firmwareRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new FirmwareDto { FirmwareId = 1 });
        _groupRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((GroupDto?)null);
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.CreateDeviceAsync(dto));
        Assert.Contains("Group not found", ex.Message);
    }

    // UPDATE DEVICE

    [Fact]
    public async Task UpdateDeviceAsync_ValidUpdate_ReturnsUpdatedDevice()
    {
        _deviceRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _deviceRepo.Setup(r => r.UpdateAsync(1, It.IsAny<DeviceUpdateDto>())).ReturnsAsync(true);
        _deviceRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new DeviceFlatDto { DeviceId = 1, Name = "Updated" });
 
        var result = await _sut.UpdateDeviceAsync(1, new DeviceUpdateDto { Name = "Updated" });
 
        Assert.Equal("Updated", result.Name);
    }
 
    [Fact]
    public async Task UpdateDeviceAsync_DeviceNotFound_ThrowsBusinessException()
    {
        _deviceRepo.Setup(r => r.ExistsAsync(42)).ReturnsAsync(false);
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateDeviceAsync(42, new DeviceUpdateDto()));
        Assert.Contains("Device not found", ex.Message);
        _deviceRepo.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<DeviceUpdateDto>()), Times.Never);
    }
 
    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public async Task UpdateDeviceAsync_LatitudeOutOfRange_ThrowsBusinessException(double lat)
    {
        _deviceRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateDeviceAsync(1, new DeviceUpdateDto { Latitude = lat }));
    }
 
    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public async Task UpdateDeviceAsync_LongitudeOutOfRange_ThrowsBusinessException(double lon)
    {
        _deviceRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateDeviceAsync(1, new DeviceUpdateDto { Longitude = lon }));
    }
 
    [Fact]
    public async Task UpdateDeviceAsync_NullLatLon_SkipsRangeValidation()
    {
        // Partial update: caller isn't touching location this time.
        _deviceRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _deviceRepo.Setup(r => r.UpdateAsync(1, It.IsAny<DeviceUpdateDto>())).ReturnsAsync(true);
        _deviceRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new DeviceFlatDto { DeviceId = 1 });
 
        var result = await _sut.UpdateDeviceAsync(1, new DeviceUpdateDto { Latitude = null, Longitude = null, Name = "Renamed" });
 
        Assert.NotNull(result);
    }
 
    [Fact]
    public async Task UpdateDeviceAsync_ChangingToUnknownFirmware_ThrowsBusinessException()
    {
        _deviceRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _firmwareRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((FirmwareDto?)null);
 
        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => _sut.UpdateDeviceAsync(1, new DeviceUpdateDto { FirmwareId = 999 }));
        Assert.Contains("Firmware not found", ex.Message);
    }
 
    [Fact]
    public async Task UpdateDeviceAsync_ChangingToValidFirmware_Succeeds()
    {
        _deviceRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _firmwareRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new FirmwareDto { FirmwareId = 2 });
        _deviceRepo.Setup(r => r.UpdateAsync(1, It.IsAny<DeviceUpdateDto>())).ReturnsAsync(true);
        _deviceRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new DeviceFlatDto { DeviceId = 1, FirmwareId = 2 });
 
        var result = await _sut.UpdateDeviceAsync(1, new DeviceUpdateDto { FirmwareId = 2 });
 
        Assert.Equal(2, result.FirmwareId);
    }
 
    [Fact]
    public async Task UpdateDeviceAsync_RepositoryUpdateFails_ThrowsBusinessException()
    {
        _deviceRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _deviceRepo.Setup(r => r.UpdateAsync(1, It.IsAny<DeviceUpdateDto>())).ReturnsAsync(false);
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.UpdateDeviceAsync(1, new DeviceUpdateDto()));
    }
 
    [Fact]
    public void DeviceUpdateDto_HasNoSerialNumberProperty_EnforcingImmutabilityAtTheTypeLevel()
    {
        var properties = typeof(DeviceUpdateDto).GetProperties().Select(p => p.Name);
        Assert.DoesNotContain("SerialNumber", properties);
    }
 
    // SEARCH / FILTER DEVICES
    
 
    [Fact]
    public async Task GetDevicesAsync_FilterByGroupId_PassesFilterThroughToRepository()
    {
       
        var parameters = new DeviceQueryParameters { Page = 1, PageSize = 25, GroupId = 5 };
        _deviceRepo
            .Setup(r => r.GetDevicesAsync(It.Is<DeviceQueryParameters>(p => p.GroupId == 5)))
            .ReturnsAsync(new PagedResponse<DeviceFlatDto> { Data = new List<DeviceFlatDto> { new() { DeviceId = 1, GroupId = 5 } } });
 
        var result = await _sut.GetDevicesAsync(parameters);
 
        Assert.Single(result.Data);
        _deviceRepo.Verify(r => r.GetDevicesAsync(It.Is<DeviceQueryParameters>(p => p.GroupId == 5)), Times.Once);
    }
 
    [Theory]
    [InlineData(-91, 0, 1)]
    [InlineData(91, 0, 1)]
    [InlineData(0, -181, 1)]
    [InlineData(0, 181, 1)]
    public async Task SearchDevicesByLocationAsync_InvalidCoordinates_ThrowsBusinessException(double lat, double lon, double radius)
    {
        var dto = new GeoLocationSearchDto { Latitude = lat, Longitude = lon, RadiusKm = radius };
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.SearchDevicesByLocationAsync(dto));
    }
 
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task SearchDevicesByLocationAsync_InvalidRadius_ThrowsBusinessException(double radius)
    {
        var dto = new GeoLocationSearchDto { Latitude = 0, Longitude = 0, RadiusKm = radius };
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.SearchDevicesByLocationAsync(dto));
        Assert.Contains("Radius", ex.Message);
    }
 
    [Fact]
    public async Task SearchDevicesByLocationAsync_ValidSearch_ReturnsMatchingDevices()
    {
        var dto = new GeoLocationSearchDto { Latitude = -25.7, Longitude = 28.2, RadiusKm = 50, IsActive = true, DeviceTypeId = 1 };
        var expected = new List<DeviceFlatDto> { new() { DeviceId = 1, Name = "ZA-Asset-01" } };
 
        _deviceRepo
            .Setup(r => r.GetDevicesByLocationAsync(-25.7, 28.2, 50, true, 1))
            .ReturnsAsync(expected);
 
        var result = await _sut.SearchDevicesByLocationAsync(dto);
 
        Assert.Single(result);
        _deviceRepo.Verify(r => r.GetDevicesByLocationAsync(-25.7, 28.2, 50, true, 1), Times.Once);
    }
 
    [Fact]
    public async Task SearchDevicesByLocationAsync_NoOptionalFilters_PassesNullsThrough()
    {
        var dto = new GeoLocationSearchDto { Latitude = 0, Longitude = 0, RadiusKm = 10 };
 
        _deviceRepo
            .Setup(r => r.GetDevicesByLocationAsync(0, 0, 10, null, null))
            .ReturnsAsync(new List<DeviceFlatDto>());
 
        await _sut.SearchDevicesByLocationAsync(dto);
 
        _deviceRepo.Verify(r => r.GetDevicesByLocationAsync(0, 0, 10, null, null), Times.Once);
    }
 

 
    [Theory]
    [InlineData(1000, 100)]
    [InlineData(10000, 100)]
    [InlineData(100, 100)]
    [InlineData(50, 50)]
    public async Task GetDevicesAsync_OversizedPageSize_ClampsToMaxInsteadOfResettingToDefault(int requested, int expectedClamped)
    {
        var parameters = new DeviceQueryParameters { Page = 1, PageSize = requested };
        _deviceRepo
            .Setup(r => r.GetDevicesAsync(It.IsAny<DeviceQueryParameters>()))
            .ReturnsAsync(new PagedResponse<DeviceFlatDto>());
 
        await _sut.GetDevicesAsync(parameters);
 
        Assert.Equal(expectedClamped, parameters.PageSize);
    }
 
    [Theory]
    [InlineData(0, 25)]
    [InlineData(-5, 25)]
    public async Task GetDevicesAsync_TooSmallPageSize_FallsBackToDefault(int requested, int expectedDefault)
    {
        var parameters = new DeviceQueryParameters { Page = 1, PageSize = requested };
        _deviceRepo
            .Setup(r => r.GetDevicesAsync(It.IsAny<DeviceQueryParameters>()))
            .ReturnsAsync(new PagedResponse<DeviceFlatDto>());
 
        await _sut.GetDevicesAsync(parameters);
 
        Assert.Equal(expectedDefault, parameters.PageSize);
    }
 
    [Fact]
    public async Task GetDevicesAsync_NegativePage_ResetsToPageOne()
    {
        var parameters = new DeviceQueryParameters { Page = -3, PageSize = 25 };
        _deviceRepo
            .Setup(r => r.GetDevicesAsync(It.IsAny<DeviceQueryParameters>()))
            .ReturnsAsync(new PagedResponse<DeviceFlatDto>());
 
        await _sut.GetDevicesAsync(parameters);
 
        Assert.Equal(1, parameters.Page);
    }
 
    [Fact]
    public async Task GetDevicesAsync_InvalidSortColumn_FallsBackToName()
    {
        var parameters = new DeviceQueryParameters { Page = 1, PageSize = 25, SortBy = "'; DROP TABLE Device; --" };
        _deviceRepo
            .Setup(r => r.GetDevicesAsync(It.IsAny<DeviceQueryParameters>()))
            .ReturnsAsync(new PagedResponse<DeviceFlatDto>());
 
        await _sut.GetDevicesAsync(parameters);
 
        Assert.Equal("Name", parameters.SortBy);
    }
 
    [Theory]
    [InlineData("SerialNumber")]
    [InlineData("createdDate")]
    [InlineData("NAME")]
    public async Task GetDevicesAsync_AllowedSortColumn_IsPreservedCaseInsensitively(string sortBy)
    {
        var parameters = new DeviceQueryParameters { Page = 1, PageSize = 25, SortBy = sortBy };
        _deviceRepo
            .Setup(r => r.GetDevicesAsync(It.IsAny<DeviceQueryParameters>()))
            .ReturnsAsync(new PagedResponse<DeviceFlatDto>());
 
        await _sut.GetDevicesAsync(parameters);
 
        Assert.Equal(sortBy, parameters.SortBy);
    }
 
    
    // GROUP / FIRMWARE ASSIGNMENT

 
    [Fact]
    public async Task AssignDeviceToGroupAsync_ValidDeviceAndGroup_AssignsSuccessfully()
    {
        _deviceRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _groupRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new GroupDto { GroupId = 5 });
        _deviceRepo.Setup(r => r.UpdateDeviceGroupAsync(1, 5)).ReturnsAsync(true);
 
        var result = await _sut.AssignDeviceToGroupAsync(1, 5);
 
        Assert.True(result);
        _deviceRepo.Verify(r => r.UpdateDeviceGroupAsync(1, 5), Times.Once);
    }
 
    [Fact]
    public async Task AssignDeviceToGroupAsync_NullGroupId_UnassignsWithoutValidatingGroup()
    {
        _deviceRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _deviceRepo.Setup(r => r.UpdateDeviceGroupAsync(1, null)).ReturnsAsync(true);
 
        var result = await _sut.AssignDeviceToGroupAsync(1, null);
 
        Assert.True(result);
        _groupRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }
 
    [Fact]
    public async Task AssignDeviceToGroupAsync_DeviceNotFound_ThrowsBusinessException()
    {
        _deviceRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(false);
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.AssignDeviceToGroupAsync(1, 5));
        Assert.Contains("Device not found", ex.Message);
        _deviceRepo.Verify(r => r.UpdateDeviceGroupAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
    }
 
    [Fact]
    public async Task AssignDeviceToGroupAsync_GroupNotFound_ThrowsBusinessException()
    {
        _deviceRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _groupRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((GroupDto?)null);
 
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.AssignDeviceToGroupAsync(1, 999));
        Assert.Contains("Group not found", ex.Message);
        _deviceRepo.Verify(r => r.UpdateDeviceGroupAsync(It.IsAny<int>(), It.IsAny<int?>()), Times.Never);
    }
 
   
    // DELETE DEVICE
 
 
    [Fact]
    public async Task DeleteDeviceAsync_DeviceNotFound_ThrowsBusinessException()
    {
        _deviceRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(false);
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.DeleteDeviceAsync(1));
        _deviceRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }
 
    [Fact]
    public async Task DeleteDeviceAsync_RepositoryFails_ThrowsBusinessException()
    {
        _deviceRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _deviceRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(false);
 
        await Assert.ThrowsAsync<BusinessException>(() => _sut.DeleteDeviceAsync(1));
    }
 
    [Fact]
    public async Task DeleteDeviceAsync_ValidDevice_DeletesSuccessfully()
    {
        _deviceRepo.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
        _deviceRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
 
        await _sut.DeleteDeviceAsync(1);
 
        _deviceRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}