public enum QuestStatus
{
    Locked,          // Player doesn't meet requirements (level or prerequisite quests)
    Available,       // Ready to accept
    InProgress,      // Accepted, working on objectives
    ReadyToDeliver,  // All objectives complete, go to quest receiver
    Completed        // Delivered and rewards received
}
