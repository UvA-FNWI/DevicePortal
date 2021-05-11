using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DevicePortal.Data
{
    public interface IEntity
    {
        public int Id { get; set; }
    }

    public class PortalContext : DbContext
    {
        public PortalContext(DbContextOptions<PortalContext> options) : base(options)
        {
            ChangeTracker.AutoDetectChangesEnabled = false;
            ChangeTracker.LazyLoadingEnabled = false;
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<User_Department> Users_Departments { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceHistory> DeviceHistories { get; set; }
        public DbSet<SecurityCheck> SecurityChecks { get; set; }
        public DbSet<SecurityCheckQuestions> SecurityCheckAnswers { get; set; }
        public DbSet<SecurityQuestions> SecurityQuestions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var device = modelBuilder.Entity<Device>();
            device.HasIndex(d => d.UserName);

            modelBuilder.Entity<DeviceHistory>()
                .HasOne(d => d.OriginalDevice)
                .WithMany(d => d.History)
                .HasForeignKey(d => d.OriginalDeviceId)
                .OnDelete(DeleteBehavior.NoAction);

            var securityCheck = modelBuilder.Entity<SecurityCheck>();
            securityCheck.HasIndex(c => c.UserName);

            var user = modelBuilder.Entity<User>();
            user.HasIndex(u => u.UserName);

            var user_department = modelBuilder.Entity<User_Department>();
            user_department.HasKey(ud => new { ud.UserName, ud.DepartmentId });
            user_department
                .HasOne(u => u.User)
                .WithMany(u => u.Departments)
                .HasForeignKey(ud => ud.UserName);
            user_department
                .HasOne(d => d.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(ud => ud.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            //var department = modelBuilder.Entity<Department>();
            //department
            //    .HasMany(d => d.Devices)
            //    .WithOne(d => d.Department)
            //    .OnDelete(DeleteBehavior.Restrict);
        }

        public void Seed() 
        {
            if (!Faculties.Any())
            { 
                Faculties.Add(new Faculty { Name = "FNWI" });
                SaveChanges();
            }

            if (!SecurityQuestions.Any()) 
            {
                List<SecurityQuestions> questions = new List<SecurityQuestions>();
                void AddQuestion(string text, DeviceType mask) 
                {
                    questions.Add(new Data.SecurityQuestions()
                    {
                        Text = text,
                        Mask = mask,
                    });
                }
                AddQuestion("Does the device have encrypted storage?", DeviceType.All);
                AddQuestion("Are local accounts only accessible with strong passwords?", DeviceType.All);
                AddQuestion("Does the device have a strong access code(minimum 6 characters) other than the SIM card PIN code?", DeviceType.Mobile | DeviceType.Tablet);
                AddQuestion("The OS and all applications are maintained by a supplier or community, and are up to date including security updates.", DeviceType.All);
                AddQuestion("Applicable anti malware and antivirus solutions are present, active and up to date.", DeviceType.All);
                AddQuestion("Local (application) firewall is active and alerts the user to unusual behaviour.", DeviceType.Desktop | DeviceType.Laptop);
                AddQuestion("Laptop or desktop should be automatically locked after a pre-set period of inactivity after a maximum of 15 minutes and phones or tablets after 5 minutes.", DeviceType.Desktop | DeviceType.Laptop);
                AddQuestion("Remote wipe, lock, or effective data protection measures to prevent loss of setting information in the event of theft should be in place.", DeviceType.All);
                SecurityQuestions.AddRange(questions);
                SaveChanges();
            }

            int facultyId = Faculties.First(f => f.Name == "FNWI").Id;
            var userIds = new string[] { User.ImporterId, User.ImportControllerId, User.IntuneServiceId };
            foreach (string id in userIds)
            {
                if (!Users.Any(u => u.UserName == id))
                {
                    Users.Add(new User()
                    {
                        UserName = id,
                        FacultyId = facultyId,
                        Email = "secure-science@uva.nl",
                    });
                    SaveChanges();
                }
            }
        }

        public void UpdateProperties<T>(T entity, params Expression<Func<T, object>>[] properties) where T : class, IEntity, new()
        {
            var dbEntityEntry = ChangeTracker.Entries<T>().FirstOrDefault(e => e.Entity.Id == entity.Id) ?? Entry<T>(entity);
            foreach (var p in properties)
            {
                dbEntityEntry.Property(p).IsModified = true;
            }
        }

        public void CreateOrUpdate<T>(T entity) where T : class, IEntity, new()
        {
            if (entity.Id > 0)
            {
                var dbEntityEntry = ChangeTracker.Entries<T>().FirstOrDefault(e => e.Entity.Id == entity.Id) ?? Entry<T>(entity);
                dbEntityEntry.State = EntityState.Modified;
            }
            else { Set<T>().Add(entity); }
        }
    }

    public static class DbSetExtensions
    {
        public static IQueryable<Device> Active(this DbSet<Device> set) 
        {
            return set.Where(d => d.Status != DeviceStatus.Disposed);
        }
        public static IQueryable<Device> Active(this IQueryable<Device> set)
        {
            return set.Where(d => d.Status != DeviceStatus.Disposed);
        }
    }
}
