using System;
using System.IO;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Configuration;
using TShockAPI.DB;

namespace Vanillafier
{
    [ApiVersion(2, 1)]
    public class Vanillafier : TerrariaPlugin
    {
        const string ConfirmationCommand = "vanillafy-confirm";
        const string GroupName = "Vanillafied";
        const string DefualtGuestGroupName = "guest";
        const string DefaultRegistrationGroupName = "default";
        static string TShockConfigPath { get { return Path.Combine(TShock.SavePath, "config.json"); } }

        public override string Author => "Pryaxis";
        public override string Description => "Instantly configure your server for vanilla gameplay";
        public override string Name => "Vanillafier";

        public override Version Version => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        public Vanillafier(Main game) : base(game) { }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command(Permissions.managegroup, vanillafy, "vanillafy"));
        }

        bool checkState()
        {
            bool hasGroup = TShock.Groups.GroupExists(GroupName);
            bool isDefualt = Group.DefaultGroup.Name == GroupName;

            TShockConfig tsConfig = TShock.Config;
            bool isDefaultOfConfig = tsConfig.Settings.DefaultGuestGroupName == GroupName && tsConfig.Settings.DefaultRegistrationGroupName == GroupName;

            return hasGroup && isDefualt && isDefaultOfConfig;
        }

        void printVanillafyState(TSPlayer player)
        {
            string stateString = checkState() ? "Open" : "Closed";
            player.SendInfoMessage($"Vanillafy State: {stateString}");
        }

        void vanillafy(CommandArgs args)
        {
            var player = args.Player;
            if (player == null)
            {
                return;
            }

            if (args.Parameters.Count <= 0)
            {
                printHelp(player);
                return;
            }

            switch (args.Parameters[0]) {
                case "on":
                    if (checkState())
                    {
                        player.SendErrorMessage("The vanillafy command has already been run.");
                        player.SendWarningMessage("If you proceed with running this command, your existing vanilla configuration will be reset.");
                    }
                    else
                    {
                        player.SendWarningMessage("This command will disable most cheat and grief protection for all non-superadmin users.");
                    }

                    player.SendWarningMessage($"Are you sure you wish to run this command? Use {TShock.Config.Settings.CommandSpecifier}{ConfirmationCommand} to complete configuration.");
                    player.AwaitingResponse.Add(ConfirmationCommand, trunOnConfirm);
                    break;
                case "off":
                    if (checkState())
                    {
                        player.SendWarningMessage("Vanilla gameplay will be closed soon.");
                        player.SendWarningMessage($"Are you sure you wish to run this command? Use {TShock.Config.Settings.CommandSpecifier}{ConfirmationCommand} to complete configuration.");
                        player.AwaitingResponse.Add(ConfirmationCommand, trunOffConfirm);
                    }
                    else
                    {
                        player.SendErrorMessage("Server is not yet open vanilla gameplay.");
                    }
                    break;
                case "state":
                    printVanillafyState(player);
                    break;
                case "help":
                default:
                    printHelp(player);
                    break;
            }
        }

        void printHelp(TSPlayer player)
        {
            player.SendInfoMessage("/vanillafy on - Open Vanillafy");
            player.SendInfoMessage("/vanillafy off - Close Vanillafy");
            player.SendInfoMessage("/vanillafy state - Get Vanillafy State");
            player.SendInfoMessage("/vanillafy help - Get Vanillafy Help");
        }

        void changeState(Group group, string defaultGuestGroupName=DefualtGuestGroupName)
        {
            var GroupName = group.Name;

            UserAccountManager um = TShock.UserAccounts;
            um.GetUserAccounts().Where(u => u.Group != "superadmin" && u.Group != "owner").ForEach(u => um.SetUserGroup(u, GroupName));

            foreach (var player in TShock.Players)
            {
                if (player?.Group == null || player.Group is SuperAdminGroup)
                {
                    continue;
                }

                player.Group = group;
            }

            Group.DefaultGroup = group;

            TShockConfig tsConfig = TShock.Config;
            tsConfig.Settings.DefaultGuestGroupName = defaultGuestGroupName;
            tsConfig.Settings.DefaultRegistrationGroupName = GroupName;
            tsConfig.Write(TShockConfigPath);
        }

        void trunOnConfirm(object obj)
        {
            if (obj == null)
            {
                return;
            }

            GroupManager groupManager = TShock.Groups;
            Group group = createVanillaGroupObject();
            if (!groupManager.GroupExists(GroupName))
            {
                groupManager.AddGroup(name: GroupName, parentname: null, permissions: group.Permissions, chatcolor: Group.defaultChatColor);
            }
            else
            {
                groupManager.UpdateGroup(name: GroupName, parentname: null, permissions: group.Permissions, chatcolor: Group.defaultChatColor, suffix: null, prefix: null);
            }
            changeState(groupManager.GetGroupByName(GroupName), GroupName);

            CommandArgs args = (CommandArgs)obj;
            var player = args.Player ?? TSPlayer.Server;
            player.SendSuccessMessage("Server has successfully been configured for vanilla gameplay.");
        }

        void trunOffConfirm(object obj)
        {
            if (obj == null)
            {
                return;
            }

            changeState(TShock.Groups.GetGroupByName(DefaultRegistrationGroupName));

            CommandArgs args = (CommandArgs)obj;
            var player = args.Player ?? TSPlayer.Server;
            player.SendSuccessMessage("Server has been unconfigured for vanilla gameplay.");
        }
        
        Group createVanillaGroupObject()
        {
            Group group = new Group(GroupName);

            group.AddPermission("tshock.ignore.*");
            group.AddPermission("!tshock.ignore.ssc");

            group.AddPermission("tshock.account.*");

            group.AddPermission("tshock.npc.hurttown");
            group.AddPermission("tshock.npc.startinvasion");
            group.AddPermission("tshock.npc.startdd2");
            group.AddPermission("tshock.npc.summonboss");
            group.AddPermission("tshock.npc.spawnpets");

            group.AddPermission("tshock.tp.rod");
            group.AddPermission("tshock.tp.wormhole");
            group.AddPermission("tshock.tp.pylon");
            group.AddPermission("tshock.tp.tppotion");
            group.AddPermission("tshock.tp.magicconch");
            group.AddPermission("tshock.tp.demonconch");

            group.AddPermission("tshock.world.editspawn");
            group.AddPermission("tshock.world.modify");
            group.AddPermission("tshock.world.movenpc");
            group.AddPermission("tshock.world.paint");
            group.AddPermission("tshock.world.time.usesundial");
            group.AddPermission("tshock.world.toggleparty");
            
            group.AddPermission("tshock.canchat");
            group.AddPermission("tshock.partychat");
            group.AddPermission("tshock.thirdperson");
            group.AddPermission("tshock.whisper");
            group.AddPermission("tshock.sendemoji");

            group.AddPermission("tshock.journey.*");

            return group;
        }
    }
}
