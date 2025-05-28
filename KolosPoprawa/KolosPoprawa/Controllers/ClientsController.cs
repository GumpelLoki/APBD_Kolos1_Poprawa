using KolosPoprawa.Exceptions;
using KolosPoprawa.Models.DTOs;
using KolosPoprawa.Services;
using Microsoft.AspNetCore.Mvc;

namespace KolosPoprawa.Controllers;
[ApiController]
[Route("api/[controller]")]
public class ClientsController(IDbService _dbService) : ControllerBase
{
    [HttpGet("{clientId}")]
    public async Task<IActionResult> GetClientRentals(int clientId)
    {
        try
        {
            var visit =  await _dbService.GetClientRentalAsync(clientId);
            return Ok(visit);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddClientTask([FromBody] CreateClientRentalDto clientDto)
    {
        try
        {
            await _dbService.AddClientRentalAsync(clientDto);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        return CreatedAtAction(nameof(GetClientRentals), clientDto);
    }
}