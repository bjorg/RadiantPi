# Configuration

> **NOTE**: RadiantPi requires the Lumagen RadiancePro to have Echo enabled:
> * MENU → Other → I/O Setup → RS-232 Setup → Echo → On

## Setting up the _appsettings.json_ file

The _appsettings.json_ file contains the startup configuration for _RadiantPi_. Here you set the location of your Lumagen RadiancePro, and any additional components.

You can configure an optional automation file that reacts to reported mode changes by the RadiancePro.

```json
{
  "Urls": "http://*:5000",
  "AllowedHosts": "*",

  "RadiancePro": {
    "PortName": "/dev/ttyUSB0",
    "Mock": true
  },

  "SonyCledis": {
    "Host": "192.168.1.190",
    "Mock": true
  },

  "Automation": "automation.json",

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

Add a `RadiancePro` section to the _appsettings.json_ file.

* `PortName`: the name of the COM port the Lumagen RadiancePro is connected to
* `BaudRate`: (optional) the baud rate at which the application connects over the COM port (default: 9600)
* `Mock`: (optional) simulate a connected device instead (default: false)

### Sony Crystal LED Settings

Add a `SonyCledis` section to the _appsettings.json_ file.

* `Host`: the IP address of the Sony Crystal LED controller
* `Port`: (optional) the telnet port on the Sony Crystal LED controller (default: 53595)
* `Mock`: (optional) simulate a connected device instead (default: false)

## Automation Configuration

> TODO: describe automation commands
> * `RadiancePro.Send`
> * `SonyCledis.PictureMode`
> * `Shell.Run`
> * `Wait`

## Logging Configuration

The various automation clients have different logging levels to show additional details, which can be essential when tracking down an issue.

Adjust the logging level for each client in the _appsettings.Development.json_ file.

```json
{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "RadiantPi.Lumagen.RadianceProClient": "Debug",
      "RadiantPi.Sony.Cledis.SonyCledisClient": "Debug",
      "RadiantPi.Automation.AutomationController": "Debug"
    }
  }
}
```

### `RadiantPi.Lumagen.RadianceProClient`

* `Information`
    * log high-level client activity
* `Debug`
    * log deserialized responses
* `Trace`
    * log data received over serial port
    * log internal event dispatching

### `RadiantPi.Sony.Cledis.SonyCledisClient`

* `Information`
    * log high-level client activity
* `Debug`
    * log responses
* `Trace`
    * log data received over telnet socket

### `RadiantPi.Automation.AutomationController`

* `Information`
    *
* `Debug`
    * log compiled rules with dependencies
    * log event details
    * log dispatched actions
    * log shell command output
* `Trace`
    * log evaluated conditions
    * log dependency tracking
