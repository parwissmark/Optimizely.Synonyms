using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synonyms.Plugins.Synonyms.Api.Dto;
using Synonyms.Plugins.Synonyms.Services;

namespace Synonyms.Plugins.Synonyms.Api;

[Route("api/plugins/synonyms")]
[Authorize]
[ApiController]
public class GraphSynonymsApiController(ISynonymsService synonymsService) : ControllerBase
{
    [HttpGet("synonyms")]
    public IActionResult GetSynonyms()
    {
        var data = synonymsService.GetSynonyms();
        return Ok(new { payload = data });
    }

    [HttpPost("synonyms")]
    public IActionResult CreateSynonym([FromBody] SynonymRequest request)
    {
        try
        {
            var data = synonymsService.CreateSynonym(request, User?.Identity?.Name);
            return Ok(new { payload = data });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("synonyms/{id:guid}")]
    public IActionResult UpdateSynonym(Guid id, [FromBody] SynonymRequest request)
    {
        try
        {
            var data = synonymsService.UpdateSynonym(id, request, User?.Identity?.Name);
            return Ok(new { payload = data });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("synonyms/{id:guid}")]
    public IActionResult DeleteSynonym(Guid id)
    {
        try
        {
            synonymsService.DeleteSynonym(id);
            return Ok(new { message = "Deleted." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromBody] GraphPublishRequest request)
    {
        var data = await synonymsService.PublishSynonyms(request, User?.Identity?.Name);
        if (!data.IsSuccess)
        {
            return BadRequest(new { message = data.Message ?? "Publish failed.", payload = data });
        }

        return Ok(new { payload = data });
    }

    [HttpGet("publish/log")]
    public IActionResult GetPublishLog()
    {
        var data = synonymsService.GetPublishLog();
        return Ok(new { payload = data });
    }

    [HttpGet("languages")]
    public IActionResult GetLanguages()
    {
        var data = synonymsService.GetLanguages();
        return Ok(new { payload = data });
    }

    [HttpGet("verify")]
    public async Task<IActionResult> VerifyUpload([FromQuery] string synonymSlot, [FromQuery] string languageRouting)
    {
        var data = await synonymsService.VerifyUpload(synonymSlot, languageRouting);
        return Ok(new { payload = data });
    }
}
