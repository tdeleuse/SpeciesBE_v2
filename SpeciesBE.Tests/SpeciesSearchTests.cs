using System.Net;
using System.Text.Json;
using SpeciesBE.Services;

namespace SpeciesBE.Tests;

/// <summary>
/// Tests pour la recherche d'espèces par nom et par groupe iconique.
/// </summary>
public class SpeciesSearchTests
{
    private static HttpClient MakeHttpClient(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var handler = new FakeSearchHttpHandler(json);
        return new HttpClient(handler);
    }

    private static object MakePayload(params (int id, string name, string common, string iconic)[] species)
    {
        return new
        {
            results = species.Select(s => new
            {
                id = s.id,
                name = s.name,
                rank = "species",
                preferred_common_name = s.common,
                ancestor_ids = new[] { 1, 2 },
                iconic_taxon_name = s.iconic,
                default_photo = (object?)null
            }).ToArray()
        };
    }

    // ===== RECHERCHE PAR NOM =====

    [Fact]
    public async Task SearchByName_ReturnsMatchingSpecies()
    {
        var payload = MakePayload(
            (1, "Vulpes vulpes", "Renard roux", "Mammalia"),
            (2, "Felis catus", "Chat domestique", "Mammalia")
        );
        var svc = new SpeciesApiService(MakeHttpClient(payload));
        var result = await svc.SearchSpecies("renard", limit: 10);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SearchByName_ExactMatch_ComesFirst()
    {
        var payload = MakePayload(
            (1, "Vulpes lagopus", "Renard arctique", "Mammalia"),
            (2, "Vulpes vulpes", "Renard roux", "Mammalia")
        );
        var svc = new SpeciesApiService(MakeHttpClient(payload));
        var result = await svc.SearchSpecies("renard roux", limit: 10);
        // Le renard roux correspond exactement → doit être en premier
        Assert.Equal("Renard roux", result[0].CommonName);
    }

    [Fact]
    public async Task SearchByName_EmptyQuery_ReturnsNothing()
    {
        var svc = new SpeciesApiService(MakeHttpClient(MakePayload()));
        var result = await svc.SearchSpecies("", limit: 10);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchByName_WhitespaceOnly_ReturnsNothing()
    {
        var svc = new SpeciesApiService(MakeHttpClient(MakePayload()));
        var result = await svc.SearchSpecies("   ", limit: 10);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchByName_NoResults_ReturnsEmpty()
    {
        var svc = new SpeciesApiService(MakeHttpClient(MakePayload()));
        var result = await svc.SearchSpecies("xyzinconnu", limit: 10);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchByName_RespectsLimit()
    {
        var payload = MakePayload(
            (1, "Species 1", "Espèce 1", "Mammalia"),
            (2, "Species 2", "Espèce 2", "Mammalia"),
            (3, "Species 3", "Espèce 3", "Mammalia"),
            (4, "Species 4", "Espèce 4", "Mammalia"),
            (5, "Species 5", "Espèce 5", "Mammalia")
        );
        var svc = new SpeciesApiService(MakeHttpClient(payload));
        var result = await svc.SearchSpecies("espèce", limit: 3);
        Assert.True(result.Count <= 3);
    }

    // ===== RECHERCHE PAR GROUPE ICONIQUE =====

    [Fact]
    public async Task SearchByGroup_Mammalia_ReturnsOnlyMammals()
    {
        var payload = MakePayload(
            (1, "Vulpes vulpes", "Renard roux", "Mammalia"),
            (2, "Quercus robur", "Chêne pédonculé", "Plantae"),
            (3, "Parus major", "Mésange charbonnière", "Aves")
        );
        var svc = new SpeciesApiService(MakeHttpClient(payload));
        var result = await svc.SearchSpecies("test", limit: 10, iconicTaxa: "Mammalia");
        Assert.All(result, s => Assert.Equal("Mammalia", s.IconicTaxonName));
    }

    [Fact]
    public async Task SearchByGroup_Aves_ReturnsOnlyBirds()
    {
        var payload = MakePayload(
            (1, "Vulpes vulpes", "Renard roux", "Mammalia"),
            (2, "Parus major", "Mésange", "Aves"),
            (3, "Turdus merula", "Merle noir", "Aves")
        );
        var svc = new SpeciesApiService(MakeHttpClient(payload));
        var result = await svc.SearchSpecies("test", limit: 10, iconicTaxa: "Aves");
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal("Aves", s.IconicTaxonName));
    }

    [Fact]
    public async Task SearchByGroup_Plantae_ReturnsOnlyPlants()
    {
        var payload = MakePayload(
            (1, "Quercus robur", "Chêne", "Plantae"),
            (2, "Rosa canina", "Rosier des chiens", "Plantae"),
            (3, "Felis catus", "Chat", "Mammalia")
        );
        var svc = new SpeciesApiService(MakeHttpClient(payload));
        var result = await svc.SearchSpecies("test", limit: 10, iconicTaxa: "Plantae");
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal("Plantae", s.IconicTaxonName));
    }

    [Fact]
    public async Task SearchByGroup_NoMatch_ReturnsEmpty()
    {
        var payload = MakePayload(
            (1, "Vulpes vulpes", "Renard roux", "Mammalia"),
            (2, "Felis catus", "Chat", "Mammalia")
        );
        var svc = new SpeciesApiService(MakeHttpClient(payload));
        // On cherche des champignons mais il n'y en a pas
        var result = await svc.SearchSpecies("test", limit: 10, iconicTaxa: "Fungi");
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchByGroup_EmptyGroup_ReturnsAll()
    {
        var payload = MakePayload(
            (1, "Vulpes vulpes", "Renard roux", "Mammalia"),
            (2, "Quercus robur", "Chêne", "Plantae"),
            (3, "Parus major", "Mésange", "Aves")
        );
        var svc = new SpeciesApiService(MakeHttpClient(payload));
        // Groupe vide = pas de filtre
        var result = await svc.SearchSpecies("test", limit: 10, iconicTaxa: "");
        Assert.Equal(3, result.Count);
    }
}

public class FakeSearchHttpHandler : HttpMessageHandler
{
    private readonly string _json;
    public FakeSearchHttpHandler(string json) => _json = json;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
