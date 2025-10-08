using System.Text.Json;
using AgenticMinds.Data;

namespace AgenticMinds.Agents.Helper;

/// <summary>
/// A utility class for managing the persistence of user progress in a JSON file.
/// </summary>
public static class ProgressStorage
{
    /// <summary>
    /// The file path where the progress data is stored.
    /// </summary>
    private static readonly string FilePath = "progress.json";

    /// <summary>
    /// Saves the user's progress to a JSON file.
    /// </summary>
    /// <param name="progress">The progress state to save, including learning type and plan.</param>
    public static void Save(ProgressState progress)
    {
        // Serialize the progress state to a JSON string with indentation for readability.
        var json = JsonSerializer.Serialize(progress, new JsonSerializerOptions { WriteIndented = true });

        // Write the JSON string to the specified file path.
        File.WriteAllText(FilePath, json);
    }

    /// <summary>
    /// Loads the user's progress from the JSON file.
    /// </summary>
    /// <returns>The deserialized ProgressState object if the file exists; otherwise, null.</returns>
    public static ProgressState? Load()
    {
        // Check if the progress file exists.
        if (!File.Exists(FilePath)) return null;

        // Read the JSON content from the file.
        var json = File.ReadAllText(FilePath);

        // Deserialize the JSON content into a ProgressState object and return it.
        return JsonSerializer.Deserialize<ProgressState>(json);
    }

    /// <summary>
    /// Deletes the progress file, effectively resetting the user's progress.
    /// </summary>
    public static void Delete()
    {
        // Check if the progress file exists and delete it if it does.
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }
}

