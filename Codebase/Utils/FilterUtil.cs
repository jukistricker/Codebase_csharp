using System.ComponentModel;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Codebase.Utils;

public static class FilterUtil
{
    // 1. ĐỊNH VỊ TRANG (Cursor Logic Only)
    public static IQueryable<T> ApplyCursor<T, TCursor>(
        this IQueryable<T> query,
        string? cursor,
        string sortField,
        bool isDescending) where T : class
    {
        if (string.IsNullOrEmpty(cursor)) return query;

        try
        {
            var converter = TypeDescriptor.GetConverter(typeof(TCursor));
            var convertedCursor = (TCursor?)converter.ConvertFromString(cursor);

            if (convertedCursor != null)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var property = Expression.Call(
                    typeof(EF),
                    nameof(EF.Property),
                    new[] { typeof(TCursor) },
                    parameter,
                    Expression.Constant(sortField));

                // Định vị: Nếu Desc thì lấy những thằng nhỏ hơn Cursor, nếu Asc thì lớn hơn
                var comparison = isDescending
                    ? Expression.LessThan(property, Expression.Constant(convertedCursor))
                    : Expression.GreaterThan(property, Expression.Constant(convertedCursor));

                return query.Where(Expression.Lambda<Func<T, bool>>(comparison, parameter));
            }
        }
        catch { return query.Take(0); }

        return query;
    }

    // 2. SẮP XẾP ĐA CỘT (Deterministic Sort)
    public static IQueryable<T> ApplyDeterministicSort<T>(this IQueryable<T> query, string? sort)
    {
        // Mặc định: Id giảm dần
        if (string.IsNullOrWhiteSpace(sort)) return query.OrderBy("Id desc");

        var isDescending = sort.StartsWith("-");
        var field = sort.TrimStart('-');
        var direction = isDescending ? "desc" : "asc";

        // Luôn gán thêm Id vào cuối để đảm bảo thứ tự không đổi (Deterministic)
        var sortExpression = field.Equals("Id", StringComparison.OrdinalIgnoreCase)
            ? $"Id {direction}"
            : $"{field} {direction}, Id {direction}";

        return query.OrderBy(sortExpression);
    }

    // 3. CHỌN TRƯỜNG (Dynamic Projection)
    public static IQueryable<TResult> ApplySelect<T, TResult>(this IQueryable<T> query, string? selectFields)
    {
        if (string.IsNullOrWhiteSpace(selectFields))
        {
            // Nếu không có select, ép kiểu về TResult (giả định T và TResult tương thích)
            return query.Cast<TResult>();
        }

        return query.Select<TResult>($"new({selectFields})");
    }
}