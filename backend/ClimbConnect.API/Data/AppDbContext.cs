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
    public DbSet<PinnwandPost>    PinnwandPosts    => Set<PinnwandPost>();
    public DbSet<ModerationCase>  ModerationCases  => Set<ModerationCase>();
    public DbSet<CasePost>        CasePosts        => Set<CasePost>();
    public DbSet<PostReport>      PostReports      => Set<PostReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Zusammengesetzter Primärschlüssel für die Subscribe-Tabelle
        modelBuilder.Entity<AppointmentUser>()
            .HasKey(au => new { au.AppointmentId, au.UserId });

        // Termine sind in UTC gespeichert. SQLite kennt keine Zeitzonen und liefert die Zeit ohne
        // Angabe zurück; so wird sie wieder als UTC markiert und im JSON mit "Z" ausgegeben.
        modelBuilder.Entity<Appointment>()
            .Property(a => a.Date)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

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

        // Pinnwand-Beiträge verschwinden mit dem Termin (#245, keine Kopie)
        modelBuilder.Entity<PinnwandPost>()
            .HasOne(p => p.Appointment).WithMany()
            .HasForeignKey(p => p.AppointmentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PinnwandPost>()
            .Property(p => p.CreatedAtUtc)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // Meldungen (#264): ein Fall pro Termin, ohne Fremdschlüssel auf Termin oder Beitrag –
        // die Kopien müssen das Löschen überleben. Innerhalb des Falls wird alles mitgelöscht.
        modelBuilder.Entity<ModerationCase>()
            .HasIndex(c => c.AppointmentId).IsUnique();
        modelBuilder.Entity<CasePost>()
            .HasOne(cp => cp.Case).WithMany(c => c.Posts)
            .HasForeignKey(cp => cp.CaseId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CasePost>()
            .HasIndex(cp => new { cp.CaseId, cp.OriginalPostId }).IsUnique();
        modelBuilder.Entity<CasePost>()
            .HasIndex(cp => cp.OriginalPostId);
        modelBuilder.Entity<PostReport>()
            .HasOne(r => r.CasePost).WithMany(cp => cp.Reports)
            .HasForeignKey(r => r.CasePostId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PostReport>()
            .HasIndex(r => new { r.CasePostId, r.ReporterId }).IsUnique();

        // Alle Zeitpunkte der Fälle in UTC (SQLite speichert ohne Zeitzone)
        modelBuilder.Entity<ModerationCase>().Property(c => c.AppointmentDate).HasConversion(v => v, v => AsUtc(v));
        modelBuilder.Entity<ModerationCase>().Property(c => c.CreatedAtUtc).HasConversion(v => v, v => AsUtc(v));
        modelBuilder.Entity<ModerationCase>().Property(c => c.LastReportAtUtc).HasConversion(v => v, v => AsUtc(v));
        modelBuilder.Entity<CasePost>().Property(cp => cp.CreatedAtUtc).HasConversion(v => v, v => AsUtc(v));
        modelBuilder.Entity<PostReport>().Property(r => r.CreatedAtUtc).HasConversion(v => v, v => AsUtc(v));
    }

    /// <summary>Markiert eine aus SQLite gelesene Zeit wieder als UTC (JSON dann mit "Z").</summary>
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
