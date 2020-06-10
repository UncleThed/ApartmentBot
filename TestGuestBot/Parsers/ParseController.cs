using AngleSharp.Html.Parser;
using System.Threading.Tasks;

namespace TestGuestBot.Parsers
{
    public class ParseController<T> where T : class
    {

        private HtmlLoader _loader;

        private IParser<T> _parser;

        public ParseController(IParser<T> parser)
        {
            _parser = parser;
            _loader = new HtmlLoader();
        }

        public async Task<T> GetDataFromSite()
        {
            var source = await _loader.GetSource(_parser.Settings);

            var domParser = new HtmlParser();
            var document = await domParser.ParseDocumentAsync(source);

            return _parser.Parse(document);
        }
    }
}
