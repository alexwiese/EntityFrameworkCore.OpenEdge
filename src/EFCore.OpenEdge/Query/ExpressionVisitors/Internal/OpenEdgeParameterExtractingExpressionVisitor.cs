using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Remotion.Linq.Parsing.ExpressionVisitors.TreeEvaluation;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal
{
    /*
     * Transforms and optimizes the expression tree before SQL generation by extracting parameters,
     * folding and evaluating constants, normalizing expressions, flattening subqueries, etc.
     */
    public class OpenEdgeParameterExtractingExpressionVisitor : ParameterExtractingExpressionVisitor
    {
        public OpenEdgeParameterExtractingExpressionVisitor(IEvaluatableExpressionFilter evaluatableExpressionFilter,
            IParameterValues parameterValues,
            IDiagnosticsLogger<DbLoggerCategory.Query> logger,
            DbContext context,
            bool parameterize, bool
                generateContextAccessors = false)
            : base(evaluatableExpressionFilter, parameterValues, logger, context, parameterize, generateContextAccessors)
        {
        }

        protected Expression VisitNewMember(MemberExpression memberExpression)
        {
            if (memberExpression.Expression is ConstantExpression constant
                && constant.Value != null)
            {
                switch (memberExpression.Member.MemberType)
                {
                    case MemberTypes.Field:
                        return Expression.Constant(constant.Value.GetType().GetField(memberExpression.Member.Name).GetValue(constant.Value));

                    case MemberTypes.Property:
                        var propertyInfo = constant.Value.GetType().GetProperty(memberExpression.Member.Name);
                        if (propertyInfo == null)
                        {
                            break;
                        }

                        return Expression.Constant(propertyInfo.GetValue(constant.Value));
                }
            }

            return base.VisitMember(memberExpression);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            var memberArguments = node.Arguments
                .Select(m => m is MemberExpression mem ? VisitNewMember(mem) : Visit(m))
                .ToList();

            var newNode = node.Update(memberArguments);

            return newNode;
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            // Handles edge cases. Apparently, 'Take' and 'Skip' can't be parameterized and must be evaluated
            // for OpenEdge SQL ??
            if (methodCallExpression.Method.Name == "Take")
            {
                return methodCallExpression;
            }

            if (methodCallExpression.Method.Name == "Skip")
            {
                return methodCallExpression;
            }

            return base.VisitMethodCall(methodCallExpression);
        }
    }
}