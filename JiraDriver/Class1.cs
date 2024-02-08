using System.Security.Principal;
using Atlassian.Jira;

namespace JiraDriver;

public class Class1
{

    private readonly Jira jira;

    public Class1(Jira jira){
        this.jira = jira;
    }

    public static Class1 Init(string url, string user, string password){
        return new Class1(Jira.CreateRestClient(url, user, password));
    }

    public string? GetSummary(string key){
        var issue = GetIssue(key);

        return issue?.Summary;
    }

    public async Task RegisterWork(string key, string time, DateTime startDate, string? comment = null, CancellationToken token = default){
        var issue = GetIssue(key);

        if(issue == null) throw new ArgumentNullException();

        var work = new Worklog(time, startDate, comment);
        await issue.AddWorklogAsync(work, WorklogStrategy.AutoAdjustRemainingEstimate, null, token);
    }

    public async Task DeleteWork(string key, string time, DateTime startDate, string? comment = null, CancellationToken token = default){
        var issue = GetIssue(key);

        if(issue == null) throw new ArgumentNullException();
        
        var worklogs = await issue.GetWorklogsAsync(token);
        worklogs = worklogs.Where(w => w.TimeSpent == time && w.StartDate == startDate);
        if(comment != null) {
            worklogs = worklogs.Where(w => w.Comment == comment);
        }

        worklogs.OrderByDescending(o => o.CreateDate);

        var worklog = worklogs.First();

        await issue.DeleteWorklogAsync(worklog, WorklogStrategy.AutoAdjustRemainingEstimate, null, token);
    }

    private Issue? GetIssue(string key){
        var issue = from i in jira.Issues.Queryable
            where i.Key == key
            select i;
        return issue.FirstOrDefault();
    }
}
