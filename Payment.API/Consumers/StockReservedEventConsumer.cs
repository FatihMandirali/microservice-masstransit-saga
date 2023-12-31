using MassTransit;
using Shared;

namespace Payment.API.Consumers;

public class StockReservedEventConsumer:IConsumer<StockReservedEvent>
{
    private readonly ILogger<StockReservedEventConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public StockReservedEventConsumer(ILogger<StockReservedEventConsumer> logger, IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        var balance = 3000;
        if (balance > context.Message.Payment.TotalPrice)
        {
            _logger.LogInformation($"{context.Message.Payment.TotalPrice} tl was with roll from credit card id= {context.Message.BuyerId}");
            await _publishEndpoint.Publish(new PaymentSuccessedEvent
            {
                BuyerId = context.Message.BuyerId,
                OrderId = context.Message.OrderId
            });
        }
        else
        {
            _logger.LogError($"tl was not enaught error");
            await _publishEndpoint.Publish(new PaymentFailedEvent
            {
                OrderId = context.Message.OrderId,
                Message = "yeterli bakiye yok"
            });
        }
    }
}