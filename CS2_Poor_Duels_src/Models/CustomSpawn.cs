using CounterStrikeSharp.API.Modules.Utils;

namespace CS2_Poor_Duels.Models
{
    public class CustomSpawn
    {
        public float posX { get; set; }
        public float posY { get; set; }
        public float posZ { get; set; }
        public float angleX { get; set; }
        public float angleY { get; set; }
        public float angleZ { get; set; }

        public Vector ToVector() => new Vector(posX, posY, posZ);
        public QAngle ToQAngle() => new QAngle(angleX, angleY, angleZ);

        public static CustomSpawn From(Vector pos, QAngle angle)
        {
            return new CustomSpawn
            {
                posX = pos.X,
                posY = pos.Y,
                posZ = pos.Z,
                angleX = angle.X,
                angleY = angle.Y,
                angleZ = angle.Z
            };
        }
    }
}
