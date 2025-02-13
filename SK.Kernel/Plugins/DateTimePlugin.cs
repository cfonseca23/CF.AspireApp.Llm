using Microsoft.SemanticKernel;

using System.ComponentModel;

namespace SK.Kernel.Plugins;

public class DateTimePlugin
{
    [KernelFunction, Description("Obtener el nombre de la zona horaria local")]
    [return: Description("El nombre de la zona horaria local")]
    public static string TimeZone()
    {
        return TimeZoneInfo.Local.DisplayName;
    }

    [KernelFunction, Description("Obtener la fecha y hora actual")]
    [return: Description("La fecha y hora actual")]
    public static string DateWithTime()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
