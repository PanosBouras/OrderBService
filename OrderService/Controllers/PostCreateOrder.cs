using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Npgsql;
using OrderService.Hubs;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostCreateOrder : Controller
    {
        private readonly IHubContext<OrdersHub> _hubContext;

        public PostCreateOrder(IHubContext<OrdersHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public class Orderitems
        {
            public string itemId { get; set; }
            public string name { get; set; }
            public int quantity { get; set; }
            public string comment { get; set; }
            public double price { get; set; }
        }

        [HttpPost(Name = "PostCreateOrder")]
        public async Task PostCreateOrderAsync(
            int companyid,
            int tableId,
            string userid,
            string username,
            int persons,
            [FromBody] List<Orderitems> orderJson)
        {
            string orderid = await GetOrderId(tableId, companyid);

            if (string.IsNullOrEmpty(orderid))
            {
                string insertHeader = @"
                    INSERT INTO orderb_orderhdr
                    (orderid, tableid, createdate, statusflg, createuser, persons, companyid)
                    VALUES
                    (to_char(now(), 'DDMMYYHH24MISS'),
                     @tableid,
                     now(),
                     0,
                     @username,
                     COALESCE(@persons,1),
                     @companyid)";

                await using (var connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    await using (var command = new NpgsqlCommand(insertHeader, connection))
                    {
                        command.Parameters.AddWithValue("tableid", tableId);
                        command.Parameters.AddWithValue("username", username);
                        command.Parameters.AddWithValue("persons", persons);
                        command.Parameters.AddWithValue("companyid", companyid);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                orderid = await GetOrderId(tableId, companyid);
                await InsertOrderDetails(companyid, tableId, orderid, orderJson, userid, username);
            }
            else
            {
                await InsertOrderDetails(companyid, tableId, orderid, orderJson, userid, username);
            }
        }

        private async Task<string> GetOrderId(int tableId, int companyID)
        {
            string orderid = "";

            try
            {
                await using (var connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT orderid
                        FROM orderb_orderhdr
                        WHERE tableid = @tableid
                          AND statusflg = 0
                          AND companyid = @companyid
                        ORDER BY orderid DESC
                        LIMIT 1";

                    await using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("tableid", tableId);
                        command.Parameters.AddWithValue("companyid", companyID);

                        await using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                orderid = reader["orderid"]?.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return orderid;
        }

        private async Task InsertOrderDetails(
            int companyid,
            int orderTableID,
            string orderID,
            List<Orderitems> items,
            string userid,
            string username)
        {
            foreach (var item in items)
            {
                for (int i = 0; i < item.quantity; i++)
                {
                    await InsertOrderDTL(orderID, item, orderTableID, username, userid, companyid);
                }
            }
        }

        private async Task InsertOrderDTL(
            string orderid,
            Orderitems item,
            int ordertable,
            string username,
            string userid,
            int companyid)
        {
            string itemname = "";

            // get item name
            await using (var connection = new NpgsqlConnection(ConnectionString.Value))
            {
                await connection.OpenAsync();

                string q = @"SELECT itemname FROM orderb_item WHERE itemid = @itemid";

                await using (var cmd = new NpgsqlCommand(q, connection))
                {
                    cmd.Parameters.AddWithValue("itemid", item.itemId);

                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            itemname = reader["itemname"]?.ToString();
                        }
                    }
                }
            }

            string unique = (DateTime.UtcNow.Ticks % 100000).ToString("D5");
            string orderDtlSeq = orderid + unique;

            string insert = @"
                INSERT INTO orderb_orderdtl
                (orderid, orderitemid, orderitemname, orderitemdescription,
                 payedflg, deletedflg, price, ordertable,
                 createduser, status, orderdtlitemisseq, createdate, companyid)
                VALUES
                (@orderid, @itemid, @itemname, @comments,
                 NULL, 0, @price, @ordertable,
                 @username, 1, @seq, now(), @companyid)";

            await using (var connection = new NpgsqlConnection(ConnectionString.Value))
            {
                await connection.OpenAsync();

                await using (var cmd = new NpgsqlCommand(insert, connection))
                {
                    cmd.Parameters.AddWithValue("orderid", orderid);
                    cmd.Parameters.AddWithValue("itemid", item.itemId);
                    cmd.Parameters.AddWithValue("itemname", itemname);
                    cmd.Parameters.AddWithValue("comments", item.comment ?? "");
                    cmd.Parameters.AddWithValue("price", item.price);
                    cmd.Parameters.AddWithValue("ordertable", ordertable);
                    cmd.Parameters.AddWithValue("username", username);
                    cmd.Parameters.AddWithValue("seq", orderDtlSeq);
                    cmd.Parameters.AddWithValue("companyid", companyid);

                    await cmd.ExecuteNonQueryAsync();
                }
            }

            await _hubContext.Clients
                .Group(companyid.ToString())
                .SendAsync("ReceiveOrdersInsert", new
                {
                    orderid,
                    item.itemId,
                    itemname,
                    item.comment,
                    item.price,
                    ordertable,
                    username,
                    orderDtlSeq
                });
        }
    }
}