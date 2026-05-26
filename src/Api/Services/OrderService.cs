using Dapper;
using Npgsql;

namespace CloudNativeApp.Services;

// --- DTO定義 ---
public record CategoryDto(int Id, string Name);

public record CreateOrderRequest(
    string OrderNo,
    string CustomerName,
    int CategoryId,
    string ItemName,
    decimal Price,
    int Qty
);

public record OrderHistoryDto(
    string orderNo,
    DateTime orderDate,
    string customerName,
    string itemName,
    decimal price,
    int qty,
    decimal totalAmount,
    string categoryName
);

public record OrderSummary(
    decimal SubTotal,
    decimal TaxAmount,
    decimal TotalAmount,
    bool IsHighAmount
);

// --- Service実装 ---
public class OrderService
{
    private readonly string _connectionString;
    private readonly TaxService _taxService;

    public OrderService(IConfiguration config, TaxService taxService)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection が設定されていません。");
        _taxService = taxService;
    }

    public OrderSummary Calculate(decimal price, int qty)
    {
        var sub = price * qty;
        var tax = _taxService.Calculate(sub);
        var total = sub + tax;
        return new OrderSummary(sub, tax, total, total > 1_000_000);
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        const string sql = "SELECT CategoryId as Id, CategoryName as Name FROM M_Category WHERE DeleteFlg = 0";
        return await conn.QueryAsync<CategoryDto>(sql);
    }

    public async Task<IEnumerable<OrderHistoryDto>> GetOrdersAsync()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT 
                o.orderno, 
                o.orderdate, 
                o.customername, 
                o.itemname, 
                o.price, 
                o.qty, 
                o.totalamount, 
                c.categoryname
            FROM Orders o
            JOIN M_Category c ON o.categoryid = c.categoryid
            ORDER BY o.orderdate DESC";

        return await conn.QueryAsync<OrderHistoryDto>(sql);
    }

    public async Task<bool> RegisterOrderAsync(CreateOrderRequest req)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tran = await conn.BeginTransactionAsync();

        try
        {
            var summary = Calculate(req.Price, req.Qty);

            const string sqlOrder = @"
                INSERT INTO Orders (OrderNo, OrderDate, CustomerName, CategoryId, ItemName, Price, Qty, TotalAmount)
                VALUES (@OrderNo, @OrderDate, @CustomerName, @CategoryId, @ItemName, @Price, @Qty, @TotalAmount)";

            await conn.ExecuteAsync(sqlOrder, new {
                req.OrderNo,
                OrderDate = DateTime.Now,
                req.CustomerName,
                req.CategoryId,
                req.ItemName,
                req.Price,
                req.Qty,
                summary.TotalAmount
            }, tran);

            const string sqlStock = "UPDATE M_Stock SET CurrentStock = CurrentStock - @Qty WHERE ItemName = @ItemName";
            await conn.ExecuteAsync(sqlStock, new { req.Qty, req.ItemName }, tran);

            await tran.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
            Console.WriteLine($"Register Error: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteOrderAsync(string orderNo)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        using var tran = await conn.BeginTransactionAsync();

        try
        {
            const string sqlSelect = "SELECT itemname, qty FROM Orders WHERE orderno = @orderNo";
            var order = await conn.QuerySingleOrDefaultAsync<StockInfo>(sqlSelect, new { orderNo }, tran);

            if (order != null)
            {
                const string sqlUpdateStock = "UPDATE M_Stock SET CurrentStock = CurrentStock + @Qty WHERE ItemName = @ItemName";
                await conn.ExecuteAsync(sqlUpdateStock, new { Qty = order.Qty, ItemName = order.ItemName }, tran);

                const string sqlDelete = "DELETE FROM Orders WHERE orderno = @orderNo";
                await conn.ExecuteAsync(sqlDelete, new { orderNo }, tran);

                await tran.CommitAsync();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
            Console.WriteLine($"Delete Error: {ex.Message}");
            throw;
        }
    }

    private class StockInfo
    {
        public string ItemName { get; set; } = string.Empty;
        public int Qty { get; set; }
    }
}
