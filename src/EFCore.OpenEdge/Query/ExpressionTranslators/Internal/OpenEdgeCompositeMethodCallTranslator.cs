using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionTranslators.Internal
{
    /// <summary>
    /// Container for OpenEdge-specific method call translators.
    /// Updated for EF Core 3.0+ architecture.
    /// </summary>
    public class OpenEdgeCompositeMethodCallTranslator : IMethodCallTranslator
    {
        private readonly List<IMethodCallTranslator> _translators;

        public OpenEdgeCompositeMethodCallTranslator(ISqlExpressionFactory sqlExpressionFactory)
        {
            // Get all method call translators in the assembly
            var translatorTypes = GetTranslatorMethods<IMethodCallTranslator>().ToList();
            
            _translators = new List<IMethodCallTranslator>();
            
            // Instantiate translators that have parameterless constructors
            foreach (var translatorType in translatorTypes)
            {
                var parameterlessConstructor = translatorType.GetConstructor(Type.EmptyTypes);
                if (parameterlessConstructor != null)
                {
                    _translators.Add((IMethodCallTranslator)Activator.CreateInstance(translatorType));
                }
                else
                {
                    // Try constructor with ISqlExpressionFactory parameter
                    var constructorWithFactory = translatorType.GetConstructor(new[] { typeof(ISqlExpressionFactory) });
                    if (constructorWithFactory != null)
                    {
                        _translators.Add((IMethodCallTranslator)Activator.CreateInstance(translatorType, sqlExpressionFactory));
                    }
                }
            }
        }

        public virtual SqlExpression Translate(
            SqlExpression instance, 
            MethodInfo method, 
            IReadOnlyList<SqlExpression> arguments,
            IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            // Try each translator until one succeeds
            foreach (var translator in _translators)
            {
                var result = translator.Translate(instance, method, arguments, logger);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static IEnumerable<Type> GetTranslatorMethods<TInterface>()
            => Assembly
                .GetExecutingAssembly()
                .GetTypes().Where(t =>
                    t.GetInterfaces().Any(i => i == typeof(TInterface))
                    && !t.IsAbstract
                    && !t.IsInterface);
    }
}
