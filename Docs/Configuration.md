# Configuration

## Setting up Lumagen RadiancePro

_RadiantPi_ requires the Lumagen RadiancePro to have Echo enabled:
* MENU → Other → I/O Setup → RS-232 Setup → Echo → On

## Setting up the _appsettings.json_ file

The _appsettings.json_ file contains the startup configuration for _RadiantPi_. Here you set the connection details of your Lumagen RadiancePro, as well as any other components.

You can also configure an optional automation file that reacts to reported mode changes by the RadiancePro.

### Sample
The following sample _appsettings.json_ file contains a configuration for a RadiancePro, Sony Crystal LED device, and an automation definition.
```json
{
  "RadiancePro": {
    "PortName": "/dev/ttyUSB0"
  },

  "SonyCledis": {
    "Host": "192.168.0.72",
    "Port": 53595
  },

  "Automation": "automation.json",

  "Urls": "http://*:5000",
  "AllowedHosts": "*",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

## Device Configuration

### RadiancePro Settings

Add a `RadiancePro` entry to the _appsettings.json_ file.

* `PortName`: the name of the COM port the Lumagen RadiancePro is connected to
* `BaudRate`: (optional) the baud rate at which the application connects over the COM port (default: 9600)
* `Mock`: (optional) simulate a connected device (default: false)

### Sony Crystal LED Settings

Add a `SonyCledis` entry to the _appsettings.json_ file.

* `Host`: the IP address of the Sony Crystal LED controller
* `Port`: (optional) the telnet port on the Sony Crystal LED controller (default: 53595)
* `Mock`: (optional) simulate a connected device (default: false)

## Automation Configuration

Add a `Automation` entry to the _appsettings.json_ file that specifies the location of the JSON automation file.

See [Automation documentation](Automation.md) for more details about the JSON automation file.

## App Configuration

* `Urls`: lists the web protocol and network interfaces the app responds to. By default, the app responds to HTTP on all interfaces over port 5000
* `AllowedHosts`: lists which host names are allowed. Dy default any host name is allowed
* `Logging`: defines the level of logging emitted by the app; see [Logging](Logging.md) for additional app-specific settings

Explore the [ASP.NET documentation](https://docs.microsoft.com/en-us/aspnet/core/?view=aspnetcore-5.0) to find about additional _RadianPi_ settings.