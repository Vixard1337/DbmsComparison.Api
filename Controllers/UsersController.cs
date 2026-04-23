using System.Net.Mail;
using DbmsComparison.Api.Data;
using DbmsComparison.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DbmsComparison.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
    {
        if (!TryCreateContext(db, out var context, out var errorResult))
        {
            return errorResult!;
        }

        await using (context)
        {
            var users = await context.Users
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Email,
                    x.CreatedAt,
                    HasProfileImage = x.ProfileImage != null
                })
                .ToListAsync(cancellationToken);

            return Ok(users);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, [FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
    {
        if (!TryCreateContext(db, out var context, out var errorResult))
        {
            return errorResult!;
        }

        await using (context)
        {
            var user = await context.Users
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Email,
                    x.CreatedAt,
                    x.ProfileImage
                })
                .FirstOrDefaultAsync(cancellationToken);

            return user is null ? NotFound() : Ok(user);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertUserRequest request, [FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        if (!TryCreateContext(db, out var context, out var errorResult))
        {
            return errorResult!;
        }

        await using (context)
        {
            var user = new User
            {
                Name = request.Name.Trim(),
                Email = request.Email.Trim(),
                CreatedAt = DateTime.UtcNow,
                ProfileImage = request.ProfileImage
            };

            context.Users.Add(user);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Could not create user. Email may already exist." });
            }

            return CreatedAtAction(nameof(GetById), new { id = user.Id, db }, new
            {
                user.Id,
                user.Name,
                user.Email,
                user.CreatedAt,
                user.ProfileImage
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertUserRequest request, [FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        if (!TryCreateContext(db, out var context, out var errorResult))
        {
            return errorResult!;
        }

        await using (context)
        {
            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            user.Name = request.Name.Trim();
            user.Email = request.Email.Trim();
            user.ProfileImage = request.ProfileImage;

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Could not update user. Email may already exist." });
            }

            return Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                user.CreatedAt,
                user.ProfileImage
            });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] string db = "sqlserver", CancellationToken cancellationToken = default)
    {
        if (!TryCreateContext(db, out var context, out var errorResult))
        {
            return errorResult!;
        }

        await using (context)
        {
            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (user is null)
            {
                return NotFound();
            }

            context.Users.Remove(user);
            await context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }

    private bool TryCreateContext(string db, out AppDbContext? context, out IActionResult? errorResult)
    {
        context = null;
        errorResult = null;

        if (!DbContextOptionsFactory.TryParse(db, out var provider))
        {
            errorResult = BadRequest(new { message = "Unsupported db value. Use: sqlserver, postgres, mysql, sqlite." });
            return false;
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        DbContextOptionsFactory.ConfigureProvider(optionsBuilder, configuration, provider);
        context = new AppDbContext(optionsBuilder.Options);

        return true;
    }

    private static string? ValidateRequest(UpsertUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return "Name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "Email is required.";
        }

        try
        {
            _ = new MailAddress(request.Email.Trim());
        }
        catch
        {
            return "Email format is invalid.";
        }

        return null;
    }

    public sealed record UpsertUserRequest(string Name, string Email, byte[]? ProfileImage);
}
