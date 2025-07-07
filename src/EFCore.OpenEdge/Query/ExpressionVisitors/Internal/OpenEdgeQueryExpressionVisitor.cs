using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal
{
    /// <summary>
    /// Handles OpenEdge-specific expression transformations.
    /// Migrated from OpenEdgeParameterExtractingExpressionVisitor.
    /// </summary>
    public class OpenEdgeQueryExpressionVisitor : ExpressionVisitor
    {
        // Existing VisitNewMember logic
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

        // Existing VisitNew logic
        protected override Expression VisitNew(NewExpression node)
        {
            var memberArguments = node.Arguments
                .Select(m => m is MemberExpression mem ? VisitNewMember(mem) : Visit(m))
                .ToList();

            var newNode = node.Update(memberArguments);
            return newNode;
        }

        // Existing VisitMethodCall logic
        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            // Prevents Take and Skip from being parameterized for OpenEdge
            if (methodCallExpression.Method.Name == "Take" || 
                methodCallExpression.Method.Name == "Skip")
            {
                return methodCallExpression;
            }

            return base.VisitMethodCall(methodCallExpression);
        }
    }
}