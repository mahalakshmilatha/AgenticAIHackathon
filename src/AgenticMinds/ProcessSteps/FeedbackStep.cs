using AgenticMinds.Agents;
using AgenticMinds.Agents.Helper;
using AgenticMinds.Data;
using AgenticMinds.ProcessSteps.ProcessStates;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgenticMinds.ProcessSteps;

/// <summary>
/// Represents the FeedbackStep, which is responsible for collecting feedback
/// on the user's assessment results and providing constructive insights.
/// </summary>
public class FeedbackStep : KernelProcessStep<FeedbackState>
{
    private FeedbackState _state = new(); // Stores the state of the feedback step.
    private readonly FeedbackAgent _feedbackAgent; // The agent responsible for generating feedback.

    /// <summary>
    /// Initializes a new instance of the FeedbackStep class with the specified FeedbackAgent.
    /// </summary>
    /// <param name="feedbackAgent">The agent responsible for generating feedback.</param>
    public FeedbackStep(FeedbackAgent feedbackAgent)
    {
        _feedbackAgent = feedbackAgent;
    }

    /// <summary>
    /// Activates the step and restores its state.
    /// </summary>
    /// <param name="state">The state to restore for this step.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    public override ValueTask ActivateAsync(KernelProcessStepState<FeedbackState> state)
    {
        _state = state.State!;
        return base.ActivateAsync(state);
    }

    /// <summary>
    /// Collects feedback on the user's assessment results by interacting with the user through a chat interface.
    /// </summary>
    /// <param name="kernel">The kernel instance for executing the process step.</param>
    /// <param name="context">The context of the process step.</param>
    /// <param name="assessmentResults">The results of the user's assessment, including scores and subject details.</param>
    [KernelFunction("Feedback")]
    public async Task FeedbackAsync(Kernel kernel, KernelProcessStepContext context, AssessmentResults assessmentResults)
    {
        // Ensure chat history exists.
        _state.ChatLog ??= new List<ChatMessageContent>();

        // Create and restore the chat with the feedback agent.
        var chat = AgentHelper.CreateAgentGroupChatWithHistory(
            [_feedbackAgent.Agent],
            _state.ChatLog,
            new AgentGroupChatSettings
            {
                SelectionStrategy = new SequentialSelectionStrategy()
            });

        // Format the assessment results to display to the user.
        string assessmentResult = $"""
            The assessment results are:
            Subject: {assessmentResults.Subject}
            Score: {string.Join(", ", assessmentResults.Score.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}
            """;

        // Add the assessment results to the chat.
        chat.AddChatMessage(new ChatMessageContent(AuthorRole.Assistant, assessmentResult));

        // Process the chat messages and collect feedback.
        await foreach (var message in chat.InvokeAsync())
        {
            // Log the feedback message.
            AgentHelper.LogAgentMessage(message.Content!);

            // Store the feedback in the assessment results.
            assessmentResults.Feedback = message.Content!;
        }

        // Emit the FeedbackCompleted event with the updated assessment results.
        await context.EmitEventAsync(ProcessEventNames.FeedbackCompleted, assessmentResults);
    }
}
