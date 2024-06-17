using Microsoft.EntityFrameworkCore;
using TzarGames.MatchFramework.Database.Server;

namespace DatabaseApp.DB
{
    public class GameDatabaseContext : BaseDatabaseContext
    {
        public DbSet<GameData> GameDatas { get; set; }
        public DbSet<Character> Characters { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Character>()
                .HasIndex(c => c.Name)
                .IsUnique();

            DeclareBlobColumnType<Character>(modelBuilder, c => c.AbilityData);
            DeclareBlobColumnType<Character>(modelBuilder, c => c.ItemData);
            DeclareBlobColumnType<Character>(modelBuilder, c => c.GameProgress);
        }
    }
}
