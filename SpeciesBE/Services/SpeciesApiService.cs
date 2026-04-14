using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SpeciesBE.Models;

namespace SpeciesBE.Services;

public class SpeciesApiService
{
    private readonly HttpClient _http;

    public SpeciesApiService(HttpClient http) => _http = http;

    public async Task<List<Species>> SearchSpecies(string query, int limit = 24)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<Species>();

        var url =
            $"https://api.inaturalist.org/v1/taxa?q={Uri.EscapeDataString(query)}&per_page={limit}&order_by=observations_count";

        var resp = await _http.GetFromJsonAsync<INatTaxaResponse>(url);
        return MapToSpecies(resp);
    }

    public async Task<List<TaxonOption>> GetTaxa(string? rank = null, int? parentId = null, int perPage = 200)
    {
        var url = $"https://api.inaturalist.org/v1/taxa?per_page={perPage}&order_by=observations_count";

        if (!string.IsNullOrWhiteSpace(rank))
            url += $"&rank={Uri.EscapeDataString(rank)}";

        if (parentId is not null)
            url += $"&taxon_id={parentId.Value}";


        var resp = await _http.GetFromJsonAsync<INatTaxaResponse>(url);

        return resp?.Results?.Select(t => new TaxonOption
        {
            Id = t.Id,
            ScientificName = t.Name,
            CommonName = t.PreferredCommonName,
            Rank = t.Rank,
            ParentId = t.ParentId,
            ObservationsCount = t.ObservationsCount,
            PhotoUrl = t.DefaultPhoto?.SquareUrl
        }).ToList() ?? new List<TaxonOption>();
    }

    public async Task<List<Species>> GetSpeciesByParent(int parentId, int limit = 24)
    {
        var url =
            $"https://api.inaturalist.org/v1/taxa?parent_id={parentId}&rank=species&per_page={limit}&order_by=observations_count";

        var resp = await _http.GetFromJsonAsync<INatTaxaResponse>(url);
        return MapToSpecies(resp);
    }

    private static List<Species> MapToSpecies(INatTaxaResponse? resp)
    {
        return resp?.Results?.Select(t => new Species
        {
            Id = t.Id,
            ScientificName = t.Name,
            CommonName = t.PreferredCommonName,
            Rank = t.Rank,
            PhotoUrl = t.DefaultPhoto?.MediumUrl
                       ?? t.DefaultPhoto?.SquareUrl
                       ?? t.DefaultPhoto?.OriginalUrl
        }).ToList() ?? new List<Species>();
    }

    private sealed class INatTaxaResponse
    {
        [JsonPropertyName("results")]
        public List<INatTaxon>? Results { get; set; }
    }

    private sealed class INatTaxon
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("rank")]
        public string? Rank { get; set; }

        [JsonPropertyName("preferred_common_name")]
        public string? PreferredCommonName { get; set; }

        [JsonPropertyName("parent_id")]
        public int? ParentId { get; set; }

        [JsonPropertyName("observations_count")]
        public int? ObservationsCount { get; set; }

        [JsonPropertyName("default_photo")]
        public INatPhoto? DefaultPhoto { get; set; }

        [JsonPropertyName("wikipedia_summary")]
        public string? WikipediaSummary { get; set; }

        [JsonPropertyName("wikipedia_url")]
        public string? WikipediaUrl { get; set; }

        [JsonPropertyName("ancestor_ids")]
        public List<int>? AncestorIds { get; set; }

    }

    private sealed class INatPhoto
    {
        [JsonPropertyName("square_url")]
        public string? SquareUrl { get; set; }

        [JsonPropertyName("medium_url")]
        public string? MediumUrl { get; set; }

        [JsonPropertyName("original_url")]
        public string? OriginalUrl { get; set; }
    }

    public async Task<SpeciesDetailsModel?> GetTaxonDetails(int taxonId, string locale = "fr")
    {
        var url = $"https://api.inaturalist.org/v1/taxa/{taxonId}?locale={Uri.EscapeDataString(locale)}";
        var resp = await _http.GetFromJsonAsync<INatTaxaResponse>(url);

        var t = resp?.Results?.FirstOrDefault();
        if (t is null) return null;

        return new SpeciesDetailsModel
        {
            Id = t.Id,
            ScientificName = t.Name,
            CommonName = t.PreferredCommonName,
            Rank = t.Rank,
            PhotoUrl = t.DefaultPhoto?.MediumUrl
                       ?? t.DefaultPhoto?.SquareUrl
                       ?? t.DefaultPhoto?.OriginalUrl,
            WikipediaSummary = t.WikipediaSummary,
            WikipediaUrl = t.WikipediaUrl,
            AncestorIds = t.AncestorIds ?? new List<int>()
        };
    }

    // récupère les noms des ancêtres en 1 call (taxon_id=1,2,3...)
    public async Task<List<(string Rank, string Name)>> GetTaxonomyLines(int taxonId, string locale = "fr")
    {
        var details = await GetTaxonDetails(taxonId, locale);
        if (details is null || details.AncestorIds.Count == 0)
            return new();

        var idsCsv = string.Join(",", details.AncestorIds);
        var url = $"https://api.inaturalist.org/v1/taxa?taxon_id={idsCsv}&locale={Uri.EscapeDataString(locale)}&per_page=200";
        var resp = await _http.GetFromJsonAsync<INatTaxaResponse>(url);

        // on garde seulement les rangs principaux
        var wanted = new HashSet<string> { "kingdom", "phylum", "class", "order", "family", "genus", "species" };

        return resp?.Results?
            .Where(a => a.Rank != null && wanted.Contains(a.Rank))
            .Select(a => (a.Rank!, a.PreferredCommonName ?? a.Name ?? $"#{a.Id}"))
            .ToList()
            ?? new();
    }

    public async Task<List<TaxonOption>> GetTaxonomy(int taxonId, string locale = "fr")
    {
        var details = await GetTaxonDetails(taxonId, locale);
        if (details is null || details.AncestorIds.Count == 0)
            return new List<TaxonOption>();

        // On récupère tous les ancêtres, puis on filtre sur les rangs majeurs
        var ids = string.Join(",", details.AncestorIds);
        var url = $"https://api.inaturalist.org/v1/taxa?taxon_id={ids}&per_page=200&locale={Uri.EscapeDataString(locale)}";

        var resp = await _http.GetFromJsonAsync<INatTaxaResponse>(url);

        var wanted = new HashSet<string> { "kingdom", "phylum", "class", "order", "family", "genus" };

        return resp?.Results?
            .Where(t => t.Rank != null && wanted.Contains(t.Rank))
            .Select(t => new TaxonOption
            {
                Id = t.Id,
                Rank = t.Rank,
                ScientificName = t.Name,
                CommonName = t.PreferredCommonName
            })
            .OrderBy(t => SortRank(t.Rank)) // affichage dans l'ordre taxonomique
            .ToList() ?? new List<TaxonOption>();
    }

    private static int SortRank(string? rank) => rank switch
    {
        "kingdom" => 1,
        "phylum" => 2,
        "class" => 3,
        "order" => 4,
        "family" => 5,
        "genus" => 6,
        _ => 99
    };

}
