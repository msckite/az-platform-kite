namespace MSCKite.Azure.Platform.Models
{
    public class GitHubLabel
    {
        public string Name { get; set; }

        // Hex value without the leading '#' (e.g. "d73a4a")
        public string Color { get; set; }

        public string Description { get; set; }
    }
}
