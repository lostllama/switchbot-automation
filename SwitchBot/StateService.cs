using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SwitchBot
{
    public class StateService
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly IOptions<OfficeOptions> _options;

        public bool IsHeaterOn { get; set; }
        [JsonIgnore]
        public bool ServiceEnabled { get; set; } = false;
        public float MinTemperature { get; set; } = 10f;
        public float MaxTemperature { get; set; } = 22f;

        public StateService(IOptions<OfficeOptions> options)
        {
            _options = options;
        }

        public async Task InitializeStateAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                await ReadStateAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task UpdateStateAsync(Func<StateService, CancellationToken, Task> updateState, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await updateState(this, cancellationToken);
                await SaveStateAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task UpdateStateAsync(Action<StateService> updateState, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                updateState(this);
                await SaveStateAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task SaveStateAsync()
        {
            var serializedState = JsonConvert.SerializeObject(this);
            await File.WriteAllTextAsync(_options.Value.StateFile, serializedState);
        }

        private async Task ReadStateAsync()
        {
            if (!File.Exists(_options.Value.StateFile))
            {
                await SaveStateAsync();
                return;
            }
            var serializedState = await File.ReadAllTextAsync(_options.Value.StateFile);
            JsonConvert.PopulateObject(serializedState, this);
        }
    }
}
