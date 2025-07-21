# EF Core Update Pipeline for OpenEdge

This document outlines the journey of a data modification from an EF Core `DbContext` to a final SQL command executed against an OpenEdge database.

High-Level Goal: Convert an in-memory entity change (e.g., `user.Name = "New"`) into an `UPDATE`, `INSERT`, or `DELETE` statement that OpenEdge can execute, respecting its specific SQL dialect and limitations.

---

### The Pipeline in Detail
```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           1. EF CORE CHANGE TRACKING                            │
│                      (Unchanged from standard EF Core)                          │
└─────────────────────────┬───────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        2. CHANGE DETECTION & MODIFICATION COMMAND               │
│                                                                                 │
│  When `context.SaveChanges()` is called, EF Core detects changes and creates    │
│  a `ModificationCommand` object. This is a provider-agnostic representation     │
│  of the required database operation.                                            │
│                                                                                 │
│  Example for an update:                                                         │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │ ModificationCommand {                                                   │    │
│  │   TableName: "Users", EntityState: Modified,                            │    │
│  │   ColumnModifications: [                                                │    │
│  │     { Col: "Name", Value: "Updated", IsWrite: true },                   │    │
│  │     { Col: "Id", OriginalValue: 1, IsCondition: true },                 │    │
│  │   ]                                                                     │    │
│  │ }                                                                       │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────┬───────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                      3. BATCH FACTORY CREATION                                  │
│                 `OpenEdgeModificationCommandBatchFactory`                       │
│                                                                                 │
│  EF Core's dependency injection system resolves OpenEdge custom factory.        │
│                                                                                 │
│  The factory's sole purpose is to create an instance of our custom batch class. │
│                                                                                 │
│  `Create()` → `new OpenEdgeModificationCommandBatch(dependencies)`              │
│                                                                                 │
└─────────────────────────┬───────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                       4. BATCHING (SINGLE COMMAND)                              │
│                   `OpenEdgeModificationCommandBatch`                            │
│                                                                                 │
│  This class inherits from `SingularModificationCommandBatch`, meaning each      │
│  batch will contain only ONE `ModificationCommand`. This is a key OpenEdge      │
│  provider design choice to handle database limitations.                         │
│                                                                                 │
│  The command is added to the batch, which is now considered "full" and ready    │
│  for SQL generation and execution. The real customization happens next.         │
└─────────────────────────┬───────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        5. SQL GENERATION                                        │
│                     `OpenEdgeUpdateSqlGenerator`                                │
│                                                                                 │
│  This is the core of generating OpenEdge-specific SQL.                          │
│                                                                                 │
│  Key Behaviors:                                                                 │
│  1. **Hybrid Literal/Parameter SQL**: It generates SQL that uses raw values     │
│     (literals) for write operations (e.g., in the `SET` clause) but uses        │
│     positional `?` parameters for conditions (e.g., in the `WHERE` clause).     │
│     This is controlled by `AppendSqlLiteral` vs. `AppendParameter`.             │
│                                                                                 │
│  2. **No `RETURNING` Clause**: OpenEdge does not support the `RETURNING`        │
│     clause. The generator omits it, which is a necessary workaround as EF       │
│     Core often uses `... RETURNING 1` to confirm an operation's success.        │
│                                                                                 │
│  Example SQL Generated:                                                         │
│  `UPDATE "Users" SET "Name" = 'Updated' WHERE "Id" = ?`                         │
│                                                                                 │
└─────────────────────────┬───────────────────────────────────────────────────────┘
                          │
                          ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│                      6. PARAMETER ORDERING                                       │
│              `OpenEdgeModificationCommandBatch.AddParameters()`                  │
│                                                                                  │
│  This is the most critical step in the batch class. Since OpenEdge uses          │
│  positional `?` parameters, their order is vital. This overridden method         │
│  ensures the parameters are added to the command in the exact order they appear  │
│  in the SQL generated in the previous step.                                      │
│                                                                                  │
│  It reorders the `ColumnModification` list before adding parameters:             │
│  ┌──────────────────────────────────────────────────────────────────────────┐    │
│  │ var ordered = modifications.OrderBy(cm => cm.IsCondition);               │    │
│  │ // 1. Write operations (`IsCondition`=false) for SET clause come FIRST.  │    │
│  │ // 2. Condition operations (`IsCondition`=true) for WHERE clause SECOND. │    │
│  └──────────────────────────────────────────────────────────────────────────┘    │
│                                                                                  │
│  This guarantees the parameter values match the `?` placeholders correctly.      │
└─────────────────────────┬────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        7. FINAL COMMAND RESULT                                  │
│                                                                                 │
│  The final `RawSqlCommand` object combines the generated SQL and the ordered    │
│  parameters.                                                                    │
│                                                                                 │
│  `RawSqlCommand` {                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │ CommandText: "UPDATE "Users" SET "Name" = 'Updated' WHERE "Id" = ?" │   │    │
│  │                                                                         │    │
│  │ Parameters: [                                                           │    │
│  │   // Note: Only one parameter for the WHERE clause `?`                  │    │
│  │   { InvariantName: "p0", Name: "?", TypeMapping: IntTypeMapping }       │    │
│  │ ],                                                                      │    │
│  │                                                                         │    │
│  │ ParameterValues: {                                                      │    │
│  │   ["p0"] = 1             // Value for the `Id` in the WHERE clause      │    │
│  │ }                                                                       │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│  }                                                                              │
└─────────────────────────┬───────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         8. DATABASE EXECUTION                                   │
│                                                                                 │
│  The ADO.NET provider sends the final command to the database.                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │ OpenEdge Database Receives:                                             │    │
│  │                                                                         │    │
│  │ SQL: "UPDATE "Users" SET "Name" = 'Updated' WHERE "Id" = ?"             │    │
│  │ Parameters: [1]                                                         │    │
│  │                                                                         │    │
│  │ Executes as:                                                            │    │
│  │ UPDATE "Users" SET "Name" = 'Updated' WHERE "Id" = 1                    │    │
│  │                                                                         │    │
│  │                                                                         │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────────┘
```