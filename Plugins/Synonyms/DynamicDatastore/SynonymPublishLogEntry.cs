using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace Synonyms.Plugins.Synonyms.DynamicDatastore;

[EPiServerDataStore]
[EPiServerDataContract]
public class SynonymPublishLogEntry
{
    public SynonymPublishLogEntry()
    {
        Key = Guid.NewGuid();
        Id = Identity.NewIdentity(Key);
        Action = string.Empty;
        Message = string.Empty;
    }

    [EPiServerDataMember]
    public Identity Id { get; set; }

    [EPiServerDataMember]
    public Guid Key { get; set; }

    [EPiServerDataMember]
    public string Action { get; set; }

    [EPiServerDataMember]
    public string Message { get; set; }

    [EPiServerDataMember]
    public DateTime OccurredAt { get; set; }

    [EPiServerDataMember]
    public string? PerformedBy { get; set; }
}
