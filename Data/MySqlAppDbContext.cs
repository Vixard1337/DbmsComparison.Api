using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Data;

public class MySqlAppDbContext(DbContextOptions<MySqlAppDbContext> options) : AppDbContext(options)
{
}
