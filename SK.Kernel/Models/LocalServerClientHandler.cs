using Microsoft.Extensions.Logging;

namespace SK.Kernel.Models;

public class LocalServerClientHandler : HttpClientHandler
{
    private readonly Uri _baseUri;
    private readonly ILogger<LocalServerClientHandler> _logger;

    public LocalServerClientHandler(string url, ILogger<LocalServerClientHandler> logger)
    {
        _baseUri = new Uri(url);
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Combinar la URI base con la URI de la solicitud
        if (!request.RequestUri.IsAbsoluteUri)
        {
            request.RequestUri = new Uri(_baseUri, request.RequestUri);
        }

        // Log the request using ILogger
        _logger.LogInformation("Sending request to: {Uri}", request.RequestUri);
        if (request.Content != null)
        {
            string requestContent = await request.Content.ReadAsStringAsync();
            _logger.LogInformation("Request Content: {RequestContent}", requestContent);
        }

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending the request: {Message}", ex.Message);
            throw;
        }

        // Log the response using ILogger
        _logger.LogInformation("Received response with status code: {StatusCode}", response.StatusCode);
        if (response.Content != null)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response Content: {ResponseContent}", responseContent);
        }

        return response;
    }
}
