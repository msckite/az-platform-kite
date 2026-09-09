namespace MSCKite.Azure.Platform.Models
{
    public class AzureContext
    {
        public string Account { get; set; }
        public string Tenant { get; set; }
        public string SubscriptionId { get; set; }
        public string SubscriptionName { get; set; }
        public string Environment { get; set; }
        public bool IsSignedIn { get; set; }
    }
}
