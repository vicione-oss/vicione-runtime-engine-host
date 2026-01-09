using System;

namespace ViciOne.ManagedEngine;

internal sealed class Disposable : IDisposable
{
    private readonly Action _action;

    internal Disposable(Action action) => _action = action;

    public void Dispose() => _action();

    public static IDisposable Empty { get; } = new EmptyDisposable();

    private sealed class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
