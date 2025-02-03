using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SwitchBot.Models;
using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SwitchBot
{
    public class ConditionsMonitorService : BackgroundService
    {
        private readonly IOptions<OfficeOptions> _options;
        private readonly SwitchBotTemperatureService _switchBot;

        public ConditionsMonitorService(
            IOptions<OfficeOptions> options,
            SwitchBotTemperatureService switchBot)
        {
            _switchBot = switchBot;
            _options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ConditionsModel? lastConditions = null;
            while (!stoppingToken.IsCancellationRequested)
            {
                var conditions = (await _switchBot.GetConditionsAsync(_options.Value.HubId, cancellationToken: stoppingToken)).After;

                Console.Title = $"Office: {conditions.Temperature}°C, {conditions.Humidity}% RHI";
                if (lastConditions is null
                    || lastConditions.Temperature != conditions.Temperature
                    || lastConditions.Humidity != conditions.Humidity)
                {
                    Console.WriteLine(
                        "{0}°C, {1}% RHI -> {2}°C, {3}% RHI.",
                        lastConditions?.Temperature ?? conditions.Temperature,
                        lastConditions?.Humidity ?? conditions.Humidity,
                        conditions.Temperature,
                        conditions.Humidity
                    );
                }
                lastConditions = conditions;

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
