using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

using SK.Kernel.Models;

namespace SK.Kernel.Service;

public class KernelService
{
    private readonly Microsoft.SemanticKernel.Kernel _kernel;

    public KernelService(IOptions<KernelOptions> options)
    {
        var kernelOptions = options.Value;
        _kernel = KernelBuilderExtension.GetKernelBuilder(kernelOptions).Build().Clone();
    }

    public Microsoft.SemanticKernel.Kernel GetKernel() => _kernel.Clone();
}
