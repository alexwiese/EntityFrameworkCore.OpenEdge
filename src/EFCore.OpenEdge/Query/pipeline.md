┌──────────────────┐
│   1. LINQ Query  │ ← User writes: users.Where(u => u.Name.Contains("John")).Take(10)
└─────────┬────────┘
          │
          ▼
┌─────────────────────────────────┐
│ 2. Query Model Generation       │
│    OpenEdgeQueryModelGenerator  │ ← Orchestrates the compilation process
│    └─ ExtractParameters()       │ ← Uses custom parameter visitor
└─────────┬───────────────────────┘
          │
          ▼
┌─────────────────────────────────┐
│ 3. Expression Visitors          │
│    (Tree Transformation)        │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ OpenEdgeParameterExtracting │ │ ← Custom visitor
│ │ ExpressionVisitor           │ │ ← Handles Skip/Take, constants
│ └─────────────────────────────┘ │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ Other EF Core Visitors      │ │ ← Normalization, optimization
│ │ (built-in)                  │ │
│ └─────────────────────────────┘ │
└─────────┬───────────────────────┘
          │
          ▼
┌─────────────────────────────────┐
│ 4. Result Operator Handling     │
│    OpenEdgeResultOperatorHandler│ ← Handles .Count(), .Sum(), etc.
│    └─ Count/Sum/Any/First...    │
└─────────┬───────────────────────┘
          │
          ▼
┌─────────────────────────────────┐
│ 5. SQL Generation Phase         │
│    OpenEdgeSqlGeneratorFactory  │ ← Creates the SQL generator
│    └─ Creates...                │
│                                 │
│ ┌─────────────────────────────┐ │
│ │ OpenEdgeSqlGenerator        │ │ ← The main SQL generator
│ │                             │ │
│ │ ┌─────────────────────────┐ │ │
│ │ │ Expression Translators  │ │ │ ← 🚨 MISSING custom translators!
│ │ │ (Method calls → SQL)    │ │ │ ← Contains() → LIKE, etc.
│ │ └─────────────────────────┘ │ │
│ │                             │ │
│ │ ┌─────────────────────────┐ │ │
│ │ │ SQL Fragment Generation │ │ │ ← Custom overrides
│ │ │ • Parameters → ?        │ │ │ ← VisitParameter()
│ │ │ • TOP without ()        │ │ │ ← GenerateTop()
│ │ │ • DateTime → {ts...}    │ │ │ ← VisitConstant()
│ │ │ • EXISTS workarounds    │ │ │ ← VisitExists()
│ │ └─────────────────────────┘ │ │
│ └─────────────────────────────┘ │
└─────────┬───────────────────────┘
          │
          ▼
┌─────────────────────────────────┐
│ 6. Final SQL String             │ ← SELECT TOP 10 * FROM Users WHERE Name = ?
│    Ready for OpenEdge           │
└─────────────────────────────────┘
