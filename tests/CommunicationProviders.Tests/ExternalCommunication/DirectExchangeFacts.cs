using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using ViciOne.ManagedEngine.Communication;
using Xunit;

namespace ViciOne.ManagedEngine.ExternalCommunication;

public class DirectExchange_
{
    private readonly JsonSerializerOptions _serializerOptions = JsonSetup.CreatePreserveTypeOptions();

    [Fact]
    public async Task Can_send_link_values_direct_Async()
    {
        using DirectExchange exchange = new();
        List<ExternalValue> receivedValues = [];
        ExternalValue value1 = new() { Channel = "A", };
        ExternalValue value2 = new() { Channel = "B", };
        List<ExternalValue> values = [value1, value2,];
        SubscriberInfo subscriber = new(1, HandleAbstractly, HandleDirectly);
        await exchange.SubscribeAsync(["A", "B",], subscriber);

        await exchange.ForwardValuesAsync(values, 1, _serializerOptions);

        receivedValues.Should().HaveCount(2).And.Contain(value1).And.Contain(value2);

        void HandleDirectly(ExternalValue v)
            => receivedValues.Add(v);

        void HandleAbstractly(string _)
            => throw new InvalidOperationException("Wrong method");
    }

    [Fact]
    public async Task Can_send_link_values_abstract_Async()
    {
        using DirectExchange exchange = new();
        List<string> receivedValues = [];
        List<ExternalValue> values =
        [
            new() { Channel = "A", },
            new() { Channel = "B", },
        ];
        SubscriberInfo subscriber = new(1, HandleAbstractly, HandleDirectly);
        await exchange.SubscribeAsync(["A", "B",], subscriber);

        await exchange.ForwardValuesAsync(values, 21, _serializerOptions);

        receivedValues.Should().HaveCount(2);

        void HandleDirectly(ExternalValue _)
            => throw new InvalidOperationException("Wrong method");

        void HandleAbstractly(string v)
            => receivedValues.Add(v);
    }

    [Fact]
    public async Task Can_handle_multiple_calls_Async()
    {
        using DirectExchange exchange = new();
        List<object> receivedValues = [];
        SubscriberInfo subscriber = new(1, receivedValues.Add, receivedValues.Add);

        await exchange.SubscribeAsync(["A",], subscriber);
        await exchange.SubscribeAsync(["A",], subscriber);

        await exchange.ForwardValuesAsync([new ExternalValue() { Channel = "A", }], 1, _serializerOptions);

        receivedValues.Should().ContainSingle();
    }

    [Fact]
    public async Task Can_serialize_null_value_Async()
    {
        using DirectExchange exchange = new();
        ExternalValue? receivedValue = null;
        ExternalValue values = new()
        {
            Channel = "A",
            Value = null,
        };
        SubscriberInfo subscriber = new(1, HandleAbstractly, HandleDirectly);
        await exchange.SubscribeAsync(["A",], subscriber);

        await exchange.ForwardValuesAsync([values,], 23, _serializerOptions);

        receivedValue.Should().NotBeNull();
        receivedValue!.Value.Should().BeNull();

        void HandleDirectly(ExternalValue _)
            => throw new InvalidOperationException("Wrong method");

        void HandleAbstractly(string v)
            => receivedValue = JsonSerializer.Deserialize<ExternalValue>(v, _serializerOptions);
    }

    [Fact]
    public async Task Can_serialize_complex_value_Async()
    {
        using DirectExchange exchange = new();
        ExternalValue? receivedValue = null;
        ExternalValue value = new()
        {
            Channel = "A",
            Value = new CancelEventArgs(true),
        };
        SubscriberInfo subscriber = new(1, HandleAbstractly, HandleDirectly);
        await exchange.SubscribeAsync(["A",], subscriber);

        await exchange.ForwardValuesAsync([value,], 23, _serializerOptions);

        receivedValue.Should().NotBeNull();
        receivedValue!.Value.Should().BeOfType<CancelEventArgs>().Which.Cancel.Should().BeTrue();

        void HandleDirectly(ExternalValue _)
            => throw new InvalidOperationException("Wrong method");

        void HandleAbstractly(string v)
            => receivedValue = JsonSerializer.Deserialize<ExternalValue>(v, _serializerOptions);
    }

    [Fact]
    public async Task Can_handle_processing_failure_Async()
    {
        using DirectExchange exchange = new();
        SubscriberInfo subscriber = new(1, _ => throw new InvalidOperationException("error"), _ => { });

        await exchange.SubscribeAsync(["A",], subscriber);
        Exception? exception = null;

        await exchange.ForwardValuesAsync([new() { Channel = "A", },], 23, _serializerOptions, ex => exception = ex);

        exception.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("error");
    }

    [Fact]
    public async Task Can_handle_unknown_targetAsync()
    {
        using DirectExchange exchange = new();

        await exchange.ForwardValuesAsync([new() { Channel = "A", },], 23, _serializerOptions, _ => { });
    }
}
