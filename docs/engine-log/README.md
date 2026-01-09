# Engine Log

## Types

| Type | Implemented |
| --- | :-: |
| [NDJSON](ndjson.md) | ✔ |
| Microsoft SQL | ✖ |
| MySQL | ✖ |
| PostgreSQL | ✖ |
| SQLite | ✖ |
| [Serilog](serilog.md) | ✔ |

## Log Entry Types

The types are mapped in [LogLevel](https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Logging.Abstractions/src/LogLevel.cs) from the package [Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/). Preferably, a number is written to the data sink instead of the text.
