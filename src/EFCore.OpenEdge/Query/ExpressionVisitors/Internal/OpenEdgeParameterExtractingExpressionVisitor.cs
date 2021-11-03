using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal
{
#pragma warning disable EF1001 // Internal EF Core API usage.
    public class OpenEdgeParameterExtractingExpressionVisitor : ParameterExtractingExpressionVisitor
#pragma warning restore EF1001 // Internal EF Core API usage.
    {
        public OpenEdgeParameterExtractingExpressionVisitor(IEvaluatableExpressionFilter evaluatableExpressionFilter,
#pragma warning disable EF1001 // Internal EF Core API usage.
                                                            IParameterValues parameterValues,
#pragma warning restore EF1001 // Internal EF Core API usage.
                                                            Type contextType,
                                                            IModel model,
                                                            IDiagnosticsLogger<DbLoggerCategory.Query> logger,
                                                            bool parameterize,
#pragma warning disable EF1001 // Internal EF Core API usage.
                                                            bool generateContextAccessors) :
            base(evaluatableExpressionFilter, parameterValues, contextType, model, logger, parameterize, generateContextAccessors)
#pragma warning restore EF1001 // Internal EF Core API usage.
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
    }
}