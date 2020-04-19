using System;
using System.IO;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;

namespace Vanillafier
{
    [ApiVersion(2, 1)]
    public class Vanillafier : TerrariaPlugin
    {
        const string ConfirmationCommand = "vanillafy-confirm";
        const string GroupName = "Vanillafied";
        static string TShockConfigPath { get { return Path.Combine(TShock.SavePath, "config.json"); } }

        public override string Author => "Pryaxis";
        public override string Description => "Instantly configure your server for vanilla gameplay";
        public override string Name => "Vanillafier";

        public override Version Version => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

        public Vanillafier(Main game) : base(game) { }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command(Permissions.managegroup, Vanillafy, "vanillafy"));
        }

        public void Vanillafy(CommandArgs args)
        {
            var player = args.Player;
            if (player == null)
            {
                return;
            }

            if (TShock.Groups.GroupExists(GroupName))
            {
                player.SendErrorMessage("The vanillafy command has already been run.");
                player.SendWarningMessage("If you proceed with running this command, your existing vanilla configuration will be reset.");
            }
            else
            {
                player.SendWarningMessage("This command will disable most cheat and grief protection for all non-superadmin users.");
            }

            player.SendWarningMessage($"Are you sure you wish to run this command? Use {TShock.Config.CommandSpecifier}{ConfirmationCommand} to complete configuration.");
            player.AwaitingResponse.Add(ConfirmationCommand, Confirm);
        }

        public void Confirm(object obj)
        {
            if (obj == null)
            {
                return;
            }

            CommandArgs args = (CommandArgs)obj;
            var player = args.Player ?? TSPlayer.Server;

            //Get the TShock group manager and setup the new vanilla group
            GroupManager gm = TShock.Groups;
            Group group = CreateVanillaGroupObject();
            if (!gm.GroupExists(GroupName))
            {
                gm.AddGroup(name: GroupName, parentname: null, permissions: group.Permissions, chatcolor: Group.defaultChatColor);
            }
            else
            {
                gm.UpdateGroup(name: GroupName, parentname: null, permissions: group.Permissions, chatcolor: Group.defaultChatColor, suffix: null, prefix: null);
            }
            //Retrieve the group again just so that the object state is synced with db state
            group = gm.GetGroupByName(GroupName);

            //Get the TShock user manager, select all non-superadmin groups, and change their group to the new vanilla group
            UserManager um = TShock.Users;
            um.GetUsers().Where(u => u.Group != "superadmin").ForEach(u => um.SetUserGroup(u, GroupName));

            //Update all active player's groups, as long as they're not a superadmin
            foreach (var ply in TShock.Players)
            {
                if (ply?.Group == null || ply.Group is SuperAdminGroup)
                {
                    continue;
                }

                ply.Group = group;
            }

            //Set the default group for any new guests joining
            Group.DefaultGroup = group;

            //Update the TShock config file so all new guest users will be assigned to the vanilla group
            ConfigFile tsConfig = TShock.Config;
            tsConfig.DefaultGuestGroupName = GroupName;
            tsConfig.DefaultRegistrationGroupName = GroupName;
            //Write the config file so that this change persists
            tsConfig.Write(TShockConfigPath);

            player.SendSuccessMessage("Server has successfully been configured for vanilla gameplay.");
        }
        
        Group CreateVanillaGroupObject()
        {
            Group g = new Group(GroupName);

            g.AddPermission("tshock.ignore.*");
            g.AddPermission("!tshock.ignore.ssc"); //Allow SSC gameplay

            g.AddPermission("tshock.account.*"); //Register, login, logout, change password

            g.AddPermission("tshock.npc.hurttown");
            g.AddPermission("tshock.npc.startinvasion");
            g.AddPermission("tshock.npc.startdd2");
            g.AddPermission("tshock.npc.summonboss");

            g.AddPermission("tshock.tp.others");
            g.AddPermission("tshock.tp.rod");
            g.AddPermission("tshock.tp.wormhole");

            g.AddPermission("tshock.world.editspawn");
            g.AddPermission("tshock.world.modify");
            g.AddPermission("tshock.world.movenpc");
            g.AddPermission("tshock.world.paint");
            g.AddPermission("tshock.world.time.usesundial");
            g.AddPermission("tshock.world.toggleparty");
            
            g.AddPermission("tshock.canchat");
            g.AddPermission("tshock.partychat");
            g.AddPermission("tshock.thirdperson");
            g.AddPermission("tshock.whisper");

            return g;
        }
    }
}
