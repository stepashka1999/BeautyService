using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.Html.Dom;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace YandexMarketParser_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = Path.GetTempPath();
            //  %20 - пробел
            var client = new HttpClient();
            var result = client.GetAsync("https://goldapple.ru/catalogsearch/result/?q=syoss").Result;
            var domParser = new AngleSharp.Html.Parser.HtmlParser();
            var content = result.Content.ReadAsStringAsync().Result;
            var document = domParser.ParseDocument(content);

            //Получаем все элементы ol
            
            var olList = document.QuerySelectorAll("ol").Where(item => item.ClassList.Contains("products"));
            

            var tempProductList = new List<IElement>();
            foreach(var ol in olList)
            {
                tempProductList.AddRange(ol.QuerySelectorAll("div").Where(x => x.ClassList.Contains("product-item-info")));
            }

            //Bechmark 1 - Sync
            var stopWathc = new Stopwatch();
            //stopWathc.Start();
            //var productList = GetProductList(tempProductList);
            //stopWathc.Stop();
            //Console.WriteLine("Sync: " + stopWathc.ElapsedMilliseconds); // Like 18.7 sec

            //Benchmark 2 - Async
            stopWathc.Reset();
            stopWathc.Start();
            var productListAsync = GetProductListAsync(tempProductList);
            stopWathc.Stop();
            Console.WriteLine("Async: " + stopWathc.ElapsedMilliseconds); // Like 6.5 sec

            //TODO - Ускорить до 2-3 секунд получение продуктов,
            //       а лучше даже еще быстрее
          

            //Output results
            Console.WriteLine("TemProducts Count: " + tempProductList.Count);
            Console.WriteLine("Products Count: " + productListAsync.Count());
            foreach(var product in productListAsync)
            {
                var separator = new string('=', 8);

                Console.WriteLine();
                Console.WriteLine(separator);
                Console.WriteLine();
                Console.WriteLine(product);
            }
        }

        static IEnumerable<Product> GetProductListAsync(IEnumerable<IElement> productList)
        {
            var products = new List<Product>();

            Parallel.ForEach(productList, product =>
            {
                //Поулчаем ссылку на страницу продукта
                var link = product.QuerySelector("a").GetAttribute("href");
                //Получаем ссылку на изображение
                var image = product.QuerySelector("source").GetAttribute("data-srcset");
                var brandName = product.QuerySelectorAll("strong").FirstOrDefault(x => x.ClassName != null && x.ClassList.Contains("product-item-name"))
                                   .QuerySelectorAll("span")[0].TextContent;
                var productName = product.QuerySelectorAll("strong").FirstOrDefault(x => x.ClassName != null && x.ClassList.Contains("product-item-name"))
                                   .QuerySelectorAll("span")[1].TextContent;
                var price = product.QuerySelectorAll("div").FirstOrDefault(x => x.ClassName != null && x.ClassList.Contains("price-final_price"))
                                   .QuerySelectorAll("span").FirstOrDefault(x => x.ClassName != null && x.ClassList.Contains("price")).TextContent;

                var volume = GetProductVolume(link);
                var composition = GetProductComposition(link);
                //Получаем категорию продукта
                var type = product.QuerySelectorAll("div").FirstOrDefault(x => x.ClassName != null && x.ClassList.Contains("product-item-category-title"))
                                  .QuerySelector("span").TextContent;

                var tempProduct = new Product()
                {
                    BrandName = brandName,
                    ProductName = productName,
                    Composition = composition,
                    Image = image,
                    Price = price,
                    Type = type,
                    Volume = volume
                };

                products.Add(tempProduct);
            });


            return products;
        }

        static IEnumerable<Product> GetProductList(IEnumerable<IElement> productList)
        {
            var products = new List<Product>();

            //Parallel.ForEach(productList, product =>
            foreach(var product in productList)
            {
                //Поулчаем ссылку на страницу продукта
                var link = product.QuerySelector("a").GetAttribute("href");
                //Получаем ссылку на изображение
                var image = product.QuerySelector("source").GetAttribute("data-srcset");
                var brandName = product.QuerySelectorAll("strong").FirstOrDefault(x => x.ClassName != null && x.ClassList.Contains("product-item-name"))
                                   .QuerySelectorAll("span")[0].TextContent;
                var productName = product.QuerySelectorAll("strong").FirstOrDefault(x => x.ClassName != null && x.ClassList.Contains("product-item-name"))
                                   .QuerySelectorAll("span")[1].TextContent;
                var price = product.QuerySelectorAll("div").FirstOrDefault(x => x.ClassName != null && x.ClassList.Contains("price-final_price"))
                                   .QuerySelectorAll("span").FirstOrDefault(x => x.ClassName != null && x.ClassList.Contains("price")).TextContent;

                var volume = GetProductVolume(link);
                var composition = GetProductComposition(link);
                //Получаем категорию продукта
                var type = product.QuerySelectorAll("div").FirstOrDefault(x => x.ClassName != null && x.ClassList.Contains("product-item-category-title"))
                                  .QuerySelector("span").TextContent;

                var tempProduct = new Product()
                {
                    BrandName = brandName,
                    ProductName = productName,
                    Composition = composition,
                    Image = image,
                    Price = price,
                    Type = type,
                    Volume = volume
                };

                products.Add(tempProduct);
            }//);
            

            return products;
        }

        static string GetProductVolume(string linq)
        {
            var client = new HttpClient();
            var result = client.GetAsync(linq).Result;

            var domParser = new AngleSharp.Html.Parser.HtmlParser();
            var content = result.Content.ReadAsStringAsync().Result;
            var document = domParser.ParseDocument(content);
            //Поулчаем описание
            var spanElements = document.QuerySelectorAll("span");
            var volume = spanElements.FirstOrDefault(item => item.ClassName != null && item.ClassList.Contains("swatch-simple__view"));

            return volume == null ? "" : volume.TextContent;
        }

        static IEnumerable<string> GetProductComposition(string linq)
        {
            var client = new HttpClient();
            var result = client.GetAsync(linq).Result;

            var domParser = new AngleSharp.Html.Parser.HtmlParser();
            var content = result.Content.ReadAsStringAsync().Result;
            var document = domParser.ParseDocument(content);
            //Поулчаем описание
            var selectionElement = document.QuerySelectorAll("li").Where(x => x.ClassList.Contains("info-tabs__item")).ToList();
            var composition = selectionElement[selectionElement.Count -2].QuerySelector("section").TextContent
                                              .Split(" · ");

            return composition;
        }

    }

    class Product
    {
        public string BrandName { get; set; }
        public string ProductName { get; set; }
        public string Volume { get; set; }
        public string Price { get; set; }
        public string Image { get; set; }
        public IEnumerable<string> Composition { get; set; }
        public string Type { get; set; }


        public override string ToString()
        {
            return $"Name: {BrandName} {ProductName}\n" +
                   $"Price: {Price}\n" +
                   $"Volume: {Volume}\n" +
                   $"ImageLink: {Image}\n" +
                   $"Type: {Type}\n" +
                   $"Composition: {string.Join(", ", Composition)}";
        }
    }
}
