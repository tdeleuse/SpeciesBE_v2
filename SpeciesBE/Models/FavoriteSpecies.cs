namespace SpeciesBE.Models;

public class FavoriteSpecies
{
    public int Id { get; set; }                 // id local
    public int SpeciesKey { get; set; }         // taxon id iNaturalist
    public string? ScientificName { get; set; }
    public string? Notes { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.Now;
}
