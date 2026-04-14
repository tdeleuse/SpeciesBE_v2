namespace SpeciesBE.Models;

public class SpeciesDetailsModel : Species
{
    public string? WikipediaSummary { get; set; }
    public string? WikipediaUrl { get; set; }
    public List<int> AncestorIds { get; set; } = new();
}
