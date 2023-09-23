using CrowApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add file service.
builder.Services.AddScoped<IFileService, FileService>();

// Add logging component.
builder.Logging.AddSimpleConsole();

var app = builder.Build();

// Configure the HTTP request pipeline.
if ( app.Environment.IsDevelopment() )
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

if ( app.Environment.IsDevelopment() )
{
    app.MapGet( "/endpoints", (IEnumerable<EndpointDataSource> endpoints) =>
    {
        // エンドポイントの一覧を取得し、レスポンスとして返す
        var endpointsList = endpoints
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => new
            {
                endpoint.DisplayName,
                endpoint.RoutePattern.RawText,
                endpoint.RoutePattern.RequiredValues,
                endpoint.RoutePattern.Defaults
            });
        return Results.Json( endpointsList );
    } );
}
app.Run();
