using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal
{
    public class OpenEdgeExtractingExpressionVisitor : ExpressionVisitor
    {

        protected override Expression VisitNew(NewExpression node)
        {
            var memberArguments = node.Arguments
#pragma warning disable EF1001 // Internal EF Core API usage.
                .Select(m => m is MemberExpression mem ? VisitNewMember(mem) : Visit(m))
#pragma warning restore EF1001 // Internal EF Core API usage.
                .ToList();

            var newNode = node.Update(memberArguments);

            return newNode;
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
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
    }
}
