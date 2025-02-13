namespace CF.AspireApp.Llm.ServiceDefaults.Models;


public record ChatResponse(string AuthorName,
                           string Role,
                           string Content,
                           string ModelId,
                           DateTime Timestamp);
