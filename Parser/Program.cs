using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Leaf.xNet;
using Newtonsoft.Json;

namespace Parser
{
    internal class Program
    {
        private static int PageNumber = 0;
        static void Main(string[] args)
        {
      
            Console.WriteLine("Укажите кол-во страниц: ");
            PageNumber = Convert.ToInt32(Console.ReadLine());
            var _timer = new Timer(TimerCallback, null, 0, 600000);
          
            Console.ReadKey();
        }

        private static void TimerCallback(Object o)
        {
            Console.WriteLine("Начинаем сбор данных :)");
            for (int page = 0; page < PageNumber - 1; page++)
            {
                Console.WriteLine($"Пройдено страниц: {page} из {PageNumber}");
                try
                {

                    using (HttpRequest req = new HttpRequest()
                    {
                        AllowAutoRedirect = true,
                        IgnoreProtocolErrors = true,
                        UserAgent =
                                   "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62",
                        ConnectTimeout = 30000,
                        ReadWriteTimeout = 30000
                    })
                    {
                        req.AddHeader("Accept", "*/*");
                        req.AddHeader("Accept-Language", "ru,en;q=0.9,en-GB;q=0.8,en-US;q=0.7");
                        req.AddHeader("DNT", "1");
                        req.AddHeader("Origin", "https://www.lesegais.ru");
                        req.AddHeader("Referer", "https://www.lesegais.ru/open-area/deal");
                        req.AddHeader("sec-ch-ua",
                            "\" Not;A Brand\";v=\"99\", \"Microsoft Edge\";v=\"103\", \"Chromium\";v=\"103\"");
                        req.AddHeader("sec-ch-ua-mobile", "?0");
                        req.AddHeader("sec-ch-ua-platform", "\"Windows\"");
                        req.AddHeader("Sec-Fetch-Dest", "empty");
                        req.AddHeader("Sec-Fetch-Mode", "cors");
                        req.AddHeader("Sec-Fetch-Site", "same-origin");

                        var Response = req.Post("https://www.lesegais.ru/open-area/graphql",
                            "{\"query\":\"query SearchReportWoodDeal($size: Int!, $number: Int!, $filter: Filter, $orders: [Order!]) {\\n  searchReportWoodDeal(filter: $filter, pageable: {number: $number, size: $size}, orders: $orders) {\\n    content {\\n      sellerName\\n      sellerInn\\n      buyerName\\n      buyerInn\\n      woodVolumeBuyer\\n      woodVolumeSeller\\n      dealDate\\n      dealNumber\\n      __typename\\n    }\\n    __typename\\n  }\\n}\\n\",\"variables\":{\"size\":20,\"number\":" + page + ",\"filter\":null,\"orders\":null},\"operationName\":\"SearchReportWoodDeal\"}",
                            "application/json").ToString();
                        if (Response.Contains("sellerName"))
                        {
                            Root JsonResponse = JsonConvert.DeserializeObject<Root>(Response);
                            foreach (var current in JsonResponse.Data.SearchReportWoodDeal.Content)
                            {
                                var sellerName = current.SellerName;
                                if (CheckRow(sellerName) == false)
                                {
                                    var buyerInn = current.BuyerInn;
                                    var buyerName = current.BuyerName;
                                    var dealDate = current.DealDate;
                                    var dealNumber = current.DealNumber;
                                    var sellerInn = current.SellerInn;
                                    var woodVolumeBuyer = current.WoodVolumeBuyer;
                                    var woodVolumeSeller = current.WoodVolumeSeller;
                                    var Result = AddToBaseSQL(buyerInn, buyerName, dealDate, dealNumber, sellerInn, sellerName, woodVolumeBuyer,
                                        woodVolumeSeller);
                                    if (Result)
                                    {

                                        Console.WriteLine("Успешно добавли запись c Номером декларации: " + dealNumber);
                                    }
                                }

                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                GC.Collect();
            }
        }

        public static bool CheckRow(string sellerName)
        {
            using (SqlConnection conn = new SqlConnection(@"Data Source=HOME-PC;Initial Catalog=Base;Integrated Security=false;User ID=sa;Password=123;"))
            {
                SqlCommand cmd = new SqlCommand($"SELECT dealNumber FROM [Base].[dbo].[DealTable] WHERE sellerName = '{sellerName}'", conn);
                try
                {
                    conn.Open();
                    return cmd.ExecuteScalar() != null;


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }

        }

        public static bool AddToBaseSQL(string buyerInn, string buyerName, DateTime dealDate, string dealNumber, string sellerInn, string sellerName, double woodVolumeBuyer, double woodVolumeSeller)
        {
            try
            {

            using (SqlConnection connection = new SqlConnection(@"Data Source=HOME-PC;Initial Catalog=Base;Integrated Security=false;User ID=sa;Password=123;"))
            {
                    String query = "INSERT INTO [Base].[dbo].[DealTable] (buyerInn,buyerName,dealDate,dealNumber,sellerInn,sellerName,woodVolumeBuyer,woodVolumeSeller) VALUES (@buyerInn,@buyerName,@dealDate,@dealNumber,@sellerInn,@sellerName,@woodVolumeBuyer,@woodVolumeSeller)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buyerInn", buyerInn);
                        command.Parameters.AddWithValue("@buyerName", buyerName);
                        command.Parameters.AddWithValue("@dealDate", dealDate);
                        command.Parameters.AddWithValue("@dealNumber", dealNumber);
                        command.Parameters.AddWithValue("@sellerInn", sellerInn);
                        command.Parameters.AddWithValue("@sellerName", sellerName);
                        command.Parameters.AddWithValue("@woodVolumeBuyer", woodVolumeBuyer);
                        command.Parameters.AddWithValue("@woodVolumeSeller", woodVolumeSeller);

                        connection.Open();
                        int result = command.ExecuteNonQuery();

                        // Check Error
                        if (result < 0)
                            Console.WriteLine("Error inserting data into Database!");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }
        public class Content
        {
            [JsonProperty("sellerName")]
            public string SellerName;

            [JsonProperty("sellerInn")]
            public string SellerInn;

            [JsonProperty("buyerName")]
            public string BuyerName;

            [JsonProperty("buyerInn")]
            public string BuyerInn;

            [JsonProperty("woodVolumeBuyer")]
            public double WoodVolumeBuyer;

            [JsonProperty("woodVolumeSeller")]
            public double WoodVolumeSeller;

            [JsonProperty("dealDate")]
            public DateTime DealDate;

            [JsonProperty("dealNumber")]
            public string DealNumber;

            [JsonProperty("__typename")]
            public string Typename;
        }

        public class Data
        {
            [JsonProperty("searchReportWoodDeal")]
            public SearchReportWoodDeal SearchReportWoodDeal;
        }

        public class Root
        {
            [JsonProperty("data")]
            public Data Data;
        }

        public class SearchReportWoodDeal
        {
            [JsonProperty("content")]
            public List<Content> Content;

            [JsonProperty("__typename")]
            public string Typename;
        }


    }
}
