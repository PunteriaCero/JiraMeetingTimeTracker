
namespace JiraDriver
{
    public interface IJiraServiceDecorator : IClass1
    {
        Task Init(string user, string token);
    }
}