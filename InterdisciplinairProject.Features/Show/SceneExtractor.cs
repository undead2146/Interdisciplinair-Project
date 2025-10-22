using System.Text.Json;

#pragma warning disable SA1600

namespace InterdisciplinairProject.Features.Show;

public class SceneExtractor
{
    public static Core.Models.Scene ExtractScene(string jsonFilePath)
    {
        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"The selected file could not be found: {jsonFilePath}");
        }

        string jsonString;

        try
        {
            jsonString = File.ReadAllText(jsonFilePath);

            var doc = JsonDocument.Parse(jsonString);
            if (!doc.RootElement.TryGetProperty("scene", out var sceneElement))
            {
                throw new InvalidDataException("The JSON file does not contain a 'scene' property.");
            }

            var scene = JsonSerializer.Deserialize<Core.Models.Scene>(sceneElement.GetRawText());

            return scene == null ? throw new InvalidDataException("Failed to deserialize the 'scene' property.") : scene;
        }
        catch (FileNotFoundException ex)
        {
            throw new FileNotFoundException($"JSON file not found: {ex.Message}", ex);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException($"Invalid data in the file: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new JsonException($"The file contains invalid JSON: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new IOException($"Error reading the file: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"An unexpected error occurred: {ex.Message}", ex);
        }
    }
}
