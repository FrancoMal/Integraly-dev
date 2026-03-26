using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<TokenPack> TokenPacks => Set<TokenPack>();
    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<WeekAvailability> WeekAvailabilities => Set<WeekAvailability>();
    public DbSet<InstructorTask> InstructorTasks => Set<InstructorTask>();
    public DbSet<WebinarDate> WebinarDates => Set<WebinarDate>();
    public DbSet<WebinarContact> WebinarContacts => Set<WebinarContact>();
    public DbSet<WebinarRegistration> WebinarRegistrations => Set<WebinarRegistration>();
    public DbSet<PaymentPlan> PaymentPlans => Set<PaymentPlan>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<DailyChangeSummary> DailyChangeSummaries => Set<DailyChangeSummary>();
    public DbSet<CommitGroup> CommitGroups => Set<CommitGroup>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasOne(u => u.RoleNav)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasIndex(rp => new { rp.RoleId, rp.MenuKey }).IsUnique();
            entity.HasOne(rp => rp.Role)
                  .WithMany(r => r.Permissions)
                  .HasForeignKey(rp => rp.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppSetting>().HasKey(a => a.Key);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(a => new { a.EntityType, a.EntityId });
            entity.HasIndex(a => a.CreatedAt);
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasIndex(i => i.Token).IsUnique();
            entity.HasIndex(i => i.Email);
            entity.HasOne(i => i.Role)
                  .WithMany()
                  .HasForeignKey(i => i.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(i => i.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(i => i.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TokenPack>(entity =>
        {
            entity.HasIndex(tp => tp.UserId);
            entity.HasOne(tp => tp.User)
                  .WithMany()
                  .HasForeignKey(tp => tp.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(tp => tp.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(tp => tp.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Availability>(entity =>
        {
            entity.HasIndex(a => a.InstructorId);
            entity.HasIndex(a => new { a.InstructorId, a.DayOfWeek, a.StartHour }).IsUnique();
            entity.HasOne(a => a.Instructor)
                  .WithMany()
                  .HasForeignKey(a => a.InstructorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WeekAvailability>(entity =>
        {
            entity.HasIndex(w => new { w.InstructorId, w.Date, w.StartHour }).IsUnique();
            entity.HasOne(w => w.Instructor)
                  .WithMany()
                  .HasForeignKey(w => w.InstructorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasIndex(b => b.UserId);
            entity.HasIndex(b => b.InstructorId);
            entity.HasIndex(b => b.ScheduledDate);
            entity.HasOne(b => b.User)
                  .WithMany()
                  .HasForeignKey(b => b.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.Instructor)
                  .WithMany()
                  .HasForeignKey(b => b.InstructorId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(b => b.TokenPack)
                  .WithMany()
                  .HasForeignKey(b => b.TokenPackId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InstructorTask>(entity =>
        {
            entity.HasIndex(t => t.InstructorId);
            entity.HasIndex(t => t.TaskDate);
            entity.HasOne(t => t.Instructor)
                  .WithMany()
                  .HasForeignKey(t => t.InstructorId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.AssignedByUser)
                  .WithMany()
                  .HasForeignKey(t => t.AssignedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WebinarContact>(entity =>
        {
            entity.HasIndex(c => c.UUID).IsUnique();
            entity.HasOne(c => c.WebinarDate)
                  .WithMany()
                  .HasForeignKey(c => c.WebinarDateId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WebinarRegistration>(entity =>
        {
            entity.HasIndex(r => r.ContactId);
            entity.HasIndex(r => r.WebinarDateId);
            entity.HasOne(r => r.Contact)
                  .WithMany()
                  .HasForeignKey(r => r.ContactId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.WebinarDate)
                  .WithMany()
                  .HasForeignKey(r => r.WebinarDateId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentPlan>(entity =>
        {
            entity.HasIndex(p => p.Active);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(p => p.UserId);
            entity.HasIndex(p => p.Status);
            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(p => p.PaymentPlan)
                  .WithMany()
                  .HasForeignKey(p => p.PaymentPlanId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(p => p.TokenPack)
                  .WithMany()
                  .HasForeignKey(p => p.TokenPackId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DailyChangeSummary>(entity =>
        {
            entity.HasIndex(d => d.Date).IsUnique();
        });

        modelBuilder.Entity<CommitGroup>(entity =>
        {
            entity.HasIndex(c => c.DailySummaryId);
            entity.HasOne(c => c.DailySummary)
                  .WithMany(d => d.CommitGroups)
                  .HasForeignKey(c => c.DailySummaryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

    }
}
