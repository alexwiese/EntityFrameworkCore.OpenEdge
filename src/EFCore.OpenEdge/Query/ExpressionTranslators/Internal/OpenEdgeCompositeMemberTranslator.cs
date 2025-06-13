using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionTranslators.Internal
{
    /// <summary>
    /// Container for OpenEdge-specific member translators.
    /// Updated for EF Core 3.0+ architecture.
    /// </summary>
    public class OpenEdgeCompositeMemberTranslator : IMemberTranslator
    {
        private readonly List<IMemberTranslator> _translators;

        public OpenEdgeCompositeMemberTranslator(ISqlExpressionFactory sqlExpressionFactory)
        {
            var translatorTypes = OpenEdgeCompositeMethodCallTranslator.GetTranslatorMethods<IMemberTranslator>().ToList();
            
            _translators = new List<IMemberTranslator>();
            
            foreach (var translatorType in translatorTypes)
            {
                var parameterlessConstructor = translatorType.GetConstructor(Type.EmptyTypes);
                if (parameterlessConstructor != null)
                {
                    _translators.Add((IMemberTranslator)Activator.CreateInstance(translatorType));
                }
                else
                {
                    var constructorWithFactory = translatorType.GetConstructor(new[] { typeof(ISqlExpressionFactory) });
                    if (constructorWithFactory != null)
                    {
                        _translators.Add((IMemberTranslator)Activator.CreateInstance(translatorType, sqlExpressionFactory));
                    }
                }
            }
        }

        public virtual SqlExpression Translate(
            SqlExpression instance, 
            MemberInfo member, 
            Type returnType)
        {
            foreach (var translator in _translators)
            {
                var result = translator.Translate(instance, member, returnType);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
