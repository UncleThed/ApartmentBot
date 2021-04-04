using AngleSharp.Html.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using ApartmentBot.Models;

namespace ApartmentBot.Parsers.SutkiTomsk

{
    public class SutkiTomskParser : IParser<List<Apartment>>
    {
        public IParserSettings Settings { get; } = new SutkiTomskSettings();

        public List<Apartment> Parse(IHtmlDocument document)
        {
            var list = new List<Apartment>();

            var items = document.QuerySelectorAll("article");

            for (var i = 0; i < items.Length; i++)
            {
                list.Add(new Apartment()
                {
                    Id = i + 1,
                    PhotoUrl = ConvertToStringUrl((items[i].QuerySelector("img") as IHtmlImageElement).Source),
                    RoomsCount = ConvertToUint(items[i].QuerySelector("h3").TextContent),
                    District = items[i].QuerySelectorAll("div").Where(item => item.TextContent.Contains("Район:")).FirstOrDefault().QuerySelector("strong").TextContent,
                    Address = items[i].QuerySelector("address").QuerySelector("strong").TextContent,
                    Cost = ConvertToUint(items[i].QuerySelectorAll("div").Where(item => item.TextContent.Contains("Стоимость:")).FirstOrDefault().QuerySelector("strong").TextContent),
                    Client = null
                });
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
