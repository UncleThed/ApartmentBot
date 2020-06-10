using AngleSharp.Html.Dom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestGuestBot.Parsers.SutkiTomsk
{
    public class SutkiTomskParser : IParser<List<ApartmentData>>
    {
        public IParserSettings Settings { get; } = new SutkiTomskSettings();

        public List<ApartmentData> Parse(IHtmlDocument document)
        {
            var list = new List<ApartmentData>();

            var items = document.QuerySelectorAll("article");

            foreach (var item in items)
            {
                list.Add(new ApartmentData(
                        ConvertToStringUrl((item.QuerySelector("img") as IHtmlImageElement).Source),
                        ConvertToUint(item.QuerySelector("h3").TextContent),
                        item.QuerySelectorAll("div").Where(item => item.TextContent.Contains("Район:")).FirstOrDefault().QuerySelector("strong").TextContent,
                        item.QuerySelector("address").QuerySelector("strong").TextContent,
                        ConvertToUint(item.QuerySelectorAll("div").Where(item => item.TextContent.Contains("Стоимость:")).FirstOrDefault().QuerySelector("strong").TextContent)

                    ));
            }

            return list;
        }

        private string ConvertToStringUrl(string source)
        {
            var prefix = source.Substring(source.IndexOf("img"));

            return Settings.BaseUrl + prefix;
        }

        private uint ConvertToUint(string source)
        {
            source = source.Remove(source.IndexOf(source.Where(ch => !char.IsDigit(ch)).First()));

            uint result;

            if (!uint.TryParse(source, out result))
            {
                throw new InvalidCastException("Не удалось преобразовать строку к численному значению параметра");
            }

            return result;
        }
    }
}
