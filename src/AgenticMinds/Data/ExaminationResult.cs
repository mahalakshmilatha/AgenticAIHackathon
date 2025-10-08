namespace AgenticMinds.Data
{
    /// <summary>
    /// Represents the result of an examination, including the resources evaluated, the overall status, and feedback.
    /// </summary>
    public class ExaminationResult
    {
        /// <summary>
        /// Gets or sets the list of resources that were part of the examination.
        /// Each resource includes details such as its ID, title, and score.
        /// </summary>
        public List<ExaminationResource> Resources { get; set; } = new();

        /// <summary>
        /// Gets or sets the overall status of the examination (e.g., "Passed", "Failed").
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the feedback provided for the examination, summarizing the user's performance.
        /// </summary>
        public string Feedback { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a resource that was part of the examination, including its ID, score, and title.
    /// </summary>
    public class ExaminationResource
    {
        /// <summary>
        /// Gets or sets the unique identifier for the resource.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the score achieved for this resource during the examination.
        /// </summary>
        public string? Score { get; set; }

        /// <summary>
        /// Gets or sets the title of the resource.
        /// </summary>
        public string? Title { get; set; }
    }
}
