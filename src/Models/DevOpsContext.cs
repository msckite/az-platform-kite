namespace MSCKite.Azure.Platform.Models
{
    public class DevOpsContext
    {
        public string Account { get; set; }
        public string Organization { get; set; }
        public string CollectionUri { get; set; }
        public bool IsSignedIn { get; set; }
    }
}
