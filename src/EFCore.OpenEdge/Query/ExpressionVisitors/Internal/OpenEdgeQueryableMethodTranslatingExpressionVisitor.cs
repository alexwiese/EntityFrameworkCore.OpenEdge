using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal
{
    /// <summary>
    /// OpenEdge-specific implementation of the queryable method translating expression visitor.
    /// This visitor translates LINQ queryable methods (OrderBy, Where, Select, etc.) into SQL expressions.
    /// 
    /// Key responsibility: Tracks ORDER BY context to coordinate with SqlTranslatingExpressionVisitor
    /// for proper boolean handling in different SQL clauses.
    /// </summary>
    public class OpenEdgeQueryableMethodTranslatingExpressionVisitor : RelationalQueryableMethodTranslatingExpressionVisitor
    {
        /// <summary>
        /// Tracks whether we're currently translating an ORDER BY expression.
        /// 
        /// Why this is needed:
        /// OpenEdge has specific requirements for boolean columns in different SQL contexts:
        /// - In WHERE/HAVING/JOIN: Boolean columns must be compared explicitly (e.g., "column = 1")
        /// - In ORDER BY: Boolean columns must be used as-is without comparison (e.g., just "column")
        /// - In SELECT: Boolean columns need CASE WHEN wrapping (handled in SqlGenerator)
        /// 
        /// This flag allows the SqlTranslatingExpressionVisitor to know when it's processing
        /// an ORDER BY expression and should NOT add the "= 1" comparison to boolean columns.
        /// 
        /// Thread safety: This is an instance field (not static) to ensure thread safety.
        /// Each query execution creates its own visitor instance, preventing race conditions
        /// between concurrent requests.
        /// 
        /// Communication: The SqlTranslatingExpressionVisitor receives a reference to this
        /// visitor instance through its constructor, allowing it to check this flag.
        /// </summary>
        internal bool IsTranslatingOrderBy { get; private set; }
        
        public OpenEdgeQueryableMethodTranslatingExpressionVisitor(
            QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
            RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
            RelationalQueryCompilationContext queryCompilationContext)
            : base(dependencies, relationalDependencies, queryCompilationContext)
        {
        }

        /// <summary>
        /// Overrides TranslateOrderBy to set context flag for ORDER BY processing.
        /// 
        /// This method is called when translating .OrderBy() or .OrderByDescending() LINQ methods.
        /// It sets the IsTranslatingOrderBy flag to true before delegating to the base implementation,
        /// ensuring that any boolean columns in the ORDER BY expression are not transformed.
        /// 
        /// Example LINQ: query.OrderBy(p => p.IsActive)
        /// With flag: ORDER BY p.IsActive (correct for OpenEdge)
        /// </summary>
        #nullable enable
        protected override ShapedQueryExpression? TranslateOrderBy(
            ShapedQueryExpression source,
            LambdaExpression keySelector,
            bool ascending)
        {
            // Save current state to support nested expressions (though rare in ORDER BY)
            var previousValue = IsTranslatingOrderBy;
            IsTranslatingOrderBy = true;
            
            try
            {
                // Delegate to base implementation which will eventually call
                // SqlTranslatingExpressionVisitor to translate the keySelector
                return base.TranslateOrderBy(source, keySelector, ascending);
            }
            finally
            {
                // Always restore previous state to ensure proper cleanup
                IsTranslatingOrderBy = previousValue;
            }
        }

        /// <summary>
        /// Overrides TranslateThenBy to set context flag for additional ORDER BY expressions.
        /// 
        /// This method is called when translating .ThenBy() or .ThenByDescending() LINQ methods.
        /// These methods add additional sorting criteria after an initial OrderBy.
        /// 
        /// Example LINQ: query.OrderBy(p => p.Name).ThenBy(p => p.IsActive)
        /// The ThenBy portion needs the same boolean handling as OrderBy.
        /// </summary>
        #nullable enable
        protected override ShapedQueryExpression? TranslateThenBy(
            ShapedQueryExpression source,
            LambdaExpression keySelector,
            bool ascending)
        {
            // Save current state (should already be true if called after OrderBy, but be safe)
            var previousValue = IsTranslatingOrderBy;
            IsTranslatingOrderBy = true;
            
            try
            {
                // Delegate to base implementation for the actual translation
                return base.TranslateThenBy(source, keySelector, ascending);
            }
            finally
            {
                // Always restore previous state to ensure proper cleanup
                IsTranslatingOrderBy = previousValue;
            }
        }
    }
}