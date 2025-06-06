using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionTranslators.Internal
{
    /*
     * Converts expression nodes to database specific SQL.
     * Handles method expression in transformed LINQ expressions by expression visitors in previous steps.
     * 
     *   // Expression node
     *   u.Name.Contains(@p0)
     *
     *   // Generated SQL fragment
     *   "Name LIKE '%' + @p0 + '%'"
     */
    public class OpenEdgeCompositeMethodCallTranslator : RelationalCompositeMethodCallTranslator
    {
        // This is a CONTAINER that holds multiple specific translators.
        // TODO: There are currently no custom translators, which is the direct reason of only basic crud and 
        //       joins being supported. To extend the functionality, we need to add more translators.
        //       For instance, OpenEdgeStringContainsTranslator that handles the Contains method.
        private static readonly List<Type> _translatorsMethods
            = GetTranslatorMethods<IMethodCallTranslator>().ToList();

        public OpenEdgeCompositeMethodCallTranslator(RelationalCompositeMethodCallTranslatorDependencies dependencies)
            : base(dependencies)
            // Finds and instantiates ALL method call translators in the assembly
            => AddTranslators(_translatorsMethods.Select(type => (IMethodCallTranslator)Activator.CreateInstance(type)));

        public static IEnumerable<Type> GetTranslatorMethods<TInteface>()
            => Assembly
                .GetExecutingAssembly()
                .GetTypes().Where(t =>
                    t.GetInterfaces().Any(i => i == typeof(TInteface))
                    && t.GetConstructors().Any(c => c.GetParameters().Length == 0));
    }
}