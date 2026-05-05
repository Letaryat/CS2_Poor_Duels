using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CS2_Poor_Duels.Core;

namespace CS2_Poor_Duels.Utils
{
    public class PluginUtils(CS2_Poor_DuelsPlugin plugin)
    {
        private CS2_Poor_DuelsPlugin _plugin = plugin;
        public void ExecConfig()
        {
            Server.ExecuteCommand("execifexists leti1v1/leti1v1duels.cfg");
        }

        public void KillServerCommandEnts()
        {
            var pointServerCommands = Utilities.FindAllEntitiesByDesignerName<CPointServerCommand>("point_servercommand");

            foreach (var servercmd in pointServerCommands)
            {
                if (servercmd == null) continue;
                servercmd.Remove();
            }
        }

        public void ChangeToOppositeTeam(CCSPlayerController p1, CCSPlayerController p2)
        {
            var player1Pawn = p1.PlayerPawn.Value;
            if (player1Pawn == null || !player1Pawn.IsValid) return;
            var player2Pawn = p2.PlayerPawn.Value;
            if (player2Pawn == null || !player2Pawn.IsValid) return;

            var newTeam = (player2Pawn.TeamNum == (byte)CsTeam.Terrorist)
                ? CsTeam.CounterTerrorist
                : CsTeam.Terrorist;

            p1.ChangeTeam(newTeam);

            _plugin.PluginExtensions!.DebugLogger($"Changed team of players {p1.PlayerName} {p1.Team}");
        }

        public void ChangeToTeamsIDontFuckingCareAnyLonger(CCSPlayerController p1, CCSPlayerController p2)
        {
            var player1Pawn = p1.PlayerPawn.Value;
            if (player1Pawn == null || !player1Pawn.IsValid) return;
            var player2Pawn = p2.PlayerPawn.Value;
            if (player2Pawn == null || !player2Pawn.IsValid) return;
            p1.ChangeTeam(CsTeam.Terrorist);
            p2.ChangeTeam(CsTeam.CounterTerrorist);
        }

        public void GenerateBeams(Vector pos, string _color)
        {
            CBeam beam = Utilities.CreateEntityByName<CBeam>("beam")!;

            var pos1 = new Vector(pos.X, pos.Y, pos.Z);
            var pos2 = new Vector(pos.X, pos.Y, pos.Z + 1000);

            if (beam.Entity == null) return;

            beam.Entity.Name = "Leti1v1Beam";

            beam.Render = Color.FromName(_color);
            beam.Width = 10.0f;
            beam.Teleport(pos1);
            beam.EndPos.Add(pos2);
            beam.DispatchSpawn();
        }

        public void CreatePlayerEntity(Vector pos, QAngle angle, bool ct = true)
        {
            var model = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (model == null)
                return;

            model.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags = (uint)(model.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags & ~(1 << 2));
            if (ct) model.SetModel("agents/models/ctm_gendarmerie/ctm_gendarmerie_varianta.vmdl");
            else model.SetModel("agents/models/tm_professional/tm_professional_vari.vmdl");

            if (model.Entity == null) return;
            model.Entity.Name = "Leti1v1PlayerModel";
            
            model.UseAnimGraph = false;
            model.AcceptInput("SetAnimation", value: "tools_preview");
            
            model.DispatchSpawn();
            model.Teleport(pos, angle, new Vector(0, 0, 0));
        }

        public void RemoveAllPlayerModels()
        {
            var playerModels = Utilities.FindAllEntitiesByDesignerName<CDynamicProp>("prop_dynamic");
            foreach (var model in playerModels)
            {
                if (model == null) continue;
                if (model.Entity == null) continue;
                string? name = model.Entity.Name;
                if (!string.IsNullOrEmpty(name) && name.Contains("Leti1v1PlayerModel"))
                {
                    model.Remove();
                }
            }
        }
        public void RemoveAllBeams()
        {
            var beams = Utilities.FindAllEntitiesByDesignerName<CBeam>("beam");
            foreach (var beam in beams)
            {
                if (beam.Entity == null) continue;
                if (beam.Entity.Name.Contains("Leti1v1Beam"))
                {
                    beam.Remove();
                }
            }
        }

        public bool IfWarmup()
        {
            var gamerules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
            if (gamerules == null)
            {
                return false;
            }
            if (gamerules.WarmupPeriod)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public int GetAllPlayers()
        {
            var playersList = Utilities.GetPlayers().Where(p => !p.IsHLTV && p.Connected == PlayerConnectedState.Connected && (p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist)).ToList();

            return playersList.Count();
        }


    }

}