using SpeciesBE.Models;
using SpeciesBE.Services;

namespace SpeciesBE.Tests;

/// <summary>
/// Tests pour les favoris avec gestion de plusieurs notes personnelles.
/// </summary>
public class FavoritesNotesTests
{
    private FavoriteService CreateService()
    {
        var js = new FakeJsRuntime();
        var auth = new AuthService(js);
        return new FavoriteService(js, auth);
    }

    private FavoriteSpecies MakeFav(int id, string name) => new FavoriteSpecies
    {
        Id = id,
        SpeciesKey = id * 100,
        ScientificName = name
    };

    private SpeciesNote MakeNote(int id, string text) => new SpeciesNote
    {
        Id = id,
        Text = text,
        DateAdded = DateTime.Now
    };

    // ===== AJOUT DE NOTES =====

    [Fact]
    public async Task NewFavorite_StartsWithNoNotes()
    {
        var svc = CreateService();
        await svc.AddFavorite(MakeFav(1, "Vulpes vulpes"));
        var result = await svc.GetFavorites();
        Assert.Empty(result[0].Notes);
    }

    [Fact]
    public async Task AddOneNote_SavedCorrectly()
    {
        var svc = CreateService();
        var fav = MakeFav(1, "Vulpes vulpes");
        await svc.AddFavorite(fav);

        fav.Notes.Add(MakeNote(1, "Vu dans la forêt de Soignes"));
        await svc.UpdateFavorite(fav);

        var result = await svc.GetFavorites();
        Assert.Single(result[0].Notes);
        Assert.Equal("Vu dans la forêt de Soignes", result[0].Notes[0].Text);
    }

    [Fact]
    public async Task AddMultipleNotes_AllSaved()
    {
        var svc = CreateService();
        var fav = MakeFav(1, "Vulpes vulpes");
        await svc.AddFavorite(fav);

        fav.Notes.Add(MakeNote(1, "Note 1"));
        fav.Notes.Add(MakeNote(2, "Note 2"));
        fav.Notes.Add(MakeNote(3, "Note 3"));
        await svc.UpdateFavorite(fav);

        var result = await svc.GetFavorites();
        Assert.Equal(3, result[0].Notes.Count);
    }

    [Fact]
    public async Task AddNote_PreservesText()
    {
        var svc = CreateService();
        var fav = MakeFav(1, "Felis catus");
        var texte = "Animal très curieux, aime les hauteurs 🐱";
        await svc.AddFavorite(fav);

        fav.Notes.Add(MakeNote(1, texte));
        await svc.UpdateFavorite(fav);

        var result = await svc.GetFavorites();
        Assert.Equal(texte, result[0].Notes[0].Text);
    }

    [Fact]
    public async Task AddNote_PreservesDate()
    {
        var svc = CreateService();
        var fav = MakeFav(1, "Felis catus");
        var date = new DateTime(2026, 4, 24, 10, 30, 0);
        await svc.AddFavorite(fav);

        fav.Notes.Add(new SpeciesNote { Id = 1, Text = "Note", DateAdded = date });
        await svc.UpdateFavorite(fav);

        var result = await svc.GetFavorites();
        Assert.Equal(date, result[0].Notes[0].DateAdded);
    }

    // ===== SUPPRESSION DE NOTES =====

    [Fact]
    public async Task RemoveNote_RemovesCorrectOne()
    {
        var svc = CreateService();
        var fav = MakeFav(1, "Vulpes vulpes");
        fav.Notes.Add(MakeNote(1, "À garder"));
        fav.Notes.Add(MakeNote(2, "À supprimer"));
        await svc.AddFavorite(fav);

        fav.Notes.RemoveAll(n => n.Id == 2);
        await svc.UpdateFavorite(fav);

        var result = await svc.GetFavorites();
        Assert.Single(result[0].Notes);
        Assert.Equal("À garder", result[0].Notes[0].Text);
    }

    [Fact]
    public async Task RemoveAllNotes_LeavesEmptyList()
    {
        var svc = CreateService();
        var fav = MakeFav(1, "Vulpes vulpes");
        fav.Notes.Add(MakeNote(1, "Note 1"));
        fav.Notes.Add(MakeNote(2, "Note 2"));
        await svc.AddFavorite(fav);

        fav.Notes.Clear();
        await svc.UpdateFavorite(fav);

        var result = await svc.GetFavorites();
        Assert.Empty(result[0].Notes);
    }

    [Fact]
    public async Task RemoveMiddleNote_OtherNotesIntact()
    {
        var svc = CreateService();
        var fav = MakeFav(1, "Vulpes vulpes");
        fav.Notes.Add(MakeNote(1, "Première"));
        fav.Notes.Add(MakeNote(2, "Deuxième — à supprimer"));
        fav.Notes.Add(MakeNote(3, "Troisième"));
        await svc.AddFavorite(fav);

        fav.Notes.RemoveAll(n => n.Id == 2);
        await svc.UpdateFavorite(fav);

        var result = await svc.GetFavorites();
        Assert.Equal(2, result[0].Notes.Count);
        Assert.Contains(result[0].Notes, n => n.Text == "Première");
        Assert.Contains(result[0].Notes, n => n.Text == "Troisième");
        Assert.DoesNotContain(result[0].Notes, n => n.Text == "Deuxième — à supprimer");
    }

    // ===== NOTES SUR PLUSIEURS FAVORIS =====

    [Fact]
    public async Task Notes_AreIndependentPerFavorite()
    {
        var svc = CreateService();
        var fav1 = MakeFav(1, "Vulpes vulpes");
        var fav2 = MakeFav(2, "Felis catus");

        fav1.Notes.Add(MakeNote(1, "Note du renard"));
        fav2.Notes.Add(MakeNote(1, "Note du chat"));

        await svc.AddFavorite(fav1);
        await svc.AddFavorite(fav2);

        await svc.UpdateFavorite(fav1);
        await svc.UpdateFavorite(fav2);

        var result = await svc.GetFavorites();
        var r1 = result.First(f => f.ScientificName == "Vulpes vulpes");
        var r2 = result.First(f => f.ScientificName == "Felis catus");

        Assert.Equal("Note du renard", r1.Notes[0].Text);
        Assert.Equal("Note du chat", r2.Notes[0].Text);
    }

    [Fact]
    public async Task UpdateNote_TextChanged_SavedCorrectly()
    {
        var svc = CreateService();
        var fav = MakeFav(1, "Vulpes vulpes");
        fav.Notes.Add(MakeNote(1, "Texte original"));
        await svc.AddFavorite(fav);
        await svc.UpdateFavorite(fav);

        // Modifie le texte
        var fetched = (await svc.GetFavorites())[0];
        fetched.Notes[0].Text = "Texte modifié";
        await svc.UpdateFavorite(fetched);

        var result = await svc.GetFavorites();
        Assert.Equal("Texte modifié", result[0].Notes[0].Text);
    }
}
