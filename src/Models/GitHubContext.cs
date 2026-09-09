namespace MSCKite.Azure.Platform.Models
{
    public class GitHubContext
    {
        public string Account { get; set; }        
        public string Host { get; set; }
        public string Protocol { get; set; }
        public string TokenScopes { get; set; }
        public bool IsSignedIn { get; set; }
    }
}
