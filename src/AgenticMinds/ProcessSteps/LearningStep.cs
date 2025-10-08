using AgenticMinds.Agents;
using AgenticMinds.Agents.Helper;
using AgenticMinds.Data;
using AgenticMinds.ProcessSteps.ProcessStates;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgenticMinds.ProcessSteps;

/// <summary>
/// Represents the LearningStep, which is responsible for guiding the user through their learning resources.
/// </summary>
public class LearningStep : KernelProcessStep<LearningState>
{
    private LearningState _state = new(); // Stores the state of the learning step.
    private readonly LearningAgent _tutorAgent; // The agent responsible for tutoring the user.

    /// <summary>
    /// Initializes a new instance of the LearningStep class with the specified LearningAgent.
    /// </summary>
    /// <param name="tutorAgent">The agent responsible for tutoring the user.</param>
    public LearningStep(LearningAgent tutorAgent)
    {
        _tutorAgent = tutorAgent;
    }

    /// <summary>
    /// Activates the step and restores its state.
    /// </summary>
    /// <param name="state">The state to restore for this step.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    public override ValueTask ActivateAsync(KernelProcessStepState<LearningState> state)
    {
        _state = state.State!;
        return base.ActivateAsync(state);
    }

    /// <summary>
    /// Guides the user through their learning resources, allowing them to interact with the tutor agent.
    /// </summary>
    /// <param name="kernel">The kernel instance for executing the process step.</param>
    /// <param name="context">The context of the process step.</param>
    /// <param name="learningPlan">The learning plan containing the user's resources.</param>
    [KernelFunction("Learn")]
    public async Task LearnAsync(Kernel kernel, KernelProcessStepContext context, LearningPlan learningPlan)
    {
        // Get the first incomplete resource from the learning plan.
        var resource = learningPlan.Resources.FirstOrDefault(x => !x.IsComplete);

        // If all resources are complete, emit the LearningCompleted event and exit.
        if (resource == null)
        {
            await context.EmitEventAsync(ProcessEventNames.LearningCompleted, learningPlan);
            return;
        }

        // Ensure chat history exists.
        _state.ChatLog ??= new List<ChatMessageContent>();

        // Create and restore the chat with the tutor agent.
        var chat = AgentHelper.CreateAgentGroupChatWithHistory(
            [_tutorAgent.Agent],
            _state.ChatLog,
            new AgentGroupChatSettings
            {
                SelectionStrategy = new SequentialSelectionStrategy()
            });

        // Provide details about the resource to the user.
        string webResourceMessage = $"""
            The resource to use is:
            Title: {resource.Title}
            URL: {resource.Url}
            Description: {resource.Description}
            """;

        chat.AddChatMessage(new ChatMessageContent(AuthorRole.Assistant, webResourceMessage));

        // Engage in a back-and-forth chat with the user.
        string? response;
        do
        {
            // Display messages from the chat.
            await foreach (var message in chat.InvokeAsync())
            {
                AgentHelper.LogAgentMessage(message.Content!);
            }

            // Get the user's response.
            response = AgentHelper.GetUserMessage()?.Trim();

            // Handle user responses to continue or stop learning.
            if (response!.Equals("continue", StringComparison.OrdinalIgnoreCase))
            {
                CompleteResourceAndSaveProgress(learningPlan, resource);
                await context.EmitEventAsync(ProcessEventNames.ContinueLearning, learningPlan);
                return; // Exit the method.
            }
            else if (response.Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                CompleteResourceAndSaveProgress(learningPlan, resource);
                await context.EmitEventAsync(ProcessEventNames.StopLearning, learningPlan);
                return; // Exit the method.
            }

            // If the response is null or empty, exit the loop.
            if (string.IsNullOrWhiteSpace(response))
            {
                break;
            }

            // Add the user's response to the chat.
            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, response));

        } while (true);
    }

    /// <summary>
    /// Marks the specified resource as complete and saves the updated learning plan to persistent storage.
    /// </summary>
    /// <param name="learningPlan">The learning plan to update.</param>
    /// <param name="resource">The resource to mark as complete.</param>
    private void CompleteResourceAndSaveProgress(LearningPlan learningPlan, Resource resource)
    {
        resource.IsComplete = true;

        ProgressStorage.Save(new ProgressState
        {
            LearningType = LearningType.New,
            LearningPlan = learningPlan
        });
    }
}
