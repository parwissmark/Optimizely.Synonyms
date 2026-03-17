using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace Synonyms.Plugins.Synonyms.DynamicDatastore;

[EPiServerDataStore]
[EPiServerDataContract]
public class SynonymEntry
{
    public SynonymEntry()
    {
        Key = Guid.NewGuid();
        Id = Identity.NewIdentity(Key);
        Synonyms = Array.Empty<string>();
        Term = string.Empty;
        Direction = "equivalent";
    }

    [EPiServerDataMember]
    public Identity Id { get; set; }

    [EPiServerDataMember]
    public Guid Key { get; set; }

    [EPiServerDataMember]
    public string Term { get; set; }

    [EPiServerDataMember]
    public string[] Synonyms { get; set; }

    [EPiServerDataMember]
    public string SynonymSlot { get; set; } = string.Empty;

    [EPiServerDataMember]
    public string LanguageRouting { get; set; } = string.Empty;

    [EPiServerDataMember]
    public string Direction { get; set; }

    [EPiServerDataMember]
    public bool IsPublished { get; set; }

    [EPiServerDataMember]
    public DateTime UpdatedAt { get; set; }

    [EPiServerDataMember]
    public string? UpdatedBy { get; set; }

    [EPiServerDataMember]
    public DateTime? PublishedAt { get; set; }

    [EPiServerDataMember]
    public string? PublishedBy { get; set; }
}
