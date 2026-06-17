var builder = DistributedApplication.CreateBuilder(args);

// SQLite is a local file inside the ApiService — no database container, so no Docker needed.
builder.AddProject<Projects.Ti4Companion_ApiService>("apiservice");

builder.Build().Run();
