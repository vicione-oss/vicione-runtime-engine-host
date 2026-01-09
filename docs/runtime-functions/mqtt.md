# MQTT

The runtime functions are implemented as MQTT RPC.

| | |
| --- | --- |
| **Request Topic** | `{EngineId}/request` |
| **Response Topic** | `{EngineId}/response` |

## Payload

The content of the commands is expected in the payload as JSON. Example for ChangeLogLevelCommand:

```json
{
    "LogLevel": "Information"
}
```

## UserProperties

The command to be executed is expected in the UserProperty `method`.

| Command | `method` Value |
| --- | --- |
| `ChangeLogLevelCommand` | `changeloglevel` |
| `LoadPersistenceSettingsCommand` | `loadsettings` |
| `LoadPersistenceVariablesCommand` | `loadvariables` |
