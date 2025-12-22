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
using System.Collections.Generic;
using InterdisciplinairProject.Fixtures.Views;

public enum UnsavedChangesAction
{
    SaveAndContinue,
    DiscardAndContinue,
    ContinueEditing
}


namespace InterdisciplinairProject.Fixtures.ViewModels
{
    /// <summary>
    /// Representeert een enkele fabrikant in de beheerderslijst.
    /// </summary>
    public partial class ManufacturerItem : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private bool isEmpty;

        [ObservableProperty]
        private bool isEditing = false;

        public string OriginalName { get; private set; }

        public bool IsSystemItem => Name.Equals("Unknown", StringComparison.OrdinalIgnoreCase);

        public string DisplayName
        {
            get
            {
                return IsEmpty ? $"{Name} (Empty)" : Name;
            }
        }

        public ManufacturerItem(string name, bool isEmpty)
        {
            Name = name;
            OriginalName = name;
            IsEmpty = isEmpty;
        }

        public void UpdateOriginalName()
        {
            OriginalName = Name;
            NotifyDisplayUpdate();
        }
        public void NotifyDisplayUpdate()
        {
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    /// <summary>
    /// ViewModel voor de Manufacturer Management View.
    /// Beheert het inladen, hernoemen en verwijderen van fabrikanten.
    /// </summary>
    public partial class ManufacturerViewModel : ObservableObject
    {
        public event EventHandler? ManufacturersUpdated;

        private readonly string _rootDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InterdisciplinairProject",
            "Fixtures"
        );

        private readonly string _centralJsonPath;
        private readonly ManufacturerService _manufacturerService;

        [ObservableProperty]
        private ObservableCollection<ManufacturerItem> manufacturers = new();

        private readonly RelayCommand<ManufacturerItem> _deleteManufacturerCommand;
        private readonly RelayCommand<ManufacturerItem> _startEditCommand;

        public ICommand DeleteManufacturerCommand => _deleteManufacturerCommand;
        public ICommand StartEditCommand => _startEditCommand;
        public ICommand SaveEditCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand LoadViewCommand { get; }

        public ManufacturerViewModel()
        {
            _manufacturerService = new ManufacturerService();

            _centralJsonPath = Path.Combine(_rootDirectory, "manufacturers.json");

            if (!Directory.Exists(_rootDirectory))
            {
                Directory.CreateDirectory(_rootDirectory);
            }

            _deleteManufacturerCommand = new RelayCommand<ManufacturerItem>(DeleteManufacturer, CanDeleteManufacturer);
            _startEditCommand = new RelayCommand<ManufacturerItem>(StartEdit, CanStartEdit);
            SaveEditCommand = new RelayCommand<ManufacturerItem>(SaveEdit, CanSaveEdit);
            CancelEditCommand = new RelayCommand<ManufacturerItem>(CancelEdit);
            LoadViewCommand = new RelayCommand(LoadManufacturers);

            // Laad fabrikanten bij initialisatie.
            LoadManufacturers();
        }

        public void LoadManufacturers()
        {
            Manufacturers.Clear();

            try
            {
                var names = _manufacturerService.GetManufacturers();

                var items = names.Select(name =>
                {
                    bool isEmpty = IsManufacturerEmpty(name);
                    return new ManufacturerItem(name, isEmpty);
                }).ToList();

                var unknownItem = items.FirstOrDefault(i => i.IsSystemItem);

                // Sorteer alle items behalve 'Unknown'.
                var sortedItems = items
                    .Where(i => !i.IsSystemItem)
                    .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Voeg 'Unknown' eerst toe.
                if (unknownItem != null)
                {
                    Manufacturers.Add(unknownItem);
                }

                // Voeg de gesorteerde items toe.
                foreach (var item in sortedItems)
                {
                    Manufacturers.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading manufacturers: {ex.Message}",
                                 "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Zorg ervoor dat de uitvoeringsstatus van de commando's wordt bijgewerkt.
            _deleteManufacturerCommand.NotifyCanExecuteChanged();
            _startEditCommand.NotifyCanExecuteChanged();
        }


        /// <summary>
        /// Controleert of de map van een fabrikant leeg is (bevat geen .json-bestanden).
        /// </summary>
        private bool IsManufacturerEmpty(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return true;
            string manufacturerPath = Path.Combine(_rootDirectory, name);
            if (!Directory.Exists(manufacturerPath)) return true;

            try
            {
                return !Directory.EnumerateFiles(manufacturerPath, "*.json").Any();
            }
            catch (Exception) { return false; }
        }

        private bool ExecuteDeleteManufacturer(string name, bool recursive)
        {
            string manufacturerPath = Path.Combine(_rootDirectory, name);
            if (!Directory.Exists(manufacturerPath)) return true;

            if (!recursive && !IsManufacturerEmpty(name)) return false;

            try
            {
                Directory.Delete(manufacturerPath, recursive);
                UpdateManufacturerNameInCentralJson(name, string.Empty);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Deletion failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Voert de daadwerkelijke hernoeming van de fabrikantenmap uit.
        /// </summary>
        private bool ExecuteRenameManufacturer(string oldName, string newName)
        {
            string oldPath = Path.Combine(_rootDirectory, oldName);
            string newPath = Path.Combine(_rootDirectory, newName);

            if (!Directory.Exists(oldPath) || Directory.Exists(newPath))
            {
                MessageBox.Show($"Cannot rename. Folder '{newName}' already exists, or folder '{oldName}' was not found.", "Rename Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            try
            {
                Directory.Move(oldPath, newPath);

                // Update 'manufacturer' veld in alle JSON-bestanden in de verplaatste map.
                foreach (var filePath in Directory.EnumerateFiles(newPath, "*.json"))
                {
                    UpdateManufacturerNameInFixtureJson(filePath, newName);
                }

                // Update de centrale JSON-lijst.
                UpdateManufacturerNameInCentralJson(oldName, newName);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Renaming failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Werkt het 'manufacturer' veld in een individueel armatuur JSON-bestand bij.
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
                MessageBox.Show($"File update failed for {Path.GetFileName(filePath)}: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Werkt de naam in de centrale 'manufacturers.json' lijst bij (verwijderen indien newName leeg is).
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
                    // Controleer op een overeenkomst met de oude naam (case-insensitive)
                    if (manufacturerArray[i]?.GetValue<string>()?.Equals(oldName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if (string.IsNullOrWhiteSpace(newName))
                        {
                            // Verwijder de vermelding
                            manufacturerArray.RemoveAt(i);
                            changed = true;
                            break;
                        }
                        else
                        {
                            // Hernoem de vermelding
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
                MessageBox.Show($"Update of manufacturers.json failed: {ex.Message}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // --------------------------------------------------------------------------------------
        // COMMANDS
        // --------------------------------------------------------------------------------------
        private bool CanStartEdit(ManufacturerItem? item)
        {
            if (item == null || item.IsSystemItem)
            {
                return false;
            }
            return true;
        }

        private bool CanDeleteManufacturer(ManufacturerItem? item)
        {
            if (item == null || item.IsSystemItem)
            {
                return false;
            }
            return true;
        }

        private void DeleteManufacturer(ManufacturerItem? item)
        {
            if (item == null || item.IsSystemItem) return;

            bool deleteRecursively = false;

            if (!item.IsEmpty)
            {
                var result = MessageBox.Show(
                    $"Manufacturer '{item.Name}' contains fixtures. Are you sure you want to permanently delete this folder and all its contents? This action cannot be undone.",
                    "Confirm Deletion Including Contents",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    return;
                }

                deleteRecursively = true;
            }
            else
            {
                deleteRecursively = false;
            }

            if (ExecuteDeleteManufacturer(item.Name, deleteRecursively))
            {
                Manufacturers.Remove(item);
                ManufacturersUpdated?.Invoke(this, EventArgs.Empty);

                _deleteManufacturerCommand.NotifyCanExecuteChanged();
            }
        }

        private void StartEdit(ManufacturerItem? item)
        {
            if (item == null) return;

            if (item.IsSystemItem)
            {
                MessageBox.Show($"Manufacturer '{item.Name}' is a system default and cannot be edited.", "Restriction", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Annuleer de bewerkingsmodus voor alle andere items
            foreach (var existingItem in Manufacturers.Where(i => i.IsEditing))
            {
                existingItem.IsEditing = false;
                existingItem.Name = existingItem.OriginalName;
                existingItem.NotifyDisplayUpdate();
            }

            item.IsEditing = true;
            item.NotifyDisplayUpdate();
        }

        private bool CanSaveEdit(ManufacturerItem? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Name)) return false;

            string newName = item.Name.Trim();
            string oldName = item.OriginalName;

            // Als de naam niet is veranderd, is opslaan toegestaan.
            if (newName.Equals(oldName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (item.IsSystemItem) return false;

            // Controleer op uniciteit (mag niet gelijk zijn aan de OriginalName van een ander item)
            if (Manufacturers.Any(m => m != item && m.OriginalName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Controleer op ongeldige tekens voor een bestandsnaam/map.
            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            return true;
        }

        private void SaveEdit(ManufacturerItem? item)
        {
            if (item == null) return;

            string oldName = item.OriginalName;
            string newName = item.Name.Trim();

            if (!CanSaveEdit(item))
            {
                MessageBox.Show("Invalid name. Ensure the name is unique, not the system name, and does not contain special characters.", "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                item.Name = oldName;
                item.IsEditing = true;
                return;
            }

            if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
            {
                item.IsEditing = false;
                item.NotifyDisplayUpdate();
                return;
            }

            // Uitvoeren van de hernoeming
            if (ExecuteRenameManufacturer(oldName, newName))
            {
                // Success: Update state
                item.IsEditing = false;
                item.Name = newName;
                item.IsEmpty = IsManufacturerEmpty(newName);
                item.UpdateOriginalName();

                // Herlaad de lijst om de gesorteerde volgorde bij te werken.
                LoadManufacturers();

                // Notificeer de hoofd-ViewModel dat de lijst met armaturen moet worden herladen.
                ManufacturersUpdated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // Failure: Blijf in bewerkingsmodus en herstel de naam (ExecuteRenameManufacturer toont al een foutmelding).
                item.IsEditing = true;
                item.Name = oldName;
                item.NotifyDisplayUpdate();
            }
        }
        private void CancelEdit(ManufacturerItem? item)
        {
            if (item == null) return;

            item.Name = item.OriginalName;
            item.IsEditing = false;
            item.NotifyDisplayUpdate();
        }

        public bool ConfirmExitWhileEditing()
        {
            var editingItem = Manufacturers.FirstOrDefault(m => m.IsEditing);

            if (editingItem == null)
            {
                return true;
            }

            if (editingItem.Name.Equals(editingItem.OriginalName, StringComparison.OrdinalIgnoreCase))
            {
                CancelEdit(editingItem);
                return true;
            }


            var dialog = new ConfirmExitDialog(editingItem.OriginalName);
            dialog.ShowDialog();

            UnsavedChangesAction action = dialog.ResultAction;

            switch (action)
            {
                case UnsavedChangesAction.SaveAndContinue:
                    SaveEdit(editingItem);
                    return !editingItem.IsEditing;

                case UnsavedChangesAction.DiscardAndContinue:
                    CancelEdit(editingItem);
                    return true;

                case UnsavedChangesAction.ContinueEditing:
                    return false;

                default:
                    return false;
            }
        }

    }

}