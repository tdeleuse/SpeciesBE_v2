using System.Text.Json;
using Microsoft.JSInterop;
using SpeciesBE.Models;

namespace SpeciesBE.Services;

public class FavoriteService
{
    private readonly IJSRuntime _js;
    private readonly AuthService _auth;

    public FavoriteService(IJSRuntime js, AuthService auth)
    {
        _js = js;
        _auth = auth;
    }

    private string StorageKey => $"favorites_{_auth.Username ?? "guest"}";

    public async Task<List<FavoriteSpecies>> GetFavorites()
    {
        var json = await _js.InvokeAsync<string>("localStorage.getItem", StorageKey);

        if (string.IsNullOrEmpty(json))
            return new List<FavoriteSpecies>();

        return JsonSerializer.Deserialize<List<FavoriteSpecies>>(json) ?? new List<FavoriteSpecies>();
    }

    public async Task AddFavorite(FavoriteSpecies species)
    {
        var favorites = await GetFavorites();

        if (favorites.Any(f => f.SpeciesKey == species.SpeciesKey))
            return;

        favorites.Add(species);
        await SaveAll(favorites);
    }

    public async Task RemoveFavorite(int id)
    {
        var favorites = await GetFavorites();
        favorites.RemoveAll(f => f.Id == id);
        await SaveAll(favorites);
    }

    public async Task UpdateFavorite(FavoriteSpecies updated)
    {
        var favorites = await GetFavorites();
        var index = favorites.FindIndex(f => f.Id == updated.Id);

        if (index >= 0)
            favorites[index] = updated;

        await SaveAll(favorites);
    }

    private async Task SaveAll(List<FavoriteSpecies> favorites)
    {
        var json = JsonSerializer.Serialize(favorites);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }
}
