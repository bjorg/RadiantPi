# Automation

The automation file defines rules for triggering actions on devices.

The following JSON automation file defines 3 conditions that use `DetectedAspectRatio` to categorize the aspect ratio of the content into _tall_, _standard_, and _wide_. Based on which condition is true, a command is sent to the RadiancePro to select the corresponding memory.
```json
{
  "RadiancePro": {
    "Conditions": {
      "$TallAR": "DetectedAspectRatio < 178",
      "$StandardAR": "(DetectedAspectRatio >= 178) && (DetectedAspectRatio <= 200)",
      "$WideAR": "DetectedAspectRatio > 200"
    },
    "Rules": [
      {
        "Name": "SelectMemoryA",
        "Condition": "$WideAR",
        "Actions": [
          {
            "RadiancePro.Send": "a"
          }
        ]
      },
      {
        "Name": "SelectMemoryB",
        "Condition": "$StandardAR",
        "Actions": [
          {
            "RadiancePro.Send": "b"
          }
        ]
      },
      {
        "Name": "SelectMemoryC",
        "Condition": "$TallAR",
        "Actions": [
          {
            "RadiancePro.Send": "c"
          }
        ]
      }
    ]
  }
}
```

## Devices

* `RadiancePro`: (optional) automation configuration for Lumagen RadiancePro

## Automation

* `Conditions`: (optional) list of conditions used by the rules
* `Rules`: (optional) list of rules to trigger actions

## Rule

* `Name`: (optional) the name of the rule (default: `Rule #1`, `Rule #2`, ...)
* `Enabled`: (optional) boolean flag to enable/disable a rule (default: `true`)
* `Condition`: a boolean expression that must be true for the actions to be triggered
* `Actions`: a list of actions

## Events

### Lumagen RadiancePro - Mode Changed Event

|Property                       |Type                                                                                       |Description
|-------------------------------|-------------------------------------------------------------------------------------------|--------------------
|`InputStatus`                  |`Undefined`, `NoSource`, `ActiveVideo`, `InternalPattern`                                  |What source is feeding the active input
|`VirtualInputSelected`         |int                                                                                        |Virtual input selected by remote/RS232 command
|`PhysicalInputSelected`        |int                                                                                        |Physical Input selected for current virtual input
|`SourceVerticalRate`           |string                                                                                     |Source vertical rate (e.g.059 for 59.94, 060 for 60.00)
|`SourceVerticalResolution`     |string                                                                                     |Source vertical resolution (e.g. 1080 for 1080p)
|`SourceVideoMode`              |`Undefined`, `NoVideo`, `Interlaced`, `Progressive`                                        |Source mode
|`Source3DMode`                 |`Undefined`, `Off`, `FrameSequential`, `FramePacked`, `TopBottom`, `SideBySide`            |Source 3D mode
|`SourceRasterAspectRatio`      |string                                                                                     |Source raster aspect(e.g. 178 for HD or UHD)
|`SourceContentAspectRatio`     |string                                                                                     |Source content aspect (e.g. 240 for 2.40)
|`SourceDynamicRange`           |`Undefined`, `SDR`, `HDR`                                                                  |Source dynamic range
|`ActiveInputConfigNumber`      |string                                                                                     |Active input config number for current input resolution
|`OutputNonLinearStretchActive` |boolean                                                                                    |NLS active
|`Output3DMode`                 |`Undefined`, `Off`, `FrameSequential`, `FramePacked`, `TopBottom`, `SideBySide`            |Output 3D mode
|`OutputEnabled`                |int                                                                                        |Outputs on
|`OutputCms`                    |`Undefined`, `Cms0`, `Cms1`, `Cms2`, `Cms3`, `Cms4`, `Cms5`, `Cms6`, `Cms7`                |Active output CMS
|`OutputStyle`                  |`Undefined`, `Style0`, `Style1`, `Style2`, `Style3`, `Style4`, `Style5`, `Style6`, `Style7`|Active output style
|`OutputVerticalRate`           |string                                                                                     |Output vertical rate (e.g.059 for 59.94, 060 for 60.00)
|`OutputVerticalResolution`     |string                                                                                     |Output vertical resolution (e.g. 1080 for 1080p)
|`OutputVideoMode`              |`Undefined`, `NoVideo`, `Interlaced`, `Progressive`                                        |Output mode
|`OutputAspectRatio`            |string                                                                                     |Output aspect (e.g. 178 for 16:9)
|`OutputColorSpace`             |`Undefined`, `CS601`, `CS709`, `CS2020`, `CS2100`                                          |Output colorspace
|`DetectedAspectRatio`          |string                                                                                     |Detected input aspect ratio (e.g. 240 for 2.40)
|`DetectedRasterAspectRatio`    |string                                                                                     |Detected raster aspect ratio (e.g. 240 for 2.40)

## Actions

### Lumagen RadiancePro Actions

> TODO: describe automation commands
> * `RadiancePro.Send`: string (use `\` for sending special characters; e.g. `\r`)

### Sony Crystal LED Actions

> TODO: describe automation commands
> * `SonyCledis.PictureMode`: enum (`Mode1` .. `Mode10`)
> * `SonyCledis.Input`: enum (`Hdmi1`, `Hdmi2`, `DisplayPort1`, `DisplayPort2`, `DisplayPortBoth`)

### Built-in Actions

> TODO: describe automation commands
> * `Shell.Run`: object
> * `Wait`: double (seconds to wait)

## Automation Expressions

Conditions and rules have boolean expressions that determine their status. Conditions can only refer to event properties to avoid circular dependencies. However, rules can refer to both conditions and event properties.

Rules are only evaluated when at least one event property or condition they depend on has changed since the last event.

The following grammar describes the available operations and their precedence.
```
EXPRESSION ::=
    TERM ( || TERM )*
    ;

TERM ::=
    COMPARISON ( && COMPARISON )*
    ;

COMPARISON ::=
    OPERAND (
        ( < | <= | == | != | >= | > )
        OPERAND
    )*
    ;

FACTOR ::=
    `(` EXPRESSION `)`
    | INTEGER
    | SINGLE_QUOTE_STRING
    | `true`
    | `false`
    | CONDITION
    | EVENT_PROPERTY
    ;

CONDITION ::=
    `$`IDENTIFIER
    ;

EVENT_PROPERTY ::=
    IDENTIFIER
    ;
```