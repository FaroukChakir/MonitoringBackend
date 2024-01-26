using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonitoringBackend.Models;
using System.Data;

namespace MonitoringBackend.Data;

public class ApiDbContext : IdentityDbContext
{
    public DbSet<RefreshToken> refreshTokens { get; set; }
    public DbSet<ServerMonitored> servers { get; set; }
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
    {

    }




}
