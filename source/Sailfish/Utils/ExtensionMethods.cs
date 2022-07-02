using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace Sailfish.Utils
{
    public static class ExtensionMethods
    {
        internal static bool HasAttribute<TAttribute>(this Type type, bool inherit = false) where TAttribute : Attribute
        {
            return type.GetCustomAttributes(typeof(TAttribute), inherit).Length > 0;
        }

        internal static bool HasAttribute<TAttribute>(this MethodInfo method, bool inherit = false) where TAttribute : Attribute
        {
            return method.GetCustomAttributes(typeof(TAttribute), inherit).Length > 0;
        }

        internal static bool HasAttribute<TAttribute>(this PropertyInfo property, bool inherit = false) where TAttribute : Attribute
        {
            return property.GetCustomAttributes(typeof(TAttribute), inherit).Length > 0;
        }

        internal static bool IsMethodWithAttribute<TAttribute>(this MethodInfo method) where TAttribute : Attribute
        {
            return method.GetCustomAttributes(typeof(TAttribute), false).Length > 0;
        }

        internal static IEnumerable<PropertyInfo> GetPropertiesWithAttribute<TAttribute>(this Type type) where TAttribute : Attribute
        {
            return type.GetProperties().Where(x => x.HasAttribute<TAttribute>());
        }

        internal static IEnumerable<MethodInfo> GetMethodsWithAttribute<TAttribute>(this Type type) where TAttribute : Attribute
        {
            return type.GetMethods().Where(x => x.IsPublic).Where(x => x.HasAttribute<TAttribute>());
        }

        internal static IEnumerable<MethodInfo> GetMethodsWithAttribute<TAttribute>(this object instance) where TAttribute : Attribute
        {
            return instance.GetType().GetMethodsWithAttribute<TAttribute>();
        }

        internal static MethodInfo? GetMethodWithAttribute<TAttribute>(this object instance) where TAttribute : Attribute
        {
            return instance.GetType().GetMethodsWithAttribute<TAttribute>().SingleOrDefault();
        }

        internal static bool IsAsyncMethod(this MethodInfo method)
        {
            return method.HasAttribute<AsyncStateMachineAttribute>();
        }

        public static async Task InvokeWith(this MethodInfo method, object instance)
        {
            if (method.IsAsyncMethod()) await (Task)method.Invoke(instance, null)!;
            else method.Invoke(instance, null);
        }

        internal static int GetNumIterations(this Type type)
        {
            var numIterations = type
                .GetCustomAttributes(true)
                .OfType<SailfishAttribute>()
                .Single()
                .NumIterations;
            return numIterations;
        }

        internal static int GetWarmupIterations(this Type type)
        {
            var numWarmupIterations = type
                .GetCustomAttributes(true)
                .OfType<SailfishAttribute>()
                .Single()
                .NumWarmupIterations;
            return numWarmupIterations;
        }
    }
}