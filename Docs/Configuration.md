# Configuration

> **NOTE**: RadiantPi requires the Lumagen RadiancePro to have Echo enabled:
> * MENU → Other → I/O Setup → RS-232 Setup → Echo → On

## appsettings.Development.json

```json
{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "RadiantPi.Lumagen.RadianceProClient": "Information",
      "RadiantPi.Sony.Cledis.SonyCledisClient": "Information",
    }
  }
}
```

Edit the `appsettings.json` file and update the `"RadiancePro`" section to match your needs.
```json
{
  "RadiancePro": {
    "PortName": "/dev/ttyUSB0",
    "BaudRate": 9600,
    "Mock": false
  }
}
```

RadiancePro Settings
* `PortName`: the name of the COM port the Lumagen RadiancePro is connected to
* `BaudRate`: (optional) the baud rate at which the application connects over the COM port (default: 9600)
* `Mock`: (optional) simulate a connected device instead (default: false)


## Logging

### RadiantPi.Lumagen.RadianceProClient

* `Information`
    * log high-level client activity
* `Debug`
    * log deserialized responses
* `Trace`
    * log data received over serial port
    * log internal event dispatching

## RadiantPi.Sony.Cledis.SonyCledisClient

* `Information`
    * log high-level client activity
* `Debug`
    * log responses
* `Trace`
    * log data received over telnet socket
