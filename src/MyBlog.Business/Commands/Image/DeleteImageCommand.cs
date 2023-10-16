namespace MyBlog.Business.Commands;

public class DeleteImageCommand
{
    public DeleteImageCommand(Guid id)
    {
        this.Id = id;
    }

    public Guid Id { get; set; }
}