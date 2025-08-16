// using Microsoft.EntityFrameworkCore.Metadata;
// using Microsoft.EntityFrameworkCore.Query;
// using Microsoft.EntityFrameworkCore.Query.Expressions;
// using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
// using Microsoft.EntityFrameworkCore.Query.Internal;
//
// namespace EntityFrameworkCore.OpenEdge.Query.Internal
// {
//     /*
//      * This class is responsible for handling aggregations and result operations.
//      * For example:
//      *
//      *   // Handles converting .Count() to SQL:
//      *   SELECT COUNT(*) FROM ...
//      */
//     public class OpenEdgeResultOperatorHandler : RelationalResultOperatorHandler
//     {
//         public OpenEdgeResultOperatorHandler(IModel model, 
//             ISqlTranslatingExpressionVisitorFactory sqlTranslatingExpressionVisitorFactory, 
//             ISelectExpressionFactory selectExpressionFactory, 
//             IResultOperatorHandler resultOperatorHandler) 
//             : base(model, sqlTranslatingExpressionVisitorFactory,
//                   selectExpressionFactory, resultOperatorHandler)
//         {
//         }
//     }
// }
