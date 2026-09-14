using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OptivosaITManager.Security;
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

        // Los accesos (contraseñas cifradas) no están entre los permisos de consulta del rol
        // Jefe: se quitan de los resultados en vez de mostrar enlaces que de todas formas
        // darían acceso denegado en AccessesController.
        if (!User.IsInRole(Roles.SistemasTI))
        {
            results.Accesses.Clear();
        }

        return View(results);
    }
}
