using HttpClientFactoryCustom.Repository;
using HttpClientFactoryCustom.Repository.UnitOfWork;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("ExternalApiClient", client =>
{
    client.BaseAddress = new Uri("url");
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    // Allowing Untrusted SSL Certificates
    var handler = new HttpClientHandler();
    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
    handler.ServerCertificateCustomValidationCallback =
        (httpRequestMessage, cert, cetChain, policyErrors) => true;

    return handler;
});
// Unit of work Factory
builder.Services.AddSingleton<ConnectionStringProvider>();
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddSingleton<IOraclelConnectionFactory, OracleConnectionFactory>();

// Register factory delegate for UnitOfWork
builder.Services.AddTransient<Func<DatabaseName, IUnitOfWork>>(serviceProvider => dbName =>
{
    var factory = serviceProvider.GetRequiredService<ISqlConnectionFactory>();
    return new UnitOfWork(factory, dbName);
});
builder.Services.AddTransient<Func<DatabaseName, IOracleUnitOfWork>>(serviceProvider => dbName =>
{
    var factory = serviceProvider.GetRequiredService<IOraclelConnectionFactory>();
    return new OracleUnitOfWork(factory, dbName);
});
// Unit of work Factory
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
