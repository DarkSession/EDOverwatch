namespace EDDataProcessor.EDDN
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class EDDNSchemaAttribute : Attribute
    {
        public string SchemaUrl { get; }

        public EDDNSchemaAttribute(string schemaUrl)
        {
            SchemaUrl = schemaUrl;
        }
    }
}
