namespace User.Infrastructure.Common
{
    public static class MongoCollectionName
    {
        public static string For<T>() => For(typeof(T));

        public static string For(Type type) => type.Name + "s";
    }
}