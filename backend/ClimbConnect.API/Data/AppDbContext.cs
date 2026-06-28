using Microsoft.EntityFrameworkCore;
using ClimbConnect.API.Models;
using Route = ClimbConnect.API.Models.Route;

namespace ClimbConnect.API.Data;

/// <summary>Datenbankkontext für die ClimbConnect-Anwendung.</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Area>            Areas            => Set<Area>();
    public DbSet<Sector>          Sectors          => Set<Sector>();
    public DbSet<Route>           Routes           => Set<Route>();
    public DbSet<User>            Users            => Set<User>();
    public DbSet<Progress>        Progresses       => Set<Progress>();
    public DbSet<Appointment>     Appointments     => Set<Appointment>();
    public DbSet<AppointmentUser> AppointmentUsers => Set<AppointmentUser>();
    public DbSet<Comment>         Comments         => Set<Comment>();
    public DbSet<Report>          Reports          => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Zusammengesetzter Primärschlüssel für die Subscribe-Tabelle
        modelBuilder.Entity<AppointmentUser>()
            .HasKey(au => new { au.AppointmentId, au.UserId });
    }
}
