using Microsoft.JSInterop;

namespace SpeciesBE.Tests;

public class FakeJsRuntime : IJSRuntime
{
    private readonly Dictionary<string, string> _storage = new();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (identifier == "localStorage.getItem")
        {
            var key = args?[0]?.ToString() ?? "";
            _storage.TryGetValue(key, out var value);
            return new ValueTask<TValue>((TValue)(object?)value!);
        }

        if (identifier == "localStorage.setItem")
        {
            var key = args?[0]?.ToString() ?? "";
            var value = args?[1]?.ToString() ?? "";
            _storage[key] = value;
            return new ValueTask<TValue>((TValue)(object?)null!);
        }

        if (identifier == "localStorage.removeItem")
        {
            var key = args?[0]?.ToString() ?? "";
            _storage.Remove(key);
            return new ValueTask<TValue>((TValue)(object?)null!);
        }

        return new ValueTask<TValue>((TValue)(object?)null!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => InvokeAsync<TValue>(identifier, args);
}