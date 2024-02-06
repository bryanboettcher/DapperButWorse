using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using SlimGenerator.Core.Extensions;

namespace SlimGenerator.Core.EntityGeneration;

/// <summary>
/// Used by the proxy generation code to track dirty fields for objects.
/// </summary>
public class DirtyFieldsInterceptor : IInterceptor
{
    private readonly Dictionary<string, object?> _currentValues = new();

    private static readonly Type ProxyDetailsType = typeof(IProxyDetails);

    private static readonly MethodInfo ResetChangesMethod = ProxyDetailsType.GetMethod(nameof(IProxyDetails.ResetChanges))!;
    private static readonly MethodInfo GetDirtyFieldsMethod = ProxyDetailsType.GetMethod(nameof(IProxyDetails.GetDirtyFields))!;

    private readonly HashSet<Type> _specialTypes = new();

    /// <summary>
    /// Used to indicate an attribute on a property type is special, and
    /// should otherwise be excluded from change tracking.
    /// </summary>
    public void AddSpecialAttribute<T>() where T : Attribute
    {
        _specialTypes.Add(typeof(T));
    }

    /// <inheritdoc />
    public void Intercept(IInvocation invocation)
    {
        var target = invocation.InvocationTarget ?? invocation.Proxy;

        var targetType = target.GetType();

        if (invocation.Method == ResetChangesMethod)
        {
            _currentValues.Clear();
            var foundKey = false;
            foreach (var property in targetType.GetProperties())
            {
                if (property.GetCustomAttributes().Any(a => _specialTypes.Contains(a.GetType())))
                {
                    foundKey = true;
                    continue;
                }

                if (!foundKey && property.IsPotentialKey())
                    continue;

                _currentValues.Add(property.Name, property.GetValue(invocation.Proxy));
            }

            return;
        }

        if (invocation.Method == GetDirtyFieldsMethod)
        {
            invocation.ReturnValue = targetType
                .GetProperties()
                .Where(p => !p.GetCustomAttributes().Any(a => _specialTypes.Contains(a.GetType())))
                .Select(p => new
                {
                    PropertyName = p.Name,
                    PropertyValue = p.GetValue(target)
                })
                .Where(p => HasChanged(p.PropertyName, p.PropertyValue))
                .Select(p => p.PropertyName)
                .ToArray();

            return;
        }

        invocation.Proceed();
    }

    private bool HasChanged(string name, object? value)
    {
        if (!_currentValues.TryGetValue(name, out var currentValue))
            return value is not null;

        if (currentValue is null && value is null)
            return false;

        if (value is null)
            return true;

        return !value.Equals(currentValue);
    }
}