namespace MyBlog.Business.Email;

public interface INotificationService
{
    Task SendNotificationAsync(Message message);
}