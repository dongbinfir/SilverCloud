namespace User.Infrastructure.Dependencies
{
    /// <summary>
    /// 泛型标识接口：用于自动依赖注入扫描
    /// 实现 IScopedDependency<T> 的类会自动注册为 Scoped 生命周期
    /// </summary>
    /// <typeparam name="T">服务接口类型</typeparam>
    public interface IScopedDependency<T> where T : class
    {
    }
}
