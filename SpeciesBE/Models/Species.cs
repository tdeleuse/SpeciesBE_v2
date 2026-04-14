namespace SpeciesBE.Models;

public class Species
{
    public int Id { get; set; }                 // iNaturalist taxon id
    public string? ScientificName { get; set; }
    public string? CommonName { get; set; }
    public string? Rank { get; set; }
    public string? PhotoUrl { get; set; }
}
