using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using FishBrowser.WPF.Data;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.ViewModels;

public class TraitCatalogViewModel : INotifyPropertyChanged
{
    private readonly WebScraperDbContext _db;

    public ObservableCollection<TraitCategory> Categories { get; } = new();
    public ObservableCollection<TraitDefinition> Traits { get; } = new();

    private TraitCategory? _selectedCategory;
    public TraitCategory? SelectedCategory
    {
        get => _selectedCategory;
        set { _selectedCategory = value; OnPropertyChanged(); ReloadTraits(); }
    }

    private string _search = string.Empty;
    public string Search
    {
        get => _search;
        set { _search = value; OnPropertyChanged(); ReloadTraits(); }
    }

    private bool _showRequiredOnly;
    public bool ShowRequiredOnly
    {
        get => _showRequiredOnly;
        set { _showRequiredOnly = value; OnPropertyChanged(); ReloadTraits(); }
    }

    private bool _showExperimentalOnly;
    public bool ShowExperimentalOnly
    {
        get => _showExperimentalOnly;
        set { _showExperimentalOnly = value; OnPropertyChanged(); ReloadTraits(); }
    }

    private bool _showRandomizableOnly;
    public bool ShowRandomizableOnly
    {
        get => _showRandomizableOnly;
        set { _showRandomizableOnly = value; OnPropertyChanged(); ReloadTraits(); }
    }

    public TraitCatalogViewModel(WebScraperDbContext db)
    {
        _db = db;
        ReloadCategories();
    }

    public void ReloadCategories()
    {
        Categories.Clear();
        foreach (var c in _db.TraitCategories.OrderBy(c => c.Order).ToList())
            Categories.Add(c);
        if (Categories.Count > 0 && SelectedCategory == null)
            SelectedCategory = Categories.First();
    }

    private void ReloadTraits()
    {
        Traits.Clear();
        var query = _db.TraitDefinitions.AsQueryable();
        if (SelectedCategory != null)
            query = query.Where(t => t.CategoryId == SelectedCategory.Id);
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();
            query = query.Where(t => t.Key.Contains(s) || t.DisplayName.Contains(s));
        }
        if (ShowExperimentalOnly)
            query = query.Where(t => t.IsExperimental);
        if (ShowRandomizableOnly)
            query = query.Where(t => _db.TraitOptions.Any(o => o.TraitDefinitionId == t.Id));
        if (ShowRequiredOnly)
            query = query.Where(t => t.DefaultValueJson != null || (t.ConstraintsJson != null && t.ConstraintsJson.Contains("required")));
        foreach (var t in query.OrderBy(t => t.Key).ToList())
            Traits.Add(t);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
