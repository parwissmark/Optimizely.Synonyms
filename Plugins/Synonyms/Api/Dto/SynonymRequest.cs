namespace Synonyms.Plugins.Synonyms.Api.Dto;

public class SynonymRequest
{
    public string? Term { get; set; }
    public List<string> Synonyms { get; set; } = new();
    public string? SynonymSlot { get; set; }
    public string? LanguageRouting { get; set; }
    public string? Direction { get; set; }
}
