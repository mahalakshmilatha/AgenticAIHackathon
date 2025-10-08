using Microsoft.SemanticKernel;

namespace AgenticMinds.ProcessSteps.ProcessStates;

/// <summary>
/// Represents the state of a process step that involves chat interactions.
/// This class is used to store the history of chat messages exchanged during the process.
/// </summary>
public class ChatLogState
{
    /// <summary>
    /// Gets or sets the chat log, which contains a list of chat messages exchanged during the process.
    /// </summary>
    public List<ChatMessageContent> ChatLog { get; set; } = new();
}
