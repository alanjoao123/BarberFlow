using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Barbearia.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var enderecoDaApi = builder.HostEnvironment.IsDevelopment()
    ? "http://localhost:5114"
    : "https://barberflow-jwnp.onrender.com";

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(enderecoDaApi) });

await builder.Build().RunAsync();
