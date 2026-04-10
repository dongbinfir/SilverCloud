using System.Reflection;

namespace User.Application.Common.Mappings;

/// <summary>
/// AutoMapper 映射配置文件
/// 自动扫描并注册所有实现了 IMapFrom<> 或 IMapTo<> 接口的类型
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>
    /// 构造函数：启动时自动扫描程序集并注册映射
    /// </summary>
    public MappingProfile()
    {
        // 扫描当前程序集中所有实现了映射接口的类型
        ApplyMappingsFromAssembly(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// 扫描指定程序集，查找所有实现了映射接口的类型，并调用它们的 Mapping 方法
    /// </summary>
    /// <param name="assembly">要扫描的程序集</param>
    private void ApplyMappingsFromAssembly(Assembly assembly)
    {
        // 获取映射接口的泛型定义类型
        var mapFromType = typeof(IMapFrom<>);  // 从实体映射到 DTO 的接口
        var mapToType = typeof(IMapTo<>);      // 从 DTO 映射到实体的接口
        var mappingMethodName = nameof(IMapFrom<object>.Mapping); // 接口定义的方法名："Mapping"

        // 定义一个本地函数：检查类型是否实现了 IMapFrom<> 或 IMapTo<> 接口
        bool HasInterface(Type t) =>
            t.IsGenericType &&
            (t.GetGenericTypeDefinition() == mapFromType ||
             t.GetGenericTypeDefinition() == mapToType);

        // 从程序集中获取所有满足条件的类型：
        // 1. 实现了 IMapFrom<> 或 IMapTo<> 接口
        // 2. 不是接口类型（跳过 IMapToWithExcludedId<> 这样的接口定义）
        // 3. 不是抽象类（抽象类无法实例化）
        var types = assembly.GetExportedTypes()
            .Where(t => t.GetInterfaces().Any(HasInterface))
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .ToList();

        // Mapping 方法的参数类型：接受一个 Profile 参数
        var argumentTypes = new Type[] { typeof(Profile) };

        // 遍历每个找到的类型
        foreach (var type in types)
        {
            // 创建该类型的实例（用于调用其 Mapping 方法）
            var instance = Activator.CreateInstance(type);

            // 尝试直接从类型中获取 Mapping 方法
            // 如果类型显式实现了接口方法（例如重写了 Mapping 方法），这里会找到
            var methodInfo = type.GetMethod(mappingMethodName);

            if (methodInfo != null)
            {
                // 找到了方法，直接调用它
                // 传入当前的 Profile 实例（即 MappingProfile 自身）
                methodInfo.Invoke(instance, new object[] { this });
            }
            else
            {
                // 类型本身没有显式的 Mapping 方法，从它实现的接口中获取
                // 这种情况更常见，因为 Mapping 方法是接口的默认实现
                var interfaces = type.GetInterfaces().Where(HasInterface).ToList();

                if (interfaces.Count > 0)
                {
                    // 遍历该类型实现的所有映射接口
                    // （可能同时实现了 IMapFrom<> 和 IMapTo<>）
                    foreach (var @interface in interfaces)
                    {
                        // 从接口中获取 Mapping 方法，并指定参数类型为 Profile
                        var interfaceMethodInfo = @interface.GetMethod(mappingMethodName, argumentTypes);

                        // 调用接口的默认 Mapping 方法
                        // 这会执行接口中定义的映射配置，例如：
                        // - profile.CreateMap<Entity, Dto>() (IMapFrom)
                        // - profile.CreateMap<Dto, Entity>() (IMapTo)
                        // - profile.CreateMap<Dto, Entity>().ForMember(dest => dest.Id, opt => opt.Ignore()) (IMapToWithExcludedId)
                        interfaceMethodInfo?.Invoke(instance, new object[] { this });
                    }
                }
            }
        }
    }
}


