# DMX Communication Architecture

## Overview
This document explains the complete architecture and data flow for DMX hardware communication in the InterdisciplinairProject lighting control application.

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────────┐
│                     UI Layer (XAML/Views)                        │
│  • SceneEditorView (Preview Scene button)                       │
│  • FixtureSettingsView (Channel sliders)                        │
└───────────────────────────┬─────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│                   ViewModel Layer (MVVM)                         │
│  • SceneEditorViewModel.PreviewSceneCommand                     │
│  • FixtureSettingsViewModel.ChannelViewModel_PropertyChanged    │
└───────────────────────────┬─────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│              Business Logic Layer (Services)                     │
│  • HardwareConnection (High-level coordination)                 │
│    - SetChannelValueAsync (single channel update)               │
│    - SendFixtureAsync (fixture update)                          │
│    - SendSceneAsync (full scene preview)                        │
└───────────────────────────┬─────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│                DMX Service Layer (State Management)              │
│  • DmxService (512-channel universe state)                      │
│    - SetChannel(address, value)                                 │
│    - SendFrame()                                                │
│    - ClearAllChannels()                                         │
└───────────────────────────┬─────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│          Hardware Communication Layer (Serial Port)              │
│  • DMXCommunication (Low-level DMX512 protocol)                 │
│    - SendDMXFrame(comPort, data)                                │
│    - SendELOFrame(comPort, data)                                │
└───────────────────────────┬─────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│                    DMX Controller Hardware                       │
│  • Serial Port (COM3, COM4, etc.)                               │
│  • DMX512 Interface Device                                      │
│  • Physical DMX Fixtures                                        │
└─────────────────────────────────────────────────────────────────┘
```

## Data Flow Scenarios

### Scenario 1: User Adjusts a Single Channel Slider

**Flow:**
1. **User Action**: Moves a slider in FixtureSettingsView
2. **ViewModel**: `FixtureSettingsViewModel.ChannelViewModel_PropertyChanged` is triggered
3. **Update Model**: Channel.Parameter is updated in the fixture model
4. **Hardware Connection**: `HardwareConnection.SetChannelValueAsync` is called
   - Validates the value (0-255)
   - Updates scenes.json file with new channel value
   - Calls `FindFixtureInScenesAsync` to retrieve complete fixture state
   - Calls `SendFixtureAsync` with the fixture
5. **Send Fixture**: `HardwareConnection.SendFixtureAsync`
   - Iterates through fixture's channels
   - Maps each channel to DMX address (StartAddress + index)
   - Calls `DmxService.SetChannel` for each channel
6. **DMX Service**: `DmxService.SetChannel`
   - Updates specific addresses in 512-byte DMX universe array
   - **Preserves all other channel values**
7. **Send Frame**: `DmxService.SendFrame`
   - Calls `DMXCommunication.SendDMXFrame`
   - Transmits complete 512-channel universe
8. **Hardware**: DMXCommunication sends via serial port
   - Break signal
   - Mark-after-break
   - Start code (0x00)
   - 512 data bytes
9. **Result**: Only the changed fixture's channels are updated, all other fixtures remain at their current values

**Key Point**: The DMX universe state is maintained across updates, so other fixtures are not affected.

### Scenario 2: User Clicks "Preview Scene" Button

**Flow:**
1. **User Action**: Clicks "▶ Preview Scene" button in SceneEditorView
2. **ViewModel**: `SceneEditorViewModel.PreviewSceneCommand` is executed
3. **Hardware Connection**: `HardwareConnection.SendSceneAsync` is called
4. **Clear Universe**: `DmxService.ClearAllChannels` sets all 512 channels to 0
5. **Load Fixtures**: For each fixture in the scene:
   - Iterate through fixture's channels
   - Map to DMX address (StartAddress + index)
   - Call `DmxService.SetChannel` for each channel
6. **Send Frame**: `DmxService.SendFrame` transmits the complete scene
7. **Hardware**: DMXCommunication sends via serial port
8. **Result**: Complete scene is sent to DMX controller, replacing any previous state

**Key Point**: This clears the universe first, ensuring a clean slate for the scene preview.

## Key Classes and Responsibilities

### IHardwareConnection (Interface)
**Location**: `InterdisciplinairProject.Core/Interfaces/IHardwareConnection.cs`

Defines the contract for high-level DMX operations:
- `SetChannelValueAsync`: Update single channel + send fixture
- `SendFixtureAsync`: Send one fixture's channels
- `SendSceneAsync`: Send complete scene

### HardwareConnection (Implementation)
**Location**: `InterdisciplinairProject/Services/HardwareConnection.cs`

Coordinates between JSON persistence and DMX hardware:
- Manages scenes.json file updates
- Maps fixtures to DMX addresses
- Delegates DMX communication to DmxService
- Dependency: `IDmxService`

### IDmxService (Interface)
**Location**: `InterdisciplinairProject/Services/IDmxService.cs`

Defines the contract for DMX universe management:
- `SetChannel(address, value)`: Update one channel
- `GetChannel(address)`: Read channel value
- `SendFrame()`: Transmit universe to hardware
- `ClearAllChannels()`: Reset universe to zeros

### DmxService (Implementation)
**Location**: `InterdisciplinairProject/Services/DmxService.cs`

Manages the 512-channel DMX universe state:
- Maintains `byte[] _dmxUniverse` (512 elements)
- Auto-discovers COM ports
- Delegates serial communication to DMXCommunication
- Dependency: `DMXCommunication`

### DMXCommunication (Static Class)
**Location**: `InterdisciplinairProject/Fixtures/DMXCommunication/DMXCommunication.cs`

Low-level DMX512 protocol implementation:
- `SendDMXFrame`: Standard DMX512 transmission
- `SendELOFrame`: ELO (Cable) protocol variant
- Handles serial port communication
- Implements DMX512 timing requirements

### FixtureSettingsViewModel
**Location**: `InterdisciplinairProject/ViewModels/Scene/FixtureSettingsViewModel.cs`

Manages fixture channel editing UI:
- Observes channel value changes
- Calls `IHardwareConnection.SetChannelValueAsync` on change
- Provides Cancel/Save functionality
- Dependency: `IHardwareConnection`

### SceneEditorViewModel
**Location**: `InterdisciplinairProject/ViewModels/Scene/SceneEditorViewModel.cs`

Manages scene editing and preview:
- `PreviewSceneCommand`: Sends complete scene
- Manages fixture list
- Dependency: `IHardwareConnection`

## DMX Universe State Management

The `DmxService` maintains a persistent 512-byte array representing the DMX universe:

```csharp
private readonly byte[] _dmxUniverse = new byte[512];
```

**Key Behaviors:**
1. **Initialization**: All channels start at 0
2. **SetChannel**: Updates one address without affecting others
3. **ClearAllChannels**: Resets all to 0
4. **SendFrame**: Transmits current state to hardware

This approach ensures:
- Multiple fixtures can coexist in the same universe
- Individual fixture updates don't affect others
- Complete universe state is always transmitted
- No partial updates that could cause flickering

## Fixture to DMX Address Mapping

Each fixture has:
- `StartAddress`: First DMX channel (1-512)
- `Channels`: List of channels (ordered)

**Mapping Formula:**
```
DMX Address = Fixture.StartAddress + Channel Index
```

**Example:**
```
Fixture: "Moving Head"
StartAddress: 10
Channels: [Pan, Tilt, Dimmer, Color]

DMX Mapping:
- Pan   → DMX Channel 10
- Tilt  → DMX Channel 11
- Dimmer → DMX Channel 12
- Color → DMX Channel 13
```

## COM Port Discovery

The `DmxService` automatically discovers available COM ports:

```csharp
private static string? DiscoverComPort()
{
    var portNames = SerialPort.GetPortNames();
    foreach (var port in portNames)
    {
        try
        {
            using var sp = new SerialPort(port);
            sp.Open();
            sp.Close();
            return port; // First available port
        }
        catch { /* Port not available */ }
    }
    return null;
}
```

**Manual Override:**
```csharp
dmxService.ComPort = "COM3"; // Force specific port
```
