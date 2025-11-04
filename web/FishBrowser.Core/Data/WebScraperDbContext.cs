using Microsoft.EntityFrameworkCore;
using FishBrowser.WPF.Models;
using System.IO;
using System;

namespace FishBrowser.WPF.Data;

public class WebScraperDbContext : DbContext
{
    public DbSet<ScrapingTask> ScrapingTasks { get; set; }
    public DbSet<Article> Articles { get; set; }
    public DbSet<FingerprintProfile> FingerprintProfiles { get; set; }
    public DbSet<ProxyServer> ProxyServers { get; set; }
    public DbSet<ProxyPool> ProxyPools { get; set; }
    public DbSet<ProxyHealthLog> ProxyHealthLogs { get; set; }
    public DbSet<ProxyUsageLog> ProxyUsageLogs { get; set; }
    public DbSet<AISummary> AISummaries { get; set; }
    public DbSet<AIClassification> AIClassifications { get; set; }
    public DbSet<LogEntry> LogEntries { get; set; }

    // Fingerprint Meta-DB
    public DbSet<TraitCategory> TraitCategories { get; set; }
    public DbSet<TraitDefinition> TraitDefinitions { get; set; }
    public DbSet<TraitOption> TraitOptions { get; set; }
    public DbSet<TraitGroupPreset> TraitGroupPresets { get; set; }
    public DbSet<FingerprintMetaProfile> FingerprintMetaProfiles { get; set; }
    public DbSet<CatalogVersion> CatalogVersions { get; set; }
    public DbSet<BrowserGroup> BrowserGroups { get; set; }
    public DbSet<BrowserEnvironment> BrowserEnvironments { get; set; }
    public DbSet<Font> Fonts { get; set; }
    public DbSet<GpuInfo> GpuInfos { get; set; }
    public DbSet<Models.ValidationRule> ValidationRules { get; set; }
    public DbSet<FingerprintValidationReport> FingerprintValidationReports { get; set; }

    // AI Provider
    public DbSet<AIProviderConfig> AIProviderConfigs { get; set; }
    public DbSet<AIApiKey> AIApiKeys { get; set; }
    public DbSet<AIProviderSettings> AIProviderSettings { get; set; }
    public DbSet<AIUsageLog> AIUsageLogs { get; set; }
    public DbSet<AIModelDefinition> AIModelDefinitions { get; set; }

    // Authentication
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    public WebScraperDbContext(DbContextOptions<WebScraperDbContext> options) : base(options)
    {
    }

    // 保留无参构造用于工具迁移（如 dotnet ef）
    public WebScraperDbContext()
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var baseDir = AppContext.BaseDirectory?.TrimEnd(Path.DirectorySeparatorChar) ?? Directory.GetCurrentDirectory();
            var dbPath = Path.Combine(baseDir, "webscraper.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ScrapingTask
        modelBuilder.Entity<ScrapingTask>()
            .HasOne(t => t.FingerprintProfile)
            .WithMany(f => f.ScrapingTasks)
            .HasForeignKey(t => t.FingerprintProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ScrapingTask>()
            .HasOne(t => t.ProxyServer)
            .WithMany(p => p.ScrapingTasks)
            .HasForeignKey(t => t.ProxyServerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ScrapingTask>()
            .HasOne(t => t.Article)
            .WithMany()
            .HasForeignKey(t => t.ArticleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Article
        modelBuilder.Entity<Article>()
            .HasOne(a => a.FingerprintProfile)
            .WithMany(f => f.Articles)
            .HasForeignKey(a => a.FingerprintProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Article>()
            .HasOne(a => a.ProxyServer)
            .WithMany(p => p.Articles)
            .HasForeignKey(a => a.ProxyServerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Article>()
            .HasOne(a => a.AISummary)
            .WithOne(s => s.Article)
            .HasForeignKey<AISummary>(s => s.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Article>()
            .HasOne(a => a.AIClassification)
            .WithOne(c => c.Article)
            .HasForeignKey<AIClassification>(c => c.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        // 索引
        modelBuilder.Entity<Article>().HasIndex(a => a.Url).IsUnique();
        modelBuilder.Entity<Article>().HasIndex(a => a.ScrapedAt);
        modelBuilder.Entity<ScrapingTask>().HasIndex(t => t.Status);
        modelBuilder.Entity<ProxyServer>().HasIndex(p => p.Status);
        modelBuilder.Entity<ProxyServer>().HasIndex(p => p.Score);
        modelBuilder.Entity<ProxyServer>().HasIndex(p => new { p.Country, p.Region });
        modelBuilder.Entity<ProxyServer>().HasIndex(p => p.PoolId);
        modelBuilder.Entity<LogEntry>().HasIndex(l => l.Timestamp);
        modelBuilder.Entity<FingerprintProfile>().HasIndex(p => p.MetaProfileId);
        modelBuilder.Entity<Font>().HasIndex(f => f.Name).IsUnique();
        modelBuilder.Entity<GpuInfo>().HasIndex(g => new { g.Vendor, g.Renderer });

        // ProxyPool
        modelBuilder.Entity<ProxyPool>()
            .HasMany(p => p.Proxies)
            .WithOne(s => s.Pool)
            .HasForeignKey(s => s.PoolId)
            .OnDelete(DeleteBehavior.SetNull);

        // ProxyHealthLog
        modelBuilder.Entity<ProxyHealthLog>()
            .HasOne(h => h.ProxyServer)
            .WithMany(s => s.HealthLogs)
            .HasForeignKey(h => h.ProxyServerId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProxyHealthLog>().HasIndex(h => new { h.ProxyServerId, h.Timestamp });

        // ProxyUsageLog
        modelBuilder.Entity<ProxyUsageLog>()
            .HasOne(u => u.ProxyServer)
            .WithMany(s => s.UsageLogs)
            .HasForeignKey(u => u.ProxyServerId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<ProxyUsageLog>()
            .HasOne(u => u.Environment)
            .WithMany()
            .HasForeignKey(u => u.EnvironmentId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<ProxyUsageLog>().HasIndex(u => new { u.ProxyServerId, u.EnvironmentId, u.StartedAt });

        // Meta-DB relationships & indexes
        modelBuilder.Entity<TraitCategory>()
            .HasMany(c => c.Traits)
            .WithOne(d => d.Category)
            .HasForeignKey(d => d.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TraitDefinition>()
            .HasIndex(d => d.Key)
            .IsUnique();

        modelBuilder.Entity<TraitOption>()
            .HasOne(o => o.TraitDefinition)
            .WithMany(d => d.Options)
            .HasForeignKey(o => o.TraitDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TraitGroupPreset>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<FingerprintMetaProfile>()
            .HasOne(m => m.BasePreset)
            .WithMany()
            .HasForeignKey(m => m.BasePresetId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CatalogVersion>()
            .HasIndex(c => c.Version)
            .IsUnique();

        // FingerprintProfile → FingerprintMetaProfile (optional)
        modelBuilder.Entity<FingerprintProfile>()
            .HasOne(p => p.MetaProfile)
            .WithMany()
            .HasForeignKey(p => p.MetaProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        // BrowserGroup → BrowserEnvironment (1:N)
        modelBuilder.Entity<BrowserGroup>()
            .HasMany(g => g.Environments)
            .WithOne(e => e.Group)
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        // BrowserEnvironment → FingerprintProfile/MetaProfile (optional)
        modelBuilder.Entity<BrowserEnvironment>()
            .HasOne(e => e.FingerprintProfile)
            .WithMany()
            .HasForeignKey(e => e.FingerprintProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<BrowserEnvironment>()
            .HasOne(e => e.MetaProfile)
            .WithMany()
            .HasForeignKey(e => e.MetaProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        // AI Provider relationships & indexes
        modelBuilder.Entity<AIProviderConfig>()
            .HasMany(p => p.ApiKeys)
            .WithOne(k => k.Provider)
            .HasForeignKey(k => k.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AIProviderConfig>()
            .HasOne(p => p.Settings)
            .WithOne(s => s.Provider)
            .HasForeignKey<AIProviderSettings>(s => s.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AIProviderConfig>()
            .HasIndex(p => p.Name);

        modelBuilder.Entity<AIProviderConfig>()
            .HasIndex(p => p.ProviderType);

        modelBuilder.Entity<AIApiKey>()
            .HasIndex(k => new { k.ProviderId, k.IsActive });

        modelBuilder.Entity<AIUsageLog>()
            .HasIndex(l => new { l.ProviderId, l.Timestamp });

        modelBuilder.Entity<AIUsageLog>()
            .HasIndex(l => l.ApiKeyId);

        modelBuilder.Entity<AIModelDefinition>()
            .HasKey(m => m.ModelId);

        modelBuilder.Entity<AIModelDefinition>()
            .HasIndex(m => m.ProviderType);

        // ValidationRule relationships
        modelBuilder.Entity<Models.ValidationRule>()
            .HasOne(r => r.BrowserGroup)
            .WithMany(g => g.ValidationRules)
            .HasForeignKey(r => r.BrowserGroupId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Models.ValidationRule>()
            .HasIndex(r => r.RuleType);

        // FingerprintProfile → BrowserGroup (optional)
        modelBuilder.Entity<FingerprintProfile>()
            .HasOne(p => p.Group)
            .WithMany()
            .HasForeignKey(p => p.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        // FingerprintProfile → FingerprintValidationReport (1:N)
        modelBuilder.Entity<FingerprintValidationReport>()
            .HasOne(r => r.FingerprintProfile)
            .WithMany(p => p.ValidationReports)
            .HasForeignKey(r => r.FingerprintProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // FingerprintProfile → LastValidationReport (1:1 optional)
        modelBuilder.Entity<FingerprintProfile>()
            .HasOne(p => p.LastValidationReport)
            .WithMany()
            .HasForeignKey(p => p.LastValidationReportId)
            .OnDelete(DeleteBehavior.SetNull);

        // FingerprintValidationReport indexes
        modelBuilder.Entity<FingerprintValidationReport>()
            .HasIndex(r => r.FingerprintProfileId);

        modelBuilder.Entity<FingerprintValidationReport>()
            .HasIndex(r => r.ValidatedAt);

        modelBuilder.Entity<FingerprintValidationReport>()
            .HasIndex(r => r.RiskLevel);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Role).HasDefaultValue("User");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
        });
    }
}
