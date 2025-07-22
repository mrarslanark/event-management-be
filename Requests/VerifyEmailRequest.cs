namespace EventManagement.Requests;

public class VerifyEmailRequest
{
    public string Token { get; set; } = string.Empty;
}