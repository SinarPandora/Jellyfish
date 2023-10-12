using Autofac;
using Autofac.Extensions.DependencyInjection;
using Jellyfish.Core.Data;
using Microsoft.EntityFrameworkCore;
using NLog.Web;
using AppContext = Jellyfish.Core.Container.AppContext;

namespace Jellyfish;

public static class JellyFish
{
    /// <summary>
    ///     The entry point of the application.
    /// </summary>
    /// <param name="args">Commandline args</param>
    public static void Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // NLog: Setup NLog for Dependency injection
            builder.Logging.ClearProviders();
            builder.Host.UseNLog();

            // Init database context
            builder.Host.ConfigureContainer<ContainerBuilder>(container =>
            {
                container.Register(_ =>
                    {
                        var options = new DbContextOptionsBuilder<DatabaseContext>();
                        options.UseNpgsql(builder.Configuration.GetValue<string>("DatabaseConnection"))
                            .UseSnakeCaseNamingConvention();
                        return new DatabaseContext(options.Options);
                    })
                    .InstancePerLifetimeScope();

                // Binding other instances
                AppContext.BindAll(container);
            });

            var app = builder.Build();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
        finally
        {
            // Flush log buffer when application shutting down
            NLog.LogManager.Shutdown();
        }
    }
}
