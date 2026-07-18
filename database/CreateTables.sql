
-- IoT Asset Tracker Database Schema

--Rules for entities in the db:
            --  We have different types of IoT devices
            -- Each device type can have multiple firmware versions
            -- We have different groups of devices, and each group can contain other groups (group of groups)
            -- Individual IoT devices are stored with their configuration and location
            
-- 0. Create the database if it doesn't exist
Create Database IoTAssetTracker_db;
GO

Use IoTAssetTracker_db;
GO

-- 1. DeviceType Table

CREATE TABLE DeviceType (
    DeviceTypeId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    CONSTRAINT UQ_DeviceType_Name UNIQUE (Name)
);


-- 2. Firmware Table 

CREATE TABLE Firmware (
    FirmwareId INT IDENTITY(1,1) PRIMARY KEY,
    DeviceTypeId INT NOT NULL,
    Version NVARCHAR(20) NOT NULL,
    ReleaseDate DATE NOT NULL,
    Notes NVARCHAR(255) NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Firmware_DeviceType FOREIGN KEY (DeviceTypeId) 
        REFERENCES DeviceType(DeviceTypeId) ON DELETE NO ACTION,
    CONSTRAINT UQ_DeviceType_Version UNIQUE (DeviceTypeId, Version)
);


-- 3. DeviceGroup Table (Hierarchical)

CREATE TABLE DeviceGroup (
    GroupId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    ParentGroupId INT NULL,
    Description NVARCHAR(255) NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_DeviceGroup_Parent FOREIGN KEY (ParentGroupId) 
        REFERENCES DeviceGroup(GroupId) ON DELETE NO ACTION,
    CONSTRAINT CK_SelfReference CHECK (GroupId != ParentGroupId),
    CONSTRAINT UQ_DeviceGroup_Name UNIQUE (Name)
);


-- 4. Device Table

CREATE TABLE Device (
    DeviceId INT IDENTITY(1,1) PRIMARY KEY,
    SerialNumber NVARCHAR(50) NOT NULL UNIQUE,
    Name NVARCHAR(100) NOT NULL,
    Latitude DECIMAL(9,6) NOT NULL,
    Longitude DECIMAL(9,6) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FirmwareId INT NOT NULL,
    GroupId INT NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    LastModified DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Device_Firmware FOREIGN KEY (FirmwareId) 
        REFERENCES Firmware(FirmwareId),
    CONSTRAINT FK_Device_Group FOREIGN KEY (GroupId) 
        REFERENCES DeviceGroup(GroupId) ON DELETE SET NULL,
    CONSTRAINT CK_Latitude CHECK (Latitude BETWEEN -90 AND 90),
    CONSTRAINT CK_Longitude CHECK (Longitude BETWEEN -180 AND 180)
);


-- 5. Indexes for Performance Optimization

-- Improve query performance on frequently accessed columns
CREATE INDEX IX_Device_GroupId ON Device(GroupId);
CREATE INDEX IX_Device_IsActive ON Device(IsActive);
CREATE INDEX IX_Firmware_DeviceTypeId ON Firmware(DeviceTypeId);
CREATE INDEX IX_DeviceGroup_ParentGroupId ON DeviceGroup(ParentGroupId);


