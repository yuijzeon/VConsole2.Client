namespace Babyduck.VConsole2.Client.Sample;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var vConsole = new VConsole2Client("127.0.0.1", 29000);
        vConsole.OnMessageReceived += message =>
        {
            var package = message.ParsePayload<Prnt>();
            if (package is Prnt prnt)
            {
                if (prnt.Rgba != 0)
                {
                    var r = (prnt.Rgba >> 24) & 0xFF;
                    var g = (prnt.Rgba >> 16) & 0xFF;
                    var b = (prnt.Rgba >> 8) & 0xFF;
                    Console.Write($"\e[38;2;{r};{g};{b}m");
                }

                Console.Write($"[{prnt.Timestamp}] {prnt.ChannelId} {prnt.Message}");
                if (prnt.Rgba != 0)
                {
                    Console.Write("\e[0m");
                }
            }
        };

        await vConsole.Connect();

        Console.WriteLine("Enter dota command (or 'exit'):");
        while (true)
        {
            await Task.Delay(500);
            var input = Console.ReadLine();

            if (string.IsNullOrEmpty(input))
            {
                continue;
            }

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            await vConsole.SendCommand(input);
        }
    }
}