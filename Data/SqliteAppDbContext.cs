using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Data;

public class SqliteAppDbContext(DbContextOptions<SqliteAppDbContext> options) : AppDbContext(options)
{
}
