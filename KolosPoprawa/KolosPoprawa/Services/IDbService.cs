using KolosPoprawa.Models.DTOs;

namespace KolosPoprawa.Services;

public interface IDbService
{
    Task<ClientRentalDto> GetClientRentalAsync(int clientId);
}