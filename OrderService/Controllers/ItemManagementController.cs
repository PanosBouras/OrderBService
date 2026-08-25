using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ItemManagementController : ControllerBase
    {
        // ---------- UPSERT ITEM WITH RECOMMENDATIONS ----------
        [HttpPost("upsert")]
        public async Task<IActionResult> UpsertItem([FromBody] UpsertItemRequest request)
        {
            // FORCE DEBUGGER BREAK
            System.Diagnostics.Debugger.Break();

            // DEBUG LOGGING
            Console.WriteLine($"=== UpsertItem Called ===");
            Console.WriteLine($"ItemId: {request?.ItemId ?? "NULL"}");
            Console.WriteLine($"Name: {request?.Name ?? "NULL"}");
            Console.WriteLine($"Category: {request?.Category ?? "NULL"}");
            Console.WriteLine($"Price: {request?.Price}");
            Console.WriteLine($"RecommendedSales Count: {request?.RecommendedSales?.Count ?? 0}");
            Console.WriteLine("========================\n");

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { success = false, message = "Name is required" });
                }

                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string itemId = request.ItemId ?? GenerateItemId();
                    Console.WriteLine($"Generated/Using ItemId: {itemId}");

                    // Determine type ID (1 = FOOD, 2 = DRINK)
                    int typeId = request.Type?.ToUpper() == "DRINK" ? 2 : 1;

                    // Delete existing item if it exists (to avoid conflicts)
                    string deleteItemQuery = "DELETE FROM orderb_item WHERE itemid = @itemId;";
                    await using (NpgsqlCommand deleteCmd = new NpgsqlCommand(deleteItemQuery, connection))
                    {
                        deleteCmd.Parameters.AddWithValue("itemId", itemId);
                        await deleteCmd.ExecuteNonQueryAsync();
                    }

                    // Insert the item
                    string insertItemQuery = @"
                        INSERT INTO orderb_item 
                        (itemid, itemname, itemcategoryid, itemtypeid, itemdescription, price, value, activeflg, companyid)
                        VALUES (@itemId, @name, @categoryId, @typeId, @description, @price, @value, 1, @companyid);";

                    await using (NpgsqlCommand command = new NpgsqlCommand(insertItemQuery, connection))
                    {
                        command.Parameters.AddWithValue("itemId", itemId);
                        command.Parameters.AddWithValue("name", request.Name);
                        command.Parameters.AddWithValue("categoryId", int.Parse(request.Category));
                        command.Parameters.AddWithValue("typeId", typeId);
                        command.Parameters.AddWithValue("description", request.Description ?? "");
                        command.Parameters.AddWithValue("price", request.Price);
                        command.Parameters.AddWithValue("value", request.Value ?? 0);
                        command.Parameters.AddWithValue("companyid", int.Parse(request.CompanyId ?? "1"));

                        await command.ExecuteNonQueryAsync();
                    }

                    // Delete existing recommendations and add new ones
                    await DeleteRecommendationsByItemId(connection, itemId);

                    if (request.RecommendedSales != null && request.RecommendedSales.Count > 0)
                    {
                        foreach (var rec in request.RecommendedSales)
                        {
                            string recId = rec.ItemRecommendationsId ?? GenerateRecommendationId();

                            string insertRecQuery = @"
                                INSERT INTO orderb_recommendations 
                                (itemrecommendationsid, itemid, categoryid, recommendationdecription, price, companyid)
                                VALUES (@recId, @itemId, @categoryId, @description, @price, @companyid);";

                            await using (NpgsqlCommand recCommand = new NpgsqlCommand(insertRecQuery, connection))
                            {
                                recCommand.Parameters.AddWithValue("recId", recId);
                                recCommand.Parameters.AddWithValue("itemId", itemId);
                                recCommand.Parameters.AddWithValue("categoryId", int.Parse(rec.CategoryId ?? request.Category));
                                recCommand.Parameters.AddWithValue("description", rec.RecommendationDescription ?? "");
                                recCommand.Parameters.AddWithValue("price", rec.Price ?? 0);
                                recCommand.Parameters.AddWithValue("companyid", int.Parse(rec.CompanyId ?? request.CompanyId ?? "1"));

                                await recCommand.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    return Ok(new { success = true, message = "Item saved successfully", itemId = itemId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error upserting item: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ---------- DELETE ITEM AND ITS RECOMMENDATIONS ----------
        [HttpDelete("delete/{itemId}")]
        public async Task<IActionResult> DeleteItem(string itemId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    return BadRequest(new { success = false, message = "ItemId is required" });
                }

                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    // Delete recommendations first
                    await DeleteRecommendationsByItemId(connection, itemId);

                    // Delete the item
                    string deleteItemQuery = "DELETE FROM orderb_item WHERE itemid = @itemId;";

                    await using (NpgsqlCommand command = new NpgsqlCommand(deleteItemQuery, connection))
                    {
                        command.Parameters.AddWithValue("itemId", itemId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return NotFound(new { success = false, message = "Item not found" });
                        }
                    }

                    return Ok(new { success = true, message = "Item and its recommendations deleted successfully" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting item: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ---------- DELETE RECOMMENDATION ----------
        [HttpDelete("recommendations/delete/{recommendationId}")]
        public async Task<IActionResult> DeleteRecommendation(string recommendationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recommendationId))
                {
                    return BadRequest(new { success = false, message = "RecommendationId is required" });
                }

                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string deleteQuery = "DELETE FROM orderb_recommendations WHERE itemrecommendationsid = @recId;";

                    await using (NpgsqlCommand command = new NpgsqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("recId", recommendationId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return NotFound(new { success = false, message = "Recommendation not found" });
                        }
                    }

                    return Ok(new { success = true, message = "Recommendation deleted successfully" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting recommendation: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ---------- HELPER: DELETE RECOMMENDATIONS BY ITEM ID ----------
        private async Task DeleteRecommendationsByItemId(NpgsqlConnection connection, string itemId)
        {
            string deleteRecQuery = "DELETE FROM orderb_recommendations WHERE itemid = @itemId;";

            await using (NpgsqlCommand recCommand = new NpgsqlCommand(deleteRecQuery, connection))
            {
                recCommand.Parameters.AddWithValue("itemId", itemId);
                await recCommand.ExecuteNonQueryAsync();
            }
        }

        // ---------- HELPER: GENERATE UNIQUE IDS ----------
        private string GenerateItemId()
        {
            // Get the highest item number from DB and increment
            // Format: F000001, F000002, etc. or Τ000001, Τ000002, etc.
            string prefix = "F"; // Default FOOD
            string randomSuffix = new Random().Next(100000, 999999).ToString();
            return $"{prefix}{randomSuffix}";
        }

        private string GenerateRecommendationId()
        {
            return $"REC_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        // ---------- REQUEST/RESPONSE MODELS ----------
        public class UpsertItemRequest
        {

            public string? ItemId { get; set; } // Optional - will be generated if null
            public string Name { get; set; }
            public string Category { get; set; }
            public string Type { get; set; } // FOOD or DRINK
            public string Description { get; set; }
            public decimal Price { get; set; }
            public decimal? Value { get; set; }
            public string CompanyId { get; set; } = "1"; // Default value
            public string Notes { get; set; }
            public List<RecommendationItem> RecommendedSales { get; set; } = new();
        }

        public class RecommendationItem
        {
            public string ItemId { get; set; }
            public string CategoryId { get; set; }
            public string ItemRecommendationsId { get; set; }
            public string RecommendationDescription { get; set; }
            public string CompanyId { get; set; }
            public decimal? Price { get; set; }
        }
    }
}