using AngleSharp.Html.Dom;

namespace TestGuestBot.Parsers
{
    public interface IParser<T> where T : class
    {
        public IParserSettings Settings { get; }

        public T Parse(IHtmlDocument document);
    }
}
