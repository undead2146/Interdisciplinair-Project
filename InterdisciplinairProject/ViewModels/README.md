# ViewModels Documentation

## Overview

This folder contains the ViewModels for the WPF application, implementing the MVVM (Model-View-ViewModel) pattern.

## FixtureSettingsViewModel

The `FixtureSettingsViewModel` provides a dynamic, extensible interface for controlling DMX lighting fixtures.

### Key Features

- **Dynamic Channel Support**: Automatically generates UI controls based on the fixture's channel configuration
- **Unlimited Channels**: Supports fixtures with any number of channels (not limited to 8 hardcoded sliders)
- **Real-time Hardware Communication**: Uses `IHardwareConnection` to send channel updates asynchronously
- **Observable Collections**: Uses `ObservableCollection<ChannelViewModel>` for automatic UI updates

### Architecture

```
FixtureSettingsViewModel
├── Channels: ObservableCollection<ChannelViewModel>
├── FixtureName: string (read-only property)
├── LoadFixture(Fixture): void - Load a new fixture dynamically
└── _hardwareConnection: IHardwareConnection - Hardware communication service
```

### Usage Example

```csharp
// Create the view model (automatically loads demo fixture)
var viewModel = new FixtureSettingsViewModel();

// Load a different fixture dynamically
var newFixture = new Fixture
{
    FixtureId = "moving-head-c",
    Name = "Moving Head C",
    Channels = new Dictionary<string, byte?>
    {
        { "pan", 127 },
        { "tilt", 127 },
        { "gobo", 0 },
        { "color", 0 },
        { "intensity", 255 },
    }
};

viewModel.LoadFixture(newFixture);
```

### Channel Updates

When a slider is moved in the UI:
1. The `ChannelViewModel.Value` property changes
2. `ChannelViewModel_PropertyChanged` event handler is triggered
3. The fixture model is updated
4. `SetChannelValueAsync()` is called on `IHardwareConnection`
5. The hardware connection writes the updated value to the scenes.json file

## ChannelViewModel

Represents a single DMX channel with two-way data binding support.

### Properties

- `Name`: string (read-only) - The channel name (e.g., "red", "dimmer", "pan")
- `Value`: byte - The DMX value (0-255) with `INotifyPropertyChanged` support

### Usage in XAML

The view uses an `ItemsControl` to dynamically generate sliders:

```xaml
<ItemsControl ItemsSource="{Binding Channels}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <StackPanel>
                <TextBlock Text="{Binding Name}"/>
                <Slider Value="{Binding Value, Mode=TwoWay}"/>
                <Label Content="{Binding Value}"/>
            </StackPanel>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

## Extensibility

### Adding More Fixtures

To support multiple fixtures in a scene, you could extend the architecture:

```csharp
public ObservableCollection<Fixture> Fixtures { get; }
public Fixture SelectedFixture { get; set; }

// When selection changes, call LoadFixture(SelectedFixture)
```

### Adding Fixture Repository

For production use, replace the hardcoded demo fixture with a service:

```csharp
public FixtureSettingsViewModel(IFixtureRepository fixtureRepository, IHardwareConnection hardwareConnection)
{
    _fixtureRepository = fixtureRepository;
    _hardwareConnection = hardwareConnection;
    
    // Load fixture from repository
    var fixture = await _fixtureRepository.GetFixtureByIdAsync("rgb-wash-b");
    LoadFixture(fixture);
}
```

## Future Enhancements

- **Scene Management**: Load entire scenes with multiple fixtures
- **Show Sequencing**: Support for timed sequences and cues
- **Channel Grouping**: Group related channels (RGB, Pan/Tilt, etc.)
- **Preset Values**: Save and recall common channel configurations
- **Channel Metadata**: Support for channel types (color, intensity, position)
- **Value Mapping**: Map 0-255 values to meaningful units (degrees, percentages, etc.)
