using ExiledToolkit.Web;
using Jint;
using PackageBank.Core;

namespace PackageBank.Tests;

/// <summary>
/// page.js exactly as the plugin embeds it, run in Jint with Exiled's Toolkit's page API (api.js, from the Toolkit's
/// source) on top of FakePage.js (Root.html, the HUD, the build shop and the vuplex bridge). The calls into the script
/// are the ones BankPage makes, and what the script posts is parsed with the real PageCommand, so a test sees the
/// commands the plugin would act on.
/// </summary>
internal sealed class PageScript
{
    private readonly Engine _engine = new();

    /// <param name="isToolkitFirst">
    /// The game runs the Toolkit's script first, or with false Package Bank's first; then the Toolkit's runs only at
    /// <see cref="LoadToolkit"/>.
    /// </param>
    public PageScript(bool isToolkitFirst = true)
    {
        _engine.Execute(Resource("PackageBank.Tests.FakePage.js"));

        if (isToolkitFirst) _engine.Execute(Resource("ExiledToolkit.api.js"));

        _engine.Execute(Resource("PackageBank.page.js"));
        Hear();
    }

    /// <summary>Every command the script sent the plugin, its ready message and diag notes aside.</summary>
    public List<PageCommand> Sent { get; } = new();

    /// <summary>Whether the script told the plugin it is ready, which has the plugin send it everything again.</summary>
    public bool HasSaidReady { get; private set; }

    /// <summary>The Toolkit's page API arriving after Package Bank's script.</summary>
    public void LoadToolkit() => Run(Resource("ExiledToolkit.api.js"));

    public void Run(string statements)
    {
        _engine.Execute(statements);
        Hear();
    }

    public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    public bool Is(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    public int Count(string expression) => (int)_engine.Evaluate($"({expression}).length").AsNumber();

    public int Number(string expression) => (int)_engine.Evaluate($"Number({expression})").AsNumber();

    /// <summary>One run of the Toolkit's 250 ms page check, which hands the HUD, the shop and the click menu to the script.</summary>
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
            if (command is PageCommand.Ready) HasSaidReady = true;
            else if (command is not PageCommand.Unknown) Sent.Add(command);
        }
    }

    private static string Resource(string name)
    {
        using var stream = typeof(PageScript).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
