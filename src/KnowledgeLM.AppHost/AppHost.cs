var builder = DistributedApplication.CreateBuilder(args);
var mauiapp = builder.AddMauiProject("KnowledgeLM", @"../KnowledgeLM.App/KnowledgeLM.App.csproj");

// Add Windows device (uses localhost directly)
mauiapp.AddWindowsDevice();

builder.Build().Run();
