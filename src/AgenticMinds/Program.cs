using AgenticMinds;
using AgenticMinds.Agents;
using AgenticMinds.ProcessSteps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

// Build the configuration from user secrets
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

// Create a kernel builder for Azure OpenAI chat completion
var builder = Kernel.CreateBuilder();

// Shared services collection for dependency injection
var sharedServices = new ServiceCollection();

// Add logging if debugging is enabled in the configuration
if (configuration["DEBUG"] == "true")
{
    sharedServices.AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddConsole();
        loggingBuilder.SetMinimumLevel(LogLevel.Information);
    });
}

// Build agents using their respective factories
var assessment = AssessmentAgentFactory.Build(configuration, sharedServices);
var feedback = FeedbackAgentFactory.Build(configuration, sharedServices);
var preferencePlanning = PreferencePlanningAgentFactory.Build(configuration, sharedServices);
var scheduling = SchedulingAgentFactory.Build(configuration, sharedServices);
var tutor = TutorAgentFactory.Build(configuration, sharedServices);
var mandatoryLearning = MandatoryLearningAgentFactory.Build(configuration, sharedServices);
var materialResourceAgent = MaterialResourceAgentFactory.Build(configuration, sharedServices);
var examination = ExaminationAgentFactory.Build(configuration, sharedServices);

// Register agents as singletons in the kernel's service collection
builder.Services.AddSingleton(assessment);
builder.Services.AddSingleton(feedback);
builder.Services.AddSingleton(preferencePlanning);
builder.Services.AddSingleton(scheduling);
builder.Services.AddSingleton(tutor);
builder.Services.AddSingleton(mandatoryLearning);
builder.Services.AddSingleton(materialResourceAgent);
builder.Services.AddSingleton(examination);

// Create a process builder for the learning cycle
var processBuilder = new ProcessBuilder("DocumentationCycle");

// Add process steps to the process builder
var greetingStep = processBuilder.AddStepFromType<GreetingStep>();
var assessmentStep = processBuilder.AddStepFromType<AssessmentStep>();
var feedbackStep = processBuilder.AddStepFromType<FeedbackStep>();
var planningStep = processBuilder.AddStepFromType<PlanningStep>();
var schedulingStep = processBuilder.AddStepFromType<SchedulingStep>();
var learningStep = processBuilder.AddStepFromType<LearningStep>();
var examinationStep = processBuilder.AddStepFromType<ExaminationStep>();
var examinationFeedbackStep = processBuilder.AddStepFromType<ExaminationFeedbackStep>();
var mandatoryLearningStep = processBuilder.AddStepFromType<MandatoryLearningStep>();

// Define the orchestration of process steps based on events
processBuilder.OnInputEvent(ProcessEventNames.Start)
   .SendEventTo(new(greetingStep, functionName: "Greet"));

// Greeting step event handling
greetingStep
    .OnEvent(ProcessEventNames.GreetingCompletedContinueLearning)
    .SendEventTo(new(learningStep, functionName: "Learn", parameterName: "learningPlan"));

greetingStep
    .OnEvent(ProcessEventNames.GreetingCompletedNewLearning)
    .SendEventTo(new(assessmentStep, functionName: "Assess"));

greetingStep
    .OnEvent(ProcessEventNames.GreetingCompletedMandatoryTraining)
    .SendEventTo(new(mandatoryLearningStep, functionName: "Learn"));

// Assessment step event handling
assessmentStep
    .OnEvent(ProcessEventNames.AssessmentCompleted)
    .SendEventTo(new(feedbackStep, functionName: "Feedback", parameterName: "assessmentResults"));

// Feedback step event handling
feedbackStep
    .OnEvent(ProcessEventNames.FeedbackCompleted)
    .SendEventTo(new(planningStep, functionName: "Plan", parameterName: "assessmentResults"));

// Planning step event handling
planningStep
    .OnEvent(ProcessEventNames.PlanningCompleted)
    .SendEventTo(new(schedulingStep, functionName: "Schedule", parameterName: "planningResult"));

// Scheduling step event handling
schedulingStep
    .OnEvent(ProcessEventNames.SchedulingCompleted)
    .SendEventTo(new(learningStep, functionName: "Learn"));

// Learning step event handling
learningStep
    .OnEvent(ProcessEventNames.ContinueLearning)
    .SendEventTo(new(learningStep, functionName: "Learn", parameterName: "learningPlan"));

learningStep
    .OnEvent(ProcessEventNames.StopLearning)
    .SendEventTo(new(greetingStep, functionName: "Greet"));

learningStep
    .OnEvent(ProcessEventNames.LearningCompleted)
    .SendEventTo(new(examinationStep, functionName: "Assess", parameterName: "learningPlan"));

// Examination step event handling
examinationStep
    .OnEvent(ProcessEventNames.ExaminationCompletedPassed)
    .SendEventTo(new(examinationFeedbackStep, functionName: "ExaminationFeedback", parameterName: "examinationResults"));

examinationStep
    .OnEvent(ProcessEventNames.ExaminationCompletedFailed)
    .SendEventTo(new(learningStep, functionName: "Learn", parameterName: "learningPlan"));

// Mandatory learning step event handling
mandatoryLearningStep
    .OnEvent(ProcessEventNames.MandatoryContinueLearning)
    .SendEventTo(new(mandatoryLearningStep, functionName: "Learn"));

mandatoryLearningStep
    .OnEvent(ProcessEventNames.MandatoryLearningCompleted)
    .SendEventTo(new(examinationStep, functionName: "Assess", parameterName: "learningPlan"));

mandatoryLearningStep
    .OnEvent(ProcessEventNames.MandatoryStopLearning)
    .SendEventTo(new(greetingStep, functionName: "Greet"));

// Examination feedback step event handling
examinationFeedbackStep
    .OnEvent(ProcessEventNames.ExaminationFeedbackCompleted)
    .SendEventTo(new(greetingStep, functionName: "Greet"));

// Build and start the process
var process = processBuilder.Build();
await process.StartAsync(builder.Build(), new KernelProcessEvent { Id = ProcessEventNames.Start });
