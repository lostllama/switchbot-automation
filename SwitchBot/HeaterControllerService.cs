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

            var isHeaterOn = await _heaterService.IsHeaterOnAsync(cancellationToken: stoppingToken);
            Console.WriteLine("Heater is currently {0}.", (isHeaterOn ? "on" : "off"));

            bool isFirstStart = false;

            var initialStatus = (await _switchBotConditions.GetConditionsAsync(_options.Value.HubId, cancellationToken: stoppingToken)).After;
            var lastKnownTemperature = initialStatus.Temperature;
            var lastProcessedTemperature = initialStatus.Temperature;

            while (!stoppingToken.IsCancellationRequested)
            {
                // Check that the expected status is the same as what we're observing through temperature changes
                var changeItem = await _switchBotConditions.GetConditionsAsync(_options.Value.HubId, cancellationToken: stoppingToken);
                //var temperatureDiff = changeItem.After.Temperature - lastKnownTemperature;
                //if (temperatureDiff >= temperatureCreepThreshold || temperatureDiff <= -temperatureCreepThreshold)
                //{
                //    var expectedHeaterState = _stateService.IsHeaterOn;
                //    var actualHeaterState = await _heaterService.IsHeaterOnAsync(forceRefresh: true, cancellationToken: stoppingToken);
                //    // if there is a difference in states, we should rectify that
                //    if (actualHeaterState != expectedHeaterState)
                //    {
                //        if (expectedHeaterState)
                //        {
                //            await _heaterService.TurnHeaterOnAsync(stoppingToken);
                //        }
                //        else
                //        {
                //            await _heaterService.TurnHeaterOffAsync(stoppingToken);
                //        }
                //    }
                //}

                // If we're running manual, we should quit
                if (!_stateService.ServiceEnabled)
                {
                    await Task.Delay(waitPeriod, stoppingToken);
                    continue;
                }

                if (lastProcessedTemperature == changeItem.After.Temperature)
                {
                    continue;
                }
                lastKnownTemperature = changeItem.After.Temperature;

                var isTooHot = changeItem.After.Temperature > _stateService.MaxTemperature;
                var turnOff = isTooHot && isHeaterOn;

                var isTooCold = changeItem.After.Temperature < _stateService.MinTemperature;
                var turnOn = isTooCold && !isHeaterOn;

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
