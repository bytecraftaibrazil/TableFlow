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