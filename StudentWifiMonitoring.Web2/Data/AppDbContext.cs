using Microsoft.EntityFrameworkCore;
using StudentWifiMonitoring.Web2.Domain;

namespace StudentWifiMonitoring.Web2.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Connection> Connections => Set<Connection>();
        public DbSet<EventLog> Events => Set<EventLog>();
        public DbSet<TestSession> TestSessions => Set<TestSession>();

        protected override void OnModelCreating(ModelBuilder b)
        {
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
        }
    }
}
