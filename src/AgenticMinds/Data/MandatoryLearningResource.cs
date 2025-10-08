namespace AgenticMinds.Data
{
    /// <summary>
    /// Represents a mandatory learning resource, including its metadata, content, and type.
    /// </summary>
    public class MandatoryLearningResource
    {
        /// <summary>
        /// Gets or sets the title of the resource.
        /// </summary>
        public string Title { get; set; } = string.Empty; // Metadata title

        /// <summary>
        /// Gets or sets the URI of the resource content in the blob storage.
        /// </summary>
        public string ContentUri { get; set; } = string.Empty; // Blob URI

        /// <summary>
        /// Gets or sets the content of the resource.
        /// </summary>
        public string Content { get; set; } = string.Empty; // Blob content

        /// <summary>
        /// Gets or sets the type of the resource (e.g., "PDF", "text").
        /// </summary>
        public string Type { get; set; } = string.Empty; // Type of content (e.g., PDF, text, etc.)
    }
}
