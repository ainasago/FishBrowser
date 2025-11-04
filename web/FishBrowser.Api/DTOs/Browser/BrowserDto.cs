namespace FishBrowser.Api.DTOs.Browser;

public class BrowserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string Engine { get; set; } = string.Empty;
    public string OS { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public int LaunchCount { get; set; }
    public bool EnablePersistence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLaunchedAt { get; set; }
}

public class BrowserQueryParams
{
    public int? GroupId { get; set; }
    public string? Search { get; set; }
    public string? Engine { get; set; }
    public string? OS { get; set; }
    public string SortBy { get; set; } = "createdat";
    public string SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class GroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int BrowserCount { get; set; }
}
