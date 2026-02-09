using Jellyfish.Core.Command;
using Kook;

namespace Jellyfish.Util;

/// <summary>
///     Common template for Help Message
/// </summary>
public static class HelpMessageHelper
{
    public const string HelpCommand = "帮助";

    /// <summary>
    ///     Create a help message for message command (already included help as option in the message)
    /// </summary>
    /// <param name="command">Message command object</param>
    /// <param name="description">Command description without title</param>
    /// <param name="options">Command options</param>
    /// <returns>Help message</returns>
    public static Card ForMessageCommand(
        GuildMessageCommand command,
        string description,
        string options
    ) =>
        new CardBuilder()
            .AddModule<HeaderModuleBuilder>(m => m.WithText(command.Name()))
            .AddModule<SectionModuleBuilder>(m => m.WithText(description, true))
            .AddModule<DividerModuleBuilder>()
            .AddModule<SectionModuleBuilder>(m =>
                m.WithText(
                    $"""
                    指令名称：{string.Join(" 或 ", command.Keywords())}
                    ---
                    **选项：**
                    {options}
                    """,
                    true
                )
            )
            .WithSize(CardSize.Large)
            .Build();
}
