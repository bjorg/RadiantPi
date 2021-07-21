# Notes

* Lumagen always displays 2.20 at the bottom of the screen on memory change

* how to set stick resolution switches on memory click?

Log to CloudWatch
* https://github.com/aws/aws-logging-dotnet

Received:
```
!I24,1,023,2160,0,0,178,220,-,0,000a,1,0,023,2160,178,2,1,p,P,05,05,178,220
```

Emitted:
```json
{
  "SourceContentAspectRatio": "220",
  "InputStatus": "ActiveVideo",
  "VirtualInputSelected": 5,
  "PhysicalInputSelected": 5,
  "SourceVerticalRate": "023",
  "SourceVerticalResolution": "2160",
  "SourceVideoMode": "Progressive",
  "Source3DMode": "Off",
  "ActiveInputConfigNumber": "0",
  "SourceRasterAspectRatio": "178",
  "SourceDynamicRange": "HDR",
  "OutputNonLinearStretchActive": false,
  "Output3DMode": "Off",
  "OutputEnabled": 10,
  "OutputCms": "Cms1",
  "OutputStyle": "Style0",
  "OutputVerticalRate": "023",
  "OutputVerticalResolution": "2160",
  "OutputVideoMode": "Progressive",
  "OutputAspectRatio": "178",
  "OutputColorSpace": "CS2020",
  "InputAspectRatio": "220"
}
```

## Action Format Evolution
```json
{
  "Device": "RadiancePro",
  "Commands": [
    {
      "Send": "c"
    },
    {
      "Display": {
        "Message": "hi",
        "Align": "center"
      }
    }
  ]
}

```

```yaml
Automation:
  Conditions:
    $FitHeight: (DetectedAspectRatio > '') && (DetectedAspectRatio < 178)
    $FitWidth: (DetectedAspectRatio > '') && (DetectedAspectRatio >= 178) &&
      (DetectedAspectRatio <= 200)
    $FitNative: (DetectedAspectRatio > '') && (DetectedAspectRatio > 200)
  Rules:
    - Name: SwitchToFitHeight
      Condition: $FitHeight
      Actions:
      - Device: RadiancePro
        Commands:
          - Send: c
          - Display:
              Message: Fit Height
              Align: center

    - Name: SwitchToFitWidth
      Condition: $FitWidth
      Actions:
      - Device: RadiancePro
        Commands:
          - Send: b
          - Display:
              Message: Fit Width
              Align: center

    - Name: SwitchToNative
      Condition: $FitNative
      Actions:
      - Device: RadiancePro
        Commands:
          - Send: a
          - Display:
              Message: Native
              Align: center
```