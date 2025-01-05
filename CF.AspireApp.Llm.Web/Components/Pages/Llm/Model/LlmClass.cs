namespace CF.AspireApp.Llm.Web.Components.Pages.Llm.Model;

public class StreamingMessage
{
    public string content { get; set; }
    public Metadata obj_metadata { get; set; }
}

public class Metadata
{
    public object? ModelId { get; set; }
    public object? ChoiceIndex { get; set; }
    public object? metadata { get; set; }
}
