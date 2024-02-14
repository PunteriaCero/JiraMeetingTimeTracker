namespace JiraDriver
{
    public interface IClass1
    {
        string Name { get; }
        Task DeleteWork(string key, DateTime startDate, CancellationToken token = default);
        Task<string> GetName(CancellationToken token = default);
        string? GetSummary(string key);
        Task<JiraWorkLog?> GetWork(string key, DateTime startDate, CancellationToken token = default);
        Task<JiraWorkLog> RegisterWork(string key, string time, DateTime startDate, string? comment = null, CancellationToken token = default);
        Task<IDictionary<string, string>> GetProjects(CancellationToken token);
    }
}