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
/// Represents the PlanningStep, which is responsible for creating a personalized learning plan
/// based on user preferences and assessment results.
/// </summary>
public class PlanningStep : KernelProcessStep<PlanningState>
{
    private PlanningState _state = new(); // Stores the state of the planning step.
    private readonly PreferencePlanningAgent _preferencePlanningAgent; // Agent for gathering user preferences.
    private readonly MaterialResourceAgent _materialResourceAgent; // Agent for retrieving learning resources.

    /// <summary>
    /// Initializes a new instance of the PlanningStep class with the specified agents.
    /// </summary>
    /// <param name="preferencePlanningAgent">The agent responsible for gathering user preferences.</param>
    /// <param name="materialResourceAgent">The agent responsible for retrieving learning resources.</param>
    public PlanningStep(PreferencePlanningAgent preferencePlanningAgent, MaterialResourceAgent materialResourceAgent)
    {
        _preferencePlanningAgent = preferencePlanningAgent;
        _materialResourceAgent = materialResourceAgent;
    }

    /// <summary>
    /// Activates the step and restores its state.
    /// </summary>
    /// <param name="state">The state to restore for this step.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    public override ValueTask ActivateAsync(KernelProcessStepState<PlanningState> state)
    {
        _state = state.State!;
        return base.ActivateAsync(state);
    }

    /// <summary>
    /// Creates a personalized learning plan based on user preferences and assessment results.
    /// </summary>
    /// <param name="kernel">The kernel instance for executing the process step.</param>
    /// <param name="context">The context of the process step.</param>
    /// <param name="assessmentResults">The assessment results containing the user's performance data.</param>
    [KernelFunction("Plan")]
    public async Task PlanAsync(Kernel kernel, KernelProcessStepContext context, AssessmentResults assessmentResults)
    {
        // Ensure chat history exists.
        _state.ChatLog ??= new List<ChatMessageContent>();

        // Create and restore the chat with the preference planning agent.
        var chat = AgentHelper.CreateAgentGroupChatWithHistory(
            [_preferencePlanningAgent.Agent],
            _state.ChatLog,
            new AgentGroupChatSettings
            {
                SelectionStrategy = new SequentialSelectionStrategy()
            });

        // Initialize an empty learning plan.
        var learningPlan = new LearningPlan();

        // Log messages from the chat.
        await foreach (var message in chat.InvokeAsync())
        {
            AgentHelper.LogAgentMessage(message.Content!);
        }

        LearningPreferences? preferences = null;
        do
        {
            // Prompt the user for input and process their response.
            var planningResponse = AgentHelper.GetUserMessage();
            if (string.IsNullOrWhiteSpace(planningResponse)) // Check for null or empty input.
            {
                continue; // Skip to the next iteration.
            }

            // Add the user's response to the chat.
            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, planningResponse));
            await foreach (var message in chat.InvokeAsync())
            {
                if (message.Content!.Contains("[LearningPreferences]"))
                {
                    // Extract JSON content from the message.
                    var jsonStartIndex = message.Content.IndexOf('{');
                    var jsonEndIndex = message.Content.LastIndexOf('}');
                    var jsonContent = message.Content.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);

                    // Deserialize the JSON content into LearningPreferences.
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    try
                    {
                        preferences = JsonSerializer.Deserialize<LearningPreferences>(jsonContent, jsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        AgentHelper.LogAgentMessage($"Error deserializing JSON: {ex.Message}");
                    }
                }
                else
                {
                    AgentHelper.LogAgentMessage(message.Content);
                }
            }
        } while (preferences is null); // Repeat until preferences are successfully retrieved.

        // Create a chat with the material resource agent to generate the learning plan.
        var resourceChat = AgentHelper.CreateAgentGroupChatWithHistory(
            [_materialResourceAgent.Agent],
            _state.ChatLog,
            new AgentGroupChatSettings
            {
                SelectionStrategy = new SequentialSelectionStrategy()
            });

        // Build instructions for generating the learning plan.
        string learningPlanInstructions = $"""
            Provide a learning plan based on the following user preferences and assessment results:
            Assessment Subject: {assessmentResults.Subject}
            Assessment Score: {string.Join(", ", assessmentResults.Score.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}
            Preferred Learning Style: {preferences.PreferredLearningStyle}
            Preferred Study Time: {preferences.PreferredStudyTime}
            Learning Goals: {preferences.LearningGoals}
            The learning plan should include a list of resources tailored to these preferences and results.
            """;

        // Add the instructions to the chat.
        resourceChat.AddChatMessage(new ChatMessageContent(AuthorRole.Assistant, learningPlanInstructions));

        try
        {
            // Process the response to generate the learning plan.
            await foreach (var message in resourceChat.InvokeAsync())
            {
                if (message.Content!.Contains("[LEARNINGPLAN]"))
                {
                    // Extract JSON content from the message.
                    var jsonContent = AgentHelper.ExtractJsonFromResponse(message.Content);

                    // Deserialize the JSON content into a ProgressState object.
                    var progressState = JsonSerializer.Deserialize<ProgressState>(jsonContent);
                    learningPlan = progressState!.LearningPlan;

                    // Save the progress state to persistent storage.
                    ProgressStorage.Save(progressState);
                    AgentHelper.LogAgentMessage("Learning plan generated:\n" + learningPlan.ToDisplayString());
                }
                else
                {
                    AgentHelper.LogAgentMessage(message.Content);
                }
            }
        }
        catch (Exception ex)
        {
            // Log any errors that occur during the chat invocation.
            AgentHelper.LogAgentMessage($"Error during chat invocation: {ex.Message}");
        }

        // Emit the planning completed event with the generated learning plan and preferences.
        await context.EmitEventAsync(ProcessEventNames.PlanningCompleted, new PlanningResult
        {
            LearningPreferences = preferences,
            LearningPlan = learningPlan
        });
    }
}
