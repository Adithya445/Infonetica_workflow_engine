using WorkflowEngine.Presentation;

var builder = WebApplication.CreateBuilder(args);
PresentationLayer.ConfigureServices(builder);

var app = builder.Build();
PresentationLayer.ConfigureEndpoints(app);

app.Run();