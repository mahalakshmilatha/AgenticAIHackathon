namespace AgenticMinds.Data;

/// <summary>
/// Represents the learning preferences of a user, including their preferred learning style,
/// study time, and learning goals.
/// </summary>
public class LearningPreferences
{
    /// <summary>
    /// Gets or sets the user's preferred learning style (e.g., "visual", "auditory", "kinesthetic").
    /// </summary>
    public string PreferredLearningStyle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's preferred time for studying (e.g., "morning", "afternoon", "evening").
    /// </summary>
    public string PreferredStudyTime { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's learning goals (e.g., "master C#", "improve problem-solving skills").
    /// </summary>
    public string LearningGoals { get; set; } = string.Empty;
}
