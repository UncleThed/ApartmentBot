namespace ApartmentBot.Parsers
{
    public interface IParserSettings
    {
        public string BaseUrl { get; }

        public string Prefix { get; }
    }
}
