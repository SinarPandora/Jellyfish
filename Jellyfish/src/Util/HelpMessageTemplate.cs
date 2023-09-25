using Jellyfish.Core.Command;

namespace Jellyfish.Util;

/// <summary>
///     Common template for help message
/// </summary>
public abstract class HelpMessageTemplate
{
    /// <summary>
    ///     Create help message for message command(already included help as option in the message)
    /// </summary>
    /// <param name="command">Message command object</param>
    /// <param name="description">Command description</param>
    /// <param name="options">Command options</param>
    /// <returns>Help message</returns>
    public static string ForMessageCommand(GuildMessageCommand command, string description, string options) =>
        $"""
         {command.Name()}
         ---
         > {string.Join("\n> ", description.Split("\n"))}
         ---

         指令名称：{string.Join(" 或 ", command.Keywords())}

         **选项：**
         0. 帮助：显示此消息
         {options}
         """;
}
