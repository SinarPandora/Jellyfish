using Jellyfish.Module.TeamPlay.Data;

namespace Jellyfish.Module.TeamPlay.Core;

public static class Args
{
    /// <summary>
    ///     Create room args
    /// </summary>
    /// <param name="Config">TeamPlay config</param>
    /// <param name="RawCommand">Raw text command</param>
    /// <param name="RoomName">(Optional)User input room name</param>
    /// <param name="RawMemberLimit">(Optional)User input member limit</param>
    /// <param name="Password">(Optional)User input room password</param>
    public record CreateRoomArgs(
        TpConfig Config,
        string RawCommand,
        string? RoomName = null,
        string? RawMemberLimit = null,
        string Password = ""
    );
}
