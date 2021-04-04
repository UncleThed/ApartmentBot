using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ApartmentBot.Parsers
{
    public class HtmlLoader
    {
        private readonly HttpClient _client;

        public HtmlLoader()
        {
            _client = new HttpClient();
        }

        public async Task<string> GetSource(IParserSettings settings)
        {
            var response = await _client.GetAsync(settings.BaseUrl + settings.Prefix);
            string source = null;

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                source = await response.Content.ReadAsStringAsync();
            }

            return source;
        }
    }
}
