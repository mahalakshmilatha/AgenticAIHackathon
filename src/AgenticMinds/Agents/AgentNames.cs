namespace AgenticMinds.Agents
{
    /// <summary>
    /// A static class containing the names of agents used in the AgenticMinds system.
    /// These names are used to identify and configure specific agents throughout the application.
    /// </summary>
    public static class AgentNames
    {
        /// <summary>
        /// The agent responsible for gathering user preferences to create a personalized learning plan.
        /// </summary>
        public const string PreferencePlanningAgent = "PreferencePlanningAgent";

        /// <summary>
        /// The agent responsible for conducting knowledge assessments to evaluate the user's current competency level.
        /// </summary>
        public const string AssessmentAgent = "AssessmentAgent";

        /// <summary>
        /// The agent responsible for collecting and processing user feedback to improve the learning experience.
        /// </summary>
        public const string FeedbackAgent = "FeedbackAgent";

        /// <summary>
        /// The agent responsible for managing and providing access to learning materials.
        /// </summary>
        public const string MaterialResourceAgent = "MaterialResourceAgent";

        /// <summary>
        /// The agent responsible for creating and managing the user's study schedule and tracking progress.
        /// </summary>
        public const string SchedulingProgressAgent = "SchedulingProgressAgent";

        /// <summary>
        /// The agent responsible for tutoring the user and guiding them through learning resources.
        /// </summary>
        public const string LearningAgent = "LearningAgent";

        /// <summary>
        /// The agent responsible for ensuring the completion of mandatory learning resources.
        /// </summary>
        public const string MandatoryLearningAgent = "MandatoryLearningAgent";

        /// <summary>
        /// The agent responsible for administering exams to evaluate the user's mastery of the subject.
        /// </summary>
        public const string ExaminationAgent = "ExaminationAgent";
    }
}
