using System.ComponentModel;

namespace PackageBank.Adapters;

/// <summary>
/// Buttons for Cheat Menu's Mod tests tab. Cheat Menu finds this class by its name, so neither mod needs the other.
/// Each method is one button: its DisplayName is the label and its Description the help, as "English\nChinese". What
/// it returns is shown in Cheat Menu's log.
/// </summary>
public static class DevCheats
{
    private static GameBank? _bank;

    internal static void Use(GameBank bank) => _bank = bank;

    [DisplayName("Fix the basement entrance and ladders\n修复地下室入口和楼梯")]
    [Description("Puts back a basement entrance, ladder or other fixture with no build spot that Pack up took in Package Bank 0.7.0, where the game puts it, and removes its package from the yard or the bank. Use it at home. On a save with nothing broken it changes nothing.\n把 Package Bank 0.7.0 打包掉的地下室入口、楼梯或其他没有建造位的家具放回游戏原本的位置，并移除院子或仓库里对应的包裹。请在家中使用。存档没有问题时不会做任何改动。")]
    public static string FixBasementEntranceAndLadders() => _bank?.RepairFixturesNow() ?? "Package Bank has not started yet";
}
