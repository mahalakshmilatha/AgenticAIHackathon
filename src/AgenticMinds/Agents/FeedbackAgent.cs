using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AgenticMinds.Agents;

/// <summary>
/// Represents the FeedbackAgent, which is responsible for evaluating the student's performance
/// and providing constructive feedback on their competency in the subject matter.
/// </summary>
public class FeedbackAgent
{
    /// <summary>
    /// The ChatCompletionAgent instance used to handle feedback generation.
    /// </summary>
    public ChatCompletionAgent Agent { get; }

    /// <summary>
    /// Initializes a new instance of the FeedbackAgent class with the specified ChatCompletionAgent.
    /// </summary>
    /// <param name="agent">The ChatCompletionAgent instance to be used for feedback generation.</param>
    public FeedbackAgent(ChatCompletionAgent agent) => Agent = agent;
}

/// <summary>
/// A factory class for building and configuring instances of the FeedbackAgent.
/// </summary>
public static class FeedbackAgentFactory
{
    /// <summary>
    /// Builds and configures a FeedbackAgent instance using the provided configuration and shared services.
    /// </summary>
    /// <param name="config">The configuration object containing settings for the agent.</param>
    /// <param name="sharedServices">A collection of shared services to be added to the agent's kernel.</param>
    /// <returns>A configured instance of the FeedbackAgent.</returns>
    public static FeedbackAgent Build(IConfiguration config, IServiceCollection sharedServices)
    {
        // Retrieve configuration values for the feedback agent.
        string deployment = config["FeedbackAgent"]!;
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
            Name = AgentNames.FeedbackAgent,
            Kernel = kernel,
            Arguments = new KernelArguments(
            new OpenAIPromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            }),
            Instructions = $"""
                Do not use Markdown formatting in your responses. Use plain text only. eg. no ***, ```, **, __ or *.
                Your role is to evaluate the students performance on the assessment and their competency in the subject matter they are interested in. 
                Provide immediate, constructive feedback, outlining the competency level: beginner, intermediate or advanced.
                Highlight areas of strength to boost their confidence and identify topics that need improvement. 
                Encourage the student by acknowledging their efforts and progress, and motivate them to continue learning.
            """
        };

        return new FeedbackAgent(agent);
    }
}
