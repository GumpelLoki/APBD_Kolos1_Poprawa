using KolosPoprawa.Exceptions;
using KolosPoprawa.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace KolosPoprawa.Services;

public class DbService : IDbService
{
    private readonly String _connectionString;

    public DbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
    }
    public async Task<ClientRentalDto> GetClientRentalAsync(int clientId)
    {
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        var query = @"SELECT c.ID, c.FirstName, c.LastName, c.Address,cr.VIN,cl.Name,m.Name,r.DateFrom,r.DateTo,r.TotalPrice
                    FROM clients c 
                    JOIN car_rentals r on c.ID = r.ClientID
                    JOIN cars cr on r.CarID= cr.ID 
                    Join colors cl on cr.ColorID = cl.ID
                    JOIN models m on cr.ModelID = m.ID
                    Where c.Id = @clientId";
        await connection.OpenAsync();
        command.CommandText = query;
        command.Parameters.AddWithValue("@clientId", clientId);
        var answer =command.ExecuteReader();
        ClientRentalDto? clientRental = null;
        List<RentalDto> rentals = new List<RentalDto>();
        while (await answer.ReadAsync())
        {
            if (clientRental == null)
            {
                clientRental = new ClientRentalDto
                {
                    Id = answer.GetInt32(0),
                    FirstName = answer.GetString(1),
                    LastName = answer.GetString(2),
                    Address = answer.GetString(3),
                    Rentals = rentals,
                };
            }

            RentalDto rentalDto = new RentalDto()
            {
                Vin = answer.GetString(4),
                Color = answer.GetString(5),
                Model = answer.GetString(6),
                DateFrom = answer.GetDateTime(7),
                DateTo = answer.GetDateTime(8),
                TotalPrice = answer.GetInt32(9)
            };
            rentals.Add(rentalDto);
        }
        if(clientRental == null)
            throw new NotFoundException("Client Rental not found");
        return clientRental;
    }
    

}