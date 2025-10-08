namespace AgenticMinds.Data;


/// <summary>
/// Represents a learning plan, which contains a list of resources for the user to study.
/// </summary>
public class LearningPlan
{
    /// <summary>
    /// Gets or sets the list of resources included in the learning plan.
    /// </summary>
    public List<Resource> Resources { get; set; } = new();

    /// <summary>
    /// Returns a string representation of the learning plan, listing all resources.
    /// </summary>
    /// <returns>A string containing details of all resources in the learning plan.</returns>
    public override string ToString()
    {
        if (Resources == null || !Resources.Any())
        {
            return "No resources available.";
        }

        return string.Join(Environment.NewLine, Resources.Select(r => r.ToString()));
    }

    /// <summary>
    /// Returns a user-friendly string representation of the learning plan for display purposes.
    /// </summary>
    /// <returns>A formatted string containing details of all resources in the learning plan.</returns>
    public string ToDisplayString()
    {
        if (Resources == null || !Resources.Any())
        {
            return "No resources available.";
        }

        return string.Join(Environment.NewLine, Resources.Select(r => r.ToDisplayString()));
    }
}

/// <summary>
/// Represents a resource in the learning plan, such as a video, article, or book.
/// </summary>
public class Resource
{
    /// <summary>
    /// Gets or sets the unique identifier for the resource.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the title of the resource.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of the resource.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the resource (e.g., "video", "article", "book").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the resource.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated time in minutes required to complete the resource.
    /// Nullable to support legacy data or manual fallback.
    /// </summary>
    public int? EstimatedMinutes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the resource has been completed by the user.
    /// </summary>
    public bool IsComplete { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the resource is part of the exam scope.
    /// </summary>
    public bool IsExamScope { get; set; } = true;

    /// <summary>
    /// Returns a string representation of the resource, including its details.
    /// </summary>
    /// <returns>A string containing the resource's details.</returns>
    public override string ToString()
    {
        return $"""
            Id: {Id}
            Title: {Title}
            URL: {Url}
            Description: {Description}
            EstimatedMinutes: {EstimatedMinutes}
            """;
    }

    /// <summary>
    /// Returns a user-friendly string representation of the resource for display purposes.
    /// </summary>
    /// <returns>A formatted string containing the resource's details.</returns>
    public string ToDisplayString()
    {
        return $"Title: {Title}\n" +
            $"  - Url: {Url}\n" +
            $"  - Type: {Type}\n" +
            $"  - Description: {Description}\n" +
            $"  - Estimated Minutes: {EstimatedMinutes}\n" +
            $"  - Completed: {IsComplete}";
    }
}
