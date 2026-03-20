namespace Codebase.Models.Dtos.Requests.Search;

public class BaseFilterRequest
{
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 20;
    public string? Search { get; set; } 
    public string? Select { get; set; }
    public string? Sort { get; set; } = "-id"; 
}