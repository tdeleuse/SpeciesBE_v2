namespace SpeciesBE.Models;

public class FavoriteSpecies
{
    public int Id { get; set; }
    public int SpeciesKey { get; set; }
    public string? ScientificName { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.Now;

    // Remplacement de l'ancienne propriété Notes par une liste
    public List<SpeciesNote> Notes { get; set; } = new();
}

public class SpeciesNote
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public DateTime DateAdded { get; set; } = DateTime.Now;
}
