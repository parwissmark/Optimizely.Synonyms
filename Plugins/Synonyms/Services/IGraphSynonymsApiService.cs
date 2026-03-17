using Synonyms.Plugins.Synonyms.Api.Dto;

namespace Synonyms.Plugins.Synonyms.Services;

public interface IGraphSynonymsApiService
{
    Task<GraphVerifyResult> VerifyUpload(string synonymSlot, string languageRouting, Func<string, bool> isEnabledLanguage);
    Task<GraphVerifyResult> PublishSynonyms(string synonymSlot, string languageRouting, string payload, Func<string, bool> isEnabledLanguage);
}