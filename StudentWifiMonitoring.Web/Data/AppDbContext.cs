using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StudentWifiMonitoring.Web.Domain;

namespace StudentWifiMonitoring.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<EventLog> Events => Set<EventLog>();
    public DbSet<TestSession> TestSessions => Set<TestSession>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppSetting>().HasKey(s => s.Key);

        b.Entity<Student>().HasIndex(x => x.MacAddress).IsUnique();
        
        b.Entity<Connection>()
            .HasOne(c => c.Student)
            .WithMany()
            .HasForeignKey(c => c.StudentId);
        
        b.Entity<EventLog>()
            .HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId);
        
        b.Entity<EventLog>()
            .HasOne(e => e.TestSession)
            .WithMany()
            .HasForeignKey(e => e.TestSessionId);

        // UTC ValueConverter voor TestSession timestamps
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        b.Entity<TestSession>()
            .Property(e => e.StartTime)
            .HasConversion(utcConverter);

        b.Entity<TestSession>()
            .Property(e => e.EndTime)
            .HasConversion(utcConverter);
    }
}
