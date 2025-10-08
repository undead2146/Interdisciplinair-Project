using Show.Model;
using System;
using System.IO;
using System.Text.Json;

namespace Show
{
    public class SceneExtractor
    {
        /// <summary>
        /// Extracts a Scene object from a JSON file.
        /// Throws descriptive exceptions if something goes wrong.
        /// </summary>
        /// <param name="jsonFilePath">The path to the JSON file containing the scene.</param>
        /// <returns>The deserialized Scene object.</returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="JsonException"></exception>
        public static Scene ExtractScene(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException(
                    $"The selected file could not be found: {jsonFilePath}");
            }

            string jsonString;
            try
            {
                jsonString = File.ReadAllText(jsonFilePath);
            }
            catch (IOException ex)
            {
                throw new IOException(
                    $"An error occurred while reading the file: {ex.Message}", ex);
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(jsonString);

                if (!doc.RootElement.TryGetProperty("scene", out JsonElement sceneElement))
                {
                    throw new InvalidDataException(
                        "The JSON file does not contain a 'scene' property.");
                }

                var scene = new Scene
                {
                    Id = sceneElement.TryGetProperty("id", out var idElem) ? idElem.GetString() ?? "" : "",
                    Name = sceneElement.TryGetProperty("name", out var nameElem) ? nameElem.GetString() ?? "" : "",
                    Dimmer = 0
                };

                if (sceneElement.TryGetProperty("fixtures", out JsonElement fixtures) &&
                    fixtures.ValueKind == JsonValueKind.Array)
                {
                    foreach (var fixture in fixtures.EnumerateArray())
                    {
                        if (fixture.TryGetProperty("channels", out JsonElement channels) &&
                            channels.TryGetProperty("dimmer", out JsonElement dimmerElem))
                        {
                            scene.Dimmer = dimmerElem.GetInt32();
                            break;
                        }
                    }
                }

                return scene;
            }
            catch (JsonException ex)
            {
                throw new JsonException(
                    $"The file contains invalid JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"An unexpected error occurred: {ex.Message}", ex);
            }
        }
    }
}
