using Microsoft.Extensions.Caching.Memory;
using SwitchBot.Models;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SwitchBot
{
    public class SwitchBotTemperatureService
    {
        const int _cacheExpirationSeconds = 120; // 2 minutes
        private readonly IMemoryCache _memoryCache;
        private readonly SwitchBotService _switchbotService;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _deviceSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly ConcurrentDictionary<string, ConditionsModel> _lastCondition = new ConcurrentDictionary<string, ConditionsModel>();

        public SwitchBotTemperatureService(
            IMemoryCache memoryCache,
            SwitchBotService switchbotService)
        {
            _memoryCache = memoryCache;
            _switchbotService = switchbotService;
        }

        public async Task<BeforeAfterModel<ConditionsModel>> GetConditionsAsync(string deviceId, bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"temperatureChange_{deviceId}";
            if (!forceRefresh && _memoryCache.TryGetValue<BeforeAfterModel<ConditionsModel>>(cacheKey, out var existingValue)
                && existingValue is not null)
            {
                return existingValue;
            }

            _deviceSemaphores.TryAdd(deviceId, new SemaphoreSlim(1));
            var semaphore = _deviceSemaphores[deviceId];
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!forceRefresh &&
                    _memoryCache.TryGetValue<BeforeAfterModel<ConditionsModel>>(cacheKey, out var newExistingValue)
                    && newExistingValue is not null)
                {
                    return newExistingValue;
                }

                _lastCondition.TryGetValue(deviceId, out var lastCondition);
                var currentCondition = await _switchbotService.GetConditionsFromHub2Async(deviceId, cancellationToken);
                _lastCondition[deviceId] = currentCondition;

                var result = new BeforeAfterModel<ConditionsModel>()
                {
                    Before = lastCondition,
                    After = currentCondition
                };

                _memoryCache.Set(cacheKey, result, new MemoryCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_cacheExpirationSeconds)
                });
                return result;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
