using Common.Protocol;
using Common.Protocol.Proto;
using FreakyProxy.Game;
using FreakyProxy.PacketProcessor;

namespace DeathSwap;

public class DeathSwapPlayer(Session session) : Player(session) {
    /// <summary>
    /// Swaps this player with the other player.
    /// </summary>
    /// <param name="other"></param>
    public async Task SwapWith(DeathSwapPlayer other) {
        #region Swap Token/Login

        var selfAccount = Plugin.Accounts[Session.AccountUid!];
        var otherAccount = Plugin.Accounts[other.Session.AccountUid!];

        var selfToken = Plugin.Tokens[selfAccount];
        var otherToken = Plugin.Tokens[otherAccount];

        var selfLogin = Plugin.Logins[selfAccount];
        var otherLogin = Plugin.Logins[otherAccount];

        Plugin.Tokens[selfAccount] = otherToken;
        Plugin.Tokens[otherAccount] = selfToken;

        Plugin.Logins[selfAccount] = otherLogin;
        Plugin.Logins[otherAccount] = selfLogin;

        #endregion

        #region Send Reconnect Packet

        var packet = new ClientReconnectNotify { Reason = ClientReconnectReason.ClientReconnnectQuitMp };

        await Session.SendClient(CmdID.ClientReconnectNotify, packet);
        await other.Session.SendClient(CmdID.ClientReconnectNotify, packet);

        #endregion
    }
}
