using Show.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Show
{
    public class SceneExtractor
    {
        /// <summary>
        /// Extracts a Scene object from a JSON file.
        /// </summary>
        /// <param name="jsonFilePath">The path to the JSON file containing the scene.</param>
        /// <returns>The deserialized Scene object.</returns>
        public static Scene ExtractScene(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine($"File not found: {jsonFilePath}");
                return new Scene();
            }

            string jsonString = File.ReadAllText(jsonFilePath);

            try
            {
                using JsonDocument doc = JsonDocument.Parse(jsonString);
                if (!doc.RootElement.TryGetProperty("scene", out JsonElement sceneElement))
                {
                    Console.WriteLine("Scene property missing.");
                    return new Scene();
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
                        if (fixture.TryGetProperty("channels", out JsonElement channels))
                        {
                            if (channels.TryGetProperty("dimmer", out JsonElement dimmerElem))
                            {
                                scene.Dimmer = dimmerElem.GetInt32();
                                break; // Only set the first found dimmer
                            }
                        }
                    }
                }

                return scene;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting scene: {ex.Message}");
                return new Scene();
            }
        }
    }
}
