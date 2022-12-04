using System.Numerics;

namespace EDDatabase.Util
{
    public class Math
    {
        public static float DistanceFromSol(float x, float y, float z) => DistanceFromSol(new Vector3(x, y, z));

        public static float DistanceFromSol(Vector3 vector)
        {
            return Vector3.Distance(vector, Vector3.Zero);
        }
    }
}
