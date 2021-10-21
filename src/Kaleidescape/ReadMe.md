# Kaleidescape

Enable events for devices with S/N 123
```
01/1/ENABLE_EVENTS:#123:
```

Command format: `id/seq/message_body`
* `id` is the device ID (use 1)
* `seq` is an arbitrary sequence number used to match up responses
* `message_body` is the message being sent

Event received from device with S/N 123
```
#123/!/000:HIGHLIGHTED_SELECTION:26-0.0-S_c44c8eeb:/76
```

Event format: `id/!/status:event:event_body/checksum`
* `id` is the device ID (use 1)
* `status` is the status code (000 is success)
* `event` is the name of the event
* `event_body` is details of the event


Get details about a given movie (e.g. from selection event)
```
01/1/GET_CONTENT_DETAILS:26-0.0-S_c4441ad8::
```

```
01/1/000:CONTENT_DETAILS_OVERVIEW:16:26-0.0-S_c4441ad8:movies:/32
01/1/000:CONTENT_DETAILS:1:Content_handle:26-0.0-S_c4441ad8:/39
01/1/000:CONTENT_DETAILS:2:Title:In the Heights:/21
01/1/000:CONTENT_DETAILS:3:Cover_URL:http\:\/\/192.168.1.147\/panelcoverart\/15857fdeb978ab39\/42232929.jpg:/44
01/1/000:CONTENT_DETAILS:4:HiRes_cover_URL:http\:\/\/192.168.1.147\/panelcoverarthr\/15857fdeb978ab39\/42232929.jpg:/65
01/1/000:CONTENT_DETAILS:5:Rating:PG-13:/35
01/1/000:CONTENT_DETAILS:6:Year:2021:/25
01/1/000:CONTENT_DETAILS:7:Running_time:143:/43
```

NOTE: `\r` in the response means additional values