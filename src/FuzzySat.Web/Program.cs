using FuzzySat.Core.Persistence;
using FuzzySat.Web.Components;
using FuzzySat.Web.Services;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRadzenComponents();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// FuzzySat services
builder.Services.Configure<ProjectStorageOptions>(
    builder.Configuration.GetSection("ProjectStorage"));
builder.Services.AddSingleton<RasterService>();
builder.Services.AddSingleton<ProjectLoaderService>();
builder.Services.AddSingleton<TrainingService>();
builder.Services.AddSingleton<ClassificationService>();
builder.Services.AddSingleton<HybridClassificationService>();
builder.Services.AddSingleton<ModelComparisonService>();
builder.Services.AddSingleton<ValidationService>();
builder.Services.AddSingleton<PixelExtractionService>();
builder.Services.AddSingleton<Sentinel2ImportService>();
builder.Services.AddSingleton<IProjectRepository, FileProjectRepository>();
builder.Services.AddScoped<ProjectStateService>();
builder.Services.AddScoped<ProjectPersistenceService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
