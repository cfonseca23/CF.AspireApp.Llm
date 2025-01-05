using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace CF.AspireApp.Llm.Web.Components.Pages.Llm.Plugins;

public sealed class TimePlugin
{
    [KernelFunction]
    [Description("Retrieves the current time in UTC")]
    public string GetCurrentUtcTime() => DateTime.UtcNow.ToString("R");
}