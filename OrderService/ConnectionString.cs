namespace OrderService
{
    public static class ConnectionString
    {
        public static string Value { get; set; } = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)));User Id=system;Password=@m2%9WFy;";

    }
}
