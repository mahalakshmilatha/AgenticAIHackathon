using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;

namespace AgenticMinds.Agents.Helper;

/// <summary>
/// A helper class providing utility methods for managing agent interactions and user input.
/// </summary>
public static class AgentHelper
{
    /// <summary>
    /// Creates an AgentGroupChat instance with a given set of agents and chat history.
    /// </summary>
    /// <param name="agents">A collection of ChatCompletionAgents to include in the group chat.</param>
    /// <param name="history">A collection of previous chat messages to restore the chat history.</param>
    /// <param name="settings">Optional settings for the AgentGroupChat, such as selection strategy.</param>
    /// <returns>An initialized AgentGroupChat instance with the provided agents and history.</returns>
    public static AgentGroupChat CreateAgentGroupChatWithHistory(
        IEnumerable<ChatCompletionAgent> agents,
        IEnumerable<ChatMessageContent> history,
        AgentGroupChatSettings? settings = null)
    {
        // Initialize the group chat with the provided agents and settings
        var chat = new AgentGroupChat(agents.ToArray())
        {
            ExecutionSettings = settings ?? new AgentGroupChatSettings
            {
                SelectionStrategy = new SequentialSelectionStrategy()
            }
        };

        // Add the provided chat history to the group chat
        foreach (var message in history)
        {
            chat.AddChatMessage(message);
        }

        return chat;
    }

    /// <summary>
    /// Extracts a JSON string from a given response by identifying the first '{' and the last '}'.
    /// </summary>
    /// <param name="response">The response string containing potential JSON content.</param>
    /// <returns>A JSON string if found; otherwise, an empty string.</returns>
    public static string ExtractJsonFromResponse(string response)
    {
        // Find the first occurrence of '{' and the last occurrence of '}'
        int startIndex = response.IndexOf('{');
        int endIndex = response.LastIndexOf('}');

        // If both indices are valid, extract the JSON substring
        if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
        {
            return response.Substring(startIndex, endIndex - startIndex + 1);
        }

        // Return an empty string if no valid JSON is found
        return string.Empty;
    }

    /// <summary>
    /// Logs a message from an agent to the console or a logging framework.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void LogAgentMessage(string message)
    {
        // Log the message with an "[AGENT]" prefix for clarity
        Console.WriteLine("[AGENT] " + message);
    }

    /// <summary>
    /// Prompts the user for input and returns their response as a trimmed, lowercase string.
    /// </summary>
    /// <returns>The user's input as a trimmed, lowercase string.</returns>
    public static string GetUserMessage()
    {
        // Display a prompt to the user
        Console.Write(">> ");

        // Read and return the user's input, trimmed and converted to lowercase
        var answer = Console.ReadLine()?.Trim().ToLower();
        return answer!;
    }
}
