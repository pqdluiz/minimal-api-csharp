using Microsoft.EntityFrameworkCore;
using MinimalApi.Models;

namespace MinimalApi.Database
{
    public class TodoDbContext : DbContext
    {
        public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TodoItem>().ToTable("TodoItems"); //u.IsTemporal()
            modelBuilder.Entity<User>().ToTable("Users"); //u.IsTemporal()
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                Password = "admin",
                Email = "admin@example.com",
                CreatedOn = DateTime.UtcNow
            });

            modelBuilder.Entity<TodoItem>().HasData(new TodoItem
            {
                Id = 1,
                Title = "Todo Item 1",
                IsCompleted = false,
                UserId = 1,
                CreatedOn = DateTime.UtcNow
            });
        }

        public DbSet<TodoItem> TodoItems => Set<TodoItem>();
        public DbSet<User> Users => Set<User>();
    }
}