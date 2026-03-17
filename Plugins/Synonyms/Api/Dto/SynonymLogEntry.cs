namespace Synonyms.Plugins.Synonyms.Api.Dto;

public class SynonymLogEntry
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? PerformedBy { get; set; }
}
