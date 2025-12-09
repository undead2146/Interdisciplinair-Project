using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Services;

/// <summary>
/// Provides hardware connection functionality for controlling lighting fixtures.
/// This class serves as the high-level coordination layer between the application and DMX hardware.
/// Architecture and Service Flow:
/// 1. FixtureSettingsViewModel: User adjusts a slider in the UI.
/// 2. HardwareConnection.SetChannelValueAsync: Saves to scenes.json and calls SendFixtureAsync.
/// 3. HardwareConnection.SendFixtureAsync: Maps fixture channels to DMX addresses.
/// 4. DmxService.SetChannel: Updates specific addresses in the 512-channel universe.
/// 5. DmxService.SendFrame: Transmits the complete universe via DMXCommunication.
/// 6. DMXCommunication: Low-level serial port communication with DMX controller.
/// Key Features:
/// - Preserves DMX universe state across individual fixture updates.
/// - Supports both single-fixture updates and full-scene previews.
/// - Automatically discovers and uses available COM ports.
/// - Maintains JSON persistence for scene state.
/// </summary>
public class HardwareConnection : IHardwareConnection
{
    private readonly string _scenesFilePath;
    private readonly IDmxService _dmxService;

    /// <summary>
    /// Initializes a new instance of the <see cref="HardwareConnection"/> class.
    /// </summary>
    public HardwareConnection()
        : this(new DmxService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HardwareConnection"/> class.
    /// </summary>
    /// <param name="dmxService">The DMX service.</param>
    public HardwareConnection(IDmxService dmxService)
    {
        Debug.WriteLine("[DEBUG] HardwareConnection constructor called");

        _dmxService = dmxService;
        _scenesFilePath = FindScenesFile();
        Debug.WriteLine($"[DEBUG] HardwareConnection scenes file path: {_scenesFilePath}");

        if (File.Exists(_scenesFilePath))
        {
            Debug.WriteLine("[DEBUG] HardwareConnection scenes.json already exists");
        }
        else
        {
            Debug.WriteLine("[DEBUG] HardwareConnection scenes.json not found - ViewModel will create it");
        }

        Debug.WriteLine("[DEBUG] HardwareConnection initialization complete");
    }

    /// <summary>
    /// Sends a value to a specific channel of a fixture asynchronously.
    /// This method handles the complete flow for updating a single fixture channel:
    /// 1. Validates the channel value (0-255)
    /// 2. Updates the scenes.json file with the new channel value
    /// 3. Retrieves the complete fixture state from the file
    /// 4. Sends ONLY the affected fixture's channels to the DMX controller via SendFixtureAsync
    /// 5. The DMX service updates only the fixture's addresses in the 512-channel universe
    /// 6. All other fixtures in the universe retain their current values
    /// 7. A complete DMX frame is sent to the controller with the updated state
    /// </summary>
    /// <param name="fixtureInstanceId">The instance ID of the fixture.</param>
    /// <param name="channelName">The name of the channel (e.g. "dimmer").</param>
    /// <param name="value">The value between 0 and 255.</param>
    /// <returns>True if successful, otherwise false.</returns>
    public async Task<bool> SetChannelValueAsync(string fixtureInstanceId, string channelName, byte value)
    {
        var msg = $"[HARDWARE] SetChannelValueAsync: fixture={fixtureInstanceId}, channel={channelName}, value={value}";
        Debug.WriteLine(msg);
        Console.WriteLine(msg);

        try
        {
            // Validate the value (0-255)
            if (value < 0 || value > 255)
            {
                Debug.WriteLine($"[DEBUG] SetChannelValueAsync validation failed: value {value} out of range");
                throw new ArgumentOutOfRangeException(nameof(value), "Waarde moet tussen 0 en 255 zijn");
            }

            Debug.WriteLine("[DEBUG] SetChannelValueAsync validation passed");

            // Read the scenes file
            if (!File.Exists(_scenesFilePath))
            {
                Debug.WriteLine($"[DEBUG] SetChannelValueAsync scenes file not found: {_scenesFilePath}");
                throw new FileNotFoundException($"Scenes bestand niet gevonden: {_scenesFilePath}");
            }

            Debug.WriteLine($"[DEBUG] SetChannelValueAsync reading scenes file: {_scenesFilePath}");

            string jsonContent = await File.ReadAllTextAsync(_scenesFilePath);
            Debug.WriteLine($"[DEBUG] SetChannelValueAsync read {jsonContent.Length} characters from scenes file");

            // Parse JSON with System.Text.Json
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            using MemoryStream stream = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

            // Write the modified JSON
            Debug.WriteLine("[DEBUG] SetChannelValueAsync writing modified JSON");
            WriteModifiedJson(doc.RootElement, writer, fixtureInstanceId, channelName, value);

            writer.Flush();

            // Write back to file
            string updatedJson = Encoding.UTF8.GetString(stream.ToArray());
            Debug.WriteLine($"[DEBUG] SetChannelValueAsync writing {updatedJson.Length} characters back to scenes file");
            await File.WriteAllTextAsync(_scenesFilePath, updatedJson);

            // Send to DMX hardware: find the fixture and update only its channels
            var fixture = await FindFixtureInScenesAsync(fixtureInstanceId);
            if (fixture != null)
            {
                await SendFixtureAsync(fixture);
            }

            var successMsg = $"[HARDWARE] ✓ Successfully updated {channelName}={value} in {fixtureInstanceId}";
            Debug.WriteLine(successMsg);
            Console.WriteLine(successMsg);
            Console.WriteLine($"[HARDWARE] File updated: {_scenesFilePath}");

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] SetChannelValueAsync failed with exception: {ex.Message}");
            Console.WriteLine($"Fout bij het updaten van kanaal: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sends the entire scene to the DMX controller asynchronously.
    /// This method is used for the "Preview Scene" button functionality.
    /// Flow:
    /// 1. Clears all 512 DMX channels to zero
    /// 2. Iterates through all fixtures in the scene
    /// 3. Maps each fixture's channels to DMX addresses based on StartAddress
    /// 4. Sets all fixture channel values in the DMX universe
    /// 5. Sends the complete DMX frame to the controller
    /// Use this when switching between scenes or previewing a complete scene state.
    /// For individual channel updates during editing, use SetChannelValueAsync instead.
    /// </summary>
    /// <param name="scene">The scene to send.</param>
    /// <returns>True if successful, otherwise false.</returns>
    public async Task<bool> SendSceneAsync(Scene scene)
    {
        if (scene == null || scene.Fixtures == null)
        {
            Debug.WriteLine("[HARDWARE] SendSceneAsync: scene or fixtures is null");
            return false;
        }

        Debug.WriteLine($"[HARDWARE] SendSceneAsync: sending scene '{scene.Name}' with {scene.Fixtures.Count} fixtures");

        try
        {
            // Clear all channels before sending the scene
            _dmxService.ClearAllChannels();

            // Set all fixture channels in the DMX universe
            foreach (var fixture in scene.Fixtures)
            {
                if (fixture.Channels == null || fixture.Channels.Count == 0)
                {
                    Debug.WriteLine($"[HARDWARE] Fixture {fixture.InstanceId} has no channels, skipping");
                    continue;
                }

                Debug.WriteLine($"[HARDWARE] Processing fixture {fixture.InstanceId} at address {fixture.StartAddress}");

                for (int i = 0; i < fixture.Channels.Count; i++)
                {
                    var channel = fixture.Channels[i];
                    int dmxAddress = fixture.StartAddress + i;

                    // Ensure Parameter is set from Value if needed
                    if (channel.Parameter == 0 && !string.IsNullOrEmpty(channel.Value))
                    {
                        if (int.TryParse(channel.Value, out int parsedValue))
                        {
                            channel.Parameter = parsedValue;
                            Debug.WriteLine($"[DEBUG] Parsed channel {channel.Name} Value='{channel.Value}' -> Parameter={channel.Parameter}");
                        }
                    }

                    byte value = (byte)channel.Parameter;
                    _dmxService.SetChannel(dmxAddress, value);
                    Debug.WriteLine($"[HARDWARE] Set DMX[{dmxAddress}] = {value} ({fixture.InstanceId}.{channel.Name})");
                }
            }

            // Send the complete DMX frame
            bool success = _dmxService.SendFrame();

            if (success)
            {
                Debug.WriteLine($"[HARDWARE] ✓ Successfully sent scene '{scene.Name}' to DMX controller");
            }
            else
            {
                Debug.WriteLine($"[HARDWARE] ✗ Failed to send scene '{scene.Name}' to DMX controller");
            }

            return success;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HARDWARE] SendSceneAsync failed with exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sends a single fixture's channel values to the DMX controller asynchronously.
    /// This method updates ONLY the specified fixture's DMX channels while preserving all other
    /// fixture values in the 512-channel DMX universe.
    /// Flow:
    /// 1. Iterates through the fixture's channels
    /// 2. Maps each channel to its DMX address (StartAddress + channel index)
    /// 3. Updates only those addresses in the DmxService's universe state
    /// 4. Sends the complete DMX frame to the controller
    /// 5. Other fixtures at different addresses remain unchanged
    /// This allows live, real-time updates to individual fixtures without affecting the rest of the scene.
    /// </summary>
    /// <param name="fixture">The fixture to send.</param>
    /// <returns>True if successful, otherwise false.</returns>
    public async Task<bool> SendFixtureAsync(Fixture fixture)
    {
        if (fixture == null || fixture.Channels == null || fixture.Channels.Count == 0)
        {
            Debug.WriteLine("[HARDWARE] SendFixtureAsync: fixture or channels is null/empty");
            return false;
        }

        Debug.WriteLine($"[HARDWARE] SendFixtureAsync: sending fixture '{fixture.InstanceId}' at address {fixture.StartAddress}");

        try
        {
            // Update only this fixture's channels in the DMX universe
            for (int i = 0; i < fixture.Channels.Count; i++)
            {
                var channel = fixture.Channels[i];
                int dmxAddress = fixture.StartAddress + i;

                // Ensure Parameter is set from Value if needed
                if (channel.Parameter == 0 && !string.IsNullOrEmpty(channel.Value))
                {
                    if (int.TryParse(channel.Value, out int parsedValue))
                    {
                        channel.Parameter = parsedValue;
                        Debug.WriteLine($"[DEBUG] Parsed channel {channel.Name} Value='{channel.Value}' -> Parameter={channel.Parameter}");
                    }
                }

                byte value = (byte)channel.Parameter;
                _dmxService.SetChannel(dmxAddress, value);
                Debug.WriteLine($"[HARDWARE] Set DMX[{dmxAddress}] = {value} ({fixture.InstanceId}.{channel.Name})");
            }

            // Send the DMX frame with updated fixture
            bool success = _dmxService.SendFrame();

            if (success)
            {
                Debug.WriteLine($"[HARDWARE] ✓ Successfully sent fixture '{fixture.InstanceId}' to DMX controller");
            }
            else
            {
                Debug.WriteLine($"[HARDWARE] ✗ Failed to send fixture '{fixture.InstanceId}' to DMX controller");
            }

            return await Task.FromResult(success);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HARDWARE] SendFixtureAsync failed with exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Finds a fixture in the scenes file by instance ID.
    /// </summary>
    /// <param name="fixtureInstanceId">The instance ID of the fixture.</param>
    /// <returns>The fixture, or null if not found.</returns>
    private async Task<Fixture?> FindFixtureInScenesAsync(string fixtureInstanceId)
    {
        try
        {
            if (!File.Exists(_scenesFilePath))
            {
                Debug.WriteLine($"[DEBUG] FindFixtureInScenesAsync: scenes file not found: {_scenesFilePath}");
                return null;
            }

            string jsonContent = await File.ReadAllTextAsync(_scenesFilePath);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);

            Debug.WriteLine($"[DEBUG] FindFixtureInScenesAsync: searching for fixture {fixtureInstanceId}");

            // JSON structure is an array of scenes at the root
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                // Iterate through each scene in the array
                foreach (JsonElement sceneElement in doc.RootElement.EnumerateArray())
                {
                    if (sceneElement.TryGetProperty("fixtures", out JsonElement fixturesArray))
                    {
                        // Iterate through fixtures in this scene
                        foreach (JsonElement fixtureElement in fixturesArray.EnumerateArray())
                        {
                            if (fixtureElement.TryGetProperty("instanceId", out JsonElement instanceId) &&
                                instanceId.GetString() == fixtureInstanceId)
                            {
                                Debug.WriteLine($"[DEBUG] FindFixtureInScenesAsync: found fixture {fixtureInstanceId}");
                                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                var fixture = JsonSerializer.Deserialize<Fixture>(fixtureElement.GetRawText(), options);
                                
                                // Convert Value (string) to Parameter (int) for each channel
                                if (fixture != null && fixture.Channels != null)
                                {
                                    foreach (var channel in fixture.Channels)
                                    {
                                        if (int.TryParse(channel.Value, out int paramValue))
                                        {
                                            channel.Parameter = paramValue;
                                            Debug.WriteLine($"[DEBUG] Channel {channel.Name}: Value='{channel.Value}' -> Parameter={channel.Parameter}");
                                        }
                                        else
                                        {
                                            channel.Parameter = 0;
                                            Debug.WriteLine($"[DEBUG] Channel {channel.Name}: Failed to parse Value='{channel.Value}', defaulting to 0");
                                        }
                                    }
                                }
                                
                                return fixture;
                            }
                        }
                    }
                }
            }

            Debug.WriteLine($"[DEBUG] FindFixtureInScenesAsync: fixture {fixtureInstanceId} not found");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HARDWARE] FindFixtureInScenesAsync failed: {ex.Message}");
            Debug.WriteLine($"[HARDWARE] Exception stack trace: {ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Writes a JSON value to the writer.
    /// </summary>
    /// <param name="element">The JSON element to write.</param>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    private static void WriteJsonValue(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var p in element.EnumerateObject())
                {
                    writer.WritePropertyName(p.Name);
                    WriteJsonValue(p.Value, writer);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteJsonValue(item, writer);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out long l))
                    writer.WriteNumberValue(l);
                else
                    writer.WriteNumberValue(element.GetDouble());
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
        }
    }

    /// <summary>
    /// Writes the modified JSON element to the writer.
    /// </summary>
    /// <param name="element">The JSON element to write.</param>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="targetInstanceId">The target fixture instance ID.</param>
    /// <param name="targetChannel">The target channel name.</param>
    /// <param name="value">The value to set.</param>
    private void WriteModifiedJson(
        JsonElement element,
        Utf8JsonWriter writer,
        string targetInstanceId,
        string targetChannel,
        byte value)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (JsonProperty prop in element.EnumerateObject())
                {
                    writer.WritePropertyName(prop.Name);

                    // Check of we bij de channels property zijn van de juiste fixture
                    if (prop.Name == "channels" && IsTargetFixture(element, targetInstanceId))
                    {
                        WriteModifiedChannels(prop.Value, writer, targetChannel, value);
                    }
                    else
                    {
                        WriteModifiedJson(prop.Value, writer, targetInstanceId, targetChannel, value);
                    }
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    WriteModifiedJson(item, writer, targetInstanceId, targetChannel, value);
                }

                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                writer.WriteNumberValue(element.GetInt32());
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
        }
    }

    /// <summary>
    /// Determines if the given fixture element is the target fixture.
    /// </summary>
    /// <param name="fixtureElement">The fixture JSON element.</param>
    /// <param name="targetInstanceId">The target instance ID.</param>
    /// <returns>True if it is the target fixture, otherwise false.</returns>
    private bool IsTargetFixture(JsonElement fixtureElement, string targetInstanceId)
    {
        if (fixtureElement.TryGetProperty("instanceId", out JsonElement instanceId))
        {
            var actualInstanceId = instanceId.GetString();
            var isMatch = actualInstanceId == targetInstanceId;
            Debug.WriteLine($"[DEBUG] IsTargetFixture: comparing '{actualInstanceId}' with '{targetInstanceId}' = {isMatch}");
            return isMatch;
        }

        Debug.WriteLine($"[DEBUG] IsTargetFixture: no instanceId property found, looking for fixtureId");

        // Fallback: try fixtureId if instanceId doesn't exist
        if (fixtureElement.TryGetProperty("fixtureId", out JsonElement fixtureId))
        {
            var actualFixtureId = fixtureId.GetString();
            var isMatch = actualFixtureId == targetInstanceId;
            Debug.WriteLine($"[DEBUG] IsTargetFixture: comparing fixtureId '{actualFixtureId}' with '{targetInstanceId}' = {isMatch}");
            return isMatch;
        }

        Debug.WriteLine($"[DEBUG] IsTargetFixture: no instanceId or fixtureId found, returning false");
        return false;
    }

    /// <summary>
    /// Writes the modified channels to the writer.
    /// Channels are stored as an array of channel objects with "name" and "value" properties.
    /// </summary>
    /// <param name="channels">The channels JSON element (array).</param>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="targetChannel">The target channel name.</param>
    /// <param name="value">The value to set.</param>
    private void WriteModifiedChannels(
        JsonElement channels,
        Utf8JsonWriter writer,
        string targetChannel,
        byte value)
    {
        Debug.WriteLine($"[DEBUG] WriteModifiedChannels called: targetChannel='{targetChannel}', value={value}, channelsType={channels.ValueKind}");

        // Channels are stored as an array of channel objects
        if (channels.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();

            foreach (JsonElement channelElement in channels.EnumerateArray())
            {
                writer.WriteStartObject();

                // Get the channel name to check if this is the target channel
                string? channelName = null;
                if (channelElement.TryGetProperty("name", out JsonElement nameElement))
                {
                    channelName = nameElement.GetString();
                }

                bool isTargetChannel = channelName != null &&
                    channelName.Equals(targetChannel, StringComparison.OrdinalIgnoreCase);

                // Write all properties, modifying "value" if this is the target channel
                foreach (JsonProperty prop in channelElement.EnumerateObject())
                {
                    writer.WritePropertyName(prop.Name);

                    if (isTargetChannel && prop.Name.Equals("value", StringComparison.OrdinalIgnoreCase))
                    {
                        // Update the value property with the new value as a string
                        Debug.WriteLine($"[DEBUG] Updating channel '{channelName}' value from '{prop.Value}' to '{value}'");
                        writer.WriteStringValue(value.ToString());
                    }
                    else
                    {
                        // Write the property unchanged
                        WriteJsonValue(prop.Value, writer);
                    }
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            Debug.WriteLine($"[DEBUG] WriteModifiedChannels completed (array format)");
        }
        else if (channels.ValueKind == JsonValueKind.Object)
        {
            // Legacy format: channels as object/dictionary
            writer.WriteStartObject();

            bool channelFound = false;
            foreach (JsonProperty channel in channels.EnumerateObject())
            {
                writer.WritePropertyName(channel.Name);

                if (channel.Name.Equals(targetChannel, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"[DEBUG] Found matching channel '{channel.Name}', updating from {channel.Value} to {value}");
                    writer.WriteNumberValue(value);
                    channelFound = true;
                }
                else
                {
                    WriteJsonValue(channel.Value, writer);
                }
            }

            // If the channel doesn't exist, add it
            if (!channelFound)
            {
                Debug.WriteLine($"[DEBUG] Channel '{targetChannel}' not found, adding it with value {value}");
                writer.WritePropertyName(targetChannel);
                writer.WriteNumberValue(value);
            }

            writer.WriteEndObject();
            Debug.WriteLine($"[DEBUG] WriteModifiedChannels completed (object format)");
        }
        else
        {
            Debug.WriteLine($"[DEBUG] WriteModifiedChannels: unexpected channels type {channels.ValueKind}, writing as-is");
            WriteJsonValue(channels, writer);
        }
    }

    private string FindScenesFile()
    {
        // ALWAYS use AppData folder - this is where the application stores scene data
        // This ensures consistency with MainViewModel, SceneRepository, and other services
        var appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InterdisciplinairProject");
        
        Directory.CreateDirectory(appFolder);
        var scenesPath = Path.Combine(appFolder, "scenes.json");

        Debug.WriteLine($"[DEBUG] Using AppData scenes path: {scenesPath}");
        return scenesPath;
    }
}
