using System;
using System.Reflection;
using Castle.DynamicProxy;

namespace SlimGenerator.Core.EntityGeneration;

public static class EntityProxying
{
    private static readonly ProxyGenerator Generator = new();

    public static TEntity CreateProxy<TEntity>(TEntity? existing = default, bool forceProxy = false)
    {
        // we only proxy entities with a [Table] attribute, since those will
        // be objects that are 1:1 with a table.  Other types may be read-only
        // composite objects, such as the return type from a stored proc or view.
        if (!(forceProxy || ShouldProxyEntity<TEntity>()))
            return Activator.CreateInstance<TEntity>();

        var interceptor = new DirtyFieldsInterceptor();
        interceptor.AddSpecialAttribute<KeyAttribute>();
        interceptor.AddSpecialAttribute<ExplicitKeyAttribute>();
        interceptor.AddSpecialAttribute<ComputedAttribute>();

        var typeOfT = typeof(TEntity);
        var proxy = Generator.CreateClassProxy(
            typeOfT,
            new[] { typeof(IProxyDetails) },
            ProxyGenerationOptions.Default,
            interceptor
        );

        if (existing is null) 
            return (TEntity)proxy;

        // copy all existing properties from 'existing' to 'proxy'
        foreach (var propertyInfo in typeOfT.GetProperties())
        {
            var sourceValue = propertyInfo.GetValue(existing);
            propertyInfo.SetValue(proxy, sourceValue);
        }
            
        ((IProxyDetails)proxy).ResetChanges();
        return (TEntity)proxy;
    }

    internal static bool IsProxiedEntity<TEntity>(TEntity entity)
        => entity is IProxyDetails;

    internal static bool ShouldProxyEntity<TEntity>()
    {
        var type = typeof(TEntity);
        return !type.IsSealed && type.GetCustomAttribute<TableNameAttribute>() is not null;
    }
}