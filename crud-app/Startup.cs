using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using UserDataAPI;
using Npgsql;

namespace UserDataAPI // Replace with the actual namespace of your project
{
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging()
           .LogTo(Console.WriteLine, LogLevel.Information));

        services.AddControllers();
    }

 public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
{
    try
    {
        using (var scope = app.ApplicationServices.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        using (var connection = new NpgsqlConnection(dbContext.Database.GetConnectionString()))
        {
            connection.Open();

            using (var command = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS Users (Id SERIAL PRIMARY KEY, Name VARCHAR(255), Age INT)", connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
    }
    catch (Exception ex)
    {
        // Handle exceptions, log, or take appropriate actions.
        // Consider using a more specific exception type, like DbUpdateException, for better error handling.
        Console.WriteLine($"An error occurred during database initialization: {ex.Message}");
    }
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
}
