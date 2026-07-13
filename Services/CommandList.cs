using NetCord.Rest;

public static class CommandList
{
    public static List<ApplicationCommandProperties> Commands { get; set; } = new List<ApplicationCommandProperties>();
    public static void setCommands(List<ApplicationCommandProperties> commands)
    {
        Commands = commands;
    }
}