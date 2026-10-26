using Jint;
using PackageBank.Core;

namespace PackageBank.Tests;

/// <summary>
/// page.js exactly as the plugin embeds it, run in Jint on top of FakePage.js (Root.html, the HUD, the build shop and
/// the vuplex bridge). The calls into the script are the ones WebPage makes, and what the script posts is parsed with
/// the real PageCommand, so a test sees the commands the plugin would act on.
/// </summary>
internal sealed class PageScript
{
    private readonly Engine _engine = new();

    public PageScript()
    {
        _engine.Execute(Resource("PackageBank.Tests.FakePage.js"));
        _engine.Execute(Resource("PackageBank.page.js"));
        Hear();
    }

    /// <summary>Every command the script sent the plugin, its ready message and diag notes aside.</summary>
    public List<PageCommand> Sent { get; } = new();

    public void Run(string statements)
    {
        _engine.Execute(statements);
        Hear();
    }

    public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    public bool Is(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    public int Count(string expression) => (int)_engine.Evaluate($"({expression}).length").AsNumber();

    public int Number(string expression) => (int)_engine.Evaluate($"Number({expression})").AsNumber();

    /// <summary>One run of the script's 400 ms page check.</summary>
    public void Poll() => Run("poll()");

    public void Click(string expression) => Run($"click({expression})");

    public void Live(bool isLive) => Call($"live({(isLive ? "true" : "false")})");

    public void Shop(string? viewJson) => Call($"shop({viewJson ?? "null"})");

    public void Open(string viewJson) => Call($"open({viewJson})");

    public void Status(string text) => Call($"status({JsText.Literal(text)})");

    private void Call(string expression) => Run("window.__slpkgbank." + expression);

    /// <summary>Takes what the script posted since the last look.</summary>
    private void Hear()
    {
        var count = (int)_engine.Evaluate("posted.length").AsNumber();
        var messages = Enumerable.Range(0, count).Select(i => _engine.Evaluate($"posted[{i}]").AsString()).ToList();

        _engine.Execute("posted.length = 0");

        foreach (var command in messages.Select(PageCommand.Parse))
        {
            if (command is not (PageCommand.Ready or PageCommand.Unknown)) Sent.Add(command);
        }
    }

    private static string Resource(string name)
    {
        using var stream = typeof(PageScript).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
