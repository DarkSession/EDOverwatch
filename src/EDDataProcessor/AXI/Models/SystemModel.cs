namespace EDDataProcessor.AXI.Models
{
    public class SystemModel
    {
        [JsonProperty("message")]
        public SystemModelMessage Message { get; set; }

        public SystemModel(SystemModelMessage message)
        {
            Message = message;
        }
    }

    public class SystemModelMessage
    {
        [JsonProperty("rows")]
        public List<SystemModelRow> Rows { get; set; }

        public SystemModelMessage(List<SystemModelRow> rows)
        {
            Rows = rows;
        }
    }

    public class SystemModelRow
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("priority")]
        public int? Priority { get; set; }

        public SystemModelRow(string name, int? priority)
        {
            Name = name;
            Priority = priority;
        }
    }
}
