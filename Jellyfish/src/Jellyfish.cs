﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using Jellyfish.Core.Data;
using Jellyfish.Core.Enum;
using Jellyfish.Module.ExpireExtendSession.Data;
using Kook;
using Microsoft.EntityFrameworkCore;
using NLog.Web;
using Npgsql;
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
                        var dataSourceBuilder = new NpgsqlDataSourceBuilder(
                            builder.Configuration.GetValue<string>("DatabaseConnection")
                        );

                        dataSourceBuilder.MapEnum<ChannelType>();
                        dataSourceBuilder.MapEnum<TimeUnit>();
                        dataSourceBuilder.MapEnum<ExtendTargetType>();

                        return new DatabaseContext(
                            new DbContextOptionsBuilder<DatabaseContext>()
                                .UseNpgsql(dataSourceBuilder.Build())
                                .UseSnakeCaseNamingConvention()
                                .Options
                        );
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
