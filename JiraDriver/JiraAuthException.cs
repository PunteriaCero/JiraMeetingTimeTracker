namespace JiraDriver
{
    public class JiraAuthException : Exception
    {
        public JiraAuthException(string? message) : base(message)
        {
        }

        public JiraAuthException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
