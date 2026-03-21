using Codebase.Utils;

namespace Codebase.Models.Dtos.Requests.Search;

public class BaseFilterRequest
{
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 20;
    public string? Search { get; set; } 
    public string? Select { get; set; }
    public string? Sort { get; set; } = "-id"; 

    // 1. Lấy ra trường Sort đầu tiên (để làm mỏ neo cho Cursor)
    public string PrimarySortField => Sort?.Split(',').FirstOrDefault()?.TrimStart('-') ?? "Id";
    public string SortField => StringUtil.ToPascalCase(PrimarySortField);
    public bool IsDescending => Sort?.Split(',').FirstOrDefault()?.StartsWith("-") ?? true;

    // 2. Chuyển đổi toàn bộ chuỗi Sort sang PascalCase (Hỗ trợ đa cột)
    // Ví dụ: "-sortOrder,-id" -> "-SortOrder,-Id"
    public string FullSortParam 
    {
        get 
        {
            if (string.IsNullOrWhiteSpace(Sort)) return "-Id";
            
            var parts = Sort.Split(',').Select(s => {
                var isDesc = s.Trim().StartsWith("-");
                var field = s.Trim().TrimStart('-');
                var pascalField = StringUtil.ToPascalCase(field);
                return isDesc ? $"-{pascalField}" : pascalField;
            });

            return string.Join(",", parts);
        }
    }
}