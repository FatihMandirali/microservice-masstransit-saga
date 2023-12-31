using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared;
using Stock.API.Models;

namespace Stock.API.Consumers;

public class OrderCreatedEventConsumer:IConsumer<OrderCreatedEvent>
{
    private readonly AppDbContext _context;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderCreatedEventConsumer(AppDbContext context, ILogger<OrderCreatedEventConsumer> logger, ISendEndpointProvider sendEndpointProvider, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _logger = logger;
        _sendEndpointProvider = sendEndpointProvider;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var stockResult = new List<bool>();

        foreach (var item in context.Message.OrderItem)
        {
            stockResult.Add(await _context.Stocks.AnyAsync(x=>x.ProductId==item.ProductId && x.Count>=item.Count));
        }

        if (stockResult.All(x => x.Equals(true)))
        {
            foreach (var item in context.Message.OrderItem)
            {
                var stock = await _context.Stocks.FirstOrDefaultAsync(x=>x.ProductId==item.ProductId);
                if (stock is not null)
                {
                    stock.Count -= item.Count;
                }

                await _context.SaveChangesAsync();
            }
            _logger.LogInformation($"reserve edildi for {context.Message.BuyerId}");
            var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{RabbitMqSettingsConst.StockReserveQueueName}"));

            StockReservedEvent stockReservedEvent = new StockReservedEvent
            {
                Payment = context.Message.Payment,
                BuyerId = context.Message.BuyerId,
                OrderId = context.Message.OrderId,
                OrderItem = context.Message.OrderItem
            };

            await sendEndpoint.Send(stockReservedEvent);

        }
        else
        {
            await _publishEndpoint.Publish(new StockNotReservedEvent
            {
                BuyerId = context.Message.BuyerId,
                OrderId = context.Message.OrderId,
                Message = "Not Stock"
            });
        }
    }
}