using System.Text.Json.Serialization;

namespace AgenticMinds.Data;

/// <summary>
/// Represents the progress state of a user's learning journey, including the type of learning
/// and the associated learning plan.
/// </summary>
public class ProgressState
{
    /// <summary>
    /// Gets or sets the type of learning (e.g., "New" or "Mandatory").
    /// </summary>
    public LearningType LearningType { get; set; }

    /// <summary>
    /// Gets or sets the learning plan associated with the user's progress.
    /// </summary>
    public LearningPlan LearningPlan { get; set; } = new LearningPlan();
}

/// <summary>
/// Represents the type of learning a user is engaged in.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LearningType
{
    /// <summary>
    /// Represents a new learning journey initiated by the user.
    /// </summary>
    New,

    /// <summary>
    /// Represents mandatory learning resources that the user is required to complete.
    /// </summary>
    Mandatory
}
