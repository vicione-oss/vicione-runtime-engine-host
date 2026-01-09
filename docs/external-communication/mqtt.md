# MQTT

The values of the _connectors_ are published and subscribed as topics on the broker via the defined channels. The value is transported in the payload in JSON format and has the following user properties:

| Name | Content |
| --- | --- |
| Type | fully qualified type of the transmitted value |
| Timestamp | timestamp of the value in UTC according to ISO 8601 |
| Validity | validity of the value as an integer |
| EngineCycle | engine cycle number as an integer |
