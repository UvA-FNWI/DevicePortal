﻿// <auto-generated />
using System;
using DevicePortal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DevicePortal.Migrations
{
    [DbContext(typeof(PortalContext))]
    [Migration("20210110200534_AddSecurityRecommendation")]
    partial class AddSecurityRecommendation
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.1");

            modelBuilder.Entity("DevicePortal.Data.Device", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<string>("DeviceId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("OS_Type")
                        .HasColumnType("int");

                    b.Property<string>("OS_Version")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Origin")
                        .HasColumnType("int");

                    b.Property<string>("SerialNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<DateTime>("StatusEffectiveDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<string>("UserName")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("UserName");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("DevicePortal.Data.SecurityCheck", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int>("DeviceId")
                        .HasColumnType("int");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<DateTime>("StatusEffectiveDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("SubmissionDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("UserName")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime?>("ValidTill")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("DeviceId");

                    b.HasIndex("UserName");

                    b.ToTable("SecurityChecks");
                });

            modelBuilder.Entity("DevicePortal.Data.SecurityCheckQuestions", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<bool>("Answer")
                        .HasColumnType("bit");

                    b.Property<string>("Explanation")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Mask")
                        .HasColumnType("int");

                    b.Property<string>("Question")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("SecurityCheckId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("SecurityCheckId");

                    b.ToTable("SecurityCheckAnswers");
                });

            modelBuilder.Entity("DevicePortal.Data.SecurityQuestions", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int>("Mask")
                        .HasColumnType("int");

                    b.Property<string>("Text")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("SecurityQuestions");
                });

            modelBuilder.Entity("DevicePortal.Data.SecurityRecommendation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<string>("Content")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("OS_Type")
                        .HasColumnType("int");

                    b.Property<int>("Order")
                        .HasColumnType("int");

                    b.Property<int>("SecurityQuestionsId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("SecurityQuestionsId");

                    b.ToTable("SecurityRecommendations");
                });

            modelBuilder.Entity("DevicePortal.Data.User", b =>
                {
                    b.Property<string>("UserName")
                        .HasColumnType("nvarchar(450)");

                    b.Property<bool>("CanAdmin")
                        .HasColumnType("bit");

                    b.Property<bool>("CanApprove")
                        .HasColumnType("bit");

                    b.Property<bool>("CanSecure")
                        .HasColumnType("bit");

                    b.Property<string>("Department")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Faculty")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Institute")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserName");

                    b.HasIndex("Faculty");

                    b.HasIndex("Institute");

                    b.HasIndex("UserName");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("DevicePortal.Data.Device", b =>
                {
                    b.HasOne("DevicePortal.Data.User", "User")
                        .WithMany("Devices")
                        .HasForeignKey("UserName");

                    b.Navigation("User");
                });

            modelBuilder.Entity("DevicePortal.Data.SecurityCheck", b =>
                {
                    b.HasOne("DevicePortal.Data.Device", "Device")
                        .WithMany("SecurityChecks")
                        .HasForeignKey("DeviceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DevicePortal.Data.User", "User")
                        .WithMany("SecurityChecks")
                        .HasForeignKey("UserName");

                    b.Navigation("Device");

                    b.Navigation("User");
                });

            modelBuilder.Entity("DevicePortal.Data.SecurityCheckQuestions", b =>
                {
                    b.HasOne("DevicePortal.Data.SecurityCheck", "SecurityCheck")
                        .WithMany("Questions")
                        .HasForeignKey("SecurityCheckId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SecurityCheck");
                });

            modelBuilder.Entity("DevicePortal.Data.SecurityRecommendation", b =>
                {
                    b.HasOne("DevicePortal.Data.SecurityQuestions", "SecurityQuestion")
                        .WithMany("Recommendations")
                        .HasForeignKey("SecurityQuestionsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SecurityQuestion");
                });

            modelBuilder.Entity("DevicePortal.Data.Device", b =>
                {
                    b.Navigation("SecurityChecks");
                });

            modelBuilder.Entity("DevicePortal.Data.SecurityCheck", b =>
                {
                    b.Navigation("Questions");
                });

            modelBuilder.Entity("DevicePortal.Data.SecurityQuestions", b =>
                {
                    b.Navigation("Recommendations");
                });

            modelBuilder.Entity("DevicePortal.Data.User", b =>
                {
                    b.Navigation("Devices");

                    b.Navigation("SecurityChecks");
                });
#pragma warning restore 612, 618
        }
    }
}
