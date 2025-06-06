﻿using System.Linq.Expressions;
using EntityFrameworkCore.OpenEdge.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Remotion.Linq.Parsing.ExpressionVisitors.TreeEvaluation;

namespace EntityFrameworkCore.OpenEdge.Query.Internal
{
    /*
     * This class is responsible for coordinating the parameter extraction phase of query compilation.
     * Orchestrates parameter extraction using OpenEdgeParameterExtractingExpressionVisitor visitor.
     */
    public class OpenEdgeQueryModelGenerator : QueryModelGenerator
    {
        private readonly IEvaluatableExpressionFilter _evaluatableExpressionFilter;
        private readonly ICurrentDbContext _currentDbContext;

        public OpenEdgeQueryModelGenerator(INodeTypeProviderFactory nodeTypeProviderFactory,
            IEvaluatableExpressionFilter evaluatableExpressionFilter,
            ICurrentDbContext currentDbContext)
            : base(nodeTypeProviderFactory, evaluatableExpressionFilter, currentDbContext)
        {
            _evaluatableExpressionFilter = evaluatableExpressionFilter;
            _currentDbContext = currentDbContext;
        }

        public override Expression ExtractParameters(IDiagnosticsLogger<DbLoggerCategory.Query> logger, Expression query, IParameterValues parameterValues,
            bool parameterize = true, bool generateContextAccessors = false)
        {
            return new OpenEdgeParameterExtractingExpressionVisitor(_evaluatableExpressionFilter, parameterValues, logger,
                _currentDbContext.Context,
                parameterize, generateContextAccessors).ExtractParameters(query);
        }
    }
}