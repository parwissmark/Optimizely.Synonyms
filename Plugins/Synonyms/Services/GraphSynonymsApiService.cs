using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Synonyms.Plugins.Synonyms.Api.Dto;

namespace Synonyms.Plugins.Synonyms.Services
{
    public class GraphSynonymsApiService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        : IGraphSynonymsApiService
    {
        public async Task<GraphVerifyResult> VerifyUpload(string synonymSlot, string languageRouting, Func<string, bool> isEnabledLanguage)
        {
            if (string.IsNullOrWhiteSpace(synonymSlot) || string.IsNullOrWhiteSpace(languageRouting))
            {
                return new GraphVerifyResult
                {
                    IsSuccess = false,
                    Message = "Synonym slot and language are required for verification."
                };
            }

            if (!isEnabledLanguage(languageRouting))
            {
                return new GraphVerifyResult
                {
                    IsSuccess = false,
                    Message = $"Language '{languageRouting}' is not enabled."
                };
            }

            var result = await TryVerifyUpload(synonymSlot, languageRouting);
            if (result.IsSuccess)
            {
                return result;
            }

            var shortLanguage = TryGetShortLanguage(languageRouting);
            if (string.IsNullOrWhiteSpace(shortLanguage) || string.Equals(shortLanguage, languageRouting, StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }
            
            var fallback = await TryVerifyUpload(synonymSlot, shortLanguage);
            if (!fallback.IsSuccess)
            {
                return result;
            }
            
            fallback.Message = $"{fallback.Message} (Used language routing '{shortLanguage}').";
            return fallback;

        }

        public async Task<GraphVerifyResult> PublishSynonyms(string synonymSlot, string languageRouting, string payload, Func<string, bool> isEnabledLanguage)
        {
            if (string.IsNullOrWhiteSpace(synonymSlot) || string.IsNullOrWhiteSpace(languageRouting))
            {
                return new GraphVerifyResult
                {
                    IsSuccess = false,
                    Message = "Synonym slot and language are required for publish."
                };
            }

            if (!isEnabledLanguage(languageRouting))
            {
                return new GraphVerifyResult
                {
                    IsSuccess = false,
                    Message = $"Language '{languageRouting}' is not enabled."
                };
            }

            if (string.IsNullOrWhiteSpace(payload))
            {
                return new GraphVerifyResult
                {
                    IsSuccess = false,
                    Message = $"No synonyms to publish for {synonymSlot} / {languageRouting}."
                };
            }

            var (gatewayAddress, appKey, secret) = GetGraphCredentials();
            if (string.IsNullOrWhiteSpace(gatewayAddress) || string.IsNullOrWhiteSpace(appKey) || string.IsNullOrWhiteSpace(secret))
            {
                return new GraphVerifyResult
                {
                    IsSuccess = false,
                    Message = "Content Graph credentials are missing in configuration."
                };
            }

            var client = httpClientFactory.CreateClient();
            var requestUri = $"{gatewayAddress}/resources/synonyms?synonym_slot={Uri.EscapeDataString(synonymSlot)}&language_routing={Uri.EscapeDataString(languageRouting)}";
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{appKey}:{secret}"));

            var requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new StringContent(payload, Encoding.UTF8, "text/plain")
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            var response = await client.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                return new GraphVerifyResult
                {
                    IsSuccess = false,
                    Message = $"Publish failed for {synonymSlot} / {languageRouting} ({(int)response.StatusCode})."
                };
            }

            return new GraphVerifyResult
            {
                IsSuccess = true,
                Message = $"Published {synonymSlot} / {languageRouting}."
            };
        }

        private async Task<GraphVerifyResult> TryVerifyUpload(string synonymSlot, string languageRouting)
        {
            var (gatewayAddress, appKey, secret) = GetGraphCredentials();
            if (string.IsNullOrWhiteSpace(gatewayAddress) || string.IsNullOrWhiteSpace(appKey) || string.IsNullOrWhiteSpace(secret))
            {
                return new GraphVerifyResult
                {
                    IsSuccess = false,
                    Message = "Content Graph credentials are missing in configuration."
                };
            }

            var client = httpClientFactory.CreateClient();
            var requestUri = $"{gatewayAddress}/resources/synonyms?synonym_slot={Uri.EscapeDataString(synonymSlot)}&language_routing={Uri.EscapeDataString(languageRouting)}";
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{appKey}:{secret}"));

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            requestMessage.Headers.Accept.Clear();
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            var response = await client.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var details = string.IsNullOrWhiteSpace(content) ? string.Empty : $" {Truncate(content, 200)}";
                return new GraphVerifyResult
                {
                    IsSuccess = false,
                    Message = $"Graph verification failed ({(int)response.StatusCode}). Endpoint: {requestUri}.{details}",
                };
            }

            var lines = ExtractSynonymLines(content);
            var count = lines.Count > 0 ? lines.Count : TryGetSynonymCount(content);
            return new GraphVerifyResult
            {
                IsSuccess = true,
                Count = count,
                Lines = lines,
                Message = count > 0
                    ? $"Verified {count} synonym entries for {synonymSlot} / {languageRouting}."
                    : $"Verification succeeded but no entries were returned for {synonymSlot} / {languageRouting}."
            };
        }

        private (string? GatewayAddress, string? AppKey, string? Secret) GetGraphCredentials()
        {
            var gatewayAddress = configuration["Optimizely:ContentGraph:GatewayAddress"]?.TrimEnd('/');
            var appKey = configuration["Optimizely:ContentGraph:AppKey"]?.Trim();
            var secret = configuration["Optimizely:ContentGraph:Secret"]?.Trim();
            return (gatewayAddress, appKey, secret);
        }

        private static string? TryGetShortLanguage(string languageRouting)
        {
            if (string.IsNullOrWhiteSpace(languageRouting))
            {
                return null;
            }

            var dashIndex = languageRouting.IndexOf('-');
            return dashIndex > 0 ? languageRouting[..dashIndex] : null;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Length <= maxLength ? value : value[..maxLength] + "...";
        }

        private static int TryGetSynonymCount(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return 0;
            }

            try
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return doc.RootElement.GetArrayLength();
                }

                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("synonyms", out var synonymsElement)
                        && synonymsElement.ValueKind == JsonValueKind.Array)
                    {
                        return synonymsElement.GetArrayLength();
                    }
                }
            }
            catch (JsonException)
            {
                return CountPlainTextEntries(content);
            }

            return CountPlainTextEntries(content);
        }

        private static int CountPlainTextEntries(string content)
        {
            return content
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Count(line => !string.IsNullOrWhiteSpace(line));
        }

        private static List<string> ExtractSynonymLines(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new List<string>();
            }

            return content
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
        }
    }
}
