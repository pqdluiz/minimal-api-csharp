using System.ComponentModel.DataAnnotations;

namespace MinimalApi.Models
{

    public record TodoItemInput(string? Title, bool IsCompleted);
    public record TodoItemOutput(string? Title, bool IsCompleted, DateTime? createdOn);

    public class TodoItemAudit
    {
        public string? Title { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    [GraphQLDescription("A todo item")]
    public class TodoItem
    {
        public int Id { get; set; }
        [Required]
        [GraphQLDescription("The title of the todo item")]
        public string? Title { get; set; }
        [GraphQLDescription("The completed status of the todo item")]
        public bool IsCompleted { get; set; }
        public User User { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}