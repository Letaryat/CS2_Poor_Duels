using CounterStrikeSharp.API.Modules.Utils;

namespace CS2_Poor_Duels.Models
{
    public class Arena
    {
        public int Id { get; set; }
        public Vector Spawn1 { get; set; }
        public Vector Spawn2 { get; set; }
        public QAngle? QAngle1 { get; set; }
        public QAngle? QAngle2 { get; set; }
        public bool isBusy { get; set; } = false;
        public bool isReserved { get; set; } = false;
        public bool showCenterHTML { get; set; } = false;
        public int roundType { get; set; }
        public Arena(int id, Vector spawn1, Vector spawn2, bool busy = false, QAngle? q1 = null, QAngle? q2 = null)
        {
            Id = id;
            Spawn1 = spawn1;
            Spawn2 = spawn2;
            QAngle1 = q1;
            QAngle2 = q2;
            isBusy = busy;
            isReserved = false;
            roundType = 0;
        }

        public static Arena FromFile(ArenaToFile file)
        {
            return new Arena(
                file.Id,
                file.Spawn1?.ToVector() ?? new Vector(0, 0, 0),
                file.Spawn2?.ToVector() ?? new Vector(0, 0, 0),
                false,
                file.Spawn1?.ToQAngle(),
                file.Spawn2?.ToQAngle()
            );
        }

    }
}
