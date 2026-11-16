using Npgsql;

namespace AssetFlow.Infrastructure.Persistence;

public static class DatabaseUrlParser
{
    public static string ToConnectionString(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return databaseUrl;
        }

        var credentials = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(credentials[0]),
            SslMode = SslMode.Require
        };

        if (credentials.Length == 2)
        {
            builder.Password = Uri.UnescapeDataString(credentials[1]);
        }

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        if (bool.TryParse(query["sslmode"], out var requireSsl) && !requireSsl)
        {
            builder.SslMode = SslMode.Disable;
        }
        else if (Enum.TryParse<SslMode>(query["sslmode"], true, out var sslMode))
        {
            builder.SslMode = sslMode;
        }

        return builder.ConnectionString;
    }
}
