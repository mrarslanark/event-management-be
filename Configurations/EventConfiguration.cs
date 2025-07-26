using System.Text.Json;
using EventManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventManagement.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.Location)
            .HasMaxLength(255);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.BannerUrl)
            .HasMaxLength(2083);

        builder.Property(e => e.IsPublished)
            .HasDefaultValue(false);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EventType)
            .WithMany(et => et.Events)
            .HasForeignKey(e => e.EventTypeId)
            .IsRequired();

        builder.Property(e => e.Tags)
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions()) ?? new()
            );
    }
}