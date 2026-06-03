namespace NewsPortalPro.Helpers
{
    public class PaginationHelper
    {
        public int TotalItems { get; set; }
        public int ItemsPerPage { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / ItemsPerPage);
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
        public int PreviousPage => CurrentPage - 1;
        public int NextPage => CurrentPage + 1;

        public IEnumerable<int> GetPageNumbers(int range = 2)
        {
            var start = Math.Max(1, CurrentPage - range);
            var end = Math.Min(TotalPages, CurrentPage + range);
            return Enumerable.Range(start, end - start + 1);
        }
    }
}