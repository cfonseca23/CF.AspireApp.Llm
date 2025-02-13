using Microsoft.Extensions.VectorData;

namespace SK.Kernel.Data;

public class DicDataRag
{
    [VectorStoreRecordKey]
    public string Id { get; set; }

    [VectorStoreRecordData]
    public string Text { get; set; }

    [VectorStoreRecordVector(384, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}
