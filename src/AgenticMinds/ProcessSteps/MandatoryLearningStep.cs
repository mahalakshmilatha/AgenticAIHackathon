using System.Text;
using AgenticMinds.Agents;
using AgenticMinds.Agents.Helper;
using AgenticMinds.Data;
using AgenticMinds.ProcessSteps.ProcessStates;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AgenticMinds.ProcessSteps;

/// <summary>
/// Represents the MandatoryLearningStep, which guides users through mandatory learning resources.
/// </summary>
public class MandatoryLearningStep : KernelProcessStep<MandatoryLearningState>
{
    private MandatoryLearningState _state = new(); // Stores the state of the mandatory learning step.
    private readonly MandatoryLearningAgent _mandatoryTutorAgent; // The agent responsible for managing mandatory learning resources.

    /// <summary>
    /// Initializes a new instance of the MandatoryLearningStep class with the specified MandatoryLearningAgent.
    /// </summary>
    /// <param name="mandatoryTutorAgent">The agent responsible for managing mandatory learning resources.</param>
    public MandatoryLearningStep(MandatoryLearningAgent mandatoryTutorAgent)
    {
        _mandatoryTutorAgent = mandatoryTutorAgent;
    }

    /// <summary>
    /// Activates the step and restores its state.
    /// </summary>
    /// <param name="state">The state to restore for this step.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    public override ValueTask ActivateAsync(KernelProcessStepState<MandatoryLearningState> state)
    {
        _state = state.State!;
        return base.ActivateAsync(state);
    }

    /// <summary>
    /// Guides the user through mandatory learning resources, allowing them to select, view, and complete resources.
    /// </summary>
    /// <param name="kernel">The kernel instance for executing the process step.</param>
    /// <param name="context">The context of the process step.</param>
    [KernelFunction("Learn")]
    public async Task LearnAsync(Kernel kernel, KernelProcessStepContext context)
    {
        // Ensure chat history exists.
        _state.ChatLog ??= new List<ChatMessageContent>();

        // Create and restore the chat with the mandatory tutor agent.
        var chat = AgentHelper.CreateAgentGroupChatWithHistory(
            [_mandatoryTutorAgent.Agent],
            _state.ChatLog,
            new AgentGroupChatSettings
            {
                SelectionStrategy = new SequentialSelectionStrategy()
            });

        // Load the saved progress state or initialize a new learning plan.
        var progressState = ProgressStorage.Load();
        var learningPlan = progressState?.LearningPlan ?? await InitializeLearningPlanAsync();

        // Display the list of incomplete resources.
        var resourceList = learningPlan.Resources.Where(r => !r.IsComplete).ToList();

        // Check if all resources are complete.
        if (!resourceList.Any())
        {
            AgentHelper.LogAgentMessage("All mandatory learning resources have been completed.");
            await context.EmitEventAsync(ProcessEventNames.MandatoryLearningCompleted, learningPlan);
            return;
        }

        // Display the list of resources to the user.
        AgentHelper.LogAgentMessage("Mandatory Learning Resources:");
        for (int i = 0; i < resourceList.Count; i++)
        {
            AgentHelper.LogAgentMessage($"{i + 1}. {resourceList[i].Title}");
        }

        // Prompt the user to select a resource.
        AgentHelper.LogAgentMessage("Enter the number of the resource you want to view:");
        if (!int.TryParse(AgentHelper.GetUserMessage(), out int selectedIndex) || selectedIndex < 1 || selectedIndex > resourceList.Count)
        {
            AgentHelper.LogAgentMessage("Invalid selection. Please try again.");
            return;
        }

        // Get the selected resource.
        var selectedResource = resourceList[selectedIndex - 1];

        // Download the selected resource.
        AgentHelper.LogAgentMessage($"Downloading '{selectedResource.Title}'...");
        var downloadPath = await _mandatoryTutorAgent.DownloadResourceAsync(new MandatoryLearningResource
        {
            Title = selectedResource.Title,
            ContentUri = selectedResource.Url,
            Type = selectedResource.Type,
            Content = selectedResource.Description
        });

        if (!string.IsNullOrEmpty(downloadPath))
        {
            AgentHelper.LogAgentMessage($"Resource downloaded successfully to: {downloadPath}");
        }
        else
        {
            AgentHelper.LogAgentMessage("Failed to download the resource.");
            return;
        }

        // Extract content from the resource if it is a PDF.
        string resourceContent = selectedResource.Type.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            ? ExtractTextFromPdf(downloadPath)
            : selectedResource.Description;

        if (string.IsNullOrWhiteSpace(resourceContent))
        {
            AgentHelper.LogAgentMessage("Failed to extract content from the resource. Please check the file.");
            return;
        }

        // Add the resource content to the chat.
        string mandatoryResourceContent = $"""
            The resource to use is:
            Title: {selectedResource.Title}            
            Content: {resourceContent}
            """;

        chat.AddChatMessage(new ChatMessageContent(AuthorRole.Assistant, mandatoryResourceContent));

        // Engage in a back-and-forth chat with the user.
        string? response;
        do
        {
            // Display messages from the chat.
            await foreach (var message in chat.InvokeAsync())
            {
                AgentHelper.LogAgentMessage(message.Content!);
            }

            // Get the user's response.
            response = AgentHelper.GetUserMessage();

            // Handle user responses to continue or stop learning.
            if (response.Equals("continue", StringComparison.OrdinalIgnoreCase))
            {
                SaveLearningPlanState(learningPlan, selectedResource);
                await context.EmitEventAsync(ProcessEventNames.MandatoryContinueLearning, learningPlan);
                return;
            }
            else if (response.Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                SaveLearningPlanState(learningPlan, selectedResource);
                await context.EmitEventAsync(ProcessEventNames.MandatoryStopLearning, learningPlan);
                return;
            }

            // If the response is null or empty, exit the loop.
            if (string.IsNullOrWhiteSpace(response))
            {
                break;
            }

            // Add the user's response to the chat.
            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, response));

        } while (true);
    }

    /// <summary>
    /// Extracts text content from a PDF file.
    /// </summary>
    /// <param name="pdfFilePath">The path to the PDF file.</param>
    /// <returns>The extracted text content, or an empty string if extraction fails.</returns>
    private string ExtractTextFromPdf(string pdfFilePath)
    {
        try
        {
            var textBuilder = new StringBuilder();

            using var pdfReader = new PdfReader(pdfFilePath);
            using var pdfDocument = new PdfDocument(pdfReader);

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                var text = PdfTextExtractor.GetTextFromPage(page);
                textBuilder.AppendLine(text);
            }

            return textBuilder.ToString();
        }
        catch (Exception ex)
        {
            AgentHelper.LogAgentMessage($"Error extracting text from PDF: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Saves the state of the learning plan, marking the specified resource as complete.
    /// </summary>
    /// <param name="learningPlan">The learning plan to update.</param>
    /// <param name="resource">The resource to mark as complete.</param>
    private void SaveLearningPlanState(LearningPlan learningPlan, Resource resource)
    {
        resource.IsComplete = true;

        ProgressStorage.Save(new ProgressState
        {
            LearningType = LearningType.Mandatory,
            LearningPlan = learningPlan
        });
    }

    /// <summary>
    /// Initializes a new learning plan by retrieving mandatory learning resources.
    /// </summary>
    /// <returns>A LearningPlan object containing the mandatory learning resources.</returns>
    private async Task<LearningPlan> InitializeLearningPlanAsync()
    {
        var resources = await _mandatoryTutorAgent.GetMandatoryLearningResourcesAsync();

        if (resources == null || !resources.Any())
        {
            AgentHelper.LogAgentMessage("No mandatory learning resources available.");
            return new LearningPlan { Resources = new List<Resource>() };
        }

        return new LearningPlan
        {
            Resources = resources.Select(r => new Resource
            {
                Title = r.Title,
                Url = r.ContentUri,
                Type = r.Type,
                Description = string.Empty,
                IsComplete = false,
                IsExamScope = true,
                Id = Guid.NewGuid()
            }).ToList()
        };
    }
}

