EF CORE QUERY PIPELINE FOR OPENEDGE
========================================

This document outlines the journey of a LINQ query from C# code to executable OpenEdge SQL within the Entity Framework Core provider.

---

### **Phase 1: LINQ Expression Tree Creation**
*   **Input:** C# LINQ query.
*   **Output:** .NET Expression Tree.
*   **Process:** The C# compiler converts LINQ query syntax into a standard `Expression` tree representation.

**Example:**
```csharp
var customers = context.Customers
    .Where(c => c.Name.StartsWith("A"))
    .OrderBy(c => c.Id)
    .Skip(5)
    .Take(10)
    .ToList();
```

---

### **Phase 2: Query Translation (LINQ to Relational Representation)**
This phase translates the generic LINQ expression tree into a relational-specific `SelectExpression` tree. It involves several components working in sequence.

1.  **Query Preprocessing (`IQueryTranslationPreprocessor`)**
    *   **Component:** EF Core's built-in preprocessor.
    *   **Responsibility:** Normalizes the expression tree, resolves closures, and performs initial optimizations.

2.  **Queryable Method Translation (`IQueryableMethodTranslatingExpressionVisitor`)**
    *   **Component:** `OpenEdgeQueryableMethodTranslatingExpressionVisitor`.
    *   **Responsibility:** Translates top-level LINQ methods like `Where`, `OrderBy`, `Select`, `Skip`, and `Take` into the corresponding parts of a `SelectExpression` (e.g., `Predicate`, `Orderings`, `Limit`, `Offset`).

3.  **Member and Method Call Translation (`IMemberTranslator` / `IMethodCallTranslator`)**
    *   **Components:**
        *   `OpenEdgeMemberTranslatorProvider` -> `OpenEdgeStringLengthTranslator`
        *   `OpenEdgeMethodCallTranslatorProvider` -> `OpenEdgeStringMethodCallTranslator`
    *   **Responsibility:** Translates specific .NET property accesses and method calls into their SQL equivalents.
        *   `string.Length` is translated to the `LENGTH()` SQL function.
        *   `string.StartsWith()`, `string.EndsWith()`, and `string.Contains()` are translated to SQL `LIKE` expressions.

4.  **General Expression to SQL Translation (`RelationalSqlTranslatingExpressionVisitor`)**
    *   **Component:** `OpenEdgeSqlTranslatingExpressionVisitor`.
    *   **Responsibility:** Translates the remaining C# expressions inside the LINQ query (e.g., boolean logic, member access) into `SqlExpression` nodes.
    *   **OpenEdge Customization:** Overrides `VisitMember` to handle OpenEdge's lack of implicit boolean evaluation. It transforms a boolean property access like `c.IsActive` into an explicit comparison `c.IsActive = 1`.

5.  **Query Post-processing (`IQueryTranslationPostprocessor`)**
    *   **Component:** `OpenEdgeQueryTranslationPostprocessor`.
    *   **Responsibility:** Performs final optimizations on the generated `SelectExpression` tree. While the current implementation is minimal, this is the stage where a visitor like `OpenEdgeQueryExpressionVisitor` would be invoked to handle provider-specific tree manipulations, such as preventing parameterization for `Take()` and `Skip()`.

---

### **Phase 3: Parameter-Based SQL Processing**
*   **Input:** `SelectExpression` tree with parameter placeholders.
*   **Output:** `SelectExpression` tree with literal values for `OFFSET`/`FETCH`.
*   **Component:** `OpenEdgeParameterBasedSqlProcessor`.
*   **Responsibility:** This is an intermediate stage that has access to both the query expression and the actual parameter values.
*   **OpenEdge Customization:** OpenEdge SQL requires `OFFSET` and `FETCH` clauses to use literal integer values, not parameters. This processor finds `SqlParameterExpression` nodes for `Offset` and `Limit` and replaces them with `SqlConstantExpression` nodes containing the actual integer values.

---

### **Phase 4: SQL Generation**
*   **Input:** The final, provider-specific `SelectExpression` tree.
*   **Output:** A SQL string and its associated database parameters.
*   **Component:** `OpenEdgeSqlGenerator`.
*   **Responsibility:** Traverses the `SelectExpression` tree and generates the final, executable OpenEdge SQL text.
*   **OpenEdge Customizations:**
    *   **Parameters:** Replaces EF Core's named parameters (`@p0`) with positional `?` placeholders required by the OpenEdge ODBC driver (`VisitParameter`).
    *   **Paging:** Generates the `OFFSET <literal> ROWS FETCH NEXT <literal> ROWS ONLY` clause. It intentionally skips the `TOP` clause to avoid conflicts (`GenerateLimitOffset`, `GenerateTop`).
    *   **Boolean Projections:** Wraps boolean expressions in `SELECT` clauses with `CASE WHEN ... THEN 1 ELSE 0 END` (`VisitProjection`).
    *   **Functions:** Casts the result of `COUNT(*)` to `INT` to align with EF Core's expectations (`VisitSqlFunction`).
    *   **Literals:** Formats `DateTime` constants into the OpenEdge-specific `{ ts '...' }` syntax (`VisitConstant`).
    *   **EXISTS:** Implements a workaround for `EXISTS` conditionals by selecting from the `pub."_File"` metaschema table (`VisitConditional`).

---

### **Visualized Flow**

```
┌──────────────────┐
│   1. LINQ Query  │ e.g., .Skip(5).Take(10)
└─────────┬────────┘
          │
          ▼
┌─────────────────────────────────┐
│ 2. Query Translation Phase      │
│   (LINQ → SelectExpression)     │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ QueryableMethodTranslator   │ │ ← .Skip(5) -> Offset: 5
│ │                             │ │   .Take(10) -> Limit: 10
│ └─────────────────────────────┘ │
│ ┌─────────────────────────────┐ │
│ │ MethodCallTranslator        │ │ ← .StartsWith("A") -> LIKE 'A%'
│ └─────────────────────────────┘ │
│ ┌─────────────────────────────┐ │
│ │ SqlTranslatingVisitor       │ │ ← c.IsActive -> c.IsActive = 1
│ └─────────────────────────────┘ │
└─────────┬───────────────────────┘
          │
          ▼
┌─────────────────────────────────┐
│ 3. Parameter-Based SQL Phase    │
│   (Parameter Value Inlining)    │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ OpenEdgeParameterBasedSql-  │ │ ← Has access to parameter values.
│ │ Processor                   │ │ ← Finds OFFSET/FETCH parameters.
│ │ └─ OffsetValueInlining-     │ │ ← Replaces parameter with literal.
│ │    ExpressionVisitor        │ │ ← Offset: (param p0) -> Offset: (const 5)
│ └─────────────────────────────┘ │
└─────────┬───────────────────────┘
          │
          ▼
┌─────────────────────────────────┐
│ 4. SQL Generation Phase         │
│   (SelectExpression → SQL Text) │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ OpenEdgeSqlGenerator        │ │  ← Main SQL generator
│ │ └─ GenerateLimitOffset()   │ │  ← Generates "OFFSET 5 ROWS..."
│ │ └─ VisitParameter()        │ │  ← Generates "?" for other params
│ │ └─ VisitProjection()       │ │  ← Generates "CASE WHEN..."
│ └─────────────────────────────┘ │
└─────────┬───────────────────────┘
          │
          ▼
┌─────────────────────────────────┐
│ 5. Final SQL String             │ ← SELECT ... FROM ... OFFSET 5 ROWS FETCH NEXT 10 ROWS ONLY
│    Ready for OpenEdge ODBC      │
└─────────────────────────────────┘
```