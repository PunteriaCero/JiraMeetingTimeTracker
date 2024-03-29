using System.Diagnostics;
using JiraDriver;

namespace JiraDriverTest;

public class Tests
{
    private Class1 clase;

    [SetUp]
    public async Task Setup()
    {
        const string TOKEN = "ATATT3xFfGF0p9Euyn1xfkKuceU0MNjX-vWkJoD3WrsZBoYCjJRAWQEwwUmSYBU95_v7-IUH-p8ltpPThDDKK0hUbain0vX_VQigRY0JQQYtzytI1X4CEvZ1vze55ucHHLYn0A-zc93Xfl-dLrPBN3e78A9F24eC8DwYjBoPHp8P1xueHbSSiK0=B34FDD77"; 
        clase = await Class1.Init("https://baufest.atlassian.net/", "hlavrencic@baufest.com", TOKEN);
    }

    [Ignore("TOKEN")]
    [Test]
    public void Test1()
    {
        var issue = clase.GetSummary("SCT310-4771");
        
        Assert.That(issue, Is.EqualTo("DRP - Analisis de riesgos"));
    }

    [Ignore("TOKEN")]
    [Test]
    public async Task Test2(){
        await clase.RegisterWork("SCT310-4771", "1m", DateTime.Now, "prueba API");
        Assert.Pass();
    }
}