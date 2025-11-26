using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    /// <summary>
    /// Model voor een enkel item in de lijst van fabrikanten.
    /// </summary>
    public partial class ManufacturerItem : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private bool isEmpty; // Is true als de map leeg is (mag verwijderd worden)

        [ObservableProperty]
        private bool isEditing = false; // Is true als de gebruiker de naam bewerkt

        // OPGELOST: Moet 'public' zijn om overal toegankelijk te zijn.
        public string OriginalName { get; private set; }

        /// <summary>
        /// Retourneert True als dit de gereserveerde 'Unknown' fabrikant is.
        /// Dit wordt gebruikt om bewerken en verwijderen in de UI te blokkeren.
        /// </summary>
        public bool IsSystemItem => Name.Equals("Unknown", StringComparison.OrdinalIgnoreCase);

        public ManufacturerItem(string name, bool isEmpty)
        {
            Name = name;
            OriginalName = name;
            IsEmpty = isEmpty;
        }

        /// <summary>
        /// Update de OriginalName na een succesvolle hernoeming.
        /// </summary>
        public void UpdateOriginalName()
        {
            OriginalName = Name;
        }
    }

    /// <summary>
    /// De ViewModel voor het Manufacturer Management scherm.
    /// </summary>
    public partial class ManufacturerViewModel : ObservableObject
    {
        public event EventHandler? ManufacturersUpdated;

        private readonly ManufacturerService _manufacturerService;

        private readonly string _rootDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InterdisciplinairProject",
            "Fixtures"
        );

        private readonly string _centralJsonPath; // Pad naar manufacturers.json

        [ObservableProperty]
        private ObservableCollection<ManufacturerItem> manufacturers = new();

        public ICommand DeleteManufacturerCommand { get; }
        public ICommand StartEditCommand { get; }
        public ICommand SaveEditCommand { get; }
        public ICommand CancelEditCommand { get; }

        public ManufacturerViewModel()
        {
            _manufacturerService = new ManufacturerService();
            LoadManufacturers();

            _centralJsonPath = Path.Combine(_rootDirectory, "manufacturers.json");

            // Zorg ervoor dat de root directory bestaat
            if (!Directory.Exists(_rootDirectory))
            {
                Directory.CreateDirectory(_rootDirectory);
            }

            LoadManufacturers();

            // Initialiseer Commands
            DeleteManufacturerCommand = new RelayCommand<ManufacturerItem>(DeleteManufacturer, CanDeleteManufacturer);
            StartEditCommand = new RelayCommand<ManufacturerItem>(StartEdit);
            SaveEditCommand = new RelayCommand<ManufacturerItem>(SaveEdit, CanSaveEdit);
            CancelEditCommand = new RelayCommand<ManufacturerItem>(CancelEdit);
        }

        /// <summary>
        /// Laadt alle bestaande fabrikanten uit de file system directories.
        /// </summary>
        public void LoadManufacturers()
        {
            Manufacturers.Clear();
            foreach (var name in _manufacturerService.GetManufacturers())
            {
                bool isEmpty = IsManufacturerEmpty(name);
                Manufacturers.Add(new ManufacturerItem(name, isEmpty));
            }
        }

        /// <summary>
        /// Controleert of een fabrikant directory leeg is (dwz geen fixtures bevat).
        /// </summary>
        private bool IsManufacturerEmpty(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return true;
            string manufacturerPath = Path.Combine(_rootDirectory, name);
            if (!Directory.Exists(manufacturerPath)) return true;

            try
            {
                // Controleert op bestanden in de directory
                return !Directory.EnumerateFiles(manufacturerPath).Any();
            }
            catch (Exception) { return false; }
        }

        /// <summary>
        /// Voert de daadwerkelijke verwijdering van de directory uit.
        /// </summary>
        private bool ExecuteDeleteManufacturer(string name)
        {
            string manufacturerPath = Path.Combine(_rootDirectory, name);
            if (!Directory.Exists(manufacturerPath)) return true;

            // Verwijderen kan alleen als de map leeg is
            if (!IsManufacturerEmpty(name)) return false;

            try
            {
                Directory.Delete(manufacturerPath);

                // Ook verwijderen uit manufacturers.json
                UpdateManufacturerNameInCentralJson(name, string.Empty);

                return true;
            }
            catch (Exception ex)
            {
                // VERTALING: Verwijderen mislukt
                MessageBox.Show($"Deletion failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Voert de daadwerkelijke hernoeming van de directory uit.
        /// </summary>
        private bool ExecuteRenameManufacturer(string oldName, string newName)
        {
            string oldPath = Path.Combine(_rootDirectory, oldName);
            string newPath = Path.Combine(_rootDirectory, newName);

            // Controleer of de oude map bestaat en de nieuwe map nog niet
            if (!Directory.Exists(oldPath) || Directory.Exists(newPath)) return false;

            try
            {
                Directory.Move(oldPath, newPath);

                // 1. Update de naam in alle JSON-bestanden in de map
                foreach (var filePath in Directory.EnumerateFiles(newPath, "*.json"))
                {
                    UpdateManufacturerNameInFixtureJson(filePath, newName);
                }

                // 2. Update de naam in de centrale manufacturers.json
                UpdateManufacturerNameInCentralJson(oldName, newName);

                return true;
            }
            catch (Exception ex)
            {
                // VERTALING: Hernoemen mislukt
                MessageBox.Show($"Renaming failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Update de 'manufacturer' property in een JSON-bestand.
        /// </summary>
        private void UpdateManufacturerNameInFixtureJson(string filePath, string newName)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                var root = JsonNode.Parse(jsonString)?.AsObject();
                if (root != null)
                {
                    root["manufacturer"] = newName;
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(filePath, root.ToJsonString(options));
                }
            }
            catch (Exception ex)
            {
                // VERTALING: Bestandsupdate mislukt voor {Path.GetFileName(filePath)}
                MessageBox.Show($"File update failed for {Path.GetFileName(filePath)}: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Update de naam in de centrale manufacturers.json.
        /// Als newName leeg is, wordt de oldName verwijderd.
        /// </summary>
        private void UpdateManufacturerNameInCentralJson(string oldName, string newName)
        {
            if (!File.Exists(_centralJsonPath)) return;

            try
            {
                string jsonString = File.ReadAllText(_centralJsonPath);
                var manufacturerArray = JsonNode.Parse(jsonString)?.AsArray();

                if (manufacturerArray == null) return;

                bool changed = false;

                for (int i = 0; i < manufacturerArray.Count; i++)
                {
                    if (manufacturerArray[i]?.GetValue<string>()?.Equals(oldName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if (string.IsNullOrWhiteSpace(newName))
                        {
                            // Verwijderen
                            manufacturerArray.RemoveAt(i);
                            changed = true;
                            break;
                        }
                        else
                        {
                            // Hernoemen
                            manufacturerArray[i] = newName;
                            changed = true;
                            break;
                        }
                    }
                }

                if (changed)
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(_centralJsonPath, manufacturerArray.ToJsonString(options));
                }
            }
            catch (Exception ex)
            {
                // VERTALING: Update van manufacturers.json mislukt
                MessageBox.Show($"Update of manufacturers.json failed: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool CanDeleteManufacturer(ManufacturerItem? item)
        {
            // CHECK: Blokkeer verwijdering van 'Unknown'
            if (item == null || item.Name.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return item.IsEmpty;
        }

        private void DeleteManufacturer(ManufacturerItem? item)
        {
            if (item == null) return;

            // CHECK: Geef een waarschuwing als de gebruiker 'Unknown' probeert te verwijderen
            if (item.Name.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Manufacturer '{item.Name}' is a system default and cannot be deleted.", "Restriction", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!item.IsEmpty)
            {
                // VERTALING: Fabrikant '{item?.Name}' kan niet worden verwijderd. Zorg ervoor dat de map leeg is.
                MessageBox.Show($"Manufacturer '{item?.Name}' cannot be deleted. Ensure the folder is empty.", "Deletion Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(
                // VERTALING: Weet u zeker dat u '{item.Name}' wilt verwijderen? Dit kan niet ongedaan worden gemaakt.
                $"Are you sure you want to delete '{item.Name}'? This cannot be undone.",
                "Confirm Deletion", // VERTALING: Verwijdering Bevestigen
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                if (ExecuteDeleteManufacturer(item.Name))
                {
                    Manufacturers.Remove(item);
                    ManufacturersUpdated?.Invoke(this, EventArgs.Empty);
                }
                // Foutmelding wordt afgehandeld in ExecuteDeleteManufacturer
            }
        }

        private void StartEdit(ManufacturerItem? item)
        {
            if (item == null) return;

            // CHECK: Blokkeer bewerken van 'Unknown'
            if (item.Name.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Manufacturer '{item.Name}' is a system default and cannot be edited.", "Restriction", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Stop met bewerken van andere items
            foreach (var existingItem in Manufacturers.Where(i => i.IsEditing))
            {
                existingItem.IsEditing = false;
                // Herstel de naam naar originele naam
                existingItem.Name = existingItem.OriginalName;
            }

            item.IsEditing = true;
        }

        private bool CanSaveEdit(ManufacturerItem? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Name)) return false;

            string newName = item.Name.Trim();
            string oldName = item.OriginalName;

            // Nieuwe naam mag niet leeg zijn en moet verschillen van de oude naam
            if (newName.Equals(oldName, StringComparison.OrdinalIgnoreCase))
            {
                return true; // Geen wijziging is geldig (geen actie nodig)
            }

            // CHECK: Blokkeer hernoemen van 'Unknown' naar iets anders
            if (oldName.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Controleer op uniekheid
            // Zorg ervoor dat de nieuwe naam niet al een OriginalName is van een ander item
            if (Manufacturers.Any(m => m != item && m.OriginalName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                return false; // Naam is niet uniek
            }

            // Controleer op ongeldige karakters voor de mapnaam
            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            return true;
        }

        private void SaveEdit(ManufacturerItem? item)
        {
            if (item == null || !CanSaveEdit(item))
            {
                // VERTALING: Ongeldige naam. Zorg ervoor dat de naam uniek is en geen speciale tekens bevat.
                MessageBox.Show("Invalid name. Ensure the name is unique and does not contain special characters.", "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string oldName = item.OriginalName;
            string newName = item.Name.Trim();

            // Stap 1: Controleer of er iets is gewijzigd
            if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
            {
                item.IsEditing = false;
                return;
            }

            // CHECK: Extra controle voor hernoemen van 'Unknown'
            if (oldName.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Manufacturer '{oldName}' is a system default and cannot be renamed.", "Restriction", MessageBoxButton.OK, MessageBoxImage.Error);
                item.IsEditing = false;
                item.Name = item.OriginalName;
                return;
            }

            // Controleer of het een bestaand item is (OriginalName mag NIET leeg zijn!)
            if (string.IsNullOrWhiteSpace(oldName))
            {
                // VERTALING: Kan geen nieuwe fabrikant creëren via deze methode. Gebruik alleen bewerken voor bestaande fabrikanten.
                MessageBox.Show("Cannot create a new manufacturer using this method. Use editing only for existing manufacturers.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                item.IsEditing = false;
                item.Name = item.OriginalName; // Herstel naam
                return;
            }

            // Stap 2: Voer de hernoem-actie uit
            if (ExecuteRenameManufacturer(oldName, newName))
            {
                // Update het model
                item.IsEditing = false;
                item.Name = newName;
                item.IsEmpty = IsManufacturerEmpty(newName);
                item.UpdateOriginalName();

                // Herlaad de lijst om de nieuwe sorteervolgorde te reflecteren.
                LoadManufacturers();

                // Activeer het event voor de hoofd-ViewModel
                ManufacturersUpdated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // Als hernoemen mislukt, blijf dan in de bewerkingsmodus en herstel de naam
                item.IsEditing = true;
                item.Name = oldName;
            }
        }

        private void CancelEdit(ManufacturerItem? item)
        {
            if (item == null) return;

            // Herstel naar originele naam en stop met bewerken
            item.Name = item.OriginalName;
            item.IsEditing = false;
        }
    }
}