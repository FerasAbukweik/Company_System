# Backend tests

Run both projects from the repository root:

```sh
dotnet test Company_System.slnx -m:1 -nr:false
```

Test classes follow the supplied service-test example:

- Inherit `ServiceTestBase` or `RepositoryTestBase` from `Shared/TestBase.cs`.
- Use the shared recursion-safe `Fixture` and injected `ITestOutputHelper`.
- Construct repeated mocks and the service under test in the constructor.
- Keep reusable arrangements in small helpers and group scenarios in regions.
- Separate Arrange, Act, and Assert, log relevant arrangements with `LogArrange`, and log results with `LogAct`.
- Assert behavior with FluentAssertions and verify repository arguments, cancellation, paging, and writes with Moq.

Service tests mock dependencies. Repository tests use isolated EF databases and clear tracking before exercising attach-based updates. Refresh-token deletion uses SQLite in memory to exercise relational `ExecuteDeleteAsync`, including repeated token consumption. No test connects to the application database.

The existing `GetParentUserIds_RequiresIntegrationTestAgainstRealSqlServer` test remains explicitly skipped because its recursive SQL Server query needs a dedicated SQL Server integration fixture.
