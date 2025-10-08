using System.Text;
using AgenticMinds.Agents;
using AgenticMinds.Agents.Helper;
using AgenticMinds.Data;
using AgenticMinds.ProcessSteps.ProcessStates;
using Ical.Net;
using Ical.Net.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgenticMinds.ProcessSteps;

/// <summary>
/// Represents the SchedulingStep, which is responsible for creating a detailed study schedule
/// based on the user's learning preferences and plan.
/// </summary>
public class SchedulingStep : KernelProcessStep<SchedulingState>
{
    private SchedulingState _state = new(); // Stores the state of the chat log for this step.
    private readonly SchedulingAgent _schedulingAgent; // The agent responsible for scheduling tasks.

    /// <summary>
    /// Initializes a new instance of the SchedulingStep class with the specified SchedulingAgent.
    /// </summary>
    /// <param name="agent">The SchedulingAgent instance to be used for scheduling tasks.</param>
    public SchedulingStep(SchedulingAgent agent)
    {
        _schedulingAgent = agent;
    }

    /// <summary>
    /// Activates the step and restores its state.
    /// </summary>
    /// <param name="state">The state to restore for this step.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    public override ValueTask ActivateAsync(KernelProcessStepState<SchedulingState> state)
    {
        _state = state.State!;
        return base.ActivateAsync(state);
    }

    /// <summary>
    /// Generates a detailed study schedule in iCalendar (.ics) format based on the user's learning preferences and plan.
    /// </summary>
    /// <param name="kernel">The kernel instance for executing the process step.</param>
    /// <param name="context">The context of the process step.</param>
    /// <param name="planningResult">The planning result containing the user's learning preferences and plan.</param>
    [KernelFunction("Schedule")]
    public async Task ScheduleAsync(
        Kernel kernel,
        KernelProcessStepContext context,
        PlanningResult planningResult)
    {
        var learningPreferences = planningResult.LearningPreferences; // User's learning preferences.
        var learningPlan = planningResult.LearningPlan; // User's learning plan.

        // Ensure chat history exists.
        _state.ChatLog ??= new List<ChatMessageContent>();

        // Create and restore the chat with the scheduling agent.
        var chat = AgentHelper.CreateAgentGroupChatWithHistory(
            [_schedulingAgent.Agent],
            _state.ChatLog,
            new AgentGroupChatSettings
            {
                SelectionStrategy = new SequentialSelectionStrategy()
            });

        // Build the user prompt with learning preferences and plan details.
        var prompt = new StringBuilder();
        prompt.AppendLine("Here are the student's learning preferences:");
        prompt.AppendLine($"- Learning Style: {learningPreferences.PreferredLearningStyle}");
        prompt.AppendLine($"- Preferred Study Time: {learningPreferences.PreferredStudyTime}");
        prompt.AppendLine($"- Learning Goals: {learningPreferences.LearningGoals}");
        prompt.AppendLine();
        prompt.AppendLine("Here is the student's learning plan:");
        prompt.AppendLine(learningPlan.ToDisplayString());
        prompt.AppendLine();
        prompt.AppendLine("Please generate a detailed study schedule in valid iCalendar (.ics) format.");
        var today = DateTime.UtcNow.Date;
        prompt.AppendLine($"Today is {today:yyyy-MM-dd}. Please ensure the schedule starts from {today.AddDays(1):yyyy-MM-dd} onwards.");

        // Add the prompt to the chat.
        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, prompt.ToString()));

        // Collect the response and process the schedule.
        var calendarSaved = false;
        do
        {
            string scheduleResponse = string.Empty;
            await foreach (var message in chat.InvokeAsync())
            {
                if (!string.IsNullOrWhiteSpace(message.Content))
                {
                    scheduleResponse += message.Content + Environment.NewLine;
                }

                // Save the chat history.
                _state.ChatLog.Add(message);
            }

            try
            {
                // Extract the iCalendar content from the response.
                var icsStart = scheduleResponse.IndexOf("BEGIN:VCALENDAR", StringComparison.OrdinalIgnoreCase);
                var icsEnd = scheduleResponse.IndexOf("END:VCALENDAR", StringComparison.OrdinalIgnoreCase);

                if (icsStart == -1 || icsEnd == -1)
                {
                    // Log the response and prompt the user for additional input if no valid iCalendar content is found.
                    AgentHelper.LogAgentMessage(scheduleResponse);
                    var response = AgentHelper.GetUserMessage()?.Trim();
                    chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, response));

                    continue;
                }

                var icsContent = scheduleResponse.Substring(icsStart, (icsEnd + "END:VCALENDAR".Length) - icsStart);

                // Parse and save the iCalendar file.
                var calendar = Calendar.Load(icsContent);
                var serializer = new CalendarSerializer();
                var icsOutput = serializer.SerializeToString(calendar);

                var directory = Path.Combine("Schedules");
                Directory.CreateDirectory(directory);
                var filePath = Path.Combine(directory, $"schedule-{DateTime.UtcNow:yyyyMMddHHmmss}.ics");
                File.WriteAllText(filePath, icsOutput);

                // Log success messages.
                AgentHelper.LogAgentMessage("Study schedule successfully generated and saved.");
                AgentHelper.LogAgentMessage($"File location: {Path.GetFullPath(filePath)}");
                AgentHelper.LogAgentMessage("You can now import this .ics file into your calendar application (such as Outlook, Google Calendar, or Apple Calendar).");
            }
            catch (Exception ex)
            {
                // Log any errors that occur during the scheduling process.
                AgentHelper.LogAgentMessage($"Failed to save schedule: {ex.Message}");
            }
            calendarSaved = true;
        } while (!calendarSaved);

        // Emit the scheduling completed event.
        await context.EmitEventAsync(ProcessEventNames.SchedulingCompleted, learningPlan);
    }
}
