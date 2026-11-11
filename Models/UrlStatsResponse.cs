namespace UrlShortener.Models;

public record DailyClickCount(DateOnly Date, int Count);

public record CountBreakdown(string Key, int Count);

public class UrlStatsResponse
{
    public required string Slug { get; set; }
    public int TotalClicks { get; set; }
    public List<DailyClickCount> ClicksPerDay { get; set; } = [];
    public List<CountBreakdown> TopReferrers { get; set; } = [];
    public List<CountBreakdown> Countries { get; set; } = [];
    public List<CountBreakdown> Devices { get; set; } = [];
    public List<CountBreakdown> Browsers { get; set; } = [];
}
