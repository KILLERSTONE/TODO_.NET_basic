using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<iTaskServices>(new TaskServices());

var app = builder.Build();
app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

app.Use(async (context, next)
    =>
{
    Console.WriteLine($"[{context.Request.Method}{context.Request.Path}{DateTime.UtcNow}]Started.");
    await next(context);
    Console.WriteLine($"[{context.Request.Method}{context.Request.Path}{DateTime.UtcNow}]Ended.");

});

var todos = new List<Todo>();
//Creating a basic TODO without Database functionality

app.MapPost("/todos", (Todo task,iTaskServices services) =>
    {
        services.addTask(task);
        return TypedResults.Created("/todos/{id}", task);
    }
).AddEndpointFilter(async(context, next) =>
{
    var taskArgument = context.GetArgument<Todo>(0);
    var error = new Dictionary<string, string[]>();
    if (taskArgument.dueDate < DateTime.UtcNow)
    {
        error.Add(nameof(Todo.dueDate),["Cannot have due date in past"]);
    }

    if (taskArgument.isDone)
    {
        error.Add(nameof(Todo.isDone),["Cant add completed to todo"]);
    }

    if (error.Count > 0)
    {
        return Results.ValidationProblem(error);
    }

    return await next(context);
});

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id,iTaskServices services) =>
{
    var targetTodo = services.getTodoById(id);
    return targetTodo is null ? TypedResults.NotFound() : TypedResults.Ok(targetTodo);
});

app.MapGet("/todos", (iTaskServices services) => services.getTodos());
app.MapDelete("/todos/{id}", (int id,iTaskServices services) =>
{
    services.deleteTodoById(id);
    return TypedResults.NoContent();
});
app.Run();

public record Todo(int ID,string name,DateTime dueDate,bool isDone);

interface iTaskServices
{
    Todo? getTodoById(int id);
    List<Todo> getTodos();
    void deleteTodoById(int id);
    Todo addTask(Todo task);
}

class TaskServices : iTaskServices
{
    private readonly List<Todo> _todos = [];

    public Todo addTask(Todo task)
    {
        _todos.Add(task);
        return task;
    }

    public void deleteTodoById(int id)
    {
        int removedCount = _todos.RemoveAll(t => id == t.ID);
        Console.WriteLine($"Removed {removedCount} todo items");
    }

    public Todo? getTodoById(int id)
    {
        return _todos.FirstOrDefault(t => t.ID == id);
    }

    public List<Todo> getTodos()
    {
        return _todos;
    }
}