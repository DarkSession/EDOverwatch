namespace Messages
{
    public class ThargoidMaelstromCreatedUpdated
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        public ThargoidMaelstromCreatedUpdated(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
