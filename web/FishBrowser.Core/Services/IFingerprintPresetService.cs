using System.Collections.Generic;
using FishBrowser.WPF.Models;

namespace FishBrowser.WPF.Services;

public interface IFingerprintPresetService
{
    List<FingerprintProfile> GetAllPresets();
    FingerprintProfile? GetPresetByName(string name);
    void ImportAllPresetsToDatabase(IDatabaseService dbService);
    FingerprintProfile? RestorePreset(string presetName);
}
