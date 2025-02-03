using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SwitchBot.ViewModels;
using System.Threading;
using System.Threading.Tasks;

namespace SwitchBot.Controllers
{
    [Route("/")]
    public class HomeController : Controller
    {
        private readonly IOptions<OfficeOptions> _options;
        private readonly StateService _stateService;
        private readonly HeaterService _heaterService;
        private readonly SwitchBotTemperatureService _temperatureService;

        public HomeController(
            IOptions<OfficeOptions> options,
            StateService stateService,
            HeaterService heaterService,
            SwitchBotTemperatureService temperatureService)
        {
            _options = options;
            _stateService = stateService;
            _heaterService = heaterService;
            _temperatureService = temperatureService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var currentConditions = (await _temperatureService.GetConditionsAsync(_options.Value.HubId, cancellationToken: cancellationToken)).After;
            var model = new StatusModel()
            {
                IsHeaterOn = _stateService.IsHeaterOn,
                ServiceEnabled = _stateService.ServiceEnabled,
                MinTemperature = _stateService.MinTemperature,
                MaxTemperature = _stateService.MaxTemperature,
                CurrentTemperature = currentConditions.Temperature
            };
            return View(model);
        }

        [HttpGet("/setmode")]
        public async Task<IActionResult> SetMode([FromQuery] bool isAutomatic, [FromQuery] bool turnHeaterOff, CancellationToken cancellationToken)
        {
            if (isAutomatic)
            {
                var currentConditions = (await _temperatureService.GetConditionsAsync(_options.Value.HubId, cancellationToken: cancellationToken)).After;
                await _stateService.UpdateStateAsync(s => s.ServiceEnabled = true, cancellationToken);
                if (currentConditions.Temperature <= _stateService.MinTemperature)
                {
                    await _heaterService.TurnHeaterOnAsync(cancellationToken);
                }
            }
            else
            {
                await _stateService.UpdateStateAsync(s => s.ServiceEnabled = false, cancellationToken);
                if (turnHeaterOff)
                {
                    await _heaterService.TurnHeaterOffAsync(cancellationToken);
                    await _stateService.UpdateStateAsync(s => s.IsHeaterOn = false, cancellationToken);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("/manual")]
        public async Task<IActionResult> SetHeaterStatus([FromQuery] bool turnHeaterOn, CancellationToken cancellationToken)
        {
            if (_stateService.ServiceEnabled)
            {
                await _stateService.UpdateStateAsync(s => s.ServiceEnabled = false, cancellationToken);
            }
            if (turnHeaterOn)
            {
                await _heaterService.TurnHeaterOnAsync(cancellationToken);
                await _stateService.UpdateStateAsync(s => s.IsHeaterOn = true, cancellationToken);
            }
            else
            {
                await _heaterService.TurnHeaterOffAsync(cancellationToken);
                await _stateService.UpdateStateAsync(s => s.IsHeaterOn = false, cancellationToken);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("/refreshdata")]
        public async Task<IActionResult> RefreshStatus(CancellationToken cancellationToken)
        {
            _ = await _heaterService.IsHeaterOnAsync(forceRefresh: true, cancellationToken);
            _ = await _temperatureService.GetConditionsAsync(_options.Value.HubId, forceRefresh: true, cancellationToken);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("/edittemps")]
        public IActionResult BeginEditTemps(CancellationToken cancellationToken)
        {
            var model = new ConfigModel()
            {
                MinTemperature = _stateService.MinTemperature,
                MaxTemperature = _stateService.MaxTemperature
            };
            return View(model);
        }

        [HttpPost("/edittemps")]
        public async Task<IActionResult> UpdateTemps(ConfigModel model, CancellationToken cancellationToken)
        {
            await _stateService.UpdateStateAsync(s =>
            {
                s.MinTemperature = model.MinTemperature;
                s.MaxTemperature = model.MaxTemperature;
            }, cancellationToken);

            if (_stateService.ServiceEnabled)
            {
                var currentConditions = (await _temperatureService.GetConditionsAsync(_options.Value.HubId, cancellationToken: cancellationToken)).After;
                if (currentConditions.Temperature > model.MaxTemperature)
                {
                    await _heaterService.TurnHeaterOffAsync(cancellationToken);
                }
                else if (currentConditions.Temperature < model.MinTemperature)
                {
                    await _heaterService.TurnHeaterOnAsync(cancellationToken);
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
