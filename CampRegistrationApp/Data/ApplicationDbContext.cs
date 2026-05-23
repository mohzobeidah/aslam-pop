using Microsoft.EntityFrameworkCore;
using CampRegistrationApp.Models;

namespace CampRegistrationApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FamilyRegistration> FamilyRegistrations { get; set; } = null!;
        public DbSet<Person> Persons { get; set; } = null!;
        public DbSet<FamilyMember> FamilyMembers { get; set; } = null!;
        public DbSet<Attachment> Attachments { get; set; } = null!;
        public DbSet<Admin> Admins { get; set; } = null!;
        public DbSet<Sector> Sectors { get; set; } = null!;
        public DbSet<HealthStatus> HealthStatuses { get; set; } = null!;
        public DbSet<ChronicDisease> ChronicDiseases { get; set; } = null!;
        public DbSet<DisabilityType> DisabilityTypes { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectSectorQuota> ProjectSectorQuotas { get; set; } = null!;
        public DbSet<Nomination> Nominations { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<Desire> Desires { get; set; } = null!;
        public DbSet<FamilyDesire> FamilyDesires { get; set; } = null!;

        // New: Aid Management System
        public DbSet<Assistance> Assistances { get; set; } = null!;
        public DbSet<AssistanceBeneficiary> AssistanceBeneficiaries { get; set; } = null!;
        public DbSet<AssistanceImport> AssistanceImports { get; set; } = null!;

        // Complaint / Ticket System
        public DbSet<Complaint> Complaints { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── FamilyRegistration ──
            modelBuilder.Entity<FamilyRegistration>()
                .HasIndex(f => f.RecordId)
                .IsUnique();

            modelBuilder.Entity<FamilyRegistration>()
                .HasOne(f => f.FamilyHead)
                .WithMany()
                .HasForeignKey(f => f.FamilyHeadId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FamilyRegistration>()
                .HasOne(f => f.Sector)
                .WithMany()
                .HasForeignKey(f => f.SectorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FamilyRegistration>()
                .HasOne(f => f.ApprovedBy)
                .WithMany()
                .HasForeignKey(f => f.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FamilyRegistration>()
                .HasOne(f => f.DeletedBy)
                .WithMany()
                .HasForeignKey(f => f.DeletedById)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<FamilyRegistration>()
                .HasQueryFilter(f => !f.IsDeleted);

            // ── FamilyDesire ──
            modelBuilder.Entity<FamilyDesire>()
                .HasOne(fd => fd.FamilyRegistration)
                .WithMany(f => f.FamilyDesires)
                .HasForeignKey(fd => fd.FamilyRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FamilyDesire>()
                .HasOne(fd => fd.Desire)
                .WithMany()
                .HasForeignKey(fd => fd.DesireId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FamilyDesire>()
                .HasIndex(fd => new { fd.FamilyRegistrationId, fd.DesireId })
                .IsUnique();

            // ── Desire ──
            modelBuilder.Entity<Desire>()
                .HasIndex(d => d.Name)
                .IsUnique();

            // ── Person ──
            modelBuilder.Entity<Person>()
                .HasIndex(p => p.IdNumber)
                .IsUnique();

            // ── Admin ──
            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.NationalId)
                .IsUnique();

            modelBuilder.Entity<Admin>()
                .HasOne(a => a.Sector)
                .WithMany(s => s.Admins)
                .HasForeignKey(a => a.SectorId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Sector ──
            modelBuilder.Entity<Sector>()
                .HasIndex(s => s.Name)
                .IsUnique();

            // ── HealthStatus / ChronicDisease / DisabilityType ──
            modelBuilder.Entity<HealthStatus>()
                .HasIndex(h => h.Name)
                .IsUnique();

            modelBuilder.Entity<ChronicDisease>()
                .HasIndex(c => c.Name)
                .IsUnique();

            modelBuilder.Entity<DisabilityType>()
                .HasIndex(d => d.Name)
                .IsUnique();

            // ── Project ──
            modelBuilder.Entity<Project>()
                .HasOne(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Project>()
                .HasQueryFilter(p => !p.IsDeleted);

            // ── ProjectSectorQuota ──
            modelBuilder.Entity<ProjectSectorQuota>()
                .HasOne(q => q.Project)
                .WithMany()
                .HasForeignKey(q => q.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectSectorQuota>()
                .HasOne(q => q.Sector)
                .WithMany()
                .HasForeignKey(q => q.SectorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectSectorQuota>()
                .HasIndex(q => new { q.ProjectId, q.SectorId })
                .IsUnique();

            // ── Nomination ──
            modelBuilder.Entity<Nomination>()
                .HasOne(n => n.Project)
                .WithMany()
                .HasForeignKey(n => n.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Nomination>()
                .HasOne(n => n.Person)
                .WithMany()
                .HasForeignKey(n => n.PersonId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Nomination>()
                .HasOne(n => n.Sector)
                .WithMany()
                .HasForeignKey(n => n.SectorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Nomination>()
                .HasOne(n => n.Delegate)
                .WithMany()
                .HasForeignKey(n => n.DelegateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Nomination>()
                .HasOne(n => n.ApprovedBy)
                .WithMany()
                .HasForeignKey(n => n.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Nomination>()
                .HasQueryFilter(n => !n.IsDeleted);

            // ── AuditLog ──
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.CreatedAt);

            // ── Notification ──
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Admin)
                .WithMany()
                .HasForeignKey(n => n.AdminId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.AdminId, n.IsRead });

            // ══════════════════════════════════════════════
            //  New: Assistance / Beneficiary / Import
            // ══════════════════════════════════════════════

            // ── Assistance ──
            modelBuilder.Entity<Assistance>()
                .HasOne(a => a.Sector)
                .WithMany()
                .HasForeignKey(a => a.SectorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Assistance>()
                .HasOne(a => a.CreatedBy)
                .WithMany()
                .HasForeignKey(a => a.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Assistance>()
                .HasOne(a => a.ApprovedBy)
                .WithMany()
                .HasForeignKey(a => a.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Assistance>()
                .HasQueryFilter(a => !a.IsDeleted);

            // ── AssistanceBeneficiary ──
            modelBuilder.Entity<AssistanceBeneficiary>()
                .HasOne(b => b.Assistance)
                .WithMany(a => a.Beneficiaries)
                .HasForeignKey(b => b.AssistanceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssistanceBeneficiary>()
                .HasOne(b => b.Sector)
                .WithMany()
                .HasForeignKey(b => b.SectorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssistanceBeneficiary>()
                .HasOne(b => b.CreatedBy)
                .WithMany()
                .HasForeignKey(b => b.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssistanceBeneficiary>()
                .HasOne(b => b.Import)
                .WithMany(i => i.Beneficiaries)
                .HasForeignKey(b => b.ImportId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AssistanceBeneficiary>()
                .HasIndex(b => new { b.NationalId, b.AssistanceId })
                .IsUnique();

            modelBuilder.Entity<AssistanceBeneficiary>()
                .HasQueryFilter(b => !b.IsDeleted);

            // ── AssistanceImport ──
            modelBuilder.Entity<AssistanceImport>()
                .HasOne(i => i.ImportedBy)
                .WithMany()
                .HasForeignKey(i => i.ImportedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssistanceImport>()
                .HasOne(i => i.Sector)
                .WithMany()
                .HasForeignKey(i => i.SectorId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Complaint / Ticket System ──
            modelBuilder.Entity<Complaint>()
                .HasIndex(c => c.TicketId)
                .IsUnique();

            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.ResolvedBy)
                .WithMany()
                .HasForeignKey(c => c.ResolvedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Complaint>()
                .HasQueryFilter(c => !c.IsDeleted);

            base.OnModelCreating(modelBuilder);
        }
    }
}
