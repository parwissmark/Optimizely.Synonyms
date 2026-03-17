using EPiServer.Data.Dynamic;
using Synonyms.Plugins.Synonyms.Api.Dto;
using Synonyms.Plugins.Synonyms.DynamicDatastore;

namespace Synonyms.Plugins.Synonyms.Services;

public class SynonymsService(
    DynamicDataStoreFactory dataStoreFactory,
    ILanguageBranchRepository languageBranchRepository,
    IGraphSynonymsApiService graphApiService) : ISynonymsService
{
    public List<SynonymResponse> GetSynonyms()
    {
        var store = GetStore<SynonymEntry>();
        return store.LoadAll<SynonymEntry>()
            .OrderBy(item => item.Term, StringComparer.OrdinalIgnoreCase)
            .Select(ToDto)
            .ToList();
    }

    public SynonymResponse CreateSynonym(SynonymRequest request, string? performedBy)
    {
        var (term, synonyms, synonymSlot, languageRouting, direction) = Normalize(request);
        var store = GetStore<SynonymEntry>();
        var items = store.LoadAll<SynonymEntry>();

        if (items.Any(item =>
                string.Equals(item.Term, term, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.SynonymSlot, synonymSlot, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.LanguageRouting, languageRouting, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Direction, direction, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Synonym term '{term}' already exists for {synonymSlot} / {languageRouting} / {direction}.");
        }

        var now = DateTime.UtcNow;
        var entry = new SynonymEntry
        {
            Term = term,
            Synonyms = synonyms.ToArray(),
            SynonymSlot = synonymSlot,
            LanguageRouting = languageRouting,
            Direction = direction,
            IsPublished = false,
            UpdatedAt = now,
            UpdatedBy = performedBy
        };

        store.Save(entry);
        return ToDto(entry);
    }

    public SynonymResponse UpdateSynonym(Guid id, SynonymRequest request, string? performedBy)
    {
        var (term, synonyms, synonymSlot, languageRouting, direction) = Normalize(request);
        var store = GetStore<SynonymEntry>();
        var items = store.LoadAll<SynonymEntry>();
        var graphSynonymEntries = items.ToList();
        var entry = graphSynonymEntries.FirstOrDefault(existing => existing.Key == id);

        if (entry == null)
        {
            throw new KeyNotFoundException($"Synonym entry '{id}' was not found.");
        }

        if (graphSynonymEntries.Any(existing =>
                existing.Key != id
                && string.Equals(existing.Term, term, StringComparison.OrdinalIgnoreCase)
                && string.Equals(existing.SynonymSlot, synonymSlot, StringComparison.OrdinalIgnoreCase)
                && string.Equals(existing.LanguageRouting, languageRouting, StringComparison.OrdinalIgnoreCase)
                && string.Equals(existing.Direction, direction, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Synonym term '{term}' already exists for {synonymSlot} / {languageRouting} / {direction}.");
        }

        entry.Term = term;
        entry.Synonyms = synonyms.ToArray();
        entry.SynonymSlot = synonymSlot;
        entry.LanguageRouting = languageRouting;
        entry.Direction = direction;
        entry.IsPublished = false;
        entry.PublishedAt = null;
        entry.PublishedBy = null;
        entry.UpdatedAt = DateTime.UtcNow;
        entry.UpdatedBy = performedBy;

        store.Save(entry, entry.Id);
        return ToDto(entry);
    }

    public void DeleteSynonym(Guid id)
    {
        var store = GetStore<SynonymEntry>();
        var items = store.LoadAll<SynonymEntry>();
        var entry = items.FirstOrDefault(existing => existing.Key == id);

        if (entry == null)
        {
            throw new KeyNotFoundException($"Synonym entry '{id}' was not found.");
        }

        store.Delete(entry.Id);
    }

    public async Task<GraphPublishResponse> PublishSynonyms(GraphPublishRequest request, string? performedBy)
    {
        var now = DateTime.UtcNow;
        var ids = request.Ids ?? new List<Guid>();
        var store = GetStore<SynonymEntry>();
        var items = store.LoadAll<SynonymEntry>();
        var itemsToPublish = ids.Count == 0
            ? items.ToList()
            : items.Where(item => ids.Contains(item.Key)).ToList();

        var grouped = itemsToPublish
            .GroupBy(item => new { item.SynonymSlot, item.LanguageRouting })
            .ToList();

        var failures = new List<string>();
        var publishedCount = 0;

        foreach (var group in grouped)
        {
            var synonymSlot = group.Key.SynonymSlot;
            var languageRouting = group.Key.LanguageRouting;
            var payload = BuildSynonymsFile(group);

            var result = await graphApiService.PublishSynonyms(synonymSlot, languageRouting, payload, IsEnabledLanguage);
            if (!result.IsSuccess)
            {
                failures.Add(result.Message ?? "Publish failed.");
                continue;
            }

            foreach (var item in group)
            {
                item.IsPublished = true;
                item.PublishedAt = now;
                item.PublishedBy = performedBy;
                item.UpdatedAt = now;
                item.UpdatedBy = performedBy;

                store.Save(item, item.Id);
                publishedCount++;
            }
        }

        var logStore = GetStore<SynonymPublishLogEntry>();
        var logEntry = new SynonymPublishLogEntry
        {
            Action = "Publish",
            Message = ids.Count == 0
                ? $"Published {publishedCount} synonym entries to Graph."
                : $"Published {publishedCount} selected synonym entries to Graph.",
            OccurredAt = now,
            PerformedBy = performedBy
        };
        logStore.Save(logEntry);
        TrimPublishLog(logStore);

        return new GraphPublishResponse
        {
            IsSuccess = failures.Count == 0,
            PublishedCount = publishedCount,
            PublishedAt = now,
            PublishedBy = performedBy,
            Message = failures.Count == 0
                ? "Publish succeeded."
                : string.Join(" | ", failures)
        };
    }

    public List<SynonymLogEntry> GetPublishLog()
    {
        var logStore = GetStore<SynonymPublishLogEntry>();
        return logStore.LoadAll<SynonymPublishLogEntry>()
            .OrderByDescending(entry => entry.OccurredAt)
            .Select(ToDto)
            .ToList();
    }

    public List<string> GetLanguages()
    {
        return languageBranchRepository
            .ListEnabled()
            .Select(language => language.Culture.Name)
            .OrderBy(language => language)
            .ToList();
    }

    public async Task<GraphVerifyResult> VerifyUpload(string synonymSlot, string languageRouting)
    {
        return await graphApiService.VerifyUpload(synonymSlot, languageRouting, IsEnabledLanguage);
    }

    private (string Term, List<string> Synonyms, string SynonymSlot, string LanguageRouting, string Direction) Normalize(SynonymRequest request)
    {
        var term = (request.Term ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            throw new ArgumentException("Term is required.");
        }

        var synonyms = (request.Synonyms ?? new List<string>())
            .Select(item => item?.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (synonyms.Count == 0)
        {
            throw new ArgumentException("At least one synonym is required.");
        }

        var synonymSlot = (request.SynonymSlot ?? string.Empty).Trim().ToLowerInvariant();
        if (synonymSlot != "one" && synonymSlot != "two")
        {
            throw new ArgumentException("Synonym slot must be 'one' or 'two'.");
        }

        var languageRouting = (request.LanguageRouting ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(languageRouting))
        {
            throw new ArgumentException("Language is required.");
        }

        if (!IsEnabledLanguage(languageRouting))
        {
            throw new ArgumentException($"Language '{languageRouting}' is not enabled.");
        }

        var direction = (request.Direction ?? string.Empty).Trim().ToLowerInvariant();
        if (direction != "equivalent" && direction != "replacement")
        {
            throw new ArgumentException("Direction must be 'equivalent' or 'replacement'.");
        }

        return (term, synonyms, synonymSlot, languageRouting, direction);
    }

    private bool IsEnabledLanguage(string languageRouting)
    {
        var enabledLanguages = languageBranchRepository
            .ListEnabled()
            .Select(language => language.Culture.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return enabledLanguages.Contains(languageRouting);
    }

    private string BuildSynonymsFile(IEnumerable<SynonymEntry> entries)
    {
        var lines = new List<string>();

        foreach (var entry in entries.OrderBy(item => item.Term, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(entry.Term))
            {
                continue;
            }

            var rightSide = entry.Synonyms
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (rightSide.Count == 0)
            {
                continue;
            }

            if (string.Equals(entry.Direction, "replacement", StringComparison.OrdinalIgnoreCase))
            {
                lines.Add($"{entry.Term} => {string.Join(", ", rightSide)}");
            }
            else
            {
                var values = new List<string> { entry.Term };
                values.AddRange(rightSide);
                lines.Add(string.Join(", ", values.Distinct(StringComparer.OrdinalIgnoreCase)));
            }
        }

        return string.Join("\n", lines);
    }

    private static void TrimPublishLog(DynamicDataStore logStore, int maxEntries = 10)
    {
        var entries = logStore.LoadAll<SynonymPublishLogEntry>()
            .OrderByDescending(entry => entry.OccurredAt)
            .ToList();

        if (entries.Count <= maxEntries)
        {
            return;
        }

        foreach (var entry in entries.Skip(maxEntries))
        {
            logStore.Delete(entry.Id);
        }
    }

    private DynamicDataStore GetStore<T>()
    {
        return dataStoreFactory.GetStore(typeof(T)) ?? dataStoreFactory.CreateStore(typeof(T));
    }

    private static SynonymResponse ToDto(SynonymEntry item)
    {
        return new SynonymResponse
        {
            Id = item.Key,
            Term = item.Term,
            Synonyms = item.Synonyms.ToList(),
            SynonymSlot = item.SynonymSlot,
            LanguageRouting = item.LanguageRouting,
            Direction = item.Direction,
            IsPublished = item.IsPublished,
            UpdatedAt = item.UpdatedAt,
            UpdatedBy = item.UpdatedBy,
            PublishedAt = item.PublishedAt,
            PublishedBy = item.PublishedBy
        };
    }

    private static SynonymLogEntry ToDto(SynonymPublishLogEntry item)
    {
        return new SynonymLogEntry
        {
            Id = item.Key,
            Action = item.Action,
            Message = item.Message,
            OccurredAt = item.OccurredAt,
            PerformedBy = item.PerformedBy
        };
    }
}
