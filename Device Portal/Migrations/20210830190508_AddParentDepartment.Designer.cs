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
    [Migration("20210830190508_AddParentDepartment")]
    partial class AddParentDepartment
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.1");

            modelBuilder.Entity("DevicePortal.Data.Department", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int>("FacultyId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("ParentDepartmentId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("FacultyId");

                    b.HasIndex("ParentDepartmentId");

                    b.ToTable("Departments");
                });

            modelBuilder.Entity("DevicePortal.Data.Device", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int>("Category")
                        .HasColumnType("int");

                    b.Property<string>("CostCentre")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("DateEdit")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DateInSyncCdmb")
                        .HasColumnType("datetime2");

                    b.Property<int>("DepartmentId")
                        .HasColumnType("int");

                    b.Property<string>("DeviceId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("Disowned")
                        .HasColumnType("bit");

                    b.Property<string>("Ipv4")
                        .HasMaxLength(15)
                        .HasColumnType("nvarchar(15)");

                    b.Property<string>("Ipv6")
                        .HasMaxLength(45)
                        .HasColumnType("nvarchar(45)");

                    b.Property<string>("ItracsBuilding")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ItracsOutlet")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ItracsRoom")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("LabnetId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("LastSeenDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Macadres")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Notes")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("OS_Type")
                        .HasColumnType("int");

                    b.Property<string>("OS_Version")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Origin")
                        .HasColumnType("int");

                    b.Property<DateTime?>("PurchaseDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("SerialNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("Shared")
                        .HasColumnType("bit");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<DateTime>("StatusEffectiveDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<string>("UserEditId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserName")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("DepartmentId");

                    b.HasIndex("LabnetId");

                    b.HasIndex("UserName");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("DevicePortal.Data.DeviceHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int>("Category")
                        .HasColumnType("int");

                    b.Property<string>("CostCentre")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("DateEdit")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("DateHistory")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DateInSyncCdmb")
                        .HasColumnType("datetime2");

                    b.Property<int>("DepartmentId")
                        .HasColumnType("int");

                    b.Property<string>("DeviceId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("Disowned")
                        .HasColumnType("bit");

                    b.Property<string>("Ipv4")
                        .HasMaxLength(15)
                        .HasColumnType("nvarchar(15)");

                    b.Property<string>("Ipv6")
                        .HasMaxLength(45)
                        .HasColumnType("nvarchar(45)");

                    b.Property<string>("ItracsBuilding")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ItracsOutlet")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ItracsRoom")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("LabnetId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("LastSeenDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Macadres")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Notes")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("OS_Type")
                        .HasColumnType("int");

                    b.Property<string>("OS_Version")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Origin")
                        .HasColumnType("int");

                    b.Property<int>("OriginalDeviceId")
                        .HasColumnType("int");

                    b.Property<DateTime?>("PurchaseDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("SerialNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("Shared")
                        .HasColumnType("bit");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<DateTime>("StatusEffectiveDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<string>("UserEditId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserName")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("DepartmentId");

                    b.HasIndex("LabnetId");

                    b.HasIndex("OriginalDeviceId");

                    b.HasIndex("UserName");

                    b.ToTable("DeviceHistories");
                });

            modelBuilder.Entity("DevicePortal.Data.Faculty", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Faculties");
                });

            modelBuilder.Entity("DevicePortal.Data.Labnet", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.Property<int?>("DepartmentId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("DepartmentId");

                    b.ToTable("Labnets");
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

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("FacultyId")
                        .HasColumnType("int");

                    b.Property<bool>("Inactive")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ObjectId")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserName");

                    b.HasIndex("FacultyId");

                    b.HasIndex("UserName");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("DevicePortal.Data.User_Department", b =>
                {
                    b.Property<string>("UserName")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("DepartmentId")
                        .HasColumnType("int");

                    b.Property<bool>("CanManage")
                        .HasColumnType("bit");

                    b.HasKey("UserName", "DepartmentId");

                    b.HasIndex("DepartmentId");

                    b.ToTable("Users_Departments");
                });

            modelBuilder.Entity("DevicePortal.Data.Department", b =>
                {
                    b.HasOne("DevicePortal.Data.Faculty", "Faculty")
                        .WithMany()
                        .HasForeignKey("FacultyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DevicePortal.Data.Department", "ParentDepartment")
                        .WithMany()
                        .HasForeignKey("ParentDepartmentId");

                    b.Navigation("Faculty");

                    b.Navigation("ParentDepartment");
                });

            modelBuilder.Entity("DevicePortal.Data.Device", b =>
                {
                    b.HasOne("DevicePortal.Data.Department", "Department")
                        .WithMany("Devices")
                        .HasForeignKey("DepartmentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DevicePortal.Data.Labnet", "Labnet")
                        .WithMany("Devices")
                        .HasForeignKey("LabnetId");

                    b.HasOne("DevicePortal.Data.User", "User")
                        .WithMany("Devices")
                        .HasForeignKey("UserName");

                    b.Navigation("Department");

                    b.Navigation("Labnet");

                    b.Navigation("User");
                });

            modelBuilder.Entity("DevicePortal.Data.DeviceHistory", b =>
                {
                    b.HasOne("DevicePortal.Data.Department", "Department")
                        .WithMany()
                        .HasForeignKey("DepartmentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("DevicePortal.Data.Labnet", "Labnet")
                        .WithMany()
                        .HasForeignKey("LabnetId");

                    b.HasOne("DevicePortal.Data.Device", "OriginalDevice")
                        .WithMany("History")
                        .HasForeignKey("OriginalDeviceId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("DevicePortal.Data.User", "User")
                        .WithMany()
                        .HasForeignKey("UserName");

                    b.Navigation("Department");

                    b.Navigation("Labnet");

                    b.Navigation("OriginalDevice");

                    b.Navigation("User");
                });

            modelBuilder.Entity("DevicePortal.Data.Labnet", b =>
                {
                    b.HasOne("DevicePortal.Data.Department", "Department")
                        .WithMany("Labnets")
                        .HasForeignKey("DepartmentId");

                    b.Navigation("Department");
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

            modelBuilder.Entity("DevicePortal.Data.User", b =>
                {
                    b.HasOne("DevicePortal.Data.Faculty", "Faculty")
                        .WithMany("Users")
                        .HasForeignKey("FacultyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Faculty");
                });

            modelBuilder.Entity("DevicePortal.Data.User_Department", b =>
                {
                    b.HasOne("DevicePortal.Data.Department", "Department")
                        .WithMany("Users")
                        .HasForeignKey("DepartmentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("DevicePortal.Data.User", "User")
                        .WithMany("Departments")
                        .HasForeignKey("UserName")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Department");

                    b.Navigation("User");
                });

            modelBuilder.Entity("DevicePortal.Data.Department", b =>
                {
                    b.Navigation("Devices");

                    b.Navigation("Labnets");

                    b.Navigation("Users");
                });

            modelBuilder.Entity("DevicePortal.Data.Device", b =>
                {
                    b.Navigation("History");

                    b.Navigation("SecurityChecks");
                });

            modelBuilder.Entity("DevicePortal.Data.Faculty", b =>
                {
                    b.Navigation("Users");
                });

            modelBuilder.Entity("DevicePortal.Data.Labnet", b =>
                {
                    b.Navigation("Devices");
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
                    b.Navigation("Departments");

                    b.Navigation("Devices");

                    b.Navigation("SecurityChecks");
                });
#pragma warning restore 612, 618
        }
    }
}
