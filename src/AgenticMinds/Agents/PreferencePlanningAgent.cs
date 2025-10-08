using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AgenticMinds.Agents;

/// <summary>
/// Represents the PreferencePlanningAgent, which is responsible for gathering user preferences
/// such as learning style, study time, and learning goals to create a personalized learning plan.
/// </summary>
public class PreferencePlanningAgent
{
    /// <summary>
    /// The ChatCompletionAgent instance used to handle preference planning interactions.
    /// </summary>
    public ChatCompletionAgent Agent { get; }

    /// <summary>
    /// Initializes a new instance of the PreferencePlanningAgent class with the specified ChatCompletionAgent.
    /// </summary>
    /// <param name="agent">The ChatCompletionAgent instance to be used for preference planning.</param>
    public PreferencePlanningAgent(ChatCompletionAgent agent) => Agent = agent;
}

/// <summary>
/// A factory class for building and configuring instances of the PreferencePlanningAgent.
/// </summary>
public static class PreferencePlanningAgentFactory
{
    /// <summary>
    /// Builds and configures a PreferencePlanningAgent instance using the provided configuration and shared services.
    /// </summary>
    /// <param name="config">The configuration object containing settings for the agent.</param>
    /// <param name="sharedServices">A collection of shared services to be added to the agent's kernel.</param>
    /// <returns>A configured instance of the PreferencePlanningAgent.</returns>
    public static PreferencePlanningAgent Build(IConfiguration config, IServiceCollection sharedServices)
    {
        // Retrieve configuration values for the preference planning agent.
        string deployment = config["preferencePlanningAgent"]!;
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
        var agent = new ChatCompletionAgent
        {
            Name = AgentNames.PreferencePlanningAgent,
            Kernel = kernel,
            Arguments = new KernelArguments(
            new OpenAIPromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            }),
            Instructions = """
                Do not use Markdown formatting in your responses. Use plain text only. eg. no ***, ```, **, __ or *.
                Start by asking the student about their preferred learning style (e.g., visual, auditory, kinesthetic), their preferred study time 
                (including both time of day and days of the week, such as "Monday, Wednesday, Friday evenings"), and their specific learning goals.

                Be supportive and motivating — explain how a personalised learning plan will help them achieve their goals, and express enthusiasm for their learning journey.

                Once the student has responded, recap the preferences in your own words to confirm understanding, and ask the student if they are happy with the preferences. 
                Request that they respond with the word "happy" if everything looks correct.

                When the student responds with "happy", return the learning preferences in the following format — and nothing else:

                [LearningPreferences]
                {
                  "PreferredLearningStyle": "visual",
                  "PreferredStudyTime": "Monday, Wednesday, Friday evenings",
                  "LearningGoals": "Become proficient in C# and .NET development" 
                }
                """
        };

        return new PreferencePlanningAgent(agent);
    }
}
