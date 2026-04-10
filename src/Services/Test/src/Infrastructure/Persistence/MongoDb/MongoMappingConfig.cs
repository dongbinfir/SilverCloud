using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using User.Domain.Common;

namespace User.Infrastructure.Persistence.MongoDb
{
    public static class MongoMappingConfig
    {
        private const string MongoEntitiesNamespace = "User.Domain.MongoDbEntities";
        private static readonly object SyncRoot = new();
        private static readonly Type[] MongoEntityTypes = LoadMongoEntityTypes();
        private static bool _initialized;

        public static void Register()
        {
            // 避免应用生命周期内重复注册 Bson 映射。
            if (_initialized)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (_initialized)
                {
                    return;
                }

                if (!BsonClassMap.IsClassMapRegistered(typeof(MongoEntity)))
                {
                    BsonClassMap.RegisterClassMap<MongoEntity>(cm =>
                    {
                        cm.AutoMap();
                        // 统一将领域基类的 Id 映射为 MongoDB 主键字段 _id。
                        cm.MapIdMember(c => c.Id)
                            .SetSerializer(new StringSerializer(BsonType.String)); //在 application 没有安装 objectid 包的情况下，使用 string 存储 GuidV7 类型的 Id。
                        cm.SetIsRootClass(true);
                        cm.SetIgnoreExtraElements(true);
                    });
                }

                // 仅扫描 Domain 中 MongoDbEntities 命名空间下的 MongoEntity 子类并自动注册。
                foreach (var entityType in MongoEntityTypes)
                {
                    // LookupClassMap: 未注册时自动注册，已注册时直接返回。
                    var classMap = BsonClassMap.LookupClassMap(entityType);
                }

                _initialized = true;
            }
        }

        private static Type[] LoadMongoEntityTypes()
        {
            // 仅扫描 MongoEntity 所在的 Domain 程序集，避免遍历系统库和第三方库。
            return typeof(MongoEntity).Assembly
                .GetTypes()
                .Where(type =>
                    type.IsClass
                    && !type.IsAbstract
                    && type.IsSubclassOf(typeof(MongoEntity))
                    && type.Namespace != null
                    && type.Namespace.StartsWith(MongoEntitiesNamespace))
                .ToArray();
        }
    }
}