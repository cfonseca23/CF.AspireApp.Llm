using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SK.Kernel.Plugins;

public sealed class TimePlugin
{
    [KernelFunction]
    [Description("Retrieves the current time in UTC")]
    public string GetCurrentUtcTime() => DateTime.UtcNow.ToString("R");
}