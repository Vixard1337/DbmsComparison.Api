using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Data;

public class PostgreSqlAppDbContext(DbContextOptions<PostgreSqlAppDbContext> options) : AppDbContext(options)
{
}
