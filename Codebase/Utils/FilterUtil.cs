using System.ComponentModel;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Codebase.Utils;

public static class FilterUtil
{
    // 1. Sắp xếp động theo chuỗi "name" hoặc "-id"
    public static IQueryable<T> ApplyDeterministicSort<T>(this IQueryable<T> query, string? sort)
    {
        // Mặc định nếu không truyền sort
        if (string.IsNullOrWhiteSpace(sort)) return query.OrderBy("Id desc");

        bool isDescending = sort.StartsWith("-");
        string field = sort.TrimStart('-');

        // Nếu trường sort đã là Id (không phân biệt hoa thường)
        if (field.Equals("Id", StringComparison.OrdinalIgnoreCase))
            return query.OrderBy($"Id {(isDescending ? "desc" : "asc")}");

        // Sửa lỗi cú pháp chuỗi: dùng dấu phẩy để phân cách các cột sort trong string
        // Cú pháp đúng của Dynamic LINQ: "Name desc, Id desc"
        string sortExpression = $"{field} {(isDescending ? "desc" : "asc")}, Id {(isDescending ? "desc" : "asc")}";

        return query.OrderBy(sortExpression);
    }

    // 2. Cursor Pagination O(1)
    public static IQueryable<T> ApplyCursor<T, TCursor>(
        this IQueryable<T> query,
        string? cursor,
        int limit,
        string? sort) where T : class
    {
        bool isDescending = sort?.StartsWith("-") ?? true;
        string sortField = sort?.TrimStart('-') ?? "Id";

        if (!string.IsNullOrEmpty(cursor))
            try
            {
                TypeConverter converter = TypeDescriptor.GetConverter(typeof(TCursor));
                TCursor? convertedCursor = (TCursor?)converter.ConvertFromString(cursor);

                if (convertedCursor != null)
                {
                    // Xây dựng Expression: x => EF.Property<TCursor>(x, sortField) < convertedCursor
                    ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
                    MethodCallExpression property = Expression.Call(
                        typeof(EF),
                        nameof(EF.Property),
                        new[] { typeof(TCursor) },
                        parameter,
                        Expression.Constant(sortField));

                    // Tạo toán tử so sánh (LessThan hoặc GreaterThan)
                    BinaryExpression comparison = isDescending
                        ? Expression.LessThan(property, Expression.Constant(convertedCursor))
                        : Expression.GreaterThan(property, Expression.Constant(convertedCursor));

                    Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);

                    // Ép kiểu tường minh IQueryable<T> để tránh Ambiguous invocation
                    query = query.Where(lambda);
                }
            }
            catch
            {
                return query.Take(0);
            }

        // Xử lý OrderBy bằng Dynamic LINQ (nhớ ép kiểu sang IQueryable<T>)
        string direction = isDescending ? "desc" : "asc";
        string orderByStr = sortField.Equals("Id", StringComparison.OrdinalIgnoreCase)
            ? $"Id {direction}"
            : $"{sortField} {direction}, Id {direction}";

        // Gọi OrderBy của Dynamic LINQ và ép về IQueryable<T>
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