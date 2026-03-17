using Synonyms.Plugins.Synonyms.Api.Dto;

namespace Synonyms.Plugins.Synonyms.Services
{
    public interface ISynonymsService
    {
        List<SynonymResponse> GetSynonyms();
        SynonymResponse CreateSynonym(SynonymRequest request, string? performedBy);
        SynonymResponse UpdateSynonym(Guid id, SynonymRequest request, string? performedBy);
        void DeleteSynonym(Guid id);
        Task<GraphPublishResponse> PublishSynonyms(GraphPublishRequest request, string? performedBy);
        List<SynonymLogEntry> GetPublishLog();
        List<string> GetLanguages();
        Task<GraphVerifyResult> VerifyUpload(string synonymSlot, string languageRouting);
    }
}

