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
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
                Username = JsonSerializer.Deserialize<string>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AuthService init error: {ex.Message}");
        }
    }

    public async Task LoginAsync(string username)
    {
        try
        {
            Username = username.Trim();
            var json = JsonSerializer.Serialize(Username);
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoginAsync error: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            Username = null;
            await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LogoutAsync error: {ex.Message}");
        }
    }
}
