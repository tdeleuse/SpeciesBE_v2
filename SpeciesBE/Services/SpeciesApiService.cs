using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SpeciesBE.Models;

namespace SpeciesBE.Services;

public class SpeciesApiService
{
    private readonly HttpClient _http;

    private static readonly Dictionary<string, string> IconicTaxaMap = new()
    {
        { "Mammalia",       "Mammalia" },
        { "Aves",           "Aves" },
        { "Reptilia",       "Reptilia" },
        { "Amphibia",       "Amphibia" },
        { "Actinopterygii", "Actinopterygii" },
        { "Insecta",        "Insecta" },
        { "Plantae",        "Plantae" },
        { "Fungi",          "Fungi" },
    };

    public SpeciesApiService(HttpClient http) => _http = http;

    public async Task<List<Species>> SearchSpecies(string query, int limit = 24, string iconicTaxa = "")
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<Species>();

        var fetchLimit = string.IsNullOrWhiteSpace(iconicTaxa) ? limit : limit * 3;
        var url = $"https://api.inaturalist.org/v1/taxa?q={Uri.EscapeDataString(query)}&per_page={fetchLimit}&order_by=observations_count&locale=fr";

        var resp = await _http.GetFromJsonAsync<INatTaxaResponse>(url);
        var species = MapToSpecies(resp);

        if (!string.IsNullOrWhiteSpace(iconicTaxa) && IconicTaxaMap.TryGetValue(iconicTaxa, out var taxaName))
        {
            species = species
                .Where(s => s.IconicTaxonName != null &&
                            s.IconicTaxonName.Equals(taxaName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var q = query.Trim().ToLowerInvariant();
        return species
            .OrderByDescending(s =>
                (s.CommonName?.ToLowerInvariant() == q || s.ScientificName?.ToLowerInvariant() == q) ? 2 :
                (s.CommonName?.ToLowerInvariant().Contains(q) == true || s.ScientificName?.ToLowerInvariant().Contains(q) == true) ? 1 : 0)
            .Take(limit)
            .ToList();
    }

    public async Task<List<TaxonOption>> GetTaxa(string? rank = null, int? parentId = null, int perPage = 200)
    {
        var url = $"https://api.inaturalist.org/v1/taxa?per_page={perPage}&order_by=observations_count&locale=fr";

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
            PhotoUrl = t.DefaultPhoto?.SquareUrl,
            AncestorIds = t.AncestorIds ?? new List<int>()
        })
        .OrderBy(t => (t.CommonName ?? t.ScientificName ?? "").ToLowerInvariant())
        .ToList() ?? new List<TaxonOption>();
    }

    public async Task<List<Species>> GetSpeciesUnderTaxon(int taxonId, int limit = 24)
    {
        var url = $"https://api.inaturalist.org/v1/taxa?taxon_id={taxonId}&rank=species&per_page={limit}&order_by=observations_count&locale=fr";
        var resp = await _http.GetFromJsonAsync<INatTaxaResponse>(url);
        return MapToSpecies(resp);
    }

    public async Task<List<Species>> GetSpeciesByParent(int parentId, int limit = 24)
    {
        var url = $"https://api.inaturalist.org/v1/taxa?parent_id={parentId}&rank=species&per_page={limit}&order_by=observations_count&locale=fr";
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
                       ?? t.DefaultPhoto?.OriginalUrl,
            IconicTaxonName = t.IconicTaxonName
        }).ToList() ?? new List<Species>();
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

    // Récupère la lignée directe de l'espèce en appelant chaque ancêtre individuellement
    public async Task<List<TaxonOption>> GetTaxonomy(int taxonId, string locale = "fr")
    {
        // 1. Récupère les détails pour avoir les AncestorIds dans l'ordre
        var details = await GetTaxonDetails(taxonId, locale);
        if (details is null || details.AncestorIds.Count == 0)
            return new List<TaxonOption>();

        var wanted = new HashSet<string> { "kingdom", "phylum", "class", "order", "family", "genus" };

        // 2. Appelle l'API avec les IDs des ancêtres — endpoint /taxa/{id} pour chacun des rangs voulus
        //    On passe tous les IDs d'un coup via le paramètre id[] de l'endpoint
        var ancestorIds = details.AncestorIds;

        // Appel en lots de 30 IDs max pour éviter les URLs trop longues
        var results = new List<TaxonOption>();
        var batches = ancestorIds
            .Select((id, i) => new { id, i })
            .GroupBy(x => x.i / 30)
            .Select(g => g.Select(x => x.id).ToList());

        foreach (var batch in batches)
        {
            var ids = string.Join(",", batch);
            var url = $"https://api.inaturalist.org/v1/taxa/{ids}?locale={Uri.EscapeDataString(locale)}";
            var resp = await _http.GetFromJsonAsync<INatTaxaResponse>(url);

            if (resp?.Results is null) continue;

            var filtered = resp.Results
                .Where(t => t.Rank != null && wanted.Contains(t.Rank))
                .Select(t => new TaxonOption
                {
                    Id = t.Id,
                    Rank = t.Rank,
                    ScientificName = t.Name,
                    CommonName = t.PreferredCommonName
                });

            results.AddRange(filtered);
        }

        // Trie dans l'ordre taxonomique correct
        return results
            .OrderBy(t => SortRank(t.Rank))
            .ToList();
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

        [JsonPropertyName("iconic_taxon_name")]
        public string? IconicTaxonName { get; set; }
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
