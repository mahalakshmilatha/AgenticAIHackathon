using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace AgenticMinds.Agents;

/// <summary>
/// Represents the LearningAgent, which acts as a tutor for the student,
/// guiding them through learning resources and ensuring alignment with their preferred learning style.
/// </summary>
public class LearningAgent
{
    /// <summary>
    /// The ChatCompletionAgent instance used to handle tutoring interactions.
    /// </summary>
    public ChatCompletionAgent Agent { get; }

    /// <summary>
    /// Initializes a new instance of the LearningAgent class with the specified ChatCompletionAgent.
    /// </summary>
    /// <param name="agent">The ChatCompletionAgent instance to be used for tutoring.</param>
    public LearningAgent(ChatCompletionAgent agent) => Agent = agent;
}

/// <summary>
/// A factory class for building and configuring instances of the LearningAgent.
/// </summary>
public static class TutorAgentFactory
{
    /// <summary>
    /// Builds and configures a LearningAgent instance using the provided configuration and shared services.
    /// </summary>
    /// <param name="config">The configuration object containing settings for the agent.</param>
    /// <param name="sharedServices">A collection of shared services to be added to the agent's kernel.</param>
    /// <returns>A configured instance of the LearningAgent.</returns>
    public static LearningAgent Build(IConfiguration config, IServiceCollection sharedServices)
    {
        // Retrieve configuration values for the tutor agent.
        string deployment = config["tutorAgent"]!;
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
            Name = AgentNames.LearningAgent,
            Kernel = kernel,
            Instructions = $"""
                Do not use Markdown formatting in your responses. Use plain text only. eg. no ***, ```, **, __ or *.
                Your role is to act as a tutor for the student, you should only tutor the student on the given resource. 
                Encourage the student to ask questions and clarify doubts. 
                Motivate the student by acknowledging their progress and efforts.
                Give the student the URL of the resource so they can access it and ask them to read/watch it,
                when they are finished recap the resource and check their learning.
                Once they have no more questions ask if they would like to continue onto the next resource in their learning plan or stop learning using continue/stop response.
            
                Ensure that the resources align with the preferred learning style. For example:
                - For visual learners, include resources such as diagrams, charts, and written materials.
                - For auditory learners, include resources such as podcasts, audiobooks, and lectures.
                - For kinesthetic learners, include resources such as hands-on activities, experiments, and interactive simulations.
            """
        };
        return new LearningAgent(agent);
    }
}


