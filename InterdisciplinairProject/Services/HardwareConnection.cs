using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using InterdisciplinairProject.Core.Interfaces;

namespace InterdisciplinairProject.Services;

/// <summary>
/// Provides hardware connection functionality for controlling lighting fixtures.
/// </summary>
public class HardwareConnection : IHardwareConnection
{
    private readonly string _scenesFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="HardwareConnection"/> class.
    /// </summary>
    public HardwareConnection()
    {
        Debug.WriteLine("[DEBUG] HardwareConnection constructor called");
        string appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InterdisciplinairProject");
        Debug.WriteLine($"[DEBUG] HardwareConnection app folder: {appFolder}");

        // Ensure the directory exists
        Directory.CreateDirectory(appFolder);
        Debug.WriteLine("[DEBUG] HardwareConnection directory created/verified");

        _scenesFilePath = Path.Combine(appFolder, "scenes.json");
        Debug.WriteLine($"[DEBUG] HardwareConnection scenes file path: {_scenesFilePath}");

        // Initialize scenes.json if missing: try several candidates; otherwise create minimal scaffold
        if (!File.Exists(_scenesFilePath))
        {
            Debug.WriteLine("[DEBUG] HardwareConnection scenes.json not found, attempting to copy from candidates");
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "InterdisciplinairProject.Features", "Scene", "data", "scenes.json"),
                Path.Combine(AppContext.BaseDirectory, "scenes.json"),
            };
            foreach (var src in candidates)
            {
                if (File.Exists(src))
                {
                    File.Copy(src, _scenesFilePath);
                    Debug.WriteLine($"[DEBUG] HardwareConnection copied scenes.json from: {src}");
                    break;
                }
            }

            if (!File.Exists(_scenesFilePath))
            {
                Debug.WriteLine("[DEBUG] HardwareConnection creating default scenes.json");
                File.WriteAllText(_scenesFilePath, "{\n  \"scene\": { \"id\": \"default\", \"name\": \"Default\", \"universe\": 1, \"fixtures\": [] }\n}");
            }
        }
        else
        {
            Debug.WriteLine("[DEBUG] HardwareConnection scenes.json already exists");
        }
        Debug.WriteLine("[DEBUG] HardwareConnection initialization complete");
    }

    /// <summary>
    /// Sends a value to a specific channel of a fixture asynchronously.
    /// </summary>
    /// <param name="fixtureInstanceId">The instance ID of the fixture.</param>
    /// <param name="channelName">The name of the channel (e.g. "dimmer").</param>
    /// <param name="value">The value between 0 and 255.</param>
    /// <returns>True if successful, otherwise false.</returns>
    public async Task<bool> SetChannelValueAsync(string fixtureInstanceId, string channelName, byte value)
    {
        Debug.WriteLine($"[DEBUG] SetChannelValueAsync called: fixture={fixtureInstanceId}, channel={channelName}, value={value}");
        try
        {
            // Valideer de waarde (0-255)
            if (value < 0 || value > 255)
            {
                Debug.WriteLine($"[DEBUG] SetChannelValueAsync validation failed: value {value} out of range");
                throw new ArgumentOutOfRangeException(nameof(value), "Waarde moet tussen 0 en 255 zijn");
            }

            Debug.WriteLine("[DEBUG] SetChannelValueAsync validation passed");

            // Lees het scenes bestand
            if (!File.Exists(_scenesFilePath))
            {
                Debug.WriteLine($"[DEBUG] SetChannelValueAsync scenes file not found: {_scenesFilePath}");
                throw new FileNotFoundException($"Scenes bestand niet gevonden: {_scenesFilePath}");
            }

            Debug.WriteLine($"[DEBUG] SetChannelValueAsync reading scenes file: {_scenesFilePath}");

            string jsonContent = await File.ReadAllTextAsync(_scenesFilePath);
            Debug.WriteLine($"[DEBUG] SetChannelValueAsync read {jsonContent.Length} characters from scenes file");

            // Parse JSON met System.Text.Json
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };

            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            using MemoryStream stream = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

            // Schrijf de aangepaste JSON
            Debug.WriteLine("[DEBUG] SetChannelValueAsync writing modified JSON");
            WriteModifiedJson(doc.RootElement, writer, fixtureInstanceId, channelName, value);

            writer.Flush();

            // Schrijf terug naar bestand
            string updatedJson = Encoding.UTF8.GetString(stream.ToArray());
            Debug.WriteLine($"[DEBUG] SetChannelValueAsync writing {updatedJson.Length} characters back to scenes file");
            await File.WriteAllTextAsync(_scenesFilePath, updatedJson);

            Debug.WriteLine("[DEBUG] SetChannelValueAsync completed successfully");
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
            return instanceId.GetString() == targetInstanceId;
        }

        return false;
    }

    /// <summary>
    /// Writes the modified channels to the writer.
    /// </summary>
    /// <param name="channels">The channels JSON element.</param>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="targetChannel">The target channel name.</param>
    /// <param name="value">The value to set.</param>
    private void WriteModifiedChannels(
        JsonElement channels,
        Utf8JsonWriter writer,
        string targetChannel,
        byte value)
    {
        writer.WriteStartObject();

        bool channelFound = false;
        foreach (JsonProperty channel in channels.EnumerateObject())
        {
            writer.WritePropertyName(channel.Name);

            if (channel.Name.Equals(targetChannel, StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteNumberValue(value);
                channelFound = true;
            }
            else
            {
                WriteJsonValue(channel.Value, writer);
            }
        }

        // Als het kanaal nog niet bestaat, voeg het toe
        if (!channelFound)
        {
            writer.WritePropertyName(targetChannel);
            writer.WriteNumberValue(value);
        }

        writer.WriteEndObject();
    }
}
