# Notes

* Lumagen always displays 2.20 at the bottom of the screen on memory change

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

# Trinnov



## Connect

```
telnet 192.168.1.180 44100
```

```
Welcome on Trinnov Optimizer (Version 4.2.16.4, ID 10486765)
```

## Declare Client
```
id RadiantPi
```

## Initial Payload
```
OK
SOURCES_CHANGED
OPTSOURCE 0 Source 1
OK
SOURCE 0
CURRENT_SOURCE_FORMAT_NAME Atmos narrow
CURRENT_SOURCE_CHANNELS_ORDER_IS_DCI 0
CURRENT_SOURCE_CHANNELS_ORDER C-L-Lw-Ls-Lrs-R-Rw-Rs-Rrs-Lfh-Lrh-Ltf-Ltm-Ltr-Rfh-Rrh-Rtf-Rtm-Rtr-Cs-LFE
START_RUNNING
CALIBRATION_DONE
STOP_COMPUTING
REMAPPING_MODE none
FAV_LIGHT 0
RIAA_PHONO 0
NETSTATUS ETH LINK "Connected" DHCP "1" IP "192.168.1.180" NETMASK "255.255.254.0" GATEWAY "192.168.1.1" DNS "192.168.1.1"
NETSTATUS WLAN WLANMODE "both" LINK "Disconnected" SSID "" AP_SSID "Altitude-1005" AP_PASSWORD "calibration" AP_IP "192.168.12.101" AP_NETMASK "255.255.255.0" IP "0.0.0.0" NETMASK "0.0.0.0" WPA_STATE "Inactive"
NETSTATUS SERVICE_STATUS "Connected to Trinnov Audio Server"
OK
LABELS_CLEAR
LABEL 0: Builtin
LABEL 1: Solfar 48 (flat)
LABEL 2: Solfar 48 (calibrated 2021-03-14)
LABEL 3: Solfar 48 (Cineramax)
LABEL 4: Solfar 48 (3D Remapping)
LABEL 5: Solfar 48 (3D Remapping+EQ)
LABEL 6: Solfar 48 (Cineramax+EQ)
LABEL 7: Solfar 48 (Cineramax+EQ+Full Left+Right)
LABEL 8: Solfar 48 (Hybrid+EQ+Full Left+Right)
LABEL 9: Steve 48
LABEL 10: Steve 48 (bis)
PROFILES_CLEAR
PROFILE 0: HDMI 1
PROFILE 1: HDMI 2
PROFILE 2: HDMI 3
PROFILE 3: HDMI 4
PROFILE 4: HDMI 5
PROFILE 5: HDMI 6
PROFILE 6: HDMI 7
PROFILE 7: HDMI 8
PROFILE 8: NETWORK
PROFILE 9: S/PDIF IN 1
PROFILE 10: S/PDIF IN 2
PROFILE 11: S/PDIF IN 3
PROFILE 12: S/PDIF IN 4
PROFILE 13: S/PDIF IN 7.1 PCM
PROFILE 14: Optical IN 5
PROFILE 15: Optical IN 6
PROFILE 16: Optical IN 7
PROFILE 17: Optical IN 8
PROFILE 18: Optical IN 7.1 PCM
PROFILE 19: DCI MCH AES IN
PROFILE 20: AES IN 1
PROFILE 21: AES IN 2
PROFILE 22: ANALOG BAL IN 1
PROFILE 23: ANALOG BAL IN 2
PROFILE 24: ANALOG BAL IN 1+2 (MIC 4 XLR)
PROFILE 25: MIC IN
PROFILE 26: ANALOG SE1 IN 7.1
PROFILE 27: ANALOG SE2 IN
PROFILE 28: ANALOG SE3 IN
PROFILE 29: ANALOG SE4 IN
PROFILE 30: ROON
OK
SRATE 48000
AUDIOSYNC_STATUS 1
DECODER NONAUDIO 0 PLAYABLE 1 DECODER none UPMIXER Dolby Surround
AUDIOSYNC Slave
```

## Events

```
DECODER NONAUDIO 0 PLAYABLE 0 DECODER PCM UPMIXER Dolby Surround
DECODER NONAUDIO 0 PLAYABLE 0 DECODER PCM UPMIXER none
DECODER NONAUDIO 0 PLAYABLE 0 DECODER PCM UPMIXER Dolby Surround
DECODER NONAUDIO 0 PLAYABLE 0 DECODER none UPMIXER Dolby Surround
DECODER NONAUDIO 0 PLAYABLE 0 DECODER none UPMIXER none
DECODER NONAUDIO 0 PLAYABLE 0 DECODER none UPMIXER Dolby Surround
DECODER NONAUDIO 0 PLAYABLE 0 DECODER none UPMIXER none
DECODER NONAUDIO 0 PLAYABLE 0 DECODER none UPMIXER Dolby Surround
DECODER NONAUDIO 1 PLAYABLE 0 DECODER none UPMIXER none
DECODER NONAUDIO 1 PLAYABLE 1 DECODER ATMOS TrueHD UPMIXER none
DECODER NONAUDIO 1 PLAYABLE 0 DECODER none UPMIXER none
DECODER NONAUDIO 1 PLAYABLE 1 DECODER ATMOS TrueHD UPMIXER none
DECODER NONAUDIO 1 PLAYABLE 0 DECODER none UPMIXER none
DECODER NONAUDIO 1 PLAYABLE 1 DECODER ATMOS TrueHD UPMIXER none
```

## REGEX
```
DECODER NONAUDIO (?<nonaudio>.+) PLAYABLE (?<playable>.+) DECODER (?<decoder>.+) UPMIXER (?<upmixer>.+)
```

* `nonaudio`: `0` or `1`
* `playable`: `0` or `1`
* `decoder`: `PCM` or `none` or `ATMOS TrueHD`
* `upmixer`: `none` or `Dolby Surround`
