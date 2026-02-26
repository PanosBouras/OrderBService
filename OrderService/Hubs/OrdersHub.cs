using Microsoft.AspNetCore.SignalR;

namespace OrderService.Hubs
{
    public class OrdersHub : Hub
    {
        public async Task JoinCompanyGroup(string companyId)
        {
            Console.WriteLine($"Client joined group: {companyId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, companyId);
        }
    }
}