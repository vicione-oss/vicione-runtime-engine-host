# External Communication

The data flow defines which connectors should be available and can be modified. Channels are defined through which values are sent and received. Channels can be used multiple times, for example by different connectors, or for different communication directions. Values from inputs and outputs can be sent, whereby only values for inputs can be received.

External communication can be used for various purposes:

- Communication between engines
- Data exchange with third parties
- Logging of process data

If the values for different purposes run through a common data node, a thematic separation and access protection of the topics is recommended. For MQTT, this could be a topic prefix with encryption. Protection during subscription by an MQTT broker is also conceivable.

No transmission medium is necessary that allows data exchange in both directions simultaneously at all times (full duplex). Often a single directed connection is sufficient (simplex). These connections can also vary in both directions depending on the scenario (dual simplex) and fit very well into mixed cloud-on-premise environments. This allows connections to be bundled, monitored and protected more easily. The parameters for the transmission media depend on the specific environment in which the data flow is to be deployed and do not belong to the data flow. The transmission media must be fully operational for their use.

When an engine starts, it may need some values from the outside world. This process is called resync. A resync is not always possible depending on the technology.

## Types

| Protocol | Implemented |
| --- | :-: |
| AMQP | ✖ |
| [Direct](direct.md) | ✔ |
| [MQTT](mqtt.md) | ✔ |
