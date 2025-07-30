using Microsoft.EntityFrameworkCore;
using TimeRecorderBACKEND.Models;

namespace TimeRecorderBACKEND.DataBaseContext
{
    public class WorkTimeDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<WorkLog> WorkLogs { get; set; }
        public DbSet<DayOffRequest> DayOffRequests { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<LastWorkOnDayMassageDate> LastWorkOnDayMassageDate { get; set; }
        public DbSet<Settings> Settings { get; set; }
        public DbSet<UserNotificationLog> UserNotificationLogs { get; set; }
        public WorkTimeDbContext(DbContextOptions<WorkTimeDbContext> options)
            : base(options)
        {
          
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Indeks na UserId w User (bo często będę pobierać użytkowników po Id)
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Id);
            // Indeks na UserId w WorkLog (bo często będę pobierać logi dla konkretnego użytkownika)
            modelBuilder.Entity<WorkLog>()
                .HasIndex(w => w.UserId); 
            // Indeks złożony na UserId i Status w DayOffRequest (bo często będę pobierać wnioski o urlop dla konkretnego użytkownika i statusu)
            modelBuilder.Entity<DayOffRequest>()
                .HasIndex(d => new { d.UserId, d.Status });
            // Indeks na Date w DayOffRequest (bo często będę pobierać wnioski o urlop dla konkretnej daty)
            modelBuilder.Entity<DayOffRequest>()
                .HasIndex(d => d.DateStart);
            // Indeks na Date w DayOffRequest (bo często będę pobierać wnioski o urlop dla konkretnej daty)
            modelBuilder.Entity<DayOffRequest>()
                .HasIndex(d => d.DateEnd);
            // Indeks na UserId w DayOffRequest (bo często będę pobierać wnioski o urlop dla konkretnego użytkownika)
            modelBuilder.Entity<DayOffRequest>()
                .HasIndex(d => d.UserId);
            modelBuilder.Entity<WorkLog>(entity =>
            {
                entity.Property(e => e.Duration)
                .HasComputedColumnSql("CASE WHEN EndTime IS NOT NULL THEN DATEDIFF(MINUTE, StartTime, EndTime) ELSE NULL END");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
