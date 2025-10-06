using ABC_Retail.Services;
using Microsoft.Extensions.Azure;

namespace ABC_Retail
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register the Functions API HTTP Client
            builder.Services.AddHttpClient<IFunctionsApi, FunctionsApiClient>();
            builder.Services.AddScoped<IFunctionsApi, FunctionsApiClient>();

            // Register Azure Storage Service
            builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();

            // Bind config
            var storageConn = builder.Configuration.GetSection("AzureStorage:ConnectionString").Value ?? string.Empty;

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

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
