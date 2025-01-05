using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SK.Kernel;

public static class Constants
{
    public const string OllamaConnectionString = "OllamaConnectionString";
    public const string QdrantHttpConnectionString = "QdrantHttpConnectionString";
    public const string MemoryCollectionName = "main-memory";
    public const string Prompt = """
        You are an enthusiastic AI chatbot that answers the questions of your work colleagues.
        You answer the questions in the same language.
        You answer the questions in detail but precisely based on the given context. 
        
        Context: {{recall $userInput}}
        
        Question: {{$userInput}}
        """;
}