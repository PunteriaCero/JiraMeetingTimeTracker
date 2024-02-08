// See https://aka.ms/new-console-template for more information

using JiraDriver;



const string TOKEN = "ATATT3xFfGF0p9Euyn1xfkKuceU0MNjX-vWkJoD3WrsZBoYCjJRAWQEwwUmSYBU95_v7-IUH-p8ltpPThDDKK0hUbain0vX_VQigRY0JQQYtzytI1X4CEvZ1vze55ucHHLYn0A-zc93Xfl-dLrPBN3e78A9F24eC8DwYjBoPHp8P1xueHbSSiK0=B34FDD77"; 

var clase = Class1.Init("https://baufest.atlassian.net/", "hlavrencic@baufest.com", TOKEN);
await clase.RegisterWork("SCT310-4771", "1m", new DateTime(2024, 01, 30), "prueba API");

await clase.DeleteWork("SCT310-4771", "1m", new DateTime(2024, 01, 30), "prueba API");

