using AgenticMinds.Agents;
using AgenticMinds.Agents.Helper;
using AgenticMinds.Data;
using AgenticMinds.ProcessSteps.ProcessStates;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgenticMinds.ProcessSteps;

/// <summary>
/// Represents the ExaminationFeedbackStep, which is responsible for collecting feedback
/// on the examination process and results.
/// </summary>
public class ExaminationFeedbackStep : KernelProcessStep<ExaminationFeedbackState>
{
    private ExaminationFeedbackState _state = new(); // Stores the state of the examination feedback step.
    private readonly FeedbackAgent _feedbackAgent; // The agent responsible for generating feedback.

    /// <summary>
    /// Initializes a new instance of the ExaminationFeedbackStep class with the specified FeedbackAgent.
    /// </summary>
    /// <param name="feedbackAgent">The agent responsible for generating feedback.</param>
    public ExaminationFeedbackStep(FeedbackAgent feedbackAgent)
    {
        _feedbackAgent = feedbackAgent;
    }

    /// <summary>
    /// Activates the step and restores its state.
    /// </summary>
    /// <param name="state">The state to restore for this step.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    public override ValueTask ActivateAsync(KernelProcessStepState<ExaminationFeedbackState> state)
    {
        _state = state.State!;
        return base.ActivateAsync(state);
    }

    /// <summary>
    /// Collects feedback on the examination process and results by interacting with the user through a chat interface.
    /// </summary>
    /// <param name="kernel">The kernel instance for executing the process step.</param>
    /// <param name="context">The context of the process step.</param>
    /// <param name="examinationResults">The results of the examination, including resources and scores.</param>
    [KernelFunction("ExaminationFeedback")]
    public async Task ExaminatonFeedbackAsync(Kernel kernel, KernelProcessStepContext context, ExaminationResult examinationResults)
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

        // Iterate over the examination resources to extract and display scores.
        foreach (var resource in examinationResults.Resources)
        {
            string examinationResult = $"""
               The assessment results are:  
               Id of resource: {resource.Id}  
               Title of resource: {resource.Title}
               Score: {resource.Score}  
               """;

            // Add the resource details to the chat.
            chat.AddChatMessage(new ChatMessageContent(AuthorRole.Assistant, examinationResult));
        }

        // Process the chat messages and collect feedback.
        await foreach (var message in chat.InvokeAsync())
        {
            Console.WriteLine(message.Content); // Log the feedback message to the console.

            // Store the feedback in the examination results.
            examinationResults.Feedback = message.Content!;
        }

        // Emit the ExaminationFeedbackCompleted event with the updated examination results.
        await context.EmitEventAsync(ProcessEventNames.ExaminationFeedbackCompleted, examinationResults);
    }
}
