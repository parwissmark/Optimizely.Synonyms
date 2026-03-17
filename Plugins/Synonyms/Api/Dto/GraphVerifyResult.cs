namespace Synonyms.Plugins.Synonyms.Api.Dto;

public class GraphVerifyResult
{
    public bool IsSuccess { get; set; }
    public int Count { get; set; }
    public string? Message { get; set; }
    public List<string> Lines { get; set; } = new();
}
