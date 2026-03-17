# Synonyms Manager

This project contains a self-contained Optimizely CMS 12 plugin for managing and publishing Content Graph synonyms.

## Routes

- UI: `/plugins/synonyms`
- API: `/api/plugins/synonyms`

## NuGet packages

The plugin depends on these packages:

- `EPiServer.CMS`
- `Optimizely.ContentGraph.Cms`
- `Optimizely.ContentGraph.Core`

The solution also uses the Optimizely package feed configured in `nuget.config`.

## Required configuration

Add Content Graph credentials to `appsettings.json`:

```json
{
  "Optimizely": {
    "ContentGraph": {
      "GatewayAddress": "https://cg.optimizely.com",
      "AppKey": "your-app-key",
      "Secret": "your-app-secret"
    }
  }
}
```

For local development, `appsettings.Development.json` also contains:

- a LocalDB connection string for the CMS database
- mapped Optimizely CMS editor/admin roles

## Startup wiring

`Program.cs` bootstraps the site with `ConfigureCmsDefaults()`.

`Startup.cs` registers the services required by the plugin:

- `AddTransient<ISynonymsService, SynonymsService>()`
- `AddTransient<IGraphSynonymsApiService, GraphSynonymsApiService>()`
- 
## Files

- `Plugins/Synonyms/SynonymsMenuProvider.cs` adds the CMS menu entry
- `Plugins/Synonyms/SynonymsController.cs` serves the plugin shell view
- `Plugins/Synonyms/SynonymsIndex.cshtml` hosts the admin UI and Optimizely shell chrome
- `Plugins/Synonyms/Api/GraphSynonymsApiController.cs` exposes the JSON API
- `Plugins/Synonyms/Services/SynonymsService.cs` handles validation, DDS persistence, publish formatting, and log entries
- `Plugins/Synonyms/Services/GraphSynonymsApiService.cs` calls the Content Graph synonyms endpoint
- `Plugins/Synonyms/DynamicDatastore/SynonymEntry.cs` stores synonym records in DDS
- `Plugins/Synonyms/DynamicDatastore/SynonymPublishLogEntry.cs` stores publish log entries in DDS
- `wwwroot/synonyms/graph-synonyms.js` contains the frontend app
- `wwwroot/synonyms/plugin-styleguide.css` contains plugin styling

## How it works

The plugin stores synonym entries in Optimizely Dynamic Data Store, grouped by synonym slot and language routing. When an editor publishes entries, the service builds the plain-text synonym payload and uploads it to the Content Graph `/resources/synonyms` endpoint using basic authentication based on the configured `AppKey` and `Secret`.

The API also includes endpoints to:

- list synonyms
- create, update, and delete synonyms
- publish all or selected synonyms
- read the publish log
- list enabled CMS languages
- verify the uploaded synonym file in Content Graph
