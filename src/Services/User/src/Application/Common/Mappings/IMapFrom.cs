namespace User.Application.Common.Mappings
{
    public interface IMapFrom<T>
    {
        void Mapping(Profile profile) => profile.CreateMap(typeof(T), GetType());
    }

    public interface IMapTo<T>
    {
        void Mapping(Profile profile) => profile.CreateMap(GetType(), typeof(T));
    }

    /// <summary>
    /// 用于更新操作，映射到实体时自动排除 Id 字段
    /// </summary>
    /// <typeparam name="T">目标实体类型</typeparam>
    public interface IMapToWithExcludedId<T> : IMapTo<T>
    {
        void IMapTo<T>.Mapping(Profile profile)
        {
            // 创建基础映射
            var mappingExpression = profile.CreateMap(GetType(), typeof(T));

            // 动态查找并排除名为 "Id" 的属性
            var destType = typeof(T);
            var idProperty = destType.GetProperty("Id");

            if (idProperty != null)
            {
                mappingExpression.ForMember("Id", opt => opt.Ignore());
            }
        }
    }
}
