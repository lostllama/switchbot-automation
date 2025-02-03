using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SwitchBot.Models;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SwitchBot
{
    public class HeaterControllerService : BackgroundService
    {
        private readonly IOptions<OfficeOptions> _options;
        private readonly SwitchBotTemperatureService _switchBotConditions;
        private readonly HeaterService _heaterService;
        private readonly StateService _stateService;

        public HeaterControllerService(
            IOptions<OfficeOptions> options,
            SwitchBotTemperatureService switchBotConditions,
            HeaterService heaterService,
            StateService stateService)
        {
            _options = options;
            _switchBotConditions = switchBotConditions;
            _heaterService = heaterService;
            _stateService = stateService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const float temperatureCreepThreshold = 0.5f;
            var waitPeriod = TimeSpan.FromSeconds(30);

            bool isFirstStart = false;

            var initialStatus = (await _switchBotConditions.GetConditionsAsync(_options.Value.HubId, cancellationToken: stoppingToken)).After;
            var lastKnownTemperature = initialStatus.Temperature;
            var lastProcessedTemperature = initialStatus.Temperature;

            while (!stoppingToken.IsCancellationRequested)
            {
                // Get current stats
                var changeItem = await _switchBotConditions.GetConditionsAsync(_options.Value.HubId, cancellationToken: stoppingToken);

                // Update current heater status periodically
                if ((DateTime.UtcNow.ToUniversalTime() - _stateService.LastChecked).TotalSeconds >= 60)
                {
                    // Turn off the heater if it's on and we think it should be off
                    var isHeaterBelievedOn = _stateService.IsHeaterOn;
                    var isHeaterOn = await _heaterService.IsHeaterOnAsync(forceRefresh: true, stoppingToken);

                    var temperatureDiff = changeItem.After.Temperature - lastKnownTemperature;
                    if (temperatureDiff >= temperatureCreepThreshold && !isHeaterBelievedOn && isHeaterOn)
                    {
                        Console.WriteLine("Heater was believed to be off, but is actually on. Turning off.");
                        await _heaterService.TurnHeaterOffAsync(stoppingToken);
                    }
                    else
                    {
                        Console.WriteLine("Heater is currently {0}", isHeaterOn ? "on" : "off");
                    }
                }

                // If we're running manual, we should quit
                if (!_stateService.ServiceEnabled)
                {
                    await Task.Delay(waitPeriod, stoppingToken);
                    continue;
                }

                // The last temperature check was too recent
                if (lastProcessedTemperature == changeItem.After.Temperature)
                {
                    continue;
                }

                lastKnownTemperature = changeItem.After.Temperature;
                lastProcessedTemperature = changeItem.After.Temperature;

                var isTooHot = changeItem.After.Temperature > _stateService.MaxTemperature;
                var turnOff = isTooHot && _stateService.IsHeaterOn;

                var isTooCold = changeItem.After.Temperature < _stateService.MinTemperature;
                var turnOn = isTooCold && !_stateService.IsHeaterOn;

                if (!isTooCold && !isTooHot && isFirstStart)
                {
                    Console.WriteLine("The temperature is in the sweet spot.");
                }
                isFirstStart = false;

                if (turnOff)
                {
                    await _heaterService.TurnHeaterOffAsync(stoppingToken);
                    Console.WriteLine("Heater turned off.");
                }
                else if (turnOn)
                {
                    await _heaterService.TurnHeaterOnAsync(stoppingToken);
                    Console.WriteLine("Heater turned on.");
                }

                await Task.Delay(waitPeriod, stoppingToken);
            }
        }
    }
}
