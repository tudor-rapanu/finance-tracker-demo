using FinanceTracker.Contracts;
using FinanceTracker.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExchangeRatesController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;

    public ExchangeRatesController(IExchangeRateService exchangeRateService) =>
        _exchangeRateService = exchangeRateService;

    /// <summary>Get latest exchange rates relative to a base currency.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ExchangeRateDto), 200)]
    public async Task<IActionResult> GetRates([FromQuery] string baseCurrency = "USD")
    {
        var rates = await _exchangeRateService.GetRatesAsync(baseCurrency);
        return Ok(rates);
    }
}
