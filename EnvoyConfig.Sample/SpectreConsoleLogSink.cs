using EnvoyConfig.Logging;
using Spectre.Console;

namespace EnvoyConfig.Sample;

public class SpectreConsoleLogSink : IEnvLogSink
{
    public void Log(EnvLogLevel level, string message)
    {
        var color = level switch
        {
            EnvLogLevel.Error => "red",
            EnvLogLevel.Warning => "yellow",
            EnvLogLevel.Debug => "grey37",
            _ => "white",
        };
        var safeMessage = message.Replace("[", "[[").Replace("]", "]]");
        AnsiConsole.MarkupLine($"[{color}]{level}[/]: {safeMessage}");
    }
}
