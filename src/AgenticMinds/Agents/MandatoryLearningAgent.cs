using AgenticMinds.Agents.Helper;
using AgenticMinds.Data;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace AgenticMinds.Agents;

/// <summary>
/// Represents the MandatoryLearningAgent, which is responsible for managing mandatory learning resources
/// and guiding students through their completion.
/// </summary>
public class MandatoryLearningAgent
{
    /// <summary>
    /// The ChatCompletionAgent instance used to handle tutoring interactions.
    /// </summary>
    public ChatCompletionAgent Agent { get; }

    /// <summary>
    /// The BlobContainerClient instance used to interact with Azure Blob Storage.
    /// </summary>
    private readonly BlobContainerClient _blobContainerClient;

    /// <summary>
    /// Initializes a new instance of the MandatoryLearningAgent class with the specified ChatCompletionAgent
    /// and BlobContainerClient.
    /// </summary>
    /// <param name="agent">The ChatCompletionAgent instance to be used for tutoring.</param>
    /// <param name="blobContainerClient">The BlobContainerClient instance for accessing Azure Blob Storage.</param>
    public MandatoryLearningAgent(ChatCompletionAgent agent, BlobContainerClient blobContainerClient)
    {
        Agent = agent;
        _blobContainerClient = blobContainerClient;
    }

    /// <summary>
    /// Retrieves a list of mandatory learning resources from Azure Blob Storage.
    /// </summary>
    /// <returns>A collection of MandatoryLearningResource objects.</returns>
    public async Task<IEnumerable<MandatoryLearningResource>> GetMandatoryLearningResourcesAsync()
    {
        var resources = new List<MandatoryLearningResource>();

        // Define the Downloads folder as the target directory
        var downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        // Ensure the directory exists (it usually does, but this is a safeguard)
        if (!Directory.Exists(downloadsFolder))
        {
            Directory.CreateDirectory(downloadsFolder);
            AgentHelper.LogAgentMessage($"Downloads folder created: {downloadsFolder}");
        }

        // List blobs in the container
        await foreach (var blobItem in _blobContainerClient.GetBlobsAsync())
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobItem.Name);

            // Fetch metadata
            var properties = await blobClient.GetPropertiesAsync();
            var title = properties.Value.Metadata.ContainsKey("title")
                ? properties.Value.Metadata["title"]
                : blobItem.Name; // Use blob name as fallback if no title metadata

            // Fetch content and content type
            var contentType = properties.Value.ContentType;
            var downloadResponse = await blobClient.DownloadContentAsync();
            var content = downloadResponse.Value.Content.ToString();

            // Add the resource to the list
            resources.Add(new MandatoryLearningResource
            {
                Title = title,
                ContentUri = blobClient.Uri.ToString(),
                Content = content,
                Type = contentType
            });
        }

        return resources;
    }

    /// <summary>
    /// Downloads a specific mandatory learning resource to the user's local Downloads folder.
    /// </summary>
    /// <param name="resource">The MandatoryLearningResource to download.</param>
    /// <returns>The local file path where the resource was saved, or an empty string if the download fails.</returns>
    public async Task<string> DownloadResourceAsync(MandatoryLearningResource resource)
    {
        try
        {
            // Define the Downloads folder path
            var downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            // Ensure the directory exists
            if (!Directory.Exists(downloadsFolder))
            {
                Directory.CreateDirectory(downloadsFolder);
            }

            // Define the local file path
            string localFilePath = Path.Combine(downloadsFolder, resource.Title);

            // Download the resource
            var blobClient = _blobContainerClient.GetBlobClient(resource.Title);
            await blobClient.DownloadToAsync(localFilePath);

            return localFilePath; // Return the path where the file was saved
        }
        catch (Exception ex)
        {
            // Log the error and return an empty string
            AgentHelper.LogAgentMessage($"An error occurred while downloading the resource: {ex.Message}");
            return string.Empty;
        }
    }
}

/// <summary>
/// A factory class for building and configuring instances of the MandatoryLearningAgent.
/// </summary>
public static class MandatoryLearningAgentFactory
{
    /// <summary>
    /// Builds and configures a MandatoryLearningAgent instance using the provided configuration and shared services.
    /// </summary>
    /// <param name="config">The configuration object containing settings for the agent.</param>
    /// <param name="sharedServices">A collection of shared services to be added to the agent's kernel.</param>
    /// <returns>A configured instance of the MandatoryLearningAgent.</returns>
    public static MandatoryLearningAgent Build(IConfiguration config, IServiceCollection sharedServices)
    {
        // Retrieve configuration values for the mandatory learning agent and Azure Blob Storage.
        string deployment = config["mandatoryLearningAgent"]!;
        string endpoint = config["endpoint"]!;
        string apiKey = config["apiKey"]!;
        string blobServiceEndpoint = config["AzureBlobServiceEndpoint"]!;
        string containerName = config["resourceContainerName"]!;
        string storageAccountKey = config["AzureBlobAccountKey"]!;
        string blobStorageAccountName = config["AzureBlobStorageAccountName"]!;

        // Create a kernel builder and add shared services.
        var builder = Kernel.CreateBuilder();
        foreach (var service in sharedServices)
        {
            builder.Services.Add(service);
        }

        // Add Azure OpenAI ChatCompletion capabilities to the kernel.
        builder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey);

        // Build the kernel and configure the BlobContainerClient.
        var kernel = builder.Build();
        var blobServiceClient = new BlobServiceClient(new Uri(blobServiceEndpoint), new Azure.Storage.StorageSharedKeyCredential(blobStorageAccountName, storageAccountKey));
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

        // Configure the ChatCompletionAgent with instructions for tutoring.
        var agent = new ChatCompletionAgent
        {
            Name = AgentNames.MandatoryLearningAgent,
            Kernel = kernel,
            Instructions = $"""
                 Do not use Markdown formatting in your responses. Use plain text only. eg. no ***, ```, **, __ or *.
                 Your role is to act as a tutor for the student, focusing only on the given content. 
                 Encourage the student to ask questions and clarify doubts. 
                 Motivate the student by acknowledging their progress and efforts. 
                 Inform the student that the resource has been downloaded to their local machine and ask them to review it. 
                 when they are finished recap the resource and check their learning.
                 Once they have no more questions ask if they would like to continue onto the next resource in their learning plan or stop learning using continue/stop response.
                 """
        };

        return new MandatoryLearningAgent(agent, blobContainerClient);
    }
}



