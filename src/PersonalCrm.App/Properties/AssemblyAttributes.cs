using System.Runtime.CompilerServices;

// Expose the implicit `internal` top-level Program class to the integration-test
// assembly so `WebApplicationFactory<Program>` can reference it. See
// https://learn.microsoft.com/aspnet/core/test/integration-tests
[assembly: InternalsVisibleTo("PersonalCrm.Tests.Integration")]
