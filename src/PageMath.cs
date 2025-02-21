namespace FspQuery;

public static class PageMath
{
    public static int GetTotalPages(int pagesize, int totalCount) => (int)Math.Ceiling(totalCount / (double)pagesize);

    public static bool GetHasMoreValue(int page, int pagesize, int totalCount) => totalCount / ((page * pagesize) + 1) >= 1;
}