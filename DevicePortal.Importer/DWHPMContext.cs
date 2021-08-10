using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace DevicePortal.Importer
{
    public partial class DWHPMContext : DbContext
    {
        public DWHPMContext()
        {
        }

        public DWHPMContext(DbContextOptions<DWHPMContext> options)
            : base(options)
        {
        }

        public virtual DbSet<FnwiPortal> FnwiPortals { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=MSSQL-PRD-AO1.FORET.NL\\MSSQLNOAGPRD_1,1710;Database=DWHPM;Trusted_Connection=True;", options =>
                {
                    options.EnableRetryOnFailure();
                });
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<FnwiPortal>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("FNWI_portal");

                entity.Property(e => e.Aanschafdatum)
                    .HasColumnType("datetime")
                    .HasColumnName("aanschafdatum");

                entity.Property(e => e.Besturingssysteem)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("besturingssysteem")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.DatumLaatstGezien)
                    .HasColumnType("datetime")
                    .HasColumnName("datum_laatst_gezien");

                entity.Property(e => e.Email)
                    .HasMaxLength(100)
                    .HasColumnName("email")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.GeregistreerdDoorGebruiker)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("geregistreerd_door_gebruiker")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.ItracsGebouw)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("itracs_gebouw")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.ItracsOutlet)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("itracs_outlet")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.ItracsRuimte)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("itracs_ruimte")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.Klantorganisatie)
                    .HasMaxLength(100)
                    .HasColumnName("klantorganisatie")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.Kostenplaats)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("kostenplaats")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.LoginGebruiker)
                    .HasMaxLength(100)
                    .HasColumnName("login_gebruiker")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.Macadres)
                    .IsRequired()
                    .HasMaxLength(30)
                    .HasColumnName("macadres")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.Merk)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("merk")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.Naam)
                    .IsRequired()
                    .HasMaxLength(60)
                    .HasColumnName("naam")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.NotitiesKlant)
                    .HasColumnType("ntext")
                    .HasColumnName("notities_klant")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.Serienummer)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("serienummer")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.Soort)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("soort")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.Status)
                    .HasMaxLength(100)
                    .HasColumnName("status")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("type")
                    .UseCollation("Latin1_General_CI_AI_KS");

                entity.Property(e => e.Versleuteld).HasColumnName("versleuteld");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
