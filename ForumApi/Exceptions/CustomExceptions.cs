namespace ForumApi.Exceptions;

public class ContentModerationException : Exception
{
    public ContentModerationException(string message) : base(message) { }
}