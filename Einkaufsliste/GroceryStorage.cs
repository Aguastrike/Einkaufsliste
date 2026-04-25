using System.Collections.ObjectModel;
using System.Text.Json;

namespace Einkaufsliste
{
    public static class GroceryStorage
    {
        public static ObservableCollection<string> Items { get; private set; }
            = new ObservableCollection<string>();

        static string FilePath =>
            Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "groceries.json");

        public static async Task SaveAsync()
        {
            string json = JsonSerializer.Serialize(Items);
            await File.WriteAllTextAsync(FilePath, json);
        }

        public static async Task LoadAsync()
        {
            if (!File.Exists(FilePath))
                return;

            string json = await File.ReadAllTextAsync(FilePath);
            var list = JsonSerializer.Deserialize<List<string>>(json);

            Items.Clear();
            foreach (var item in list)
                Items.Add(item);
        }

        public static void DeleteFile()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);

            Items.Clear();
        }
    }
}
