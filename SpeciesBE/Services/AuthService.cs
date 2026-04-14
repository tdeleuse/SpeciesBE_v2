using System.Text.Json;
using Microsoft.JSInterop;

namespace SpeciesBE.Services;

public class AuthService
{
    private readonly IJSRuntime _js;
    private const string StorageKey = "currentUser";

    public string? Username { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Username);

    public AuthService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync()
    {
        var json = await _js.InvokeAsync<string>("localStorage.getItem", StorageKey);
        if (!string.IsNullOrWhiteSpace(json))
            Username = JsonSerializer.Deserialize<string>(json);
    }

    public async Task LoginAsync(string username)
    {
        Username = username.Trim();
        var json = JsonSerializer.Serialize(Username);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    public async Task LogoutAsync()
    {
        Username = null;
        await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }
}
