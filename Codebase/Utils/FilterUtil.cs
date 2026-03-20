using System.ComponentModel;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Codebase.Utils;

public static class FilterUtil
{

    // 2. Cursor Pagination O(1)
    public static IQueryable<T> ApplyCursor<T>(
        this IQueryable<T> query,
        string? cursor,
        int limit,
        string? sort) where T : class
    {
        bool isDescending = sort?.StartsWith("-") ?? true;
        string sortField = sort?.TrimStart('-') ?? "Id";

        if (!string.IsNullOrEmpty(cursor))
        {
            try
            {
                // Lấy Type của thuộc tính đang dùng để sort (ví dụ: SortOrder là int, Id là Guid)
                var propertyInfo = typeof(T).GetProperty(sortField);
                if (propertyInfo != null)
                {
                    var type = propertyInfo.PropertyType;
                    var converter = TypeDescriptor.GetConverter(type);
                    var convertedCursor = converter.ConvertFromString(cursor);

                    if (convertedCursor != null)
                    {
                        var parameter = Expression.Parameter(typeof(T), "x");
                        var property = Expression.Call(typeof(EF), nameof(EF.Property), new[] { type }, parameter, Expression.Constant(sortField));

                        // Tạo toán tử so sánh
                        BinaryExpression comparison = isDescending
                            ? Expression.LessThan(property, Expression.Constant(convertedCursor))
                            : Expression.GreaterThan(property, Expression.Constant(convertedCursor));

                        query = query.Where(Expression.Lambda<Func<T, bool>>(comparison, parameter));
                    }
                }
            }
            catch { return query.Take(0); }
        }

        // --- ĐÂY CHÍNH LÀ MULTIPLE SORT ---
        string direction = isDescending ? "desc" : "asc";
    
        // Nếu sortField không phải Id, ta thêm Id vào để đảm bảo tính Deterministic (không trùng lặp trang)
        string orderByStr = sortField.Equals("Id", StringComparison.OrdinalIgnoreCase)
            ? $"Id {direction}"
            : $"{sortField} {direction}, Id {direction}";

        return query.OrderBy(orderByStr).Take(limit + 1);
    }

    // 3. Select động - Trả về đúng những cột Frontend cần
    public static IQueryable<T> ApplySelect<T>(this IQueryable<T> query, string? selectFields)
    {
        if (string.IsNullOrWhiteSpace(selectFields)) return query;

        // Kỹ thuật này sử dụng System.Linq.Dynamic.Core để select động
        // Giúp giảm băng thông từ DB -> App cực lớn
        return query.Select<T>($"new({selectFields})");
    }
}