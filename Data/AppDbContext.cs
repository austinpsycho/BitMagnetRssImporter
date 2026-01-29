using BitMagnetRssImporter.Models;
using Microsoft.EntityFrameworkCore;

namespace BitMagnetRssImporter.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RssFeed> RssFeeds => Set<RssFeed>();
    public DbSet<RssSeenItem> RssSeenItems => Set<RssSeenItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RssFeed>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Url).IsRequired();
            b.Property(x => x.SourceName).IsRequired();
            b.HasIndex(x => x.Url).IsUnique(false);
        });

        modelBuilder.Entity<RssSeenItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ItemKey).IsRequired();

            b.HasIndex(x => new { x.FeedId, x.ItemKey }).IsUnique(); // dedupe guarantee
            b.HasOne(x => x.Feed).WithMany().HasForeignKey(x => x.FeedId);
        });
    }
}