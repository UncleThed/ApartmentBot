namespace TestGuestBot.Parsers.SutkiTomsk
{
    public class SutkiTomskSettings : IParserSettings
    {
        public string BaseUrl { get; } = "https://sutkitomsk.ru/";

        public string Prefix { get; } = "Apartment/";
    }
}
