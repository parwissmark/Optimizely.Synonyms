namespace Synonyms.Plugins.Synonyms.Api.Dto;

public class SynonymResponse
{
    public Guid Id { get; set; }
    public string Term { get; set; } = string.Empty;
    public List<string> Synonyms { get; set; } = new();
    public string SynonymSlot { get; set; } = string.Empty;
    public string LanguageRouting { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
}
