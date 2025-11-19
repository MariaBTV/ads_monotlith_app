# Lessons Learned: Monolith Decomposition

This document captures key learnings and issues encountered during the decomposition process.

---

## Build Issue: Duplicate Assembly Attributes (19 Nov 2025)

**Problem:**  
The `RetailMonolith` project failed to build with duplicate `TargetFrameworkAttribute` errors:
```
error CS0579: Duplicate 'global::System.Runtime.Versioning.TargetFrameworkAttribute' attribute
```

**Root Cause:**  
The `RetailMonolith.Checkout.Tests/` folder was nested **inside** the main `RetailMonolith` project directory. MSBuild's default file globbing patterns inadvertently included the test project's generated `obj` files (specifically `RetailMonolith.Checkout.Tests\obj\Debug\net8.0\.NETCoreApp,Version=v8.0.AssemblyAttributes.cs`) during the main project's compilation, resulting in duplicate assembly attributes.

**Solution:**  
Added explicit exclusions to `RetailMonolith.csproj` to prevent MSBuild from compiling files from nested project folders:

```xml
<ItemGroup>
  <!-- Exclude nested project folders from compilation -->
  <Compile Remove="RetailMonolith.Checkout.Api/**" />
  <Compile Remove="RetailMonolith.Checkout.Tests/**" />
  <Content Remove="RetailMonolith.Checkout.Api/**" />
  <Content Remove="RetailMonolith.Checkout.Tests/**" />
  <EmbeddedResource Remove="RetailMonolith.Checkout.Api/**" />
  <EmbeddedResource Remove="RetailMonolith.Checkout.Tests/**" />
  <None Remove="RetailMonolith.Checkout.Api/**" />
  <None Remove="RetailMonolith.Checkout.Tests/**" />
</ItemGroup>
```

**Key Takeaway:**  
When nesting project folders within a parent project directory (common in monorepo structures), always explicitly exclude them from the parent project's file glob patterns to avoid MSBuild including their build outputs.

**Verification:**  
- ✅ `dotnet build RetailMonolith.csproj` succeeded after applying exclusions
- ✅ No duplicate attribute errors
- ✅ Application starts (database connection issue is separate runtime concern)

---

## Future Entries

Additional lessons and issues will be documented here as the decomposition progresses.
