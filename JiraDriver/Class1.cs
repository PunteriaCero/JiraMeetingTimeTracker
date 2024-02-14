using Atlassian.Jira;

namespace JiraDriver;

public class Class1 : IClass1
{

    private readonly Jira jira;

    public string Name { get; private set; }

    public Class1(Jira jira)
    {
        this.jira = jira;
    }

    public static async Task<Class1> Init(string url, string user, string password)
    {
        var imp = new Class1(Jira.CreateRestClient(url, user, password));
        imp.Name = await imp.GetName();
        return imp;
    }

    public string? GetSummary(string key)
    {
        var issue = GetIssue(key);

        return issue?.Summary;
    }

    public async Task<JiraWorkLog?> GetWork(string key, DateTime startDate, CancellationToken token = default)
    {
        var worklogs = await jira.Issues.GetWorklogsAsync(key, token);
        worklogs = worklogs.Where(w => w.StartDate == startDate);
        worklogs.OrderByDescending(o => o.CreateDate);

        var worklog = worklogs.FirstOrDefault();

        if (worklog == null) { return null; }

        return Map(worklog);
    }

    public async Task<JiraWorkLog> RegisterWork(string key, string time, DateTime startDate, string? comment = null, CancellationToken token = default)
    {
        var work = new Worklog(time, startDate, comment);
        var worklog = await jira.Issues.AddWorklogAsync(key, work, WorklogStrategy.AutoAdjustRemainingEstimate, null, token);
        return Map(worklog);
    }

    private static JiraWorkLog Map (Worklog worklog)
    {
        return new JiraWorkLog
        {
            Id = worklog.Id,
            StartDate = worklog.StartDate,
            Length = TimeSpan.FromSeconds(worklog.TimeSpentInSeconds),
            CreateDate = worklog.CreateDate
        };
    }

    public async Task DeleteWork(string key, DateTime startDate, CancellationToken token = default)
    {
        var worklog = await GetWork(key, startDate, token);

        if (worklog == null)
        {
            throw new KeyNotFoundException($"No work found for {key} at {startDate}");
        }

        await jira.Issues.DeleteWorklogAsync(key, worklog.Id, WorklogStrategy.AutoAdjustRemainingEstimate, null, token);
    }

    private Issue? GetIssue(string key)
    {
        var issue = from i in jira.Issues.Queryable
                    where i.Key == key
                    select i;
        return issue.FirstOrDefault();
    }

    public async Task<string> GetName(CancellationToken token = default)
    {
        var myself = await jira.Users.GetMyselfAsync(token);
        return myself.DisplayName;
    }

    public async Task<IDictionary<string, string>> GetProjects(CancellationToken token = default)
    {
        var projects = await jira.Projects.GetProjectsAsync(token);
        return projects.ToDictionary(p => p.Key, p => p.Name);
    }
}

public class JiraWorkLog
{
    public DateTime? CreateDate { get; internal set; }
    public TimeSpan Length { get; internal set; }
    public DateTime? StartDate { get; internal set; }
    public string Id { get; internal set; }
}