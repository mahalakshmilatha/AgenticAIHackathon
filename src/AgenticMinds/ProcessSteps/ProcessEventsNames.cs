namespace AgenticMinds
{
    /// <summary>
    /// A static class containing constants for process event names used to orchestrate the workflow.
    /// These event names are used to trigger transitions between process steps.
    /// </summary>
    public static class ProcessEventNames
    {
        /// <summary>
        /// Event to start the process.
        /// </summary>
        public const string Start = "Start";

        // Greeting step events
        /// <summary>
        /// Event triggered when the greeting step is completed, and the user chooses to continue learning.
        /// </summary>
        public const string GreetingCompletedContinueLearning = "GreetingCompletedContinueLearning";

        /// <summary>
        /// Event triggered when the greeting step is completed, and the user opts for new learning.
        /// </summary>
        public const string GreetingCompletedNewLearning = "GreetingCompletedNewLearning";

        /// <summary>
        /// Event triggered when the greeting step is completed, and the user selects mandatory training.
        /// </summary>
        public const string GreetingCompletedMandatoryTraining = "GreetingCompletedMandatoryTraining";

        // Assessment step events
        /// <summary>
        /// Event triggered when the assessment step is completed.
        /// </summary>
        public const string AssessmentCompleted = "AssessmentCompleted";

        // Feedback step events
        /// <summary>
        /// Event triggered when the feedback step is completed.
        /// </summary>
        public const string FeedbackCompleted = "FeedbackCompleted";

        // Planning step events
        /// <summary>
        /// Event triggered when the planning step is completed.
        /// </summary>
        public const string PlanningCompleted = "PlanningCompleted";

        // Scheduling step events
        /// <summary>
        /// Event triggered when the scheduling step is completed.
        /// </summary>
        public const string SchedulingCompleted = "SchedulingCompleted";

        // Learning step events
        /// <summary>
        /// Event triggered when the user chooses to continue learning.
        /// </summary>
        public const string ContinueLearning = "ContinueLearning";

        /// <summary>
        /// Event triggered when the user chooses to stop learning.
        /// </summary>
        public const string StopLearning = "StopLearning";

        /// <summary>
        /// Event triggered when the learning step is completed.
        /// </summary>
        public const string LearningCompleted = "LearningCompleted";

        // Examination step events
        /// <summary>
        /// Event triggered when the examination step is completed, and the user passes the exam.
        /// </summary>
        public const string ExaminationCompletedPassed = "ExaminationCompletedPassed";

        /// <summary>
        /// Event triggered when the examination step is completed, and the user fails the exam.
        /// </summary>
        public const string ExaminationCompletedFailed = "ExaminationCompletedFailed";

        // Mandatory learning step events
        /// <summary>
        /// Event triggered when the user chooses to continue mandatory learning.
        /// </summary>
        public const string MandatoryContinueLearning = "MandatoryContinueLearning";

        /// <summary>
        /// Event triggered when the user chooses to stop mandatory learning.
        /// </summary>
        public const string MandatoryStopLearning = "MandatoryStopLearning";

        /// <summary>
        /// Event triggered when the mandatory learning step is completed.
        /// </summary>
        public const string MandatoryLearningCompleted = "MandatoryLearningCompleted";

        // Examination feedback step events
        /// <summary>
        /// Event triggered when the examination feedback step is completed.
        /// </summary>
        public const string ExaminationFeedbackCompleted = "ExaminationFeedbackCompleted";
    }
}
