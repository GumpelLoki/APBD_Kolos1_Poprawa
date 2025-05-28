using System.Data.Common;
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
                    Where c.Id = @clientId;";
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

    public async Task AddClientRentalAsync(CreateClientRentalDto clientRental)
    {
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;
        try
        {
            command.CommandText = @"Select ID from cars where ID = @id;";
            command.Parameters.AddWithValue("@id", clientRental.CarId);
            var car = await command.ExecuteScalarAsync();
            if (car == null)
                throw new NotFoundException($"Car with ID {clientRental.CarId} not found");
            command.Parameters.Clear();
            command.CommandText = @"Select Max(Id) from clients;";
            var res = await command.ExecuteReaderAsync();
            int maxid = 10;
            while (await res.ReadAsync())
            {
                maxid = res.GetInt32(0);
            }
            maxid++;
            command.Parameters.Clear();
            command.CommandText = @"Select PricePerDay from cars where ID = @id;";
            command.Parameters.AddWithValue("@id", clientRental.CarId);
            var temppricePerDay = await command.ExecuteReaderAsync();
            int pricePerDay = 0;
            while (await temppricePerDay.ReadAsync())
            {
                pricePerDay = temppricePerDay.GetInt32(0);
            }
            command.Parameters.Clear();
            command.CommandText = @"Insert into clients(ID,FirstName,LastName,Address)
                                    VALUES (@id,@FirstName,@LastName,@Address);";
            command.Parameters.AddWithValue("@id", maxid);
            command.Parameters.AddWithValue("@FirstName", clientRental.Client.FirstName);
            command.Parameters.AddWithValue("@LastName", clientRental.Client.LastName);
            command.Parameters.AddWithValue("@Address", clientRental.Client.Address);
            await command.ExecuteNonQueryAsync();
            command.Parameters.Clear();
            command.CommandText = @"Select Max(Id) from car_rentals;";
            var resu = await command.ExecuteReaderAsync();
            int maxidr = 10;
            while (await resu.ReadAsync())
            {
                maxidr = resu.GetInt32(0);
            }

            maxidr++;
            command.Parameters.Clear();
            command.CommandText = @"INSERT into car_rentals(ID,ClientID,CarID,DateFrom,DateTo,TotalPrice,Discount)
                                     values (@id,@ClientId,@CarID,@DateFrom,@DateTo,@TotalPrice,@Discount) ;";
            command.Parameters.AddWithValue("@id", maxidr);
            command.Parameters.AddWithValue("@Client", maxid);
            command.Parameters.AddWithValue("@CarID", clientRental.CarId);
            command.Parameters.AddWithValue("@DateFrom", clientRental.DateFrom);
            command.Parameters.AddWithValue("@DateTo", clientRental.DateTo);
            double TotalPrice = pricePerDay*(clientRental.DateTo - clientRental.DateFrom).TotalDays;
            command.Parameters.AddWithValue("@TotalPrice", TotalPrice);
            command.Parameters.AddWithValue("@Discount", null);
            command.ExecuteNonQuery();

            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw e;
        }
    }
}