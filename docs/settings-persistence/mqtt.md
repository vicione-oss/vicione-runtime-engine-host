# MQTT

The values of the _settings_ are published on the broker with their setting IDs as topics. The value is transported in JSON format and has the following user properties:

| Name | Content |
| --- | --- |
| Timestamp | timestamp of the value in UTC according to ISO 8601 |

Sets are published on the topic `settings`.
