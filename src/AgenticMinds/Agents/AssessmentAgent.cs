using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AgenticMinds.Agents;

/// <summary>
/// Represents the AssessmentAgent, which is responsible for conducting knowledge assessments
/// to evaluate the student's current competency level in a specific subject.
/// </summary>
public class AssessmentAgent
{
    /// <summary>
    /// The ChatCompletionAgent instance used to handle the assessment process.
    /// </summary>
    public ChatCompletionAgent Agent { get; }

    /// <summary>
    /// Initializes a new instance of the AssessmentAgent class with the specified ChatCompletionAgent.
    /// </summary>
    /// <param name="agent">The ChatCompletionAgent instance to be used for assessments.</param>
    public AssessmentAgent(ChatCompletionAgent agent) => Agent = agent;
}

public static class AssessmentAgentFactory
{
    /// <summary>
    /// Builds an instance of the AssessmentAgent using the provided configuration and shared services.
    /// </summary>
    /// <param name="config">The configuration object containing necessary settings.</param>
    /// <param name="sharedServices">The collection of shared services to be added to the kernel.</param>
    /// <returns>An instance of AssessmentAgent.</returns>
    public static AssessmentAgent Build(IConfiguration config, IServiceCollection sharedServices)
    {
        // Retrieve configuration values for the agent's deployment, endpoint, and API key.
        string deployment = config["assessmentAgent"]!;
        string endpoint = config["endpoint"]!;
        string apiKey = config["apiKey"]!;

        // Create a new Kernel builder instance.
        var builder = Kernel.CreateBuilder();

        // Add all shared services to the kernel's service collection.
        foreach (var service in sharedServices)
        {
            builder.Services.Add(service);
        }

        // Configure the kernel to use Azure OpenAI Chat Completion with the provided settings.
        builder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey);

        // Build the kernel instance.
        var kernel = builder.Build();

        // Create a new ChatCompletionAgent with specific instructions and settings.
        var agent = new ChatCompletionAgent
        {
            Name = AgentNames.AssessmentAgent, // Assign a name to the agent.
            Kernel = kernel, // Set the kernel instance for the agent.
            Arguments = new KernelArguments(
            new OpenAIPromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            }),
            Instructions = """
                Do not use Markdown formatting in your responses. Use plain text only. eg. no ***, ```, **, __ or *.
                First, ask the user about the subject matter they want to learn.
                Then, your role is to ask a set of questions to test the student's current knowledge level and competency in the subject matter they are interested in. 
                Ask a variety of questions to measure the student's competency level, but keep it to a maximum of 3 questions.
                Break down your questions into 2 beginner, 2 intermediate and 2 advanced level questions and make it multiple choice giving the student 3 options to choose from. 
                Your role is not to give feedback, your role is to ask a set of questions to tests the students current knowledge level in the subject matter they are interested in.
                The assessment results should include the following in JSON format: 
                - StudentId, which should be a randomly generated ID
                - AssessmentId, which should be relevant to the Subject area
                - Subject, which is the users subject to learn
                - Score, which is the assessment result, broken down into beginner, intermediate and advanced results.

                For example,the response should look similar to this:
                [AssessmentResult]
                {
                  "StudentId": "95731",
                  "AssessmentId": "CSharp-101",
                  "Subject": "C#",
                  "Score": {
                    "Beginner": "0/2",
                    "Intermediate": "1/2",
                    "Advanced": "2/2",
                   }
                }
            """
        };

        return new AssessmentAgent(agent);
    }
}
