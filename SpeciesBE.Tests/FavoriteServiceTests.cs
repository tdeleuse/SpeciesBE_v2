using Xunit;
using SpeciesBE.Services;
using SpeciesBE.Models;

namespace SpeciesBE.Tests;

public class FavoriteServiceTests
{
    private async Task<FavoriteService> CreateService()
    {
        var js = new FakeJsRuntime();
        var auth = new AuthService(js);

        await auth.LoginAsync("test");

        return new FavoriteService(js, auth);
    }

    [Fact]
    public async Task AddFavorite_Ajoute_Un_Element()
    {
        var service = await CreateService();

        var fav = new FavoriteSpecies
        {
            Id = 1,
            SpeciesKey = 100,
            ScientificName = "Test species"
        };

        await service.AddFavorite(fav);
        var list = await service.GetFavorites();

        Assert.Single(list);
    }

    [Fact]
    public async Task AddFavorite_Doublon_Non_Ajoute()
    {
        var service = await CreateService();

        var fav = new FavoriteSpecies
        {
            Id = 1,
            SpeciesKey = 100
        };

        await service.AddFavorite(fav);
        await service.AddFavorite(fav);

        var list = await service.GetFavorites();

        Assert.Single(list);
    }

    [Fact]
    public async Task RemoveFavorite_Supprime_Element()
    {
        var service = await CreateService();

        var fav = new FavoriteSpecies
        {
            Id = 1,
            SpeciesKey = 100
        };

        await service.AddFavorite(fav);
        await service.RemoveFavorite(1);

        var list = await service.GetFavorites();

        Assert.Empty(list);
    }
}