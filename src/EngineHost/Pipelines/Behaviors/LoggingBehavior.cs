using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.Pipelines.Behaviors;

internal class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters =
        {
            new JsonStringEnumConverter(),
            new TypeJsonConverter(),
        },
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    public async Task<TResponse> Handle(TRequest request, Func<CancellationToken, Task<TResponse>> next, CancellationToken cancellationToken)
    {
        logger.RequestHandling(typeof(TRequest).Name, JsonSerializer.Serialize<object>(request, _options));
        var stopwatch = Stopwatch.StartNew();
        TResponse response;

        try
        {
            response = await next(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.RequestFailed(ex, typeof(TRequest).Name, stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }

        logger.RequestHandled(typeof(TRequest).Name, stopwatch.Elapsed.TotalMilliseconds,
            response?.GetType().Name ?? "<unknown type>", JsonSerializer.Serialize(response ?? new object(), _options));
        return response;
    }
}
