using BitMagnetRssImporter.Models;
using Microsoft.EntityFrameworkCore;

namespace BitMagnetRssImporter.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RssFeed> RssFeeds => Set<RssFeed>();
    public DbSet<RssSeenItem> RssSeenItems => Set<RssSeenItem>();
    public DbSet<RssFeedRun> RssFeedRuns => Set<RssFeedRun>();
    public DbSet<IngestionRun> IngestionRuns => Set<IngestionRun>();
    public DbSet<HtmlTracker> HtmlTrackers => Set<HtmlTracker>();
    public DbSet<HtmlSeenItem> HtmlSeenItems => Set<HtmlSeenItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RssFeed>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Url).IsRequired();
            b.Property(x => x.SourceName).IsRequired();
            b.HasIndex(x => x.Url).IsUnique(false);
            b.HasOne(x => x.LastRun)
                .WithMany()
                .HasForeignKey(x => x.LastRunId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RssSeenItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ItemKey).IsRequired();

            b.HasIndex(x => new { x.FeedId, x.ItemKey }).IsUnique(); // dedupe guarantee
            b.HasOne(x => x.Feed).WithMany().HasForeignKey(x => x.FeedId);
        });
        modelBuilder.Entity<RssFeedRun>(b =>
        {
            b.HasKey(x => x.Id);

            b.HasIndex(x => new { x.FeedId, x.StartedAt });

            b.Property(x => x.Error).HasMaxLength(2048);

            b.HasOne(x => x.Feed)
                .WithMany() // keep it simple: we don't need navigation collection
                .HasForeignKey(x => x.FeedId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<IngestionRun>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Phase).HasMaxLength(64);
            b.Property(x => x.Error).HasMaxLength(2048);

            b.HasIndex(x => new { x.SourceType, x.SourceId, x.StartedAt });
            b.HasIndex(x => new { x.SourceType, x.SourceId, x.IsActive });
        });

        modelBuilder.Entity<HtmlTracker>(b =>
        {
            b.HasKey(x => x.Id);

            b.Property(x => x.Name).HasMaxLength(256);
            b.Property(x => x.StartUrl).HasMaxLength(2048);
            b.Property(x => x.SourceName).HasMaxLength(128);

            b.Property(x => x.RowSelector).HasMaxLength(512);
            b.Property(x => x.DetailLinkSelector).HasMaxLength(512);
            b.Property(x => x.TitleSelector).HasMaxLength(512);
            b.Property(x => x.NextPageSelector).HasMaxLength(512);
            b.Property(x => x.InfoHashRegex).HasMaxLength(512);

            b.HasOne(x => x.LastRun)
                .WithMany()
                .HasForeignKey(x => x.LastRunId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<HtmlSeenItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ItemKey).HasMaxLength(512);
            b.Property(x => x.InfoHash).HasMaxLength(64);
            b.Property(x => x.Title).HasMaxLength(512);

            b.HasIndex(x => new { x.TrackerId, x.ItemKey }).IsUnique();
        });
    }
}