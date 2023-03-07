using FormPortal.Core.Services;
using DatabaseControllerProvider;
using Blazor.Pagination;
using FormPortal.Core.Models;
using FormPortal.Core.Filters;

namespace FormularPortal.Pages.Admin.Forms
{
    public partial class FormEntries : IHasPagination
    {
        public FormEntryFilter Filter { get; set; } = new();
        public int Page { get; set; } = 1;
        public int TotalItems { get; set; }
        public List<EntryListItem> Data { get; set; } = new();

        protected override async Task OnParametersSetAsync()
        {
            if (Page < 1)
            {
                Page = 1;
            }

            await LoadAsync();
        }

        public async Task LoadAsync(bool navigateToPage1 = false)
        {
            Filter.PageNumber = navigateToPage1 ? 1 : Page;
            using IDbController dbController = dbProviderService.GetDbController(AppdatenService.DbProvider, AppdatenService.ConnectionString);
            TotalItems = await formEntryService.GetTotalAsync(Filter, dbController);
            Data = await formEntryService.GetAsync(Filter, dbController);
        }
    }
}