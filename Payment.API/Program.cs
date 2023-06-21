using MassTransit;
using Payment.API.Consumers;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

//MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StockReservedEventConsumer>();
    x.UsingRabbitMq((context, conf) =>
    {
        conf.Host(builder.Configuration.GetConnectionString("RabbitMq"));
        conf.ReceiveEndpoint(RabbitMqSettingsConst.StockReserveQueueName, e =>
        {
            e.ConfigureConsumer<StockReservedEventConsumer>(context);
        });
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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