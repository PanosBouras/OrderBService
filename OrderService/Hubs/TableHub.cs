using Microsoft.AspNetCore.SignalR;

namespace OrderService.Hubs
{
    public class TableHub : Hub
    {
        public async Task JoinCompanyGroup(string companyId)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                companyId);

            Console.WriteLine(
                $"JOINED GROUP {companyId} - {Context.ConnectionId}"
            );
        }
        public async Task LeaveCompanyGroup(string companyId)
        {
            Console.WriteLine($"Client left table group: {companyId}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, companyId);
        }

    }
}