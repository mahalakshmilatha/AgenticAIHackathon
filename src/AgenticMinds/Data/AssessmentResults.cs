namespace AgenticMinds.Data;

/// <summary>
/// Represents the results of an assessment, including details about the student, assessment, and performance.
/// </summary>
public class AssessmentResults
{
    /// <summary>
    /// Gets or sets the unique identifier for the student who took the assessment.
    /// </summary>
    public string StudentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for the assessment.
    /// </summary>
    public string AssessmentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject of the assessment (e.g., "Mathematics", "C#").
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scores for the assessment, broken down by categories (e.g., "Beginner", "Intermediate", "Advanced").
    /// </summary>
    public Dictionary<string, string> Score { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the date and time when the assessment was completed.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the feedback provided for the assessment, summarizing the student's performance.
    /// </summary>
    public string Feedback { get; set; } = string.Empty;
}
