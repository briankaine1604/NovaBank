namespace NovaBank;

public class Admin
{
    public string Username { get; }
    private string Password { get; }

    public Admin(string username, string password)
    {
        Username = username;
        Password = password;
    }

    public bool Authenticate(string username, string password)
    {
        return Username == username && Password == password;
    }
}