using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace StockTracker
{
    public class DiscountedTickersModel : PageModel
    {
        public class TickerDiscountPrice
        {
            public string Ticker { get; set; }
            public double PercentChange { get; set; }
            public string Date { get; set; }
            public double CurrentCost { get; set; }
            public string PreviousDate { get; set; }
            public double PreviousCost { get; set; }
        }
        public void OnGet()
        {

        }

        public JsonResult OnGetDiscounts(double percent, int dayCount)
        {
            List<TickerDiscountPrice> listOfTickers = new List<TickerDiscountPrice>();
            try
            {
                SqlConnection con = new SqlConnection(@"server=DESKTOP-7GM0KG3\SQLEXPRESS; database= master; integrated security = true");

                using (con)
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "[dbo].[GetLoss_SPY_QQQ]";
                    cmd.Parameters.Add("@PercentChange", SqlDbType.Decimal).Value = (100 - percent) / 100; //in the SP .9 gets all stocks that have fallen at least 10%, .8 all that have fallen at least 20%, etc.
                    cmd.Parameters.Add("@Days", SqlDbType.Int).Value = dayCount;
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TickerDiscountPrice TDP = new TickerDiscountPrice();
                            TDP.Ticker = reader.GetString(0);
                            TDP.PercentChange = (double) reader.GetDecimal(1);
                            TDP.Date = reader.GetDateTime(2).ToString("dd-MM-yy");
                            TDP.CurrentCost = (double) reader.GetDecimal(3);
                            TDP.PreviousDate = reader.GetDateTime(4).ToString("dd-MM-yy");
                            TDP.PreviousCost = (double)reader.GetDecimal(5);
                            listOfTickers.Add(TDP);
                        }
                    }
                    con.Close();
                }
            } catch (Exception ex) { }

            return new JsonResult(new { listOfTickers });
        }       
    }
}