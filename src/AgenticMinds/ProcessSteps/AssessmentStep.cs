using System.Text.Json;
using AgenticMinds.Agents;
using AgenticMinds.Agents.Helper;
using AgenticMinds.Data;
using AgenticMinds.ProcessSteps.ProcessStates;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgenticMinds.ProcessSteps;

/// <summary>
/// Represents the AssessmentStep, which is responsible for conducting knowledge assessments
/// to evaluate the student's current competency level in a specific subject.
/// </summary>
public class AssessmentStep : KernelProcessStep<AssessmentState>
{
    private AssessmentState _state = new(); // Stores the state of the assessment step.
    private readonly AssessmentAgent _assessmentAgent; // The agent responsible for conducting assessments.

    /// <summary>
    /// Initializes a new instance of the AssessmentStep class with the specified AssessmentAgent.
    /// </summary>
    /// <param name="assessmentAgent">The agent responsible for conducting assessments.</param>
    public AssessmentStep(AssessmentAgent assessmentAgent)
    {
        _assessmentAgent = assessmentAgent;
    }

    /// <summary>
    /// Activates the step and restores its state.
    /// </summary>
    /// <param name="state">The state to restore for this step.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    public override ValueTask ActivateAsync(KernelProcessStepState<AssessmentState> state)
    {
        _state = state.State!;
        return base.ActivateAsync(state);
    }

    /// <summary>
    /// Conducts the assessment by interacting with the user through a chat interface.
    /// </summary>
    /// <param name="kernel">The kernel instance for executing the process step.</param>
    /// <param name="context">The context of the process step.</param>
    [KernelFunction("Assess")]
    public async Task AssessAsync(Kernel kernel, KernelProcessStepContext context)
    {
        // Ensure chat history exists.
        _state.ChatLog ??= new List<ChatMessageContent>();

        // Create and restore the chat with the assessment agent.
        var chat = AgentHelper.CreateAgentGroupChatWithHistory(
            [_assessmentAgent.Agent],
            _state.ChatLog,
            new AgentGroupChatSettings
            {
                SelectionStrategy = new SequentialSelectionStrategy()
            });

        // Initialize an empty AssessmentResults object to store the results.
        var assessmentResults = new AssessmentResults();

        string? response;
        bool isAssessmentCompleted = false;

        try
        {
            // Log initial messages from the chat.
            await foreach (var message in chat.InvokeAsync())
            {
                AgentHelper.LogAgentMessage(message.Content!);
            }
        }
        catch (Exception ex)
        {
            // Log any errors that occur during chat invocation.
            AgentHelper.LogAgentMessage($"Error during chat invocation: {ex.Message}");
        }

        do
        {
            // Prompt the user for input and process their response.
            response = AgentHelper.GetUserMessage();
            if (string.IsNullOrWhiteSpace(response)) // Check for null or empty input.
            {
                break; // Exit the loop.
            }

            // Add the user's response to the chat.
            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, response));
            await foreach (var message in chat.InvokeAsync())
            {
                if (message.Content!.Contains("[AssessmentResult]"))
                {
                    // Extract JSON content from the message.
                    var jsonContent = AgentHelper.ExtractJsonFromResponse(message.Content);

                    // Parse the JSON content and populate the AssessmentResults object.
                    try
                    {
                        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);

                        if (data != null)
                        {
                            if (data.TryGetValue("StudentId", out var studentId))
                                assessmentResults.StudentId = studentId.GetString()!;

                            if (data.TryGetValue("AssessmentId", out var assessmentId))
                                assessmentResults.AssessmentId = assessmentId.GetString()!;

                            if (data.TryGetValue("Subject", out var subject))
                                assessmentResults.Subject = subject.GetString()!;

                            if (data.TryGetValue("Score", out var score) && score.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var scoreEntry in score.EnumerateObject())
                                {
                                    assessmentResults.Score[scoreEntry.Name] = scoreEntry.Value.GetString()!;
                                }
                            }

                            assessmentResults.Date = DateTime.UtcNow;
                        }
                    }
                    catch (JsonException ex)
                    {
                        // Log any errors that occur during JSON parsing.
                        AgentHelper.LogAgentMessage($"Failed to parse message content: {ex.Message}");
                    }

                    // Emit the AssessmentCompleted event with the results and mark the assessment as completed.
                    await context.EmitEventAsync(ProcessEventNames.AssessmentCompleted, assessmentResults);
                    isAssessmentCompleted = true;
                }
                else
                {
                    // Log other messages from the chat.
                    AgentHelper.LogAgentMessage(message.Content);
                }
            }

        } while (!isAssessmentCompleted); // Repeat until the assessment is completed.
    }
}
