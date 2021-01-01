![RadiantPi Input Labels](Docs/InputLabels.png)

# RadiantPi

RadianPi is a web application for controlling a Lumagen RadiancePro from a RaspberryPi 4B device.

## Features

* See model number and software revision
* Edit labels for inputs, custom modes, CMS, and styles.

## App Setup

1. Check out the [hardware guide](Docs/Hardware.md) to use RadiantPi.
1. SSH into your RaspberryPi 4B device.
1. [Install .NET 5](https://www.petecodes.co.uk/install-and-use-microsoft-dot-net-5-with-the-raspberry-pi/) on your RaspberryPi 4B device.
1. Clone the RadiantPi code to your device: `git clone https://github.com/bjorg/RadiantPi.git`
1. Switch into the application folder `RadiantPi/Src/RadiantPi`.
1. Update the `appsettings.json` configuration file (see below).
1. Launch RadiantPi: `dotnet run` (Hint: check [this page](https://thomaslevesque.com/2018/04/17/hosting-an-asp-net-core-2-application-on-a-raspberry-pi/) for running RadiantPi as a service)

## App Configuration

Edit the `appsettings.json` file and update the `"RadiancePro`" section to match your needs.
```json
{
  "RadiancePro": {
    "PortName": "/dev/ttyUSB0",
    "BaudRate": 9600,
    "Mock": false,
    "Verbose": false
  }
}
```

RadiancePro Settings
* `PortName`: the name of the COM port the Lumagen Radiance Pro is connected to
* `BaudRate`: (optional) the baud rate at which the application connects over the COM port (default: 9600)
* `Mock`: (optional) simulate a connected device instead (default: false)
* `Verbose`: (optional) log communication messages sent between the device and application (default: false)

# License

This application is distributed under the GNU Affero General Public License v3.0 or later.

Copyright (C) 2020 - Steve G. Bjorg