using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionTranslators.Internal
{
    /*
     * Handles property/member access in LINQ expressions
     */
    public class OpenEdgeCompositeMemberTranslator : RelationalCompositeMemberTranslator
    {
        private static readonly List<Type> _translatorsMethods
            = OpenEdgeCompositeMethodCallTranslator.GetTranslatorMethods<IMemberTranslator>().ToList();

        public OpenEdgeCompositeMemberTranslator(RelationalCompositeMemberTranslatorDependencies dependencies)
            : base(dependencies)
            => AddTranslators(_translatorsMethods.Select(type => (IMemberTranslator)Activator.CreateInstance(type)));
    }
}