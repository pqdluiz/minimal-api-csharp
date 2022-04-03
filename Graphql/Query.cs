using HotChocolate.Subscriptions;
using MinimalApi.Database;
using MinimalApi.Models;

namespace MinimalApi.Graphql
{
    public class Query
    {
        [UseDbContext(typeof(TodoDbContext))]
        public IQueryable<TodoItem> GetTodoItems([ScopedService] TodoDbContext context) => context.TodoItems;

    }

}