# Logging

The various device clients have different logging levels to show additional details, which can be useful when tracking down an issue.

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
