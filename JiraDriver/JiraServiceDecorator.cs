using System.Security.Authentication;

namespace JiraDriver
{
    public class JiraServiceDecorator : IJiraServiceDecorator
    {
        const string AUTH_MSG = "Authentication failed.";
        const string NO_INITIALIZED_MSG = "No initilized.";
        private IClass1? class1;

        public string Name => class1.Name;

        public async Task Init(string user, string token)
        {
            try
            {
                class1 = await Class1.Init("https://baufest.atlassian.net/", user, token);
            }
            catch (AuthenticationException ex)
            {
                throw new JiraAuthException(AUTH_MSG, ex);
            }
        }

        public Task DeleteWork(string key, DateTime startDate, CancellationToken token = default)
        {
            if (class1 == null) throw new JiraAuthException(NO_INITIALIZED_MSG);

            try
            {
                return class1.DeleteWork(key, startDate, token);
            }
            catch (AuthenticationException ex)
            {
                throw new JiraAuthException(AUTH_MSG, ex);
            }
            
        }

        public string GetSummary(string key)
        {
            if (class1 == null) throw new JiraAuthException(NO_INITIALIZED_MSG);

            try
            {
                return class1.GetSummary(key);
            }
            catch (AuthenticationException ex)
            {
                throw new JiraAuthException(AUTH_MSG, ex);
            }
        }

        public Task<JiraWorkLog?> GetWork(string key, DateTime startDate, CancellationToken token = default)
        {
            if (class1 == null) throw new JiraAuthException(NO_INITIALIZED_MSG);

            
            try
            {
                return class1.GetWork(key, startDate, token);
            }
            catch (AuthenticationException ex)
            {
                throw new JiraAuthException(AUTH_MSG, ex);
            }
        }

        public Task<JiraWorkLog> RegisterWork(string key, string time, DateTime startDate, string comment = null, CancellationToken token = default)
        {
            if (class1 == null) throw new JiraAuthException(NO_INITIALIZED_MSG);

            
            try
            {
                return class1.RegisterWork(key, time, startDate, comment, token);
            }
            catch (AuthenticationException ex)
            {
                throw new JiraAuthException(AUTH_MSG, ex);
            }
        }

        public Task<string> GetName(CancellationToken token = default)
        {
            if (class1 == null) throw new JiraAuthException(NO_INITIALIZED_MSG);
            
            try
            {
                return class1.GetName(token);
            }
            catch (AuthenticationException ex)
            {
                throw new JiraAuthException(AUTH_MSG, ex);
            }
        }

        public Task<IDictionary<string, string>> GetProjects(CancellationToken token)
        {
            if (class1 == null) throw new JiraAuthException(NO_INITIALIZED_MSG);

            try
            {
                return class1.GetProjects(token);
            }
            catch (AuthenticationException ex)
            {
                throw new JiraAuthException(AUTH_MSG, ex);
            }
        }
    }
}
