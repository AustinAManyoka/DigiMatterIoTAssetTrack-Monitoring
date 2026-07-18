USE IoTAssetTracker_db;
GO


-- 1. SEED DEVICE TYPES (Using Digital Matter Hardware Profiles)

SET IDENTITY_INSERT DeviceType ON;
INSERT INTO DeviceType (DeviceTypeId, Name, Description) VALUES
(1, 'Griffin Air', 'High-performance, high-precision Indoor/Outdoor GPS asset tracking device and Bluetooth® Gateway'),
(2, 'Yabby3 LoRaWAN', 'Compact indoor/outdoor asset tracker utilizing LoRaWAN networks'),
(3, 'Barra Edge', 'Low-cost battery-powered location tracker with peripheral inputs');
SET IDENTITY_INSERT DeviceType OFF;


-- 2. SEED FIRMWARE VERSIONS

INSERT INTO Firmware (DeviceTypeId, Version, ReleaseDate, Notes) VALUES
(1, 'v1.0.0', '2025-01-10', 'Factory master build'),
(1, 'v1.2.4', '2026-03-15', 'Optimized cellular sleep cycles for battery conservation'),
(2, 'v3.1.0', '2025-06-20', 'Enhanced LoRaWAN region scaling updates'),
(3, 'v1.0.2', '2026-02-11', 'Improved accelerometer wake-on-motion filtering');


-- 3. SEED DEVICE GROUPS (Sanitized from non-breaking spaces)

-- Top Level Global Fleet
INSERT INTO DeviceGroup (Name, ParentGroupId, Description) VALUES 
('Global Enterprise Fleet', NULL, 'Root group for all corporate logistical assets');

-- Regional Level (Under Global Group ID 1)
INSERT INTO DeviceGroup (Name, ParentGroupId, Description) VALUES 
('South Africa Region', 1, 'Assets deployed across Southern African territories'),
('Australia Region', 1, 'Assets deployed across APAC and Australian states'),
('United States Region', 1, 'Assets deployed across North American corridors');

-- Local Hubs (Under Regional Groups)
INSERT INTO DeviceGroup (Name, ParentGroupId, Description) VALUES 
('Gauteng Logistics Hub', 2, 'Pretoria and Johannesburg tracking networks'), -- Group 5
('Sydney Port Terminals', 3, 'Marine and overland freight tracking'),        -- Group 6
('Texas Distribution Corridors', 4, 'Dallas-Fort Worth tracking networks'); -- Group 7


-- 4. PROGRAMMATIC SEEDING OF 67 DEVICES

DECLARE @Counter INT = 1;

WHILE @Counter <= 67
BEGIN
    DECLARE @Serial NVARCHAR(50);
    DECLARE @DeviceName NVARCHAR(100);
    DECLARE @Lat DECIMAL(9,6);
    DECLARE @Lon DECIMAL(9,6);
    DECLARE @TargetGroup INT;
    DECLARE @IterationType INT;
    DECLARE @TargetFirmware INT;

    -- Alternate through calculations to distribute device profiles evenly
    SET @IterationType = (@Counter % 3) + 1;
    SET @TargetFirmware = CASE 
        WHEN @IterationType = 1 THEN (SELECT TOP 1 FirmwareId FROM Firmware WHERE DeviceTypeId = 1 ORDER BY Version DESC)
        WHEN @IterationType = 2 THEN (SELECT TOP 1 FirmwareId FROM Firmware WHERE DeviceTypeId = 2)
        ELSE (SELECT TOP 1 FirmwareId FROM Firmware WHERE DeviceTypeId = 3)
    END;

    -- Distribute evenly across the 3 geographical regions
    IF @Counter <= 22  -- 22 Devices in South Africa
    BEGIN
        SET @Serial = 'ZA-DM3-' + RIGHT('000' + CAST(@Counter AS NVARCHAR(3)), 3);
        SET @DeviceName = 'ZA-Asset-' + CAST(@Counter AS NVARCHAR(3));
        SET @TargetGroup = 5; -- Gauteng Logistics Hub
        SET @Lat = -24.000000 - CAST((@Counter * 0.456) AS DECIMAL(9,6)) % 10.0;
        SET @Lon = 18.000000 + CAST((@Counter * 0.632) AS DECIMAL(9,6)) % 14.0;
    END
    ELSE IF @Counter <= 44 -- 22 Devices in Australia
    BEGIN
        SET @Serial = 'AU-DM3-' + RIGHT('000' + CAST(@Counter AS NVARCHAR(3)), 3);
        SET @DeviceName = 'AU-Asset-' + CAST(@Counter AS NVARCHAR(3));
        SET @TargetGroup = 6; -- Sydney Port Terminals
        SET @Lat = -12.000000 - CAST((@Counter * 0.381) AS DECIMAL(9,6)) % 26.0;
        SET @Lon = 115.000000 + CAST((@Counter * 0.714) AS DECIMAL(9,6)) % 35.0;
    END
    ELSE                  -- 23 Devices in the United States
    BEGIN
        SET @Serial = 'US-DM3-' + RIGHT('000' + CAST(@Counter AS NVARCHAR(3)), 3);
        SET @DeviceName = 'US-Asset-' + CAST(@Counter AS NVARCHAR(3));
        SET @TargetGroup = 7; -- Texas Distribution Corridors
        SET @Lat = 26.000000 + CAST((@Counter * 0.521) AS DECIMAL(9,6)) % 22.0;
        SET @Lon = -122.000000 + CAST((@Counter * 0.491) AS DECIMAL(9,6)) % 47.0;
    END

    -- Execute insertion (DeviceTypeId omitted to match your modified table schema)
    INSERT INTO Device (SerialNumber, Name, Latitude, Longitude, IsActive, FirmwareId, GroupId)
    VALUES (@Serial, @DeviceName, @Lat, @Lon, 1, @TargetFirmware, @TargetGroup);

    SET @Counter = @Counter + 1;
END;
GO


-- 5. VERIFICATION REPORT

SELECT 
    dg.Name AS RegionalGroup,
    dt.Name AS HardwareModel,
    COUNT(d.DeviceId) AS DeployedDeviceCount,
    AVG(d.Latitude) AS CentroidLatitude,
    AVG(d.Longitude) AS CentroidLongitude
FROM Device d
JOIN Firmware f ON d.FirmwareId = f.FirmwareId          -- Route through Firmware to uncover Device Type info
JOIN DeviceType dt ON f.DeviceTypeId = dt.DeviceTypeId
JOIN DeviceGroup dg ON d.GroupId = dg.GroupId
GROUP BY dg.Name, dt.Name
ORDER BY dg.Name;