namespace CF.AspireApp.Llm.Web.Components.Pages.Llm.Model;

public class ProcessArticlesRequest
{
    public List<DicDataRag> articles { get; set; }
    public string userInput { get; set; }
}

public class DicDataRag
{
    public string Id { get; set; }
    public string Text { get; set; }
}