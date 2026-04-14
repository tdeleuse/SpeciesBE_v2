namespace SpeciesBE.Models;

public class TaxonOption
{
    public int Id { get; set; }
    public string? ScientificName { get; set; }
    public string? CommonName { get; set; }
    public string? Rank { get; set; }

    public int? ParentId { get; set; }
    public int? ObservationsCount { get; set; }
    public string? PhotoUrl { get; set; } // mini optionnel
}
