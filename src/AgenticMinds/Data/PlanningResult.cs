namespace AgenticMinds.Data;

/// <summary>
/// Represents the result of the planning process, which includes the user's learning preferences
/// and the generated learning plan.
/// </summary>
public class PlanningResult
{
    /// <summary>
    /// Gets or sets the user's learning preferences, such as preferred learning style, study time, and goals.
    /// </summary>
    public LearningPreferences LearningPreferences { get; set; } = new();

    /// <summary>
    /// Gets or sets the learning plan generated based on the user's preferences and assessment results.
    /// </summary>
    public LearningPlan LearningPlan { get; set; } = new();
}

