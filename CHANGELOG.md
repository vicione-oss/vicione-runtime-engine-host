# Changelog

## Next - Unreleased

### Changed

- Native library handles are now freed only after the owning AssemblyLoadContext is confirmed collected, preventing use-after-free crashes
- Cache `JsonSerializerOptions` per deployment to avoid using the default instance which is shared with the host
- Clear `JsonSerializer` caches on context unload to prevent memory leaks from unloaded assemblies
- Progressive GC collection on context unload to speed up memory cleanup
- Update `OpenTelemetry.Exporter.OpenTelemetryProtocol` to `1.15.3`
- Update `OpenTelemetry.Extensions.Hosting` to `1.15.3`
- Update `OpenTelemetry.Instrumentation.Runtime` to `1.15.1`
- Update `Microsoft.Extensions.DependencyModel` to `10.0.7`
- Update `Microsoft.Extensions.Hosting` to `10.0.7`
- Update `Microsoft.Extensions.Logging.Abstractions` to `10.0.7`
- Update `Microsoft.Extensions.Logging.Configuration` to `10.0.7`
- Update `Serilog.Sinks.Journal` to `1.2.0`
- Update `Testably.Abstractions` to `10.2.0`
- Update `NuGet.Commands` to `7.3.1`

### Fixed

- Cache assemblies in `PackagesAssemblyLoadContext` with weak references to ensure they can be collected when the context is unloaded
- Update `FastCloner` to `3.5.5` to fix unloading issues until core engine updates the version

## 1.0.0 - 2026-03-18

### Added

- Add GetPackages command
- Support OpenTelemetry

### Changed

- Update `.NET` to `10.0`
- Use bounded channel with `DropOldest` for log message queue in `JsonFileLoggerProvider`
- Skip cycle and crash report publishing when previous report is still in progress
- Wait for report tasks in StopAsync
- Update `ViciOne.ManagedEngine` to `1.0.0`
- Update `ViciOne.ManagedEngine.Contracts` to `1.0.0`
- Update `Microsoft.Extensions.DependencyModel` to `10.0.5`
- Update `Microsoft.Extensions.Hosting` to `10.0.5`
- Update `Microsoft.Extensions.Logging.Abstractions` to `10.0.5`
- Update `Microsoft.Extensions.Logging.Configuration` to `10.0.5`
- Update `Serilog.Sinks.Journal` to `1.1.0`

## 0.30.0 - 2025-11-06

### Added

- Set client id of MQTT clients
- Support cancellation of engine chain execution

### Changed

- Use an drift free timer for engine chain cycles

### Fixed

- Skip deactivated chain links

## 0.29.0 - 2025-10-21

### Changed

- Separate tick processing from tick analysis in engine chain timer
- Update `Microsoft.Extensions.DependencyModel` to `9.0.10`
- Update `Microsoft.Extensions.Hosting` to `9.0.10`
- Update `Microsoft.Extensions.Logging.Abstractions` to `9.0.10`
- Update `Microsoft.Extensions.Logging.Configuration` to `9.0.10`
- Update `ViciOne.ManagedEngine` to `0.62.0`
- Update `ViciOne.ManagedEngine.Contracts` to `0.62.0`

### Fixed

- Fix detection of skipped cycles in engine chain timer

## 0.28.0 - 2025-09-11

### Added

- Publish meta data in zip artifacts
- Support configuration of retain flag and expiry interval for MQTT messages

### Fixed

- Dispose provider factory on deploy failure
- Publish state message on startup after successful subscribe

## 0.27.0 - 2025-07-11

### Added

- Support journal for engine-host logging
- Support journal for engine logging
- Support configuration of max pending messages for MQTT

### Changed

- Update `MQTTnet.Extensions` to `0.19.0`
- Update `Microsoft.Extensions.DependencyModel` to `9.0.7`
- Update `Microsoft.Extensions.Hosting` to `9.0.7`
- Update `Microsoft.Extensions.Logging.Abstractions` to `9.0.7`
- Update `Microsoft.Extensions.Logging.Configuration` to `9.0.7`
- Update `Serilog.Extensions.Logging` to `9.0.2`

## 0.26.0 - 2025-05-19

### Changed

- Improve stop workflow of engine chain timer
- Update `Microsoft.Extensions.DependencyModel` to `9.0.5`
- Update `Microsoft.Extensions.Hosting` to `9.0.5`
- Update `Microsoft.Extensions.Logging.Abstractions` to `9.0.5`
- Update `Microsoft.Extensions.Logging.Configuration` to `9.0.5`
- Update `ViciOne.ManagedEngine` to `0.61.0`
- Update `ViciOne.ManagedEngine.Contracts` to `0.61.0`
- Update `NuGet.Commands` to `6.14.0`
- Update `Serilog.Extensions.Logging` to `9.0.1`
- Update `Serilog.Sinks.File` to `6.0.0`

### Fixed

- Prevent context from loading `System.Runtime` again

## 0.25.0 - 2025-03-04

### Added

- Cache load assembly result in assembly load context

### Changed

- Optimize Serilog initialization
- Reduce IO operations when reading assembly versions
- Load assemblies from default AssemblyLoadContext directly
- Load native assemblies like mentioned in the documentation
- Regard version in names when aggregating assemblies
- Store deployment information as GZip compressed JSON files
- Update `ViciOne.ManagedEngine` to `0.58.0`
- Update `ViciOne.ManagedEngine.Contracts` to `0.58.0`
- Update `NuGet.Commands` to `6.13.2`

## 0.24.0 - 2025-02-12

### Fixed

- Handle command to get the state of all deployments

### Added

- Use ContentType `text/plain` for RPC failure response
- Support ContentType `application/gzip` for RPC request

### Changed

- Update `Microsoft.Extensions.DependencyModel` to `9.0.2`
- Update `Microsoft.Extensions.Hosting` to `9.0.2`
- Update `Microsoft.Extensions.Logging.Abstractions` to `9.0.2`
- Update `Microsoft.Extensions.Logging.Configuration` to `9.0.2`
- Update `MQTTnet.Extensions` to `0.18.0`
- Update `NuGet.Commands` to `6.13.1`
- **Breaking:** Require ContentType `application/json` or `application/gzip` for RPC request

## 0.23.0 - 2025-02-06

### Added

- Initiate graceful termination with 'terminate' file
- Implement command to get the state of all deployments

### Removed

- Initiate graceful termination with MQTT RPC method 'terminate'
- Optionally force the engine host to stop
- Restart forcibly stopped engines automatically when starting the application

## 0.22.0 - 2024-12-18

### Changed

- **Breaking:** Update `MQTTnet.Extensions` to `0.17.0`
- Use CommandBus for Monitoring if Monitoring configuration section is not specified
- Update `.NET` to `9.0`
- Update `Microsoft.Extensions.DependencyModel` to `9.0.0`
- Update `Microsoft.Extensions.Hosting` to `9.0.0`
- Update `Microsoft.Extensions.Logging.Abstractions` to `9.0.0`
- Update `Microsoft.Extensions.Logging.Configuration` to `9.0.0`
- Update `ViciOne.ManagedEngine` to `0.56.0`
- Update `ViciOne.ManagedEngine.Contracts` to `0.56.0`
- Update `NuGet.Commands` to `6.12.1`
- Update `Serilog.Extensions.Hosting` to `9.0.0`
- Update `Serilog.Extensions.Logging` to `9.0.0`
- Update `Serilog.Settings.Configuration` to `9.0.0`

### Fixed

- Keep MQTT connection alive for last will until app shutdown
- Read configuration correctly for assembly preloading
- Configure last will for CommandBus globally

## 0.21.0 - 2024-10-16

### Added

- Support preloading of `System.ComponentModel.TypeConverter` in `AssemblyLoadContext`
- Cache important assembly names for assembly load context creation

### Changed

- Fine tune log messages and levels
- Catch deployment start failures during recovery
- Update `Microsoft.Extensions.DependencyModel` to `8.0.2`
- Update `Microsoft.Extensions.Hosting` to `8.0.1`
- Update `Microsoft.Extensions.Logging.Abstractions` to `8.0.2`
- Update `Microsoft.Extensions.Logging.Configuration` to `8.0.1`
- Update `ViciOne.ManagedEngine` to `0.55.0`
- Update `ViciOne.ManagedEngine.Contracts` to `0.55.0`
- Update `MQTTnet.Extensions` to `0.16.0`
- Update `NuGet.Commands` to `6.11.1`
- Update `Serilog.Settings.Configuration` to `8.0.4`
- Update `Serilog.Sinks.SyslogMessages` to `4.0.0`

### Fixed

- Use asynchronous IO operations
- Remove chain link if recovery of a deployment fails
- Regard disable certificate validation value

### Removed

- Remove `UseSymLinks` option from host config

## 0.20.0 - 2024-08-30

### Added

- Add reference to `Serilog.Sinks.SyslogMessages`
- Support linux syslog for engine logging
- Support linux syslog for engine host

### Changed

- Update `NuGet.Commands` to `6.11.0`
- Update `ViciOne.ManagedEngine` to `0.54.0`
- Update `ViciOne.ManagedEngine.Contracts` to `0.54.0`

## 0.19.0 - 2024-07-22

### Changed

- Throw an exception when receiving an MQTT message without user properties
- Update Microsoft.Extensions.DependencyModel to 8.0.1
- Update MQTTnet.Extensions to 0.13.0
- Update NuGet.Commands to 6.10.1
- Update Serilog.Settings.Configuration to 8.0.2
- Update Serilog.Sinks.File to 6.0.0
- Update ViciOne.ManagedEngine to 0.53.0
- Update ViciOne.ManagedEngine.Contracts to 0.53.0

### Fixed

- Use same type information for sending as for receiving

## 0.18.0 - 2024-06-24

### Added

- Assign ID to each communication

### Changed

- Update MQTTnet.Extensions to 0.12.0
- Update NuGet.Commands to 6.10.0
- Update Serilog.Settings.Configuration to 8.0.1
- Update Serilog.Sinks.Console to 6.0.0
- Update ViciOne.ManagedEngine to 0.52.0
- Update ViciOne.ManagedEngine.Contracts to 0.52.0

## 0.17.0 - 2024-04-19

### Fixed

- Read assembly version only from .NET assemblies

### Changed

- Update ViciOne.ManagedEngine to 0.50.0
- Update ViciOne.ManagedEngine.Contracts to 0.50.0

## 0.16.0 - 2024-04-17

### Changed

- Update Microsoft.Extensions.Logging.Abstractions to 8.0.1
- Update ViciOne.ManagedEngine to 0.49.0
- Update ViciOne.ManagedEngine.Contracts to 0.49.0

## 0.15.0 - 2024-03-22

### Added

- Extend assembly loading messages with a stack trace snippet
- Force garbage collector to clean up aggressively on context unload
- Resolve native assemblies which are not included in a dependency file

### Changed

- Increase log level of assembly loading messages
- Load assemblies using their path instead of a stream

### Fixed

- Call dispose of deployments in deployment pool
- Prevent static caching in JSON options

## 0.14.0 - 2024-01-19

### Changed

- Update ViciOne.ManagedEngine to 0.48.0
- Update ViciOne.ManagedEngine.Contracts to 0.48.0
- Update NuGet.Commands to 6.9.1

## 0.13.0 - 2024-01-19

### Added

- Provide info command with informational version

### Changed

- Update MQTTnet.Extensions to 0.11.0
- Update ViciOne.ManagedEngine to 0.47.0
- Update ViciOne.ManagedEngine.Contracts to 0.47.0

### Fixed

- Remove revision information from version command result

## 0.12.0 - 2023-12-18

### Added

- Log successful unload of an AssemblyLoadContext

### Changed

- Recognize subsets of package references when creating contexts
- Invoke garbage collection without waiting when tear down
- Update .NET to 8.0
- Update Microsoft.Extensions.DependencyModel to 8.0.0
- Update Microsoft.Extensions.Hosting to 8.0.0
- Update Microsoft.Extensions.Logging.Abstractions to 8.0.0
- Update Microsoft.Extensions.Logging.Configuration to 8.0.0
- Update NuGet.Commands to 6.8.0
- Update Serilog.Extensions.Hosting to 8.0.0
- Update Serilog.Extensions.Logging to 8.0.0
- Update Serilog.Settings.Configuration to 8.0.0
- Update Serilog.Sinks.Console to 5.0.1
- Update MQTTnet.Extensions to 0.10.0
- Update ViciOne.ManagedEngine to 0.46.0
- Update ViciOne.ManagedEngine.Contracts to 0.46.0

### Removed

- **Breaking:** Support load in MQTT persistence

## 0.11.0 - 2023-11-02

### Changed

- Move default configuration values into appsettings.json
- Report configuration errors early and accurately
- Change default value of DeploymentsDirectory from "engines" to "deployments"
- Deploy engines without recovering from file
- Update ViciOne.ManagedEngine to 0.45.0
- Update ViciOne.ManagedEngine.Contracts to 0.45.0

## 0.10.0 - 2023-10-06

### Changed

- Update MQTTnet.Extensions to 0.9.0
- Update ViciOne.ManagedEngine to 0.44.0
- Update ViciOne.ManagedEngine.Contracts to 0.44.0
- Update NuGet.Commands to 6.7.0
- Update Serilog.Settings.Configuration to 7.0.1

### Removed

- Remove reference to System.Reactive
- Remove base path from package asset

## 0.9.0 - 2023-08-03

### Added

- Respond with outcome of start and stop command

### Changed

- Update MQTTnet.Extensions to 0.8.0
- Update ViciOne.ManagedEngine to 0.43.0
- Update ViciOne.ManagedEngine.Contracts to 0.43.0

## 0.8.0 - 2023-06-28

### Changed

- Identify direct communication subscribers by channel
- Update ViciOne.ManagedEngine to 0.42.0
- Update ViciOne.ManagedEngine.Contracts to 0.42.0
- Update Microsoft.Extensions.Logging.Abstractions to 7.0.1
- Update MQTTnet.Extensions to 0.7.0
- Update NuGet.Commands to 6.6.1
- Use JsonDerivedType attribute instead of TypeNameHandlingConverter

### Removed

- Remove hybrid direct MQTT communication

## 0.7.0 - 2023-06-08

### Added

- Support MQTT session options
- Log failed request execution
- Use json serializer context in cycle info service
- Add directory to package resolvers package class

### Changed

- **Breaking:** Return generated deployment identifier unmodified
- Allow reading from Serilog files during engine execution
- Log file path of loaded assembly
- Load assemblies directly from package directories
- Extend native assembly loaded log message with file path
- Format numbers in cycle time exceeded log message correctly
- Update 'ViciOne.ManagedEngine' to 0.41.0
- Update 'ViciOne.ManagedEngine.Contracts' to 0.41.0
- Update 'MQTTnet.Extensions' to 0.5.0
- Update 'NuGet.Commands' to 6.6.0
- Update 'Microsoft.Extensions.Hosting' to 7.0.1
- **Breaking:** Use default output template of Serilog as fallback
- Share 'ViciOne.ManagedEngine.Serialization' with all ALCs
- Update 'Serilog.Extensions.Hosting' to 7.0.0
- Update 'Serilog.Extensions.Logging' to 7.0.0
- Update 'Serilog.Settings.Configuration' to 7.0.0
- Update 'System.Reactive' to 6.0.0

### Removed

- Remove methods to configure context directory
- Remove reference to 'ViciOne.ManagedEngine.Serialization'

## 0.6.0 - 2023-02-08

### Added

- Report crashed chain cycles
- Provide more log messages for debugging purposes
- Provide message in case of connection interruption

### Changed

- **Breaking:** Change cycle info topic
- Separate context files from deployment files
- Update 'ViciOne.ManagedEngine' to 0.38.0
- Update 'ViciOne.ManagedEngine.Contracts' to 0.38.0
- Update 'ViciOne.ManagedEngine.Serialization' to 0.38.0
- **Breaking:** Move up and running host message to another topic
- Update 'MQTTnet.Extensions' to 0.4.0
- **Breaking:** Move runtime functions to another topic
- Reset engine chain after removing last chain link
- **Breaking:** Use uppercase deployment identifier

## 0.5.0 - 2022-12-16

### Added

- Send host version with up and running message

### Changed

- Set JSON number handling to 'AllowNamedFloatingPointLiterals'
- Update 'ViciOne.ManagedEngine' to 0.37.0
- Update 'ViciOne.ManagedEngine.Contracts' to 0.37.0
- Update 'ViciOne.ManagedEngine.Serialization' to 0.37.0

## 0.4.0 - 2022-12-07

### Added

- Provide cycle number in engine chain report
- Support load in MQTT persistence
- Observe unloading of assembly load contexts
- Log host version after start
- Provide host version with a command line argument

### Changed

- Simplify deployment creation
- Update .NET to 7.0
- Update 'Microsoft.Extensions.Logging' to 7.0.0
- Update 'Microsoft.Extensions.Hosting' to 7.0.0
- Update 'Serilog.Settings.Configuration' to 3.4.0
- Update 'Microsoft.Extensions.DependencyModel' to 7.0.0
- Update 'NuGet.Commands' to 6.4.0
- Load available assembly symbols into assembly load context
- Update 'MediatR.Extensions.Microsoft.DependencyInjection' to 11.0.0
- Update 'ViciOne.ManagedEngine' to 0.36.0
- Update 'ViciOne.ManagedEngine.Contracts' to 0.36.0
- Update 'ViciOne.ManagedEngine.Serialization' to 0.36.0
- Rename engine to deployment in engine chain report
- Only regard relevant entries on loading in file system persistence
- Update 'MQTTnet.Extensions' to 0.3.0
- Send messages with retain flag in MQTT persistence
- Replace 'Newtonsoft.Json' serialization with 'System.Text.Json'

### Fixed

- Handle idempotence correctly at tear down command

### Removed

- Remove reference to 'Newtonsoft.Json'
- Remove 'Newtonsoft.Json' from shared assemblies

## 0.3.0 - 2022-10-11

### Added

- Only allow execution of one request per deployment at the same time
- Support hybrid direct-MQTT inter engine communication

### Changed

- Update ViciOne Managed Engine to 0.35.0

### Fixed

- Clear static caches from Newtonsoft.Json on tear down
- Create configured directory of file system persistence

## 0.2.0 - 2022-09-12

### Added

- Support Serilog for engine logging
- Add reference to 'MQTTnet.Extensions' 0.2.0

### Changed

- Use PeriodicTimer for engine chain
- Use async/await for MqttOptimizer
- Configure waiting on async tasks
- Update 'NuGet.Commands' to 6.3.0
- Update 'ViciOne.ManagedEngine' to 0.34.0
- Update 'ViciOne.ManagedEngine.Contracts' to 0.34.0
- Update 'ViciOne.ManagedEngine.Serialization' to 0.34.0
- Manage engine states in memory
- Catch exceptions on file system persistence save
- Use 'Channel' instead of 'BlockingCollection' in JsonFileLogger
- Use Serilog for host logging
- Log cycle durations when cycle time is exceeded

### Removed

- Remove reference to 'MQTTnet.Extensions.ManagedClient'

## 0.1.0 - 2022-08-24

- Initial Release
