using System.Collections.ObjectModel;
using System.Text.Json;

namespace Einkaufsliste
{
    public static class GroceryStorage
    {
        // Statische Hilfsklasse zum Verwalten der gespeicherten Einkaufsliste.
        // Bietet Laden, Speichern und Löschen der JSON-Datei im App-Datenverzeichnis.
        public static ObservableCollection<string> Items { get; private set; }
            = new ObservableCollection<string>();

        // Pfad zur JSON-Datei im AppData-Verzeichnis der App
        static string FilePath =>
            Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "groceries.json");

        // Speichert die aktuelle Items-Sammlung als JSON-Datei.
        public static async Task SaveAsync()
        {
            string json = JsonSerializer.Serialize(Items);
            await File.WriteAllTextAsync(FilePath, json);
        }

        // Lädt die JSON-Datei, falls sie existiert, und füllt die Items-Sammlung.
        public static async Task LoadAsync()
        {
            if (!File.Exists(FilePath))
                return;

            string json = await File.ReadAllTextAsync(FilePath);
            var list = JsonSerializer.Deserialize<List<string>>(json);

            Items.Clear();
            if (list is null)
                return;

            foreach (var item in list)
                Items.Add(item);
        }

        // Löscht die JSON-Datei (falls vorhanden) und leert die Items-Sammlung.
        public static void DeleteFile()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);

            Items.Clear();
        }
    }
}
