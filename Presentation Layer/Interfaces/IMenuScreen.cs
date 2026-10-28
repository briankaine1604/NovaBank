namespace NovaBank
{
    /// <summary>The shell's own screens: the top-level menu and the way out of it.</summary>
    public interface IMenuScreen
    {
        void DisplayMainMenu();

        void DisplayInvalidOption();

        void DisplayLogout();
    }
}
