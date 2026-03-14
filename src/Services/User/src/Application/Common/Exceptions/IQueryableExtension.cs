using System.Linq.Expressions;

namespace User.Application.Common.Exceptions
{
    public static class IQueryableExtension
    {
        public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
        {
            return condition ? query.Where(predicate) : query;
        }
    }
}
