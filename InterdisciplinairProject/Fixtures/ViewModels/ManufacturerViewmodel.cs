using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        public ManufacturerItem(string name, bool isEmpty)
        {
            Name = name;
            OriginalName = name;
            IsEmpty = isEmpty;
        }

        public void UpdateOriginalName()
        {
            OriginalName = Name;
        }
    }

    public partial class ManufacturerViewModel : ObservableObject
    {
        public event EventHandler? ManufacturersUpdated;

        private readonly string _rootDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InterdisciplinairProject",
            "Fixtures"
        );

        private readonly string _centralJsonPath;

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

            LoadManufacturers();
        }

        public void LoadManufacturers()
        {
            Manufacturers.Clear();

            try
            {
                var names = Directory.GetDirectories(_rootDirectory)
                                             .Select(Path.GetFileName)
                                             .Where(name => name != null)
                                             .ToList();

                var items = names.Select(name =>
                {
                    bool isEmpty = IsManufacturerEmpty(name!);
                    return new ManufacturerItem(name!, isEmpty);
                }).ToList();

                var unknownItem = items.FirstOrDefault(i => i.IsSystemItem);

                var sortedItems = items
                    .Where(i => !i.IsSystemItem)
                    .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (unknownItem != null)
                {
                    Manufacturers.Add(unknownItem);
                }

                foreach (var item in sortedItems)
                {
                    Manufacturers.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading manufacturers: {ex.Message}", "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            _deleteManufacturerCommand.NotifyCanExecuteChanged();
            _startEditCommand.NotifyCanExecuteChanged();
        }

        private bool IsManufacturerEmpty(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return true;
            string manufacturerPath = Path.Combine(_rootDirectory, name);
            if (!Directory.Exists(manufacturerPath)) return true;

            try
            {
                return !Directory.EnumerateFiles(manufacturerPath).Any();
            }
            catch (Exception) { return false; }
        }

        private bool ExecuteDeleteManufacturer(string name)
        {
            string manufacturerPath = Path.Combine(_rootDirectory, name);
            if (!Directory.Exists(manufacturerPath)) return true;

            if (!IsManufacturerEmpty(name)) return false;

            try
            {
                Directory.Delete(manufacturerPath);
                UpdateManufacturerNameInCentralJson(name, string.Empty);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Deletion failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool ExecuteRenameManufacturer(string oldName, string newName)
        {
            string oldPath = Path.Combine(_rootDirectory, oldName);
            string newPath = Path.Combine(_rootDirectory, newName);

            if (!Directory.Exists(oldPath) || Directory.Exists(newPath)) return false;

            try
            {
                Directory.Move(oldPath, newPath);

                foreach (var filePath in Directory.EnumerateFiles(newPath, "*.json"))
                {
                    UpdateManufacturerNameInFixtureJson(filePath, newName);
                }

                UpdateManufacturerNameInCentralJson(oldName, newName);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Renaming failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

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
                            manufacturerArray.RemoveAt(i);
                            changed = true;
                            break;
                        }
                        else
                        {
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
            return item.IsEmpty;
        }

        private void DeleteManufacturer(ManufacturerItem? item)
        {
            if (item == null) return;

            if (item.IsSystemItem)
            {
                MessageBox.Show($"Manufacturer '{item.Name}' is a system default and cannot be deleted.", "Restriction", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!item.IsEmpty)
            {
                MessageBox.Show($"Manufacturer '{item?.Name}' cannot be deleted. Ensure the folder is empty.", "Deletion Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{item.Name}'? This cannot be undone.",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                if (ExecuteDeleteManufacturer(item.Name))
                {
                    Manufacturers.Remove(item);
                    ManufacturersUpdated?.Invoke(this, EventArgs.Empty);

                    _deleteManufacturerCommand.NotifyCanExecuteChanged();
                }
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

            foreach (var existingItem in Manufacturers.Where(i => i.IsEditing))
            {
                existingItem.IsEditing = false;
                existingItem.Name = existingItem.OriginalName;
            }

            item.IsEditing = true;
        }

        private bool CanSaveEdit(ManufacturerItem? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Name)) return false;

            string newName = item.Name.Trim();
            string oldName = item.OriginalName;

            if (newName.Equals(oldName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (item.IsSystemItem)
            {
                return false;
            }

            if (Manufacturers.Any(m => m != item && m.OriginalName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

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
                MessageBox.Show("Invalid name. Ensure the name is unique and does not contain special characters.", "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string oldName = item.OriginalName;
            string newName = item.Name.Trim();

            if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
            {
                item.IsEditing = false;
                return;
            }

            if (item.IsSystemItem)
            {
                MessageBox.Show($"Manufacturer '{oldName}' is a system default and cannot be renamed.", "Restriction", MessageBoxButton.OK, MessageBoxImage.Error);
                item.IsEditing = false;
                item.Name = item.OriginalName;
                return;
            }

            if (string.IsNullOrWhiteSpace(oldName))
            {
                MessageBox.Show("Cannot create a new manufacturer using this method. Use editing only for existing manufacturers.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                item.IsEditing = false;
                item.Name = item.OriginalName;
                return;
            }

            if (ExecuteRenameManufacturer(oldName, newName))
            {
                item.IsEditing = false;
                item.Name = newName;
                item.IsEmpty = IsManufacturerEmpty(newName);
                item.UpdateOriginalName();

                LoadManufacturers();

                ManufacturersUpdated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                item.IsEditing = true;
                item.Name = oldName;
            }
        }

        private void CancelEdit(ManufacturerItem? item)
        {
            if (item == null) return;

            item.Name = item.OriginalName;
            item.IsEditing = false;
        }
    }
}