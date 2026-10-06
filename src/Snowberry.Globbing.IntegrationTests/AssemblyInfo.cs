using Snowberry.Globbing.IntegrationTests.Conformance;
using Snowberry.Globbing.IntegrationTests.Infrastructure;

[assembly: AssemblyFixture(typeof(ConformanceFixture))]
[assembly: AssemblyFixture(typeof(PostgreSqlFixture))]
[assembly: AssemblyFixture(typeof(NodeFixture))]