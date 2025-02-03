using SwitchBot.Models;
using SwitchBot.SwitchBotModels;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SwitchBot
{
    public class SwitchBotService
    {
        private readonly HttpClient _httpClient;

        public SwitchBotService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("switchbot");
            _httpClient.BaseAddress = new Uri("https://api.switch-bot.com");
        }

        public async Task<string> GetDevicesAsync(CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetStringAsync("/v1.1/devices", cancellationToken);
        }

        public async Task<string> GetResultAsync(string path, CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetStringAsync(path, cancellationToken);
        }

        private async Task<SwitchBotResponse<TBody>> GetDeviceStatusAsync<TBody>(string deviceId, CancellationToken cancellationToken = default)
            where TBody : class
        {
            var status = await _httpClient.GetFromJsonAsync<SwitchBotResponse<TBody>>($"/v1.1/devices/{deviceId}/status", cancellationToken);
            if (status is null || !status.IsSuccess)
            {
                throw new Exception("Not able to get status.");
            }
            return status;
        }

        private async Task PostCommandAsync<TBody>(string deviceId, TBody data, CancellationToken cancellationToken = default)
        {
            var result = await _httpClient.PostAsJsonAsync($"/v1.1/devices/{deviceId}/commands", data);
            result.EnsureSuccessStatusCode();
        }

        public async Task<ConditionsModel> GetConditionsFromHub2Async(string deviceId, CancellationToken cancellationToken = default)
        {
            var status = await GetDeviceStatusAsync<Hub2Status>(deviceId, cancellationToken);

            return new ConditionsModel()
            {
                Temperature = status.Body.Temperature,
                Humidity = status.Body.Humidity,
                LightLevel = status.Body.LightLevel
            };
        }

        public async Task<PlugModel> GetPlugStatusAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            var status = await GetDeviceStatusAsync<PlugMiniStatus>(deviceId, cancellationToken);

            return new PlugModel()
            {
                Status = string.Equals(status.Body.Power, "on") ? PlugStatus.PoweredOn : PlugStatus.PoweredOff,
                Voltage = status.Body.Voltage,
                Current = status.Body.ElectricCurrent / 500.0f,
                DayPowerConsumptionWatts = status.Body.Weight,
                DayUsageDuration = TimeSpan.FromMinutes(status.Body.ElectricityOfDay)
            };
        }

        public async Task<ConditionsModel> GetConditionsFromOutdoorMeterAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            var status = await GetDeviceStatusAsync<OutdoorMeterStatus>(deviceId, cancellationToken);

            return new ConditionsModel()
            {
                Temperature = status.Body.Temperature,
                Humidity = status.Body.Humidity
            };
        }

        public async Task<ConditionsModel> GetConditionsFromMeterAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            var status = await GetDeviceStatusAsync<MeterStatus>(deviceId, cancellationToken);

            return new ConditionsModel()
            {
                Temperature = status.Body.Temperature,
                Humidity = status.Body.Humidity
            };
        }

        public async Task PressButtonAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            var model = new
            {
                command = "press"
            };

            await PostCommandAsync(deviceId, model, cancellationToken);
        }

        public async Task LockDoorAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            var model = new
            {
                command = "lock"
            };

            await PostCommandAsync(deviceId, model, cancellationToken);
        }

        public async Task UnlockDoorAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            var model = new
            {
                command = "unlock"
            };

            await PostCommandAsync(deviceId, model, cancellationToken);
        }
    }
}
