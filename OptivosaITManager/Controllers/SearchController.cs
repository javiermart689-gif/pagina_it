using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptivosaITManager.Services;

namespace OptivosaITManager.Controllers;

[Authorize]
public class SearchController : Controller
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    public async Task<IActionResult> Index(string? term)
    {
        ViewBag.Term = term;
        var results = await _searchService.SearchAsync(term ?? string.Empty);
        return View(results);
    }
}
