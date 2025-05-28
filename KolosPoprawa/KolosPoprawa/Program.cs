

using KolosPoprawa.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddScoped<IDbService,DbService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapControllers();

app.UseAuthorization();

app.Run();