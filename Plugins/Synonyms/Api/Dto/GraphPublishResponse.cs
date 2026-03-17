namespace Synonyms.Plugins.Synonyms.Api.Dto;

public class GraphPublishResponse
{
    public bool IsSuccess { get; set; }
    public int PublishedCount { get; set; }
    public DateTime PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
    public string? Message { get; set; }
}
