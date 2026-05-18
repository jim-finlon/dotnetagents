using DotNetAgents.Workflow.Designer;
using DotNetAgents.Workflow.Designer.Web;
using DotNetAgents.Workflow.Designer.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register services
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IWorkflowDesignerService, WorkflowDesignerServiceClient>();

await builder.Build().RunAsync();
