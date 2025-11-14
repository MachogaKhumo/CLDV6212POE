using ABC_Retail.Data;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register Entity Framework with Azure SQL - FIXED THIS LINE
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

// Add Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register the Functions API HTTP Client
builder.Services.AddHttpClient<IFunctionsApi, FunctionsApiClient>((sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["AzureFunctions:BaseUrl"]?.TrimEnd('/');
    if (string.IsNullOrEmpty(baseUrl))
        throw new InvalidOperationException("AzureFunctions:BaseUrl not set in configuration.");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddScoped<IFunctionsApi, FunctionsApiClient>();

// Register Azure Storage Service
builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();

// Add Azure clients
builder.Services.AddAzureClients(clientBuilder =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureStorage");
    clientBuilder.AddTableServiceClient(connectionString);
    clientBuilder.AddBlobServiceClient(connectionString);
    clientBuilder.AddQueueServiceClient(connectionString);
    clientBuilder.AddFileServiceClient(connectionString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Add Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();