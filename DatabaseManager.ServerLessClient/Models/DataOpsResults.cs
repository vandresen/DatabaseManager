namespace DatabaseManager.ServerLessClient.Models
{
    public class DataOpsResults
    {
        public string statusQueryGetUri { get; set; }
        public string sendEventPostUri { get; set; }
        public string terminatePostUri { get; set; }
        public string purgeHistoryDeleteUri { get; set; }
    }
}
