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
/// Represents the ExaminationStep, which is responsible for administering exams
/// and evaluating the user's performance based on their learning plan.
/// </summary>
public class ExaminationStep : KernelProcessStep<ExaminationState>
{
    private ExaminationState _state = new(); // Stores the state of the examination step.
    private readonly ExaminationAgent _examinationAgent; // The agent responsible for administering exams.

    /// <summary>
    /// Initializes a new instance of the ExaminationStep class with the specified ExaminationAgent.
    /// </summary>
    /// <param name="examinationAgent">The agent responsible for administering exams.</param>
    public ExaminationStep(ExaminationAgent examinationAgent)
    {
        _examinationAgent = examinationAgent;
    }

    /// <summary>
    /// Activates the step and restores its state.
    /// </summary>
    /// <param name="state">The state to restore for this step.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    public override ValueTask ActivateAsync(KernelProcessStepState<ExaminationState> state)
    {
        _state = state.State!;
        return base.ActivateAsync(state);
    }

    /// <summary>
    /// Administers the exam by interacting with the user through a chat interface and evaluates their performance.
    /// </summary>
    /// <param name="kernel">The kernel instance for executing the process step.</param>
    /// <param name="context">The context of the process step.</param>
    /// <param name="learningPlan">The learning plan containing the user's resources.</param>
    [KernelFunction("Assess")]
    public async Task AssesAsync(Kernel kernel, KernelProcessStepContext context, LearningPlan learningPlan)
    {
        // Filter resources that are marked as part of the exam scope.
        var examScope = learningPlan.Resources.FindAll(x => x.IsExamScope);

        // Ensure chat history exists.
        _state.ChatLog ??= new List<ChatMessageContent>();

        // Create and restore the chat with the examination agent.
        var chat = AgentHelper.CreateAgentGroupChatWithHistory(
            [_examinationAgent.Agent],
            _state.ChatLog,
            new AgentGroupChatSettings
            {
                SelectionStrategy = new SequentialSelectionStrategy()
            });

        // Provide the list of resources in the exam scope to the user.
        string examMessage = $"""
            The is the list of resources in the learning plan to create the test with:
            {string.Join(Environment.NewLine, examScope)}
            """;
        chat.AddChatMessage(new ChatMessageContent(AuthorRole.Assistant, examMessage));

        // Log initial messages from the chat.
        await foreach (var message in chat.InvokeAsync())
        {
            AgentHelper.LogAgentMessage(message.Content!);
        }

        // Prompt the user for input and process their response.
        var response = AgentHelper.GetUserMessage();
        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, response));

        await foreach (var message in chat.InvokeAsync())
        {
            if (message.Content!.Contains("[EXAMINATIONRESULTS]"))
            {
                // Extract JSON content from the message.
                var jsonContent = AgentHelper.ExtractJsonFromResponse(message.Content);

                // Parse the JSON content into an ExaminationResult object.
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var result = JsonSerializer.Deserialize<ExaminationResult>(jsonContent, jsonOptions);
                string assistantMessage;

                if (result!.Status == "Failed")
                {
                    // Handle the case where the user fails the exam.
                    foreach (var resource in learningPlan.Resources)
                    {
                        var learningResource = result.Resources.FirstOrDefault(x => x.Id == resource.Id);
                        if (learningResource == null)
                        {
                            resource.IsExamScope = false;
                        }
                        else
                        {
                            resource.IsComplete = false;
                        }
                    }
                    assistantMessage = "Unfortunately you have failed the exam, you will need to revisit the learning materials that you performed poorly on and try again.";
                    Console.WriteLine(assistantMessage);

                    // Save the updated progress state.
                    ProgressStorage.Save(new ProgressState
                    {
                        LearningType = LearningType.New,
                        LearningPlan = learningPlan
                    });

                    // Emit the ExaminationCompletedFailed event.
                    await context.EmitEventAsync(ProcessEventNames.ExaminationCompletedFailed, learningPlan);
                }
                else
                {
                    // Handle the case where the user passes the exam.
                    foreach (var resource in learningPlan.Resources)
                    {
                        resource.IsExamScope = false;
                    }
                    assistantMessage = "Congratulations! You have passed the exam and completed the learning plan.";

                    // Delete the progress state as the learning plan is complete.
                    ProgressStorage.Delete();

                    Console.WriteLine(assistantMessage);
                    // Emit the ExaminationCompletedPassed event.
                    await context.EmitEventAsync(ProcessEventNames.ExaminationCompletedPassed, result);
                }

                // Log the assistant's message.
                AgentHelper.LogAgentMessage(assistantMessage);
            }
        }
    }
}
