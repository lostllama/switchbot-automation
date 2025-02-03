using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SwitchBot.Models;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SwitchBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddEnvironmentVariables();
            builder.Services.AddControllersWithViews();
            builder.Services.AddMvc(opts => opts.EnableEndpointRouting = false);

            // Add services to the container.
            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient("switchbot")
                .AddHttpMessageHandler<SwitchBotHandler>()
                .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromMinutes(5));

            builder.Services.AddTransient<SwitchBotHandler>();
            builder.Services.AddSingleton<SwitchBotService>();
            builder.Services.AddSingleton<SwitchBotTemperatureService>();
            builder.Services.AddSingleton<StateService>();
            builder.Services.AddSingleton<HeaterService>();

            builder.Services.AddOptions<SwitchBotOptions>().BindConfiguration("SwitchBot");
            builder.Services.AddOptions<OfficeOptions>().BindConfiguration("Office");

            builder.Services.AddSingleton<ConditionsMonitorService>();
            builder.Services.AddHostedService<ConditionsMonitorService>();

            builder.Services.AddSingleton<HeaterControllerService>();
            builder.Services.AddHostedService<HeaterControllerService>();

            builder.Services.AddMemoryCache();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();

            await app.Services.GetRequiredService<StateService>().InitializeStateAsync();
            await app.Services.GetRequiredService<HeaterService>().TurnHeaterOffAsync();

            app.Run();

        }
    }
}