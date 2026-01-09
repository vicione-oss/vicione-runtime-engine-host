# Runtime functions

The running engine offers various commands and queries. This makes it possible to call these as a third party. Any number of integrations can be configured per engine.

The possible commands are defined in the [Managed Engine](https://gitlab.i40.ifm-datalink.net/acx/vicione/runtime/managed-engine/-/tree/master/src/ManagedEngine.Contracts/Pipelines/Commands).

## Types

| Protocol | Implemented | Example |
| --- | :-: | --- |
| AMQP | ✖ | Centralized: [RabbitMQ](https://www.rabbitmq.com/) / Direct: [AMQPNetLite](https://github.com/Azure/amqpnetlite) |
| [MQTT](mqtt.md) | ✔ | Centralized: [Emitter](https://emitter.io/) / Direct: [NetMQ](https://github.com/zeromq/netmq), [MQTTnet](https://github.com/dotnet/MQTTnet) |
