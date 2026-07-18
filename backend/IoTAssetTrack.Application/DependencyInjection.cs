using IoTAssetTrack.Application.Interfaces;
using IoTAssetTrack.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IoTAssetTrack.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IFirmwareService, FirmwareService>();
        services.AddScoped<IDeviceService, DeviceService>();
        return services;
    }
}
