using Microsoft.EntityFrameworkCore;
using TableFlow.Api.Entities;

namespace TableFlow.Api.Data;

public class TableFlowDbContext : DbContext
{
    public TableFlowDbContext(
        DbContextOptions<TableFlowDbContext> options
    ) : base(options)
    {
    }

    public DbSet<Restaurant> Restaurants
        => Set<Restaurant>();

    public DbSet<RestaurantTable> Tables
        => Set<RestaurantTable>();

    public DbSet<Reservation> Reservations
        => Set<Reservation>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder
    )
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.ToTable("Restaurants");

            entity.HasKey(restaurant => restaurant.Id);

            entity.Property(restaurant => restaurant.Name)
            .IsRequired()
            .HasMaxLength(120);

            entity.Property(restaurant => restaurant.CuisineType)
            .IsRequired()
            .HasMaxLength(80);

            entity.Property(restaurant => restaurant.City)
            .IsRequired()
            .HasMaxLength(80);
        });

        modelBuilder.Entity<RestaurantTable>(entity =>
        {
            entity.ToTable("Tables");

            entity.HasKey(table =>
                table.Id
            );

            entity.HasIndex(table => new
            {
                table.RestaurantId,
                table.Number
            })
            .IsUnique();
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("Reservations");

            entity.HasKey(reservation =>
                reservation.Id
            );

            entity.Property(reservation =>
                reservation.CustomerName
            )
            .IsRequired()
            .HasMaxLength(120);

            entity.Property(reservation =>
                reservation.ReservationDate
            )
            .HasColumnType("datetime2");

            entity.Property(reservation =>
                reservation.Status
            )
            .IsRequired()
            .HasMaxLength(30);
        });

        modelBuilder.Entity<RestaurantTable>()
            .HasOne(table =>
                table.Restaurant
            )
            .WithMany(restaurant =>
                restaurant.Tables
            )
            .HasForeignKey(table =>
                table.RestaurantId
            )
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reservation>()
            .HasOne(reservation =>
                reservation.Restaurant
            )
            .WithMany(restaurant =>
                restaurant.Reservations
            )
            .HasForeignKey(reservation =>
                reservation.RestaurantId
            )
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reservation>()
            .HasOne(reservation =>
                reservation.Table
            )
            .WithMany(table =>
                table.Reservations
            )
            .HasForeignKey(reservation =>
                reservation.TableId
            )
            .OnDelete(DeleteBehavior.Restrict);
    }
}