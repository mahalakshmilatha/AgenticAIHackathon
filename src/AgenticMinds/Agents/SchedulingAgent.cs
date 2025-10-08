using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace AgenticMinds.Agents;

/// <summary>
/// Represents the SchedulingAgent, which is responsible for creating and managing
/// personalized study schedules for students based on their preferences and learning goals.
/// </summary>
public class SchedulingAgent
{
    /// <summary>
    /// The ChatCompletionAgent instance used to handle scheduling interactions.
    /// </summary>
    public ChatCompletionAgent Agent { get; }

    /// <summary>
    /// Initializes a new instance of the SchedulingAgent class with the specified ChatCompletionAgent.
    /// </summary>
    /// <param name="agent">The ChatCompletionAgent instance to be used for scheduling.</param>
    public SchedulingAgent(ChatCompletionAgent agent) => Agent = agent;
}

/// <summary>
/// A factory class for building and configuring instances of the SchedulingAgent.
/// </summary>
public static class SchedulingAgentFactory
{
    /// <summary>
    /// Builds and configures a SchedulingAgent instance using the provided configuration and shared services.
    /// </summary>
    /// <param name="config">The configuration object containing settings for the agent.</param>
    /// <param name="sharedServices">A collection of shared services to be added to the agent's kernel.</param>
    /// <returns>A configured instance of the SchedulingAgent.</returns>
    public static SchedulingAgent Build(IConfiguration config, IServiceCollection sharedServices)
    {
        // Retrieve configuration values for the scheduling agent.
        string deployment = config["schedulingProgressAgent"] ?? throw new InvalidOperationException("Missing configuration: schedulingProgressAgent");
        string endpoint = config["endpoint"]!;
        string apiKey = config["apiKey"]!;

        // Create a kernel builder and add shared services.
        var builder = Kernel.CreateBuilder();
        foreach (var service in sharedServices)
        {
            builder.Services.Add(service);
        }

        // Add Azure OpenAI ChatCompletion capabilities to the kernel.
        builder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey);

        // Build the kernel and configure the ChatCompletionAgent.
        var kernel = builder.Build();
        var timeZoneId = TimeZoneInfo.Local.Id; // Retrieve the local timezone ID (e.g., "Europe/London").

        var agent = new ChatCompletionAgent
        {
            Name = AgentNames.SchedulingProgressAgent,
            Kernel = kernel,
            Instructions = $"""
            Do not use Markdown formatting in your responses. Use plain text only. eg. no ***, ```, **, __ or *.
            Your role is to create and manage the student's study schedule and return it as a valid iCalendar (.ics) file.

            Use the student's provided study preferences, including their preferred study days and times, to create a personalised schedule. 
            This information is given as a single field describing when they like to study (e.g., "Monday, Wednesday, Friday evenings").

            Generate a study schedule that:

            - Begins in the future, starting from tomorrow or the next available day, relative to the current date.
            - Matches the student's preferred study time (which may include specific days and parts of the day).
            - Allocates appropriate time blocks based on the estimated durations of the learning resources.
            - Schedules only one study resource per day.
            - Reflects the student's learning goals and topics to study.
            - Is realistic and achievable within the timeframe implied by the resource list.

            Output the complete schedule in valid iCalendar (.ics) format only, using local time for the Europe/London timezone. 
            Each event must use TZID=Europe/London in the DTSTART and DTEND fields instead of UTC.

            Include a VTIMEZONE definition for Europe/London in the calendar so it can be correctly interpreted by Outlook and other clients.

            Do not include any commentary, instructions, or explanations outside the .ics content.

            Here is an example of the correct format:

            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//YourApp//LearningSchedule//EN
            BEGIN:VTIMEZONE
            TZID:{timeZoneId}
            BEGIN:STANDARD
            DTSTART:20251026T020000
            TZOFFSETFROM:+0100
            TZOFFSETTO:+0000
            TZNAME:Standard Time
            END:STANDARD
            BEGIN:DAYLIGHT
            DTSTART:20250330T010000
            TZOFFSETFROM:+0000
            TZOFFSETTO:+0100
            TZNAME:Daylight Time
            END:DAYLIGHT
            END:VTIMEZONE
            BEGIN:VEVENT
            UID:study-session-1@example.com
            DTSTAMP:20250430T090000Z
            ORGANIZER;CN=Study Scheduler:MAILTO:scheduler@example.com
            DTSTART;TZID={timeZoneId}:20250501T100000
            DTEND;TZID={timeZoneId}:20250501T110000
            SUMMARY:Math Study Session
            DESCRIPTION:Focus on algebra and calculus.
            LOCATION:Home
            END:VEVENT
            END:VCALENDAR
            """
        };

        return new SchedulingAgent(agent);
    }
}
