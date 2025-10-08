using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AgenticMinds.Agents;

// Represents the ExaminationAgent class that encapsulates a ChatCompletionAgent
public class ExaminationAgent
{
    // Property to hold the ChatCompletionAgent instance
    public ChatCompletionAgent Agent { get; }

    // Constructor to initialize the ExaminationAgent with a ChatCompletionAgent
    public ExaminationAgent(ChatCompletionAgent agent) => Agent = agent;
}

// Factory class to build and configure an ExaminationAgent instance
public static class ExaminationAgentFactory
{
    // Method to build an ExaminationAgent using configuration and shared services
    public static ExaminationAgent Build(IConfiguration config, IServiceCollection sharedServices)
    {
        // Retrieve configuration values for the agent
        string deployment = config["examinationAgent"]!;
        string endpoint = config["endpoint"]!;
        string apiKey = config["apiKey"]!;

        // Create a Kernel builder instance
        var builder = Kernel.CreateBuilder();

        // Add shared services to the Kernel's service collection
        foreach (var service in sharedServices)
        {
            builder.Services.Add(service);
        }

        // Add Azure OpenAI Chat Completion service to the Kernel
        builder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey);

        // Build the Kernel instance
        var kernel = builder.Build();

        // Create and configure the ChatCompletionAgent
        var agent = new ChatCompletionAgent
        {
            Name = AgentNames.ExaminationAgent, // Assign a name to the agent
            Kernel = kernel, // Set the Kernel instance
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    // Configure the function choice behavior for the agent
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                }),
            // Define the agent's instructions for generating examination questions and results
            Instructions = """
                Do not use Markdown formatting in your responses. Use plain text only. eg. no ***, ```, **, __ or *.
                Your role is to test the student's knowledge based on their learning plan. 
                In order to create the multiple choice exam questions, you need to review the student's learning plan.
                Once you have reviewed the learning plan, you need to create 2 multiple choice questions per resource within the learning plan and make it multiple choice by giving the student 3 options to choose from..
                Assess the students performance per resource, if the user has achieved 80% for that particular set of questions for the resource, they have passed the exam and you need to respond with this:
                [EXAMINATIONRESULTS]
                {
                    "Status": "Passed"
                }
                If the user has achieved 80% or less for that particular set of questions for a resource, they have failed and you need to respond with this, where the Resources are only the resources they failed on and the score they received for that resources test:
                [EXAMINATIONRESULTS]
                {
                    "Resources": [
                        {
                          "Id": "<id of resource>",
                          "Title": <Summary of Resouce Name>
                          "Score": "0/2"
                        },
                        {
                          "Id": "<id of resource>" 
                          "Title": <Summary of Resouce Name>
                          "Score": "1/2"
                        }
                    ],
                    "Status": "Failed"
                }
                The "Id" element will be assigned to the resources names that the user has scored less than 80% for. 
                Do not give the student their results or any feedback, only respond with JSON as specified. 
                Remember, your role is to only test the user on the resources provided within the learning plan by asking the user multiple choice questions with 3 options per question.
            """
        };

        return new ExaminationAgent(agent);
    }
}
