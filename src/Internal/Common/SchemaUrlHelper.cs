namespace MSCKite.Azure.Platform.Internal.Common
{
    internal static class SchemaUrlHelper
    {
        // Templates and schemas in the repository reference each other through main; a copy taken from a release tag must reference that tag instead,
        // otherwise an editor validates a pinned config against whatever main holds at the time
        private const string MainPrefix = "https://raw.githubusercontent.com/msckite/az-platform-kite/refs/heads/main/";

        internal static string PinToTag(string content, string tag)
        {
            return content.Replace(MainPrefix, $"https://raw.githubusercontent.com/msckite/az-platform-kite/refs/tags/{tag}/");
        }
    }
}
