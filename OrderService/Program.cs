using OrderService;
using OrderService.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Initialize connection string
ConnectionString.Initialize(builder.Configuration);

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UsePathBase("/orderservice");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/orderservice/swagger/v1/swagger.json", "OrderService API V1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<OrdersHub>("/ordersHub");
app.MapHub<TableHub>("/tableHub");

app.Run();