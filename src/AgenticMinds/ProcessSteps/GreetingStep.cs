using System.Globalization;
using AgenticMinds.Agents.Helper;
using AgenticMinds.Data;
using AgenticMinds.ProcessSteps.ProcessStates;
using Microsoft.SemanticKernel;

namespace AgenticMinds.ProcessSteps;

/// <summary>
/// Represents the GreetingStep, which is responsible for welcoming the user,
/// handling resumption of previous progress, or starting a new learning session.
/// </summary>
public class GreetingStep : KernelProcessStep<GreetingState>
{
    private GreetingState _state = new(); // Stores the state of the greeting step.

    /// <summary>
    /// Activates the step and restores its state.
    /// </summary>
    /// <param name="state">The state to restore for this step.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    public override ValueTask ActivateAsync(KernelProcessStepState<GreetingState> state)
    {
        _state = state.State!;
        return base.ActivateAsync(state);
    }

    /// <summary>
    /// Greets the user, checks for previous progress, and handles user input to continue or start fresh.
    /// </summary>
    /// <param name="kernel">The kernel instance for executing the process step.</param>
    /// <param name="context">The context of the process step.</param>
    [KernelFunction("Greet")]
    public async Task GreetAsync(Kernel kernel, KernelProcessStepContext context)
    {
        // Ensure chat history exists.
        _state.ChatLog ??= new List<ChatMessageContent>();

        // Load the user's progress from persistent storage.
        var progress = ProgressStorage.Load();
        bool hasProgress = progress != null;

        if (hasProgress)
        {
            // Inform the user about their previous progress and ask if they want to continue.
            AgentHelper.LogAgentMessage($"Welcome back! Last time you chose '{progress!.LearningType}' learning.");
            AgentHelper.LogAgentMessage("Would you like to continue from where you left off? (yes/no)");
            AgentHelper.LogAgentMessage("WARNING: If you choose 'no', your previous progress will be permanently deleted.");

            string? answer;
            do
            {
                answer = AgentHelper.GetUserMessage();

                if (answer == "yes")
                {
                    // Handle continuation based on the previous learning type.
                    switch (progress.LearningType)
                    {
                        case LearningType.New:
                            await context.EmitEventAsync(ProcessEventNames.GreetingCompletedContinueLearning, progress.LearningPlan);
                            return;
                        case LearningType.Mandatory:
                            await context.EmitEventAsync(ProcessEventNames.GreetingCompletedMandatoryTraining);
                            return;
                    }
                }
                else if (answer == "no")
                {
                    // Delete the previous progress and start fresh.
                    AgentHelper.LogAgentMessage("Alright, starting fresh.");
                    ProgressStorage.Delete();
                    break;
                }
                else
                {
                    // Handle invalid input.
                    AgentHelper.LogAgentMessage("Invalid input. Please enter 'yes' or 'no'.");
                }
            }
            while (true);
        }

        // If no previous progress exists or the user chooses to start fresh.
        AgentHelper.LogAgentMessage("Hi! What would you like to learn today?");
        AgentHelper.LogAgentMessage("You can choose 'new' or 'mandatory'.");

        do
        {
            // Prompt the user to select a learning type.
            var response = AgentHelper.GetUserMessage();
            if (Enum.TryParse<LearningType>(CultureInfo.InvariantCulture.TextInfo.ToTitleCase(response), out var choice))
            {
                // Emit events based on the user's choice.
                if (choice == LearningType.New)
                {
                    await context.EmitEventAsync(ProcessEventNames.GreetingCompletedNewLearning);
                }
                else if (choice == LearningType.Mandatory)
                {
                    await context.EmitEventAsync(ProcessEventNames.GreetingCompletedMandatoryTraining);
                }

                break;
            }
            else
            {
                // Handle invalid input.
                AgentHelper.LogAgentMessage("Invalid response. Please enter 'new' or 'mandatory'.");
            }
        } while (true);
    }
}
