var builder = DistributedApplication.CreateBuilder(args);

// Stable dev password so the persisted Postgres data volume keeps working across restarts.
// Production (docker-compose on Hetzner) uses its own password via environment variables.
var postgresPassword = builder.AddParameter("PostgresPassword", "dev-password-removed", secret: true);

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume("ti4-pgdata");

var ti4db = postgres.AddDatabase("ti4db");

builder.AddProject<Projects.Ti4Companion_ApiService>("apiservice")
    .WithReference(ti4db)
    .WaitFor(ti4db);

builder.Build().Run();
