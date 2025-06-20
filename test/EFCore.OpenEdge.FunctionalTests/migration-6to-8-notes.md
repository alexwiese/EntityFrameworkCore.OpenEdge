# EF Core OpenEdge Provider - .NET 6 to .NET 8 Migration Notes

This document outlines the key changes made during migration and the specific OpenEdge provider functionality that requires testing.

## 🔄 Key Changes Made

### 1. Parameter Handling (`OpenEdgeSingularModificationCommandBatch`)
**Change**: Removed custom `CreateStoreCommand()` override - now relies on base class automatic parameter handling.

**Risk**: OpenEdge uses positional `?` parameters, not named parameters.

### 2. Concurrency Control (`OpenEdgeUpdateSqlGenerator`)
**Change**: Integrated `AppendRowsAffectedWhereCondition` and `AppendIdentityWhereCondition` logic into existing `AppendWhereCondition` method.

**Risk**: Concurrency checks might not be properly disabled for OpenEdge limitations.

### 3. SQL Generation Method Signatures
**Change**: `AppendUpdateCommand` now requires `readOperations` parameter, `ResultSetMapping.NoResultSet` → `ResultSetMapping.NoResults`.

**Risk**: SQL generation might be incorrect or missing parameters.
---

## 🧪 Critical Testing Areas

### 1. **Positional Parameter Generation**
Verify OpenEdge's `?` parameters are still generated correctly:

```csharp
[Fact]
public void Insert_Should_Generate_Positional_Parameters()
{
    // Test that generated SQL uses ? not @param1, @param2, etc.
}

[Fact] 
public void Update_Should_Maintain_Parameter_Order()
{
    // Test that SET clause parameters come before WHERE clause parameters
}
```

### 2. **Concurrency Control Bypass**
Verify that concurrency checks are properly disabled:

```csharp
[Fact]
public void Update_Should_Disable_Identity_Concurrency_Checks()
{
    // Test that identity columns don't generate problematic WHERE conditions
}

[Fact]
public void Update_Should_Handle_Concurrent_Modifications()
{
    // Test that OpenEdge's limited concurrency support doesn't cause failures
}
```

### 3. **SQL Generation Correctness**
Verify that SQL statements are generated correctly:

```csharp
[Fact]
public void Insert_Should_Generate_Valid_OpenEdge_SQL()
{
    // Test INSERT statement format
}

[Fact]
public void Update_Should_Generate_Valid_OpenEdge_SQL()
{
    // Test UPDATE statement format with proper WHERE clause
}

[Fact]
public void Delete_Should_Generate_Valid_OpenEdge_SQL()
{
    // Test DELETE statement format
}
```