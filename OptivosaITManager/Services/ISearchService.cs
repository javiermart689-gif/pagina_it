using OptivosaITManager.ViewModels;

namespace OptivosaITManager.Services;

public interface ISearchService
{
    Task<SearchResultViewModel> SearchAsync(string term);
}
