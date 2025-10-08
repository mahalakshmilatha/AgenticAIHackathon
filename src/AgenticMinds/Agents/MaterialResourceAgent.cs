using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace AgenticMinds.Agents;

/// <summary>
/// Represents the MaterialResourceAgent, which is responsible for suggesting learning materials
/// and additional resources based on the student's preferences and assessment results.
/// </summary>
public class MaterialResourceAgent
{
    /// <summary>
    /// The ChatCompletionAgent instance used to handle resource recommendations.
    /// </summary>
    public ChatCompletionAgent Agent { get; }

    /// <summary>
    /// Initializes a new instance of the MaterialResourceAgent class with the specified ChatCompletionAgent.
    /// </summary>
    /// <param name="agent">The ChatCompletionAgent instance to be used for resource recommendations.</param>
    public MaterialResourceAgent(ChatCompletionAgent agent) => Agent = agent;
}

/// <summary>
/// A factory class for building and configuring instances of the MaterialResourceAgent.
/// </summary>
public static class MaterialResourceAgentFactory
{
    /// <summary>
    /// Builds and configures a MaterialResourceAgent instance using the provided configuration and shared services.
    /// </summary>
    /// <param name="config">The configuration object containing settings for the agent.</param>
    /// <param name="sharedServices">A collection of shared services to be added to the agent's kernel.</param>
    /// <returns>A configured instance of the MaterialResourceAgent.</returns>
    public static MaterialResourceAgent Build(IConfiguration config, IServiceCollection sharedServices)
    {
        // Retrieve configuration values for the material resource agent.
        string deployment = config["materialResourceAgent"]!;
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
            Name = AgentNames.MaterialResourceAgent,
            Kernel = kernel,
            Instructions = """
                Do not use Markdown formatting in your responses. Use plain text only. eg. no ***, ```, **, __ or *.
                Your role is to suggest learning materials and additional resources that match the student's learning style and preferences. 
                Based on the personalized preferences, recommend a variety of materials such as articles, videos, interactive quizzes, 
                practical exercises, books, online courses, and forums. Ensure the materials are relevant to the topics from the assessment results
                and cater to the student's preferred learning style. Encourage the student by highlighting the benefits of each resource 
                and how it will help them master the subject. Provide positive reinforcement to keep the student motivated and engaged.

                For each recommended resource, include an estimated duration in minutes indicating how long the student should spend engaging with it.
                Base your estimate on the type and depth of the content. Add this as the 'EstimatedMinutes' property.
                If you cannot estimate, then simply use a default value of 30 minutes.
            
                The response should be in the following format, IsComplete will always be false and IsExamScope will always be true, LastChoice will always be "new":
                [LEARNINGPLAN]
                {
                  "LastChoice": "new",
                  "LearningPlan": {
                    "Resources": [
                      {
                        "Id": "00000000-0000-0000-0000-000000000001",
                        "Title": "C# Fundamentals for Absolute Beginners",
                        "Url": "https://dev.to/moh_moh701/mastering-c-fundamentals-a-beginners-journey-into-net-development-37ob",
                        "Type": "article",
                        "Description": "description of resource",
                        "EstimatedMinutes": 15,
                        "IsComplete": false,
                        "IsExamScope": true
                      },
                      {
                        "Id": "00000000-0000-0000-0000-000000000002",
                        "Title": "C# Fundamentals for Absolute Beginners",
                        "Url": "https://www.pluralsight.com/courses/csharp-fundamentals-dev",
                        "Type": "video",
                        "Description": "description of resource",
                        "EstimatedMinutes": 15,
                        "IsComplete": false,
                        "IsExamScope": true
                      }
                    ]
                  }
                }
            """
        };

        return new MaterialResourceAgent(agent);
    }
}
