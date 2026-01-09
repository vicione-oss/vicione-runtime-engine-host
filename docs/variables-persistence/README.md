# FunctionBlock variables persistence

FunctionBlocks can hold variables that have long-term significance and influence the execution logic. Variable contents can change in every engine cycle. The storage of information in a cloud environment must be implemented with appropriate data sinks. All current values of the variables can be saved as a set for later reuse. Versioned storage of the values is also conceivable.

## Types

| Type | Implemented | Example |
| --- | :-: | --- |
| AMQP | ✖ | Centralized: [RabbitMQ](https://www.rabbitmq.com/) / Direct: [AMQPNetLite](https://github.com/Azure/amqpnetlite) |
| [JSON File](json-file.md) | ✔ | |
| [MQTT](mqtt.md) | ✔ | Centralized: [Emitter](https://emitter.io/) / Direct: [NetMQ](https://github.com/zeromq/netmq), [MQTTnet](https://github.com/dotnet/MQTTnet) |
| PostgreSQL | ✖ | |
| Redis | ✖ | |
| S3 Storage | ✖ | |
| SQLite | ✖ | |
| TimescaleDB | ✖ | |
