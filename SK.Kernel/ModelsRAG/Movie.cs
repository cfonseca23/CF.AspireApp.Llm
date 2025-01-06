using Microsoft.Extensions.VectorData;

namespace SK.Kernel.ModelsRAG;

public class Movie
{
    [VectorStoreRecordKey]
    public int Key { get; set; }

    [VectorStoreRecordData]
    public string Title { get; set; }

    [VectorStoreRecordData]
    public string Description { get; set; }

    [VectorStoreRecordVector(384, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}
