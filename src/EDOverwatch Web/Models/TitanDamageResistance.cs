namespace EDOverwatch_Web.Models
{
    public class TitanDamageResistance(string name)
    {
        public string Name { get; set; } = name;

        public static TitanDamageResistance GetDamageResistance(int systemsThargoidControlled, int heartsRemaining)
        {
            string damageResistanceName = systemsThargoidControlled switch
            {
                >= 0 when heartsRemaining <= 0 => string.Empty,
                <= 3 => "Completely vulnerable",
                _ => "Maximum",
            };
            return new TitanDamageResistance(damageResistanceName);
        }
    }
}
