using Xunit;
using SpeciesBE.Services;

namespace SpeciesBE.Tests;

public class SpeciesApiServiceTests
{
    [Fact]
    public async Task SearchSpecies_QueryVide_RetourneListeVide()
    {
        var http = new HttpClient();
        var service = new SpeciesApiService(http);

        var result = await service.SearchSpecies("");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSpeciesByParent_IdInvalide_NeCrashPas()
    {
        var http = new HttpClient();
        var service = new SpeciesApiService(http);

        var result = await service.GetSpeciesByParent(-1);

        Assert.NotNull(result);
    }
}