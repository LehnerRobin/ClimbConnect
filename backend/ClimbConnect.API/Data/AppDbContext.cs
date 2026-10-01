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

        // Kommentare und Reports hängen wahlweise an einem Gebiet oder einer Route.
        // Wird das Gebiet bzw. die Route gelöscht, sollen sie mitgelöscht werden –
        // sonst scheitert das Löschen am Fremdschlüssel.
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Area).WithMany(a => a.Comments)
            .HasForeignKey(c => c.AreaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Route).WithMany(r => r.Comments)
            .HasForeignKey(c => c.RouteId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Report>()
            .HasOne(r => r.Area).WithMany(a => a.Reports)
            .HasForeignKey(r => r.AreaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Report>()
            .HasOne(r => r.Route).WithMany(rt => rt.Reports)
            .HasForeignKey(r => r.RouteId).OnDelete(DeleteBehavior.Cascade);
    }
}
