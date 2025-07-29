/*
TShock, a server mod for Terraria
Copyright (C) 2011-2022 Pryaxis & TShock Contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using CaiBotLiteMod.Services;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Timer = System.Timers.Timer;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace CaiBotLiteMod.Moudles;

/// <summary>
///     Bitflags used with the <see cref="Disable(string, DisableFlags)"></see> method
/// </summary>
[Flags]
public enum DisableFlags
{
    /// <summary>
    ///     Disable the player and leave no messages
    /// </summary>
    None,

    /// <summary>
    ///     Write the Disable message to the console
    /// </summary>
    WriteToConsole,

    /// <summary>
    ///     Write the Disable message to the log
    /// </summary>
    WriteToLog,

    /// <summary>
    ///     Equivalent to WriteToConsole | WriteToLog
    /// </summary>
    WriteToLogAndConsole
}

public class TSPlayer
{
    /// <summary>
    ///     This represents the server as a player.
    /// </summary>
    /// <summary>
    ///     This player represents all the players.
    /// </summary>
    public static readonly TSPlayer All = new ("All");

    // ReSharper disable once InconsistentNaming
    public static readonly string[] UUIDs = new string[256];


    private readonly Item EmptySentinelItem = new ();

    private readonly Player FakePlayer = null!;

    private int _respawnTimer;

    /// <summary>
    ///     Whether the player is accepting whispers from other users
    /// </summary>
    public bool AcceptingWhispers = true;

    /// <summary>
    ///     Represents the ID of the chest that the player is viewing.
    /// </summary>
    public int ActiveChest = -1;


    /// <summary>
    ///     A list of command callbacks indexed by the command they need to do.
    /// </summary>
    public Dictionary<string, Action<object>> AwaitingResponse;

    private string CacheIP = null!;

    /// <summary>
    ///     Players controls are inverted if using SSC
    /// </summary>
    public bool Confused = false;

    public string Country = "??";

    /// <summary>
    ///     Contains data stored by plugins
    /// </summary>
    protected ConcurrentDictionary<string, object> data = new ();

    /// <summary>
    ///     Whether the player should see logs.
    /// </summary>
    public bool DisplayLogs = true;
    

    /// <summary>
    ///     Whether the player has been nagged about logging in.
    /// </summary>
    public bool HasBeenNaggedAboutLoggingIn;

    /// <summary>
    ///     Whether the player is logged in or not.
    /// </summary>
    public bool IsLoggedIn;

    /// <summary>
    ///     Represents the current item the player is holding.
    /// </summary>
    public Item ItemInHand = new ();

    private DateTime LastDisableNotification = DateTime.UtcNow;

    /// <summary>
    ///     The last projectile type this player tried to kill.
    /// </summary>
    public int LastKilledProjectile = 0;

    /// <summary>
    ///     The player's last known position from PlayerUpdate packet.
    /// </summary>
    public Vector2 LastNetPosition = Vector2.Zero;


    /// <summary>
    ///     The last time the player was warned for build permissions.
    ///     In MS, defaults to 1 (so it will warn on the first attempt).
    /// </summary>
    public long lastPermissionWarning = 1;

    /// <summary>
    ///     The last time the player changed their team or pvp status.
    /// </summary>
    public DateTime LastPvPTeamChange;

    /// <summary>
    ///     The last player that the player whispered with (to or from).
    /// </summary>
    public TSPlayer LastWhisper = null!;

    /// <summary>
    ///     Whether the player has been harrassed about logging in due to server side inventory or forced login.
    /// </summary>
    public bool LoginHarassed = false;

    /// <summary>
    ///     The time in ms when the player has logged in.
    /// </summary>
    public long LoginMS;

    /// <summary>
    ///     Whether the player is muted or not.
    /// </summary>
    public bool mute;

    /// <summary>
    ///     A timer to keep track of whether or not the player has recently thrown an explosive
    /// </summary>
    public int RecentFuse = 0;

    public bool RequestedSection;

    /// <summary>
    ///     Easy check if a player has any of IsDisabledForSSC, IsDisabledForStackDetection, IsDisabledForBannedWearable,
    ///     or IsDisabledPendingTrashRemoval set. Or if they're not logged in and a login is required.
    /// </summary>
    /// <returns>
    ///     If any of the checks that warrant disabling are set on this player. If true, Disable() is repeatedly called on
    ///     them.
    /// </returns>
    /// <summary>
    ///     Whether the player needs to specify a password upon connection( either server or user account ).
    /// </summary>
    public bool RequiresPassword;

    /// <summary>
    ///     A system to delay Remembered Position Teleports a few seconds
    /// </summary>
    public int RPPending = 0;

    public bool SilentJoinInProgress;

    public bool SilentKickInProgress;

    public bool SscLogin = false;

    public int sX = -1;
    public int sY = -1;

    /// <summary>
    ///     Unused.
    /// </summary>
    public Vector2 TeleportCoords = new (-1, -1);

    /// <summary>
    ///     The player's temporary group.  This overrides the user's actual group.
    /// </summary>
    public Group tempGroup = null!;

    public Timer tempGroupTimer = null!;

    /// <summary>
    ///     Temp points for use in regions and other plugins.
    /// </summary>
    public Point[] TempPoints = new Point[2];

    /// <summary>
    ///     Whether other players can teleport to the player.
    /// </summary>
    public bool TPAllow = true;

    /// <summary>
    ///     Logs the player out of an account.
    /// </summary>
    /// <summary>
    ///     Initializes a new instance of the <see cref="TSPlayer" /> class.
    /// </summary>
    /// <param name="index">The player's index in the.</param>
    public TSPlayer(int index)
    {
        this.Index = index;
        this.AwaitingResponse = new Dictionary<string, Action<object>>();
        this.UUID = string.Empty;
        this.IsLoggedIn = false;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TSPlayer" /> class.
    /// </summary>
    /// <param name="playerName">The player's name.</param>
    protected TSPlayer(string playerName)
    {
        this.Index = -1;
        this.FakePlayer = new Player { name = playerName, whoAmI = -1 };
        this.AwaitingResponse = new Dictionary<string, Action<object>>();
    }

    /// <summary>
    ///     Used in preventing players from seeing the npc spawnrate permission error on join.
    /// </summary>
    internal bool HasReceivedNPCPermissionError { get; set; }

    /// <summary>
    ///     The amount of tiles that the player has killed in the last second.
    /// </summary>
    public int TileKillThreshold { get; set; }

    /// <summary>
    ///     The amount of tiles the player has placed in the last second.
    /// </summary>
    public int TilePlaceThreshold { get; set; }

    /// <summary>
    ///     The amount of liquid (in tiles) that the player has placed in the last second.
    /// </summary>
    public int TileLiquidThreshold { get; set; }

    /// <summary>
    ///     The amount of tiles that the player has painted in the last second.
    /// </summary>
    public int PaintThreshold { get; set; }

    /// <summary>
    ///     The number of projectiles created by the player in the last second.
    /// </summary>
    public int ProjectileThreshold { get; set; }

    /// <summary>
    ///     The number of HealOtherPlayer packets sent by the player in the last second.
    /// </summary>
    public int HealOtherThreshold { get; set; }

    /// <summary>
    ///     Whether to ignore packets that are SSC-relevant.
    /// </summary>
    public bool IgnoreSSCPackets { get; set; }

    /// <summary>
    ///     A queue of tiles destroyed by the player for reverting.
    /// </summary>
    /// <summary>
    ///     The player's group.
    /// </summary>

    public bool ReceivedInfo { get; set; }

    /// <summary>
    ///     The players index in the player array( Main.players[] ).
    /// </summary>
    public int Index { get; protected set; }

    /// <summary>
    ///     Whether the player is waiting to place/break a tile to set as a temp point.
    /// </summary>
    public int AwaitingTempPoint { get; set; }

    public bool AwaitingName { get; set; }

    public string[] AwaitingNameParameters { get; set; } = null!;

    /// <summary>
    ///     The last time a player broke a grief check.
    /// </summary>
    public DateTime LastThreat { get; set; }

    /// <summary>
    ///     The number of unsuccessful login attempts.
    /// </summary>
    public int LoginAttempts { get; set; }

    /// <summary>
    ///     UserAccount object associated with the player.
    ///     Set when the player logs in.
    /// </summary>
    /// <summary>
    ///     Whether the player performed a valid login attempt (i.e. entered valid user name and password) but is still blocked
    ///     from logging in because of SSI.
    /// </summary>
    public bool LoginFailsBySsi { get; set; }

    /// <summary>
    ///     Whether the player has sent their whole inventory to the server while connecting.
    /// </summary>
    public bool HasSentInventory { get; set; }

    /// <summary>
    ///     The player's respawn timer.
    /// </summary>
    public int RespawnTimer
    {
        get => this._respawnTimer;
        set => this.TPlayer.respawnTimer = (this._respawnTimer = value) * 60;
    }

    /// <summary>
    ///     Whether the player is dead or not.
    /// </summary>
    public bool Dead => this.TPlayer.dead;

    public string UUID
    {
        get => UUIDs[this.Index];
        set => UUIDs[this.Index] = value;
    }

    /// <summary>
    ///     The players difficulty( normal[softcore], mediumcore, hardcore ).
    /// </summary>
    public int Difficulty => this.TPlayer.difficulty;

    /// <summary>
    ///     Controls the journey godmode
    /// </summary>
    public bool GodMode
    {
        get =>
            CreativePowerManager.Instance.GetPower<CreativePowers.GodmodePower>().IsEnabledForPlayer(this.Index);
        set =>
            CreativePowerManager.Instance.GetPower<CreativePowers.GodmodePower>().SetEnabledState(this.Index, value);
    }

    /// <summary>
    ///     Whether the player is a real, human, player on the server.
    /// </summary>
    public bool RealPlayer => this.Index >= 0 && this.Index < Main.maxNetPlayers && Main.player[this.Index] != null;

    /// <summary>
    ///     Checks if the player is active and not pending termination.
    /// </summary>
    public bool ConnectionAlive =>
        this.RealPlayer && this.Client is { IsActive: true, PendingTermination: false };

    /// <summary>
    ///     Gets the item that the player is currently holding.
    /// </summary>
    public Item SelectedItem => this.TPlayer.inventory[this.TPlayer.selectedItem];

    /// <summary>
    ///     Gets the player's Client State.
    /// </summary>
    public int State
    {
        get => this.Client.State;
        set => this.Client.State = value;
    }


    /// <summary>
    ///     Gets the player's IP.
    /// </summary>
    public string IP
    {
        get
        {
            if (string.IsNullOrEmpty(this.CacheIP))
            {
                return this.CacheIP = (this.RealPlayer
                    ? this.Client.Socket.IsConnected()
                        ? this.Client.Socket.GetRemoteAddress().ToString()!.Split(':')[0]
                        : ""
                    : "127.0.0.1")!;
            }

            return this.CacheIP;
        }
    }

    /// <summary>
    ///     Gets the player's inventory (first 5 rows)
    /// </summary>
    public IEnumerable<Item> Inventory
    {
        get
        {
            for (var i = 0; i < 50; i++)
            {
                yield return this.TPlayer.inventory[i];
            }
        }
    }

    /// <summary>
    ///     Gets the player's accessories.
    /// </summary>
    public IEnumerable<Item> Accessories
    {
        get
        {
            for (var i = 3; i < 10; i++)
            {
                yield return this.TPlayer.armor[i];
            }
        }
    }

    /// <summary>
    ///     Saves the player's inventory to SSC
    /// </summary>
    /// <returns>bool - True/false if it saved successfully</returns>
    /// <summary>
    ///     Sends the players server side character to client
    /// </summary>
    /// <returns>bool - True/false if it saved successfully</returns>
    /// <summary>
    ///     Player RemoteClient.
    /// </summary>
    public RemoteClient Client => Netplay.Clients[this.Index];

    /// <summary>
    ///     Gets the Terraria Player object associated with the player.
    /// </summary>
    public Player TPlayer => this.FakePlayer ?? Main.player[this.Index];

    /// <summary>
    ///     Gets the player's name.
    /// </summary>
    public string Name => this.TPlayer.name;

    /// <summary>
    ///     Gets the player's active state.
    /// </summary>
    public bool Active => this.TPlayer != null && this.TPlayer.active;

    /// <summary>
    ///     Gets the player's team.
    /// </summary>
    public int Team => this.TPlayer.team;

    /// <summary>
    ///     Gets PvP player mode.
    /// </summary>
    public bool Hostile => this.TPlayer.hostile;

    /// <summary>
    ///     Gets the player's X coordinate.
    /// </summary>
    public float X => this.RealPlayer ? this.TPlayer.position.X : Main.spawnTileX * 16;

    /// <summary>
    ///     Gets the player's Y coordinate.
    /// </summary>
    public float Y => this.RealPlayer ? this.TPlayer.position.Y : Main.spawnTileY * 16;

    /// <summary>
    ///     Player X coordinate divided by 16. Supposed X world coordinate.
    /// </summary>
    public int TileX => (int) (this.X / 16);

    /// <summary>
    ///     Player Y coordinate divided by 16. Supposed Y world coordinate.
    /// </summary>
    public int TileY => (int) (this.Y / 16);

    /// <summary>
    ///     Checks if the player has any inventory slots available.
    /// </summary>
    public bool InventorySlotAvailable
    {
        get
        {
            var flag = false;
            if (this.RealPlayer)
            {
                for (var i = 0; i < 50; i++) //51 is trash can, 52-55 is coins, 56-59 is ammo
                {
                    if (this.TPlayer.inventory[i] == null || !this.TPlayer.inventory[i].active || this.TPlayer.inventory[i].Name == "")
                    {
                        flag = true;
                        break;
                    }
                }
            }

            return flag;
        }
    }

    /// <summary>
    ///     Finds a TSPlayer based on name or ID.
    ///     If the string comes with tsi: or tsn:, we'll only return a list with one element,
    ///     either the player with the matching ID or name, respectively.
    /// </summary>
    /// <param name="plr">Player name or ID</param>
    /// <returns>A list of matching players</returns>
    public static List<TSPlayer> FindByNameOrID(string search)
    {
        List<TSPlayer> found = [];

        search = search.Trim();

        // tsi: and tsn: are used to disambiguate between usernames and not
        // and are also both 3 characters to remove them from the search
        // (the goal was to pick prefixes unlikely to be used by names)
        // (and not to collide with other prefixes used by other commands)
        var exactIndexOnly = search.StartsWith("tsi:");
        var exactNameOnly = search.StartsWith("tsn:");

        if (exactNameOnly || exactIndexOnly)
        {
            search = search.Remove(0, 4);
        }

        // Avoid errors caused by null search
        if (search == null || search == "")
        {
            return found;
        }

        byte searchID;
        if (byte.TryParse(search, out searchID) && searchID < Main.maxPlayers)
        {
            var player = CaiBotLiteMod.Players[searchID];
            if (player != null && player.Active)
            {
                if (exactIndexOnly)
                {
                    return [player];
                }

                found.Add(player);
            }
        }

        var searchLower = search.ToLower();
        foreach (var player in CaiBotLiteMod.Players)
        {
            if (player != null)
            {
                if (search == player.Name && exactNameOnly)
                {
                    return [player];
                }

                if (player.Name.ToLower().StartsWith(searchLower))
                {
                    found.Add(player);
                }
            }
        }

        return found;
    }

    /// <summary>Determines if the player is disabled by the SSC subsystem for not being logged in.</summary>
    public bool IsBouncerThrottled()
    {
        return (DateTime.UtcNow - this.LastThreat).TotalMilliseconds < 5000;
    }

    /// <summary>Checks if a player is in range of a given tile if range checks are enabled.</summary>
    /// <param name="x"> The x coordinate of the tile.</param>
    /// <param name="y">The y coordinate of the tile.</param>
    /// <param name="range">The range to check for.</param>
    /// <returns>True if the player is in range of a tile or if range checks are off. False if not.</returns>
    public bool IsInRange(int x, int y, int range = 32)
    {
        var rgX = Math.Abs(this.TileX - x);
        var rgY = Math.Abs(this.TileY - y);
        if (rgX > range || rgY > range)
        {
            return false;
        }

        return true;
    }


    /// <summary>
    ///     Determines whether the player's storage contains the given key.
    /// </summary>
    /// <param name="key">Key to test.</param>
    /// <returns></returns>
    public bool ContainsData(string key)
    {
        return this.data.ContainsKey(key);
    }

    /// <summary>
    ///     Returns the stored object associated with the given key.
    /// </summary>
    /// <typeparam name="T">Type of the object being retrieved.</typeparam>
    /// <param name="key">Key with which to access the object.</param>
    /// <returns>The stored object, or default(T) if not found.</returns>
    public T GetData<T>(string key)
    {
        object obj;
        if (!this.data.TryGetValue(key, out obj!))
        {
            return default!;
        }

        return (T) obj;
    }

    /// <summary>
    ///     Stores an object on this player, accessible with the given key.
    /// </summary>
    /// <typeparam name="T">Type of the object being stored.</typeparam>
    /// <param name="key">Key with which to access the object.</param>
    /// <param name="value">Object to store.</param>
    public void SetData<T>(string key, T value)
    {
        if (!this.data.TryAdd(key, value!))
        {
            this.data.TryUpdate(key, value!, this.data[key]);
        }
    }

    /// <summary>
    ///     Removes the stored object associated with the given key.
    /// </summary>
    /// <param name="key">Key with which to access the object.</param>
    /// <returns>The removed object.	</returns>
    public object RemoveData(string key)
    {
        object rem;
        if (this.data.TryRemove(key, out rem!))
        {
            return rem;
        }

        return null!;
    }

    /// <summary>
    ///     Disconnects the player from the server.
    /// </summary>
    /// <param name="reason">The reason why the player was disconnected.</param>
    public virtual void Disconnect(string reason)
    {
        this.SendData(MessageID.Kick, reason);
    }

    /// <summary>
    ///     Fired when the player's temporary group access expires.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public void TempGroupTimerElapsed(object sender, ElapsedEventArgs args)
    {
        this.SendWarningMessage("Your temporary group access has expired.");

        this.tempGroup = null!;
        ((Timer) sender)?.Stop();
    }

    /// <summary>
    ///     Teleports the player to the given coordinates in the world.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="style">The teleportation style.</param>
    /// <returns>True or false.</returns>
    public bool Teleport(float x, float y, byte style = 1)
    {
        if (x > Main.rightWorld - 992)
        {
            x = Main.rightWorld - 992;
        }

        if (x < 992)
        {
            x = 992;
        }

        if (y > Main.bottomWorld - 992)
        {
            y = Main.bottomWorld - 992;
        }

        if (y < 992)
        {
            y = 992;
        }

        this.SendTileSquareCentered((int) (x / 16), (int) (y / 16), 15);
        this.TPlayer.Teleport(new Vector2(x, y), style);
        NetMessage.SendData(MessageID.TeleportEntity, -1, -1, NetworkText.Empty, 0, this.TPlayer.whoAmI, x, y, style);
        return true;
    }

    /// <summary>
    ///     Heals the player.
    /// </summary>
    /// <param name="health">Heal health amount.</param>
    public void Heal(int health = 600)
    {
        NetMessage.SendData(MessageID.PlayerHeal, -1, -1, NetworkText.Empty, this.TPlayer.whoAmI, health);
    }

    /// <summary>
    ///     Spawns the player at his spawn point.
    /// </summary>
    /// <summary>
    ///     Sends a tile square at a location with a given size.
    ///     Typically used to revert changes by Bouncer through sending the
    ///     "old" version of modified data back to a client.
    ///     Prevents desync issues.
    /// </summary>
    /// <param name="x">The x coordinate to send.</param>
    /// <param name="y">The y coordinate to send.</param>
    /// <param name="size">The size square set of tiles to send.</param>
    /// <returns>true if the tile square was sent successfully, else false</returns>
    [Obsolete(
        "This method may not send tiles the way you would expect it to. The (x,y) coordinates are the top left corner of the tile square, switch to " +
        nameof(SendTileSquareCentered) + " if you wish for the coordindates to be the center of the square.")]
    public virtual bool SendTileSquare(int x, int y, int size = 10)
    {
        return this.SendTileRect((short) x, (short) y, (byte) size, (byte) size);
    }

    /// <summary>
    ///     Sends a tile square at a center location with a given size.
    ///     Typically used to revert changes by Bouncer through sending the
    ///     "old" version of modified data back to a client.
    ///     Prevents desync issues.
    /// </summary>
    /// <param name="x">The x coordinates of the center of the square.</param>
    /// <param name="y">The y coordinates of the center of the square.</param>
    /// <param name="size">The size square set of tiles to send.</param>
    /// <returns>true if the tile square was sent successfully, else false</returns>
    public virtual bool SendTileSquareCentered(int x, int y, byte size = 10)
    {
        return this.SendTileRect((short) (x - (size / 2)), (short) (y - (size / 2)), size, size);
    }

    /// <summary>
    ///     Sends a rectangle of tiles at a location with the given length and width.
    /// </summary>
    /// <param name="x">The x coordinate the rectangle will begin at</param>
    /// <param name="y">The y coordinate the rectangle will begin at</param>
    /// <param name="width">The width of the rectangle</param>
    /// <param name="length">The length of the rectangle</param>
    /// <param name="changeType">Optional change type. Default None</param>
    /// <returns></returns>
    public virtual bool SendTileRect(short x, short y, byte width = 10, byte length = 10,
        TileChangeType changeType = TileChangeType.None)
    {
        try
        {
            NetMessage.SendTileSquare(this.Index, x, y, width, length, changeType);
            return true;
        }
        catch (Exception)
        {
            // ignored
        }

        return false;
    }

    /// <summary>
    ///     Changes the values of the <see cref="RemoteClient.TileSections" /> array.
    /// </summary>
    /// <param name="rectangle">
    ///     The area of the sections you want to set a value to.
    ///     The minimum size should be set to 200x150. If null, then the entire map is specified.
    /// </param>
    /// <param name="isLoaded">Is the section loaded.</param>
    // The server does not send the player the whole world, it sends it in sections. To do this, it sets up visible and invisible sections.
    // If the player was not in any section(Client.TileSections[x, y] == false) then the server will send the missing section of the world.
    // This method allows you to simulate what the player has or has not seen these sections.
    // For example, we can put some number of earths blocks in some vast area, for example, for the whole world, but the player will not see the changes, because some section is already loaded for him. At this point this method can come into effect! With it we will be able to select some zone and make it both visible and invisible to the player.
    // The server will assume that the zone is not loaded on the player, and will resend the data, but with earth blocks.
    public void UpdateSection(Rectangle? rectangle = null, bool isLoaded = false)
    {
        if (rectangle.HasValue)
        {
            for (var i = Netplay.GetSectionX(rectangle.Value.X);
                 i < Netplay.GetSectionX(rectangle.Value.X + rectangle.Value.Width) && i < Main.maxSectionsX;
                 i++)
            for (var j = Netplay.GetSectionY(rectangle.Value.Y);
                 j < Netplay.GetSectionY(rectangle.Value.Y + rectangle.Value.Height) && j < Main.maxSectionsY;
                 j++)
            {
                this.Client.TileSections[i, j] = isLoaded;
            }
        }
        else
        {
            for (var i = 0; i < Main.maxSectionsX; i++)
            for (var j = 0; j < Main.maxSectionsY; j++)
            {
                this.Client.TileSections[i, j] = isLoaded;
            }
        }
    }


    /// <summary>
    ///     Gives an item to the player.
    /// </summary>
    /// <param name="type">The item ID.</param>
    /// <param name="stack">The item stack.</param>
    /// <param name="prefix">The item prefix.</param>
    public virtual void GiveItem(int type, int stack, int prefix = 0)
    {
        this.GiveItemByDrop(type, stack, prefix);
    }

    private bool Depleted(Item item)
    {
        return item.type == ItemID.None || item.stack == 0;
    }


    private void SendItemSlotPacketFor(int slot)
    {
        var prefix = this.TPlayer.inventory[slot].prefix;
        NetMessage.SendData(MessageID.SyncEquipment, this.Index, -1, null, this.Index, slot, prefix);
    }


    private Item GiveItemDirectly_FillEmptyInventorySlot(Item item, int slot)
    {
        Item[]? inv = this.TPlayer.inventory;
        if (inv[slot].type != ItemID.None)
        {
            return item;
        }

        inv[slot] = item;
        this.SendItemSlotPacketFor(slot);
        return this.EmptySentinelItem;
    }

    private void GiveItemByDrop(int type, int stack, int prefix)
    {
        var itemIndex = Item.NewItem(new EntitySource_DebugCommand(null), (int) this.X, (int) this.Y, this.TPlayer.width, this.TPlayer.height,
            type, stack, true, prefix, true);
        Main.item[itemIndex].playerIndexTheItemIsReservedFor = this.Index;
        this.SendData(MessageID.SyncItem, "", itemIndex, 1);
        this.SendData(MessageID.ItemOwner, "", itemIndex);
    }

    /// <summary>
    ///     Sends an information message to the player.
    /// </summary>
    /// <param name="msg">The message.</param>
    public virtual void SendInfoMessage(string? msg)
    {
        this.SendMessage(msg, Color.Yellow);
    }

    /// <summary>
    ///     Sends an information message to the player.
    ///     Replaces format items in the message with the string representation of a specified object.
    /// </summary>
    /// <param name="format">The message.</param>
    /// <param name="args">An array of objects to format.</param>
    public void SendInfoMessage(string format, params object[] args)
    {
        this.SendInfoMessage(string.Format(format, args));
    }

    /// <summary>
    ///     Sends a success message to the player.
    /// </summary>
    /// <param name="msg">The message.</param>
    public virtual void SendSuccessMessage(string? msg)
    {
        this.SendMessage(msg, Color.LimeGreen);
    }

    /// <summary>
    ///     Sends a success message to the player.
    ///     Replaces format items in the message with the string representation of a specified object.
    /// </summary>
    /// <param name="format">The message.</param>
    /// <param name="args">An array of objects to format.</param>
    public void SendSuccessMessage(string format, params object[] args)
    {
        this.SendSuccessMessage(string.Format(format, args));
    }

    /// <summary>
    ///     Sends a warning message to the player.
    /// </summary>
    /// <param name="msg">The message.</param>
    public virtual void SendWarningMessage(string? msg)
    {
        this.SendMessage(msg, Color.OrangeRed);
    }

    /// <summary>
    ///     Sends a warning message to the player.
    ///     Replaces format items in the message with the string representation of a specified object.
    /// </summary>
    /// <param name="format">The message.</param>
    /// <param name="args">An array of objects to format.</param>
    public void SendWarningMessage(string format, params object[] args)
    {
        this.SendWarningMessage(string.Format(format, args));
    }

    /// <summary>
    ///     Sends an error message to the player.
    /// </summary>
    /// <param name="msg">The message.</param>
    public virtual void SendErrorMessage(string? msg)
    {
        this.SendMessage(msg, Color.Red);
    }

    /// <summary>
    ///     Sends an error message to the player.
    ///     Replaces format items in the message with the string representation of a specified object
    /// </summary>
    /// <param name="format">The message.</param>
    /// <param name="args">An array of objects to format.</param>
    public void SendErrorMessage(string format, params object[] args)
    {
        this.SendErrorMessage(string.Format(format, args));
    }

    /// <summary>
    ///     Sends a message with the specified color.
    /// </summary>
    /// <param name="msg">The message.</param>
    /// <param name="color">The message color.</param>
    public virtual void SendMessage(string? msg, Color color)
    {
        this.SendMessage(msg!, color.R, color.G, color.B);
    }

    /// <summary>
    ///     Sends a message with the specified RGB color.
    /// </summary>
    /// <param name="msg">The message.</param>
    /// <param name="red">The amount of red color to factor in. Max: 255.</param>
    /// <param name="green">The amount of green color to factor in. Max: 255</param>
    /// <param name="blue">The amount of blue color to factor in. Max: 255</param>
    public virtual void SendMessage(string msg, byte red, byte green, byte blue)
    {
        if (msg.Contains("\n"))
        {
            string?[] msgs = msg.Split('\n');
            foreach (var message in msgs)
            {
                this.SendMessage(message!, red, green, blue);
            }

            return;
        }

        if (this.Index == -1) //-1 is our broadcast index - this implies we're using TSPlayer.All.SendMessage and broadcasting to all clients
        {
            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(msg),
                new Microsoft.Xna.Framework.Color(red, green, blue));
        }
        else
        {
            ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(msg),
                new Microsoft.Xna.Framework.Color(red, green, blue), this.Index);
        }
    }

    /// <summary>
    ///     Sends a message to the player with the specified RGB color.
    /// </summary>
    /// <param name="msg">The message.</param>
    /// <param name="red">The amount of red color to factor in. Max: 255.</param>
    /// <param name="green">The amount of green color to factor in. Max: 255.</param>
    /// <param name="blue">The amount of blue color to factor in. Max: 255.</param>
    /// <param name="ply">The player who receives the message.</param>
    public virtual void SendMessageFromPlayer(string msg, byte red, byte green, byte blue, int ply)
    {
        if (msg.Contains("\n"))
        {
            var msgs = msg.Split('\n');
            foreach (var message in msgs)
            {
                this.SendMessageFromPlayer(message, red, green, blue, ply);
            }

            return;
        }

        ChatHelper.BroadcastChatMessageAs((byte) ply, NetworkText.FromLiteral(msg),
            new Microsoft.Xna.Framework.Color(red, green, blue));
    }


    /// <summary>
    ///     Wounds the player with the given damage.
    /// </summary>
    /// <param name="damage">The amount of damage the player will take.</param>
    public virtual void DamagePlayer(int damage)
    {
        this.DamagePlayer(damage, PlayerDeathReason.LegacyDefault());
    }

    /// <summary>
    ///     Wounds the player with the given damage.
    /// </summary>
    /// <param name="damage">The amount of damage the player will take.</param>
    /// <param name="reason">The reason for causing damage to player.</param>
    public virtual void DamagePlayer(int damage, PlayerDeathReason reason)
    {
        NetMessage.SendPlayerHurt(this.Index, new Player.HurtInfo { Damage = damage, DamageSource = reason });
    }

    /// <summary>
    ///     Kills the player.
    /// </summary>
    public virtual void KillPlayer()
    {
        this.KillPlayer(PlayerDeathReason.LegacyDefault());
    }

    /// <summary>
    ///     Kills the player.
    /// </summary>
    /// <param name="reason">Reason for killing a player.</param>
    public virtual void KillPlayer(PlayerDeathReason reason)
    {
        NetMessage.SendPlayerDeath(this.Index, reason, 99999, new Random().Next(-1, 1), false);
    }

    /// <summary>
    ///     Sets the player's team.
    /// </summary>
    /// <param name="team">The team color index.</param>
    public virtual void SetTeam(int team)
    {
        if (team < 0 || team >= Main.teamColor.Length)
        {
            throw new ArgumentException("The player's team is not in the range of available.");
        }

        Main.player[this.Index].team = team;
        NetMessage.SendData(MessageID.PlayerTeam, -1, -1, NetworkText.Empty, this.Index);
    }

    /// <summary>
    ///     Sets the player's pvp.
    /// </summary>
    /// <param name="mode">The state of the pvp mode.</param>
    /// <param name="withMsg">Whether a chat message about the change should be sent.</param>
    public virtual void SetPvP(bool mode, bool withMsg = false)
    {
        Main.player[this.Index].hostile = mode;
        NetMessage.SendData(MessageID.TogglePVP, -1, -1, NetworkText.Empty, this.Index);
        if (withMsg)
        {
            All.SendMessage(Language.GetTextValue(mode ? "LegacyMultiplayer.11" : "LegacyMultiplayer.12", this.Name),
                Main.teamColor[this.Team].R, Main.teamColor[this.Team].G, Main.teamColor[this.Team].B);
        }
    }


    /// <summary>
    ///     Sends the player an error message stating that more than one match was found
    ///     appending a csv list of the matches.
    /// </summary>
    /// <param name="matches">An enumerable list with the matches</param>
    public void SendMultipleMatchError(IEnumerable<object> matches)
    {
        this.SendErrorMessage("More than one match found -- unable to decide which is correct: ");

        var lines = PaginationTools.BuildLinesFromTerms(matches.ToArray());
        lines.ForEach(this.SendInfoMessage);

        this.SendErrorMessage("Use \"my query\" for items with spaces.");
        this.SendErrorMessage("Use tsi:[number] or tsn:[username] to distinguish between user IDs and usernames.");
    }


    /// <summary>
    ///     Annoys the player for a specified amount of time.
    /// </summary>
    /// <param name="time">The</param>
    public virtual void Whoopie(object time)
    {
        var time2 = (int) time;
        var launch = DateTime.UtcNow;
        var startname = this.Name;
        while ((DateTime.UtcNow - launch).TotalSeconds < time2 && startname == this.Name)
        {
            this.SendData(MessageID.MiscDataSync, number: this.Index, number2: 2f);
            Thread.Sleep(50);
        }
    }

    /// <summary>
    ///     Applies a buff to the player.
    /// </summary>
    /// <param name="type">The buff type.</param>
    /// <param name="time">The buff duration.</param>
    /// <param name="bypass"></param>
    public virtual void SetBuff(int type, int time = 3600, bool bypass = false)
    {
        if ((DateTime.UtcNow - this.LastThreat).TotalMilliseconds < 5000 && !bypass)
        {
            return;
        }

        this.SendData(MessageID.AddPlayerBuff, number: this.Index, number2: type, number3: time);
    }

    //Todo: Separate this into a few functions. SendTo, SendToAll, etc
    /// <summary>
    ///     Sends data to the player.
    /// </summary>
    /// <param name="msgType">The sent packet</param>
    /// <param name="text">The packet text.</param>
    /// <param name="number"></param>
    /// <param name="number2"></param>
    /// <param name="number3"></param>
    /// <param name="number4"></param>
    /// <param name="number5"></param>
    public virtual void SendData(byte msgType, string text = "", int number = 0, float number2 = 0f,
        float number3 = 0f, float number4 = 0f, int number5 = 0)
    {
        if (this.RealPlayer && !this.ConnectionAlive)
        {
            return;
        }

        NetMessage.SendData(msgType, this.Index, -1, text == null ? null : NetworkText.FromLiteral(text), number,
            number2, number3, number4, number5);
    }

    /// <summary>
    ///     Sends data from the given player.
    /// </summary>
    /// <param name="msgType">The sent packet.</param>
    /// <param name="ply">The packet sender.</param>
    /// <param name="text">The packet text.</param>
    /// <param name="number2"></param>
    /// <param name="number3"></param>
    /// <param name="number4"></param>
    /// <param name="number5"></param>
    public virtual void SendDataFromPlayer(byte msgType, int ply, string text = "", float number2 = 0f,
        float number3 = 0f, float number4 = 0f, int number5 = 0)
    {
        if (this.RealPlayer && !this.ConnectionAlive)
        {
            return;
        }

        NetMessage.SendData((int) msgType, this.Index, -1, NetworkText.FromFormattable(text), ply, number2, number3, number4,
            number5);
    }

    /// <summary>
    ///     Sends raw data to the player's socket object.
    /// </summary>
    /// <param name="data">The data to send.</param>
    public virtual void SendRawData(byte[] data)
    {
        if (!this.RealPlayer || !this.ConnectionAlive)
        {
            return;
        }

        this.Client.Socket.AsyncSend(data, 0, data.Length, this.Client.ServerWriteCallBack);
    }

    /// <summary>
    ///     Adds a command callback to a specified command string.
    /// </summary>
    /// <param name="name">The string representing the command i.e "yes" == /yes</param>
    /// <param name="callback">The method that will be executed on confirmation ie user accepts</param>
    public void AddResponse(string name, Action<object> callback)
    {
        if (this.AwaitingResponse.ContainsKey(name))
        {
            this.AwaitingResponse.Remove(name);
        }

        this.AwaitingResponse.Add(name, callback);
    }

    public void Kick(string reason)
    {
        NetMessage.SendData(MessageID.Kick, this.Index, -1, NetworkText.FromLiteral(reason));
    }

    private enum BuildPermissionFailPoint
    {
        GeneralBuild,
        SpawnProtect,
        Regions
    }
}

public class TSRestPlayer : TSPlayer
{
    internal List<string?> CommandOutput = [];

    public TSRestPlayer(string playerName, Group playerGroup) : base(playerName)
    {
        this.AwaitingResponse = new Dictionary<string, Action<object>>();
    }

    public override void SendMessage(string? msg, Color color)
    {
        this.SendMessage(msg, color.R, color.G, color.B);
    }

    public override void SendMessage(string? msg, byte red, byte green, byte blue)
    {
        this.CommandOutput.Add(msg);
    }

    public override void SendInfoMessage(string? msg)
    {
        this.SendMessage(msg, Color.Yellow);
    }

    public override void SendSuccessMessage(string? msg)
    {
        this.SendMessage(msg, Color.Green);
    }

    public override void SendWarningMessage(string? msg)
    {
        this.SendMessage(msg, Color.OrangeRed);
    }

    public override void SendErrorMessage(string? msg)
    {
        this.SendMessage(msg, Color.Red);
    }

    public List<string?> GetCommandOutput()
    {
        return this.CommandOutput;
    }
}