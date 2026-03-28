using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace ReCoreVery
{
    [ApiVersion(2, 1)]
    public class ReCoreVery : TerrariaPlugin
    {
        public override string Name => "ReCoreVery";
        public override string Author => "Assistant";
        public override string Description => "Gives players starter items after death in mediumcore/hardcore mode";
        public override Version Version => new Version(1, 0, 0);

        private Configuration Config;
        private HashSet<int> DeadPlayers;

        public ReCoreVery(Main game) : base(game)
        {
            DeadPlayers = new HashSet<int>();
        }

        public override void Initialize()
        {
            Config = Configuration.Load();
            
            ServerApi.Hooks.GameInitialize.Register(this, OnGameInitialize);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            
            GetDataHandlers.KillMe += OnKillMe;
            GeneralHooks.ReloadEvent += OnReload;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnGameInitialize);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
                
                GetDataHandlers.KillMe -= OnKillMe;
                GeneralHooks.ReloadEvent -= OnReload;
            }
            base.Dispose(disposing);
        }

        private void OnGameInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("recorevery.admin", CommandReload, "rcreload"));
            Commands.ChatCommands.Add(new Command("recorevery.admin", CommandGiveKit, "rcgive"));
            Commands.ChatCommands.Add(new Command("recorevery.use", CommandKits, "rckits"));
        }

        private void OnReload(ReloadEventArgs args)
        {
            Config = Configuration.Load();
            args.Player.SendSuccessMessage("[ReCoreVery] Configuration reloaded.");
        }

        private void CommandReload(CommandArgs args)
        {
            Config = Configuration.Load();
            args.Player.SendSuccessMessage("[ReCoreVery] Configuration reloaded.");
        }

        private void CommandGiveKit(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Usage: /rcgive <player> [kitname]");
                return;
            }

            string playerName = args.Parameters[0];
            var players = TSPlayer.FindByNameOrID(playerName);

            if (players.Count == 0)
            {
                args.Player.SendErrorMessage("Player not found.");
                return;
            }

            if (players.Count > 1)
            {
                args.Player.SendErrorMessage("Multiple players found. Be more specific.");
                return;
            }

            var target = players[0];
            SpawnKit kit = null;

            if (args.Parameters.Count >= 2)
            {
                string kitName = args.Parameters[1];
                kit = Config.SpawnKits.FirstOrDefault(k => k.Name.Equals(kitName, StringComparison.OrdinalIgnoreCase));
                if (kit == null)
                {
                    args.Player.SendErrorMessage($"Kit '{kitName}' not found.");
                    return;
                }
            }
            else
            {
                kit = Config.GetKitForPlayer(target);
            }

            if (kit == null)
            {
                args.Player.SendErrorMessage("No kit available for this player.");
                return;
            }

            GiveKit(target, kit);
            args.Player.SendSuccessMessage($"Gave kit '{kit.Name}' to {target.Name}.");
        }

        private void CommandKits(CommandArgs args)
        {
            var availableKits = Config.SpawnKits
                .Where(k => string.IsNullOrEmpty(k.Permission) || args.Player.HasPermission(k.Permission))
                .Select(k => k.Name)
                .ToList();

            if (availableKits.Count == 0)
            {
                args.Player.SendInfoMessage("No kits available for you.");
                return;
            }

            args.Player.SendInfoMessage($"Available kits: {string.Join(", ", availableKits)}");
        }

        private void OnKillMe(object sender, GetDataHandlers.KillMeEventArgs args)
        {
            if (!Config.Enabled)
                return;

            var player = TShock.Players[args.PlayerId];
            if (player == null || !player.Active)
                return;

            int difficulty = player.TPlayer.difficulty;
            
            bool shouldGiveKit = false;
            
            if (Config.MediumcoreOnly)
            {
                if (difficulty == 1)
                    shouldGiveKit = true;
                if (Config.HardcoreIncluded && difficulty == 2)
                    shouldGiveKit = true;
            }
            else
            {
                shouldGiveKit = true;
            }

            if (shouldGiveKit)
            {
                lock (DeadPlayers)
                {
                    DeadPlayers.Add(args.PlayerId);
                }
            }
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.MsgID != PacketTypes.PlayerSpawn)
                return;

            if (!Config.Enabled)
                return;

            int playerId = args.Msg.whoAmI;

            bool wasDead;
            lock (DeadPlayers)
            {
                wasDead = DeadPlayers.Remove(playerId);
            }

            if (!wasDead)
                return;

            var player = TShock.Players[playerId];
            if (player == null || !player.Active)
                return;

            var kit = Config.GetKitForPlayer(player);
            if (kit == null)
                return;

            if (Config.RespawnDelaySeconds > 0)
            {
                Task.Delay(Config.RespawnDelaySeconds * 1000).ContinueWith(t =>
                {
                    if (player != null && player.Active)
                    {
                        GiveKit(player, kit);
                    }
                });
            }
            else
            {
                Task.Delay(500).ContinueWith(t =>
                {
                    if (player != null && player.Active)
                    {
                        GiveKit(player, kit);
                    }
                });
            }
        }

        private void OnServerLeave(LeaveEventArgs args)
        {
            lock (DeadPlayers)
            {
                DeadPlayers.Remove(args.Who);
            }
        }

        private void GiveKit(TSPlayer player, SpawnKit kit)
        {
            foreach (var itemData in kit.Items)
            {
                int itemId = GetItemId(itemData.ItemNameOrId);
                if (itemId <= 0 || itemId >= ItemID.Count)
                    continue;

                int stack = Math.Max(1, itemData.Stack);
                byte prefix = itemData.Prefix;

                player.GiveItem(itemId, stack, prefix);
            }

            if (Config.AnnounceToPlayer && !string.IsNullOrEmpty(Config.AnnounceMessage))
            {
                player.SendSuccessMessage(Config.AnnounceMessage);
            }
        }

        private int GetItemId(string itemNameOrId)
        {
            if (int.TryParse(itemNameOrId, out int itemId))
            {
                return itemId;
            }

            var items = TShock.Utils.GetItemByIdOrName(itemNameOrId);
            if (items.Count == 1)
            {
                return items[0].netID;
            }

            return 0;
        }
    }
}