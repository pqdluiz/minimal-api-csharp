using MinimalApi.Models;

namespace MinimalApi.Graphql
{
    public class Subscription
    {
        [Subscribe]
        [Topic]
        public TodoItem OnCreateTodoItem([EventMessage] TodoItem todoItem)
        {
            return todoItem;
        }
    }
}