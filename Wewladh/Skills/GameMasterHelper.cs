using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh.Skills
{
    public class GameMasterHelper : Skill
    {
        public GameMasterHelper()
        {
            this.Name = "Game Master Helper";
            this.Icon = 265;

            this.Pane = SkillPane.Miscellaneous;
            this.Target = SkillTargetType.NoTarget;

            this.RequiresAdmin = true;
        }

        public override void Invoke(Character c, Character target)
        {
            var dialog = new Dialog_GameMasterHelper();
            dialog.Message = string.Format("What's up, {0}?", c.Name);
            GiveDialog(c, dialog);
        }

        public class Dialog_GameMasterHelper : OptionDialog
        {
            public Dialog_GameMasterHelper()
            {
                this.CanGoBack = false;
                this.CanGoNext = true;
                this.Options.Add("Add Arena Host");
                this.Options.Add("Remove Arena Host");
                this.Options.Add("Change Player Appearance");
            }
            public override DialogB Back(Player p, ClientPacket msg)
            {
                return null;
            }
            public override DialogB Next(Player p, ClientPacket msg)
            {
                msg.ReadByte();
                switch (msg.ReadByte())
                {
                    case 0x01:
                        {
                            var dialog = new Dialog_GameMasterHelper_AddArenaHost();
                            dialog.Message = "Add who as arena host?";
                            return dialog;
                        }
                    case 0x02:
                        {
                            var dialog = new Dialog_GameMasterHelper_RemoveArenaHost();
                            dialog.Message = "Remove who as arena host?";
                            return dialog;
                        }
                    case 0x03:
                        {
                            var dialog = new Dialog_GameMasterHelper_ChangeAppearance_01();
                            dialog.Message = "Whose appearance are you changing?";
                            return dialog;
                        }
                    default: return null;
                }
            }
            public override DialogB Exit(Player p, ClientPacket msg)
            {
                return null;
            }
        }

        public class Dialog_GameMasterHelper_AddArenaHost : InputDialog
        {
            public Dialog_GameMasterHelper_AddArenaHost()
            {
                this.CanGoBack = false;
                this.CanGoNext = true;
                this.InputLength = 12;
            }
            public override DialogB Back(Player p, ClientPacket msg)
            {
                return null;
            }
            public override DialogB Next(Player p, ClientPacket msg)
            {
                msg.ReadByte();
                var text = msg.ReadString(msg.ReadByte());
                foreach (var client in p.GameServer.Clients)
                {
                    if (client != null && client.Player != null && client.Player.Name == text)
                    {
                        client.Player.AdminRights |= AdminRights.ArenaHost;
                        client.Player.AddLegendMark("Legend_Arena_Host");
                        p.GameServer.BroadcastMessage("{0} is now an arena host!", client.Player.Name);
                    }
                }
                return null;
            }
            public override DialogB Exit(Player p, ClientPacket msg)
            {
                return null;
            }
        }

        public class Dialog_GameMasterHelper_RemoveArenaHost : InputDialog
        {
            public Dialog_GameMasterHelper_RemoveArenaHost()
            {
                this.CanGoBack = false;
                this.CanGoNext = true;
                this.InputLength = 12;
            }
            public override DialogB Back(Player p, ClientPacket msg)
            {
                return null;
            }
            public override DialogB Next(Player p, ClientPacket msg)
            {
                msg.ReadByte();
                var text = msg.ReadString(msg.ReadByte());
                foreach (var client in p.GameServer.Clients)
                {
                    if (client != null && client.Player != null && client.Player.Name == text)
                    {
                        client.Player.AdminRights &= ~AdminRights.ArenaHost;
                        client.Player.RemoveLegendMark("Legend_Arena_Host");
                        p.GameServer.BroadcastMessage("{0} is no longer an arena host.", client.Player.Name);
                    }
                }
                return null;
            }
            public override DialogB Exit(Player p, ClientPacket msg)
            {
                return null;
            }
        }

        public class Dialog_GameMasterHelper_ChangeAppearance_01 : InputDialog
        {
            public Dialog_GameMasterHelper_ChangeAppearance_01()
            {
                this.CanGoBack = false;
                this.CanGoNext = true;
                this.InputLength = 12;
            }
            public override DialogB Back(Player p, ClientPacket msg)
            {
                return null;
            }
            public override DialogB Next(Player p, ClientPacket msg)
            {
                msg.ReadByte();
                var text = msg.ReadString(msg.ReadByte());
                p.Session("WhoseAppearance", text);

                var dialog = new Dialog_GameMasterHelper_ChangeAppearance_02();
                dialog.Message = "What are you changing about them?";
                return dialog;
            }
            public override DialogB Exit(Player p, ClientPacket msg)
            {
                return null;
            }
        }

        public class Dialog_GameMasterHelper_ChangeAppearance_02 : OptionDialog
        {
            public Dialog_GameMasterHelper_ChangeAppearance_02()
            {
                this.CanGoBack = false;
                this.CanGoNext = true;
                this.Options.Add("Hair Style");
                this.Options.Add("Hair Color");
                this.Options.Add("Face Style");
                this.Options.Add("Face Color");
                this.Options.Add("Body Color");
                this.Options.Add("Opposite Gender Hair");
            }
            public override DialogB Back(Player p, ClientPacket msg)
            {
                return null;
            }
            public override DialogB Next(Player p, ClientPacket msg)
            {
                msg.ReadByte();
                switch (msg.ReadByte())
                {
                    case 0x01:
                        {
                            var dialog = new Dialog_GameMasterHelper_HairStyle();
                            dialog.Message = "Which hair style?";
                            return dialog;
                        }
                    case 0x02:
                        {
                            var dialog = new Dialog_GameMasterHelper_HairColor();
                            dialog.Message = "Which hair color?";
                            return dialog;
                        }
                    case 0x03:
                        {
                            var dialog = new Dialog_GameMasterHelper_FaceStyle();
                            dialog.Message = "Which face style?";
                            return dialog;
                        }
                    case 0x04:
                        {
                            var dialog = new Dialog_GameMasterHelper_FaceColor();
                            dialog.Message = "Which face color?";
                            return dialog;
                        }
                    case 0x05:
                        {
                            var dialog = new Dialog_GameMasterHelper_BodyColor();
                            dialog.Message = "Which body color?";
                            return dialog;
                        }
                    case 0x06:
                        {
                            foreach (var client in p.GameServer.Clients)
                            {
                                if (client != null && client.Player != null && p.Session<string>("WhoseAppearance") == client.Player.Name)
                                {
                                    int color = client.Player.HairColor;
                                    client.Player.OppositeGenderHair = !client.Player.OppositeGenderHair;
                                    client.Player.Display();
                                }
                            }

                            return null;
                        }
                    default: return null;
                }
            }
            public override DialogB Exit(Player p, ClientPacket msg)
            {
                return null;
            }
        }

        public class Dialog_GameMasterHelper_HairStyle : InputDialog
        {
            public Dialog_GameMasterHelper_HairStyle()
            {
                this.CanGoBack = false;
                this.CanGoNext = true;
                this.InputLength = 3;
            }
            public override DialogB Back(Player p, ClientPacket msg)
            {
                return null;
            }
            public override DialogB Next(Player p, ClientPacket msg)
            {
                msg.ReadByte();
                var text = msg.ReadString(msg.ReadByte());
                foreach (var client in p.GameServer.Clients)
                {
                    if (client != null && client.Player != null && p.Session<string>("WhoseAppearance") == client.Player.Name)
                    {
                        int style = client.Player.HairStyle;
                        if (int.TryParse(text, out style))
                        {
                            client.Player.HairStyle = style;
                            client.Player.Display();
                        }
                    }
                }
                return null;
            }
            public override DialogB Exit(Player p, ClientPacket msg)
            {
                return null;
            }
        }

        public class Dialog_GameMasterHelper_HairColor : InputDialog
        {
            public Dialog_GameMasterHelper_HairColor()
            {
                this.CanGoBack = false;
                this.CanGoNext = true;
                this.InputLength = 3;
            }
            public override DialogB Back(Player p, ClientPacket msg)
            {
                return null;
            }
            public override DialogB Next(Player p, ClientPacket msg)
            {
                msg.ReadByte();
                var text = msg.ReadString(msg.ReadByte());
                foreach (var client in p.GameServer.Clients)
                {
                    if (client != null && client.Player != null && p.Session<string>("WhoseAppearance") == client.Player.Name)
                    {
                        int color = client.Player.HairColor;
                        if (int.TryParse(text, out color))
                        {
                            client.Player.HairColor = color;
                            client.Player.Display();
                        }
                    }
                }
                return null;
            }
            public override DialogB Exit(Player p, ClientPacket msg)
            {
                return null;
            }
        }

        public class Dialog_GameMasterHelper_FaceStyle : InputDialog
        {
            public Dialog_GameMasterHelper_FaceStyle()
            {
                this.CanGoBack = false;
                this.CanGoNext = true;
                this.InputLength = 3;
            }
            public override DialogB Back(Player p, ClientPacket msg)
            {
                return null;
            }
            public override DialogB Next(Player p, ClientPacket msg)
            {
                msg.ReadByte();
                var text = msg.ReadString(msg.ReadByte());
                foreach (var client in p.GameServer.Clients)
                {
                    if (client != null && client.Player != null && p.Session<string>("WhoseAppearance") == client.Player.Name)
                    {
                        int style = client.Player.FaceStyle;
                        if (int.TryParse(text, out style))
                        {
                            client.Player.FaceStyle = style;
                            client.Player.Display();
                        }
                    }
                }
                return null;
            }
            public override DialogB Exit(Player p, ClientPacket msg)
            {
                return null;
            }
        }

        public class Dialog_GameMasterHelper_FaceColor : InputDialog
        {
            public Dialog_GameMasterHelper_FaceColor()
            {
                this.CanGoBack = false;
                this.CanGoNext = true;
                this.InputLength = 3;
            }
            public override DialogB Back(Player p, ClientPacket msg)
            {
                return null;
            }
            public override DialogB Next(Player p, ClientPacket msg)
            {
                msg.ReadByte();
                var text = msg.ReadString(msg.ReadByte());
                foreach (var client in p.GameServer.Clients)
                {
                    if (client != null && client.Player != null && p.Session<string>("WhoseAppearance") == client.Player.Name)
                    {
                        int color = client.Player.FaceColor;
                        if (int.TryParse(text, out color))
                        {
                            client.Player.FaceColor = color;
                            client.Player.Display();
                        }
                    }
                }
                return null;
            }
            public override DialogB Exit(Player p, ClientPacket msg)
            {
                return null;
            }
        }

        public class Dialog_GameMasterHelper_BodyColor : InputDialog
        {
            public Dialog_GameMasterHelper_BodyColor()
            {
                this.CanGoBack = false;
                this.CanGoNext = true;
                this.InputLength = 3;
            }
            public override DialogB Back(Player p, ClientPacket msg)
            {
                return null;
            }
            public override DialogB Next(Player p, ClientPacket msg)
            {
                msg.ReadByte();
                var text = msg.ReadString(msg.ReadByte());
                foreach (var client in p.GameServer.Clients)
                {
                    if (client != null && client.Player != null && p.Session<string>("WhoseAppearance") == client.Player.Name)
                    {
                        int color = client.Player.BodyColor;
                        if (int.TryParse(text, out color))
                        {
                            client.Player.BodyColor = color;
                            client.Player.Display();
                        }
                    }
                }
                return null;
            }
            public override DialogB Exit(Player p, ClientPacket msg)
            {
                return null;
            }
        }
    }
}