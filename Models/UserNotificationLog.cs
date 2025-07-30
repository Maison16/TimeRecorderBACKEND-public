public class UserNotificationLog
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string NotificationType { get; set; } 
    public DateTime DateSent { get; set; }
}