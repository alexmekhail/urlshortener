namespace UrlShortener.Models;

public class User
{
    internal string hashedpass;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? PasswordSalt { get; set; }
    public List<string> Roles { get; set; } = [];
}