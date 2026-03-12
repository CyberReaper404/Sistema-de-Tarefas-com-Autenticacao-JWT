using System;
using backend_dotnet.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace backend_dotnet.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "8.0.3");

            modelBuilder.Entity("backend_dotnet.Models.RefreshToken", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("TEXT");

                b.Property<DateTime>("ExpiresAt")
                    .HasColumnType("TEXT");

                b.Property<DateTime?>("RevokedAt")
                    .HasColumnType("TEXT");

                b.Property<string>("TokenId")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.Property<int>("UserId")
                    .HasColumnType("INTEGER");

                b.HasKey("Id");

                b.HasIndex("TokenId")
                    .IsUnique();

                b.HasIndex("UserId");

                b.ToTable("RefreshTokens");
            });

            modelBuilder.Entity("backend_dotnet.Models.RevokedToken", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<DateTime>("RevokedAt")
                    .HasColumnType("TEXT");

                b.Property<string>("TokenId")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.Property<string>("TokenType")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.Property<int>("UserId")
                    .HasColumnType("INTEGER");

                b.HasKey("Id");

                b.HasIndex("TokenId")
                    .IsUnique();

                b.HasIndex("UserId");

                b.ToTable("RevokedTokens");
            });

            modelBuilder.Entity("backend_dotnet.Models.TodoTask", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("TEXT");

                b.Property<string>("Description")
                    .HasColumnType("TEXT");

                b.Property<bool>("IsCompleted")
                    .HasColumnType("INTEGER");

                b.Property<string>("Title")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.Property<DateTime>("UpdatedAt")
                    .HasColumnType("TEXT");

                b.Property<int>("UserId")
                    .HasColumnType("INTEGER");

                b.HasKey("Id");

                b.HasIndex("UserId");

                b.ToTable("Tasks");
            });

            modelBuilder.Entity("backend_dotnet.Models.User", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("TEXT");

                b.Property<string>("Email")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.Property<string>("PasswordHash")
                    .IsRequired()
                    .HasColumnType("TEXT");

                b.HasKey("Id");

                b.HasIndex("Email")
                    .IsUnique();

                b.ToTable("Users");
            });

            modelBuilder.Entity("backend_dotnet.Models.RefreshToken", b =>
            {
                b.HasOne("backend_dotnet.Models.User", "User")
                    .WithMany("RefreshTokens")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("User");
            });

            modelBuilder.Entity("backend_dotnet.Models.RevokedToken", b =>
            {
                b.HasOne("backend_dotnet.Models.User", "User")
                    .WithMany("RevokedTokens")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("User");
            });

            modelBuilder.Entity("backend_dotnet.Models.TodoTask", b =>
            {
                b.HasOne("backend_dotnet.Models.User", "User")
                    .WithMany("Tasks")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("User");
            });

            modelBuilder.Entity("backend_dotnet.Models.User", b =>
            {
                b.Navigation("RefreshTokens");
                b.Navigation("RevokedTokens");
                b.Navigation("Tasks");
            });
        }
    }
}
