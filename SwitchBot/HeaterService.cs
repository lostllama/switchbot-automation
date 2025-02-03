using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SwitchBot.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SwitchBot
{
    public class HeaterService
    {
        const int _statusCacheTimeSeconds = 30;
        const float _wattageThreshold = 15f;

        private readonly IOptions<OfficeOptions> _options;
        private readonly IMemoryCache _memoryCache;
        private readonly SwitchBotService _switchBot;
        private readonly StateService _stateService;

        public HeaterService(
            IOptions<OfficeOptions> options,
            IMemoryCache memoryCache,
            SwitchBotService switchBot,
            StateService stateService)
        {
            _options = options;
            _memoryCache = memoryCache;
            _switchBot = switchBot;
            _stateService = stateService;
        }

        public async Task<bool> IsHeaterOnAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"plugStatus_{_options.Value.PlugId}";
            PlugModel plugStatus;
            if (!forceRefresh
                && _memoryCache.TryGetValue<PlugModel>(cacheKey, out var existingPlugStatus)
                && existingPlugStatus is not null)
            {
                plugStatus = existingPlugStatus;
            }
            else
            {
                plugStatus = await _switchBot.GetPlugStatusAsync(_options.Value.PlugId, cancellationToken);
                _memoryCache.Set(cacheKey, plugStatus, new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_statusCacheTimeSeconds) });
            }

            bool isHeaterOn = plugStatus.Wattage >= _wattageThreshold;
            await _stateService.UpdateStateAsync(s =>
            {
                s.IsHeaterOn = isHeaterOn;
                s.LastChecked = DateTime.UtcNow;
            }, cancellationToken);
            return isHeaterOn;
        }

        public async Task TurnHeaterOnAsync(CancellationToken cancellationToken = default)
        {
            Console.WriteLine("Turning heater on");
            await _switchBot.PressButtonAsync(_options.Value.HeaterId, cancellationToken);
            await _stateService.UpdateStateAsync(s =>
            {
                s.IsHeaterOn = true;
                s.LastChecked = DateTime.UtcNow;
            }, cancellationToken);
        }

        public async Task TurnHeaterOffAsync(CancellationToken cancellationToken = default)
        {
            Console.WriteLine("Turning heater off");
            await _switchBot.PressButtonAsync(_options.Value.HeaterId, cancellationToken);
            await _stateService.UpdateStateAsync(s =>
            {
                s.IsHeaterOn = false;
                s.LastChecked = DateTime.UtcNow;
            }, cancellationToken);
        }
    }
}
