namespace EDOverwatch_Web.Models
{
    public class TitanDamageResistance
    {
        public string Name { get; set; }

        public TitanDamageResistance(string name)
        {
            Name = name;
        }

        public static TitanDamageResistance GetDamageResistance(int systemsThargoidControlled, int heartsRemaining)
        {
            string damageResistanceName = systemsThargoidControlled switch
            {
                >= 0 when heartsRemaining <= 0 => string.Empty,
                0 => "Completely vulnerable",
                <= 3 => "Compromised",
                <= 6 => "Moderate",
                <= 7 => "High",
                <= 10 => "Very high",
                <= 40 => "Extremely high",
                _ => "Maximum",
            };
            return new TitanDamageResistance(damageResistanceName);
        }
    }
}
