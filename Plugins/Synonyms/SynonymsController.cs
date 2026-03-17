using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Synonyms.Plugins.Synonyms;

[Authorize]
[Route("plugins/synonyms")]
public class SynonymsController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View("~/Plugins/Synonyms/SynonymsIndex.cshtml");
    }
}
