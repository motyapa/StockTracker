using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Drawing;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using System.Collections;

namespace StockTracker
{
    public class StockModel : PageModel
    {
        public static string APIKey = System.IO.File.ReadAllText(@"C:\Users\Arthur\Desktop\Scripts\AlphaVantageCode.txt");
        public static double smoothing = 2; //Higher number shifts focus on newer date. 2 seems to be the industry standard so use for now
        public class DateTimeValue
        {
            public string DateTime;
            public double ClosingValue;
        }

        //Home page (for the time being)
        public ActionResult OnGet(string ticker, int dayCount, int movingAverageDays, int exponentialAverageDays)
        {
            if (ticker == null || dayCount == 0 || (movingAverageDays == 0 && exponentialAverageDays == 0))
            {
                return null;
            } else
            {
                return OnGetData(ticker, dayCount, movingAverageDays, exponentialAverageDays);
            }          
        }
        public ActionResult OnGetData(string ticker, int dayCount, int movingAverageDays, int exponentialAverageDays)
        {
            Dictionary<string, Dictionary<string, string>> timeData = getAPIData(ticker);
            var returnResult = new Dictionary<string, string>();
            List<DateTimeValue> averageValues = new List<DateTimeValue>();
            List<DateTimeValue> stockCloseValues = new List<DateTimeValue>();
            //List<DateTimeValue> stockCloseValues = getCloseValues(timeData, dayCount + movingAverageDays);
            //List<DateTimeValue> movingAverage = getCenterAverages(stockCloseValues, movingAverageDays);

            if (movingAverageDays == 0 && exponentialAverageDays > 0 && (dayCount + exponentialAverageDays - 1 <= timeData.Count))
            {
                stockCloseValues = getCloseValues(timeData, dayCount + exponentialAverageDays - 1);
                averageValues = getExponentialAverages(stockCloseValues, exponentialAverageDays);                                        
            } 
            else if (exponentialAverageDays == 0 && movingAverageDays > 0 && (dayCount + movingAverageDays - 1 <= timeData.Count))
            {
                stockCloseValues = getCloseValues(timeData, dayCount + movingAverageDays - 1);
                averageValues = getMovingAverages(stockCloseValues, movingAverageDays);                                
            } 
            //Error checking
            //Only one input should be entered at a time - in the future will change the UI to have a dropdown where the user selects which average they want
            else if (exponentialAverageDays > 0 && movingAverageDays > 0)
            {
                return new JsonResult(new { error = "Error, both Exponential Average Days and Moving Average Days cannot contain non-zero values"});
            } 
            //Can't get average data for more days than the ticker has existed (as well as the trailing days, for a 30 day average of 30 days we need 59 data points)
            else
            {
                return new JsonResult(new { error = "Error, please make sure that the day count and average add up to less than " + (timeData.Keys.Count + 1)});
            }

            foreach (var data in averageValues)
            {
                returnResult.Add(data.DateTime, Convert.ToString(data.ClosingValue));
            }

            return new JsonResult(new { dates = returnResult.Keys.Reverse(), values = returnResult.Values.Reverse() });
        }  

        //Get API data for a particular ticker
        public static Dictionary<string, Dictionary<string, string>> getAPIData(string ticker)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(String.Format("https://www.alphavantage.co/query?function=TIME_SERIES_DAILY_ADJUSTED&symbol={0}&outputsize=full&apikey={1}", ticker, APIKey)));
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            StreamReader sr = new StreamReader(response.GetResponseStream());
            string results = sr.ReadToEnd();
            sr.Close();

            dynamic data = JsonConvert.DeserializeObject<Dictionary<string, object>>(results);
            var timeData = Convert.ToString(data["Time Series (Daily)"]);
            Dictionary<string, Dictionary<string, string>> timeDataDic = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(timeData);
            return timeDataDic;
        }

        //Filter the API data to only have the stock price at close
        public static List<DateTimeValue> getCloseValues(Dictionary<string, Dictionary<string, string>> timeDataDic, int totalDays)
        {
            List<DateTimeValue> closeValuesDateTime = new List<DateTimeValue>();
            for (int i = 0; i < totalDays; i++)
            {
                var entry = timeDataDic.ElementAt(i).Value;
                var newModel = new DateTimeValue();
                newModel.DateTime = Convert.ToDateTime(timeDataDic.ElementAt(i).Key).ToString("MM/dd/yyyy");
                newModel.ClosingValue = Convert.ToDouble(entry["5. adjusted close"]);
                closeValuesDateTime.Add(newModel);
            }
            return closeValuesDateTime;
        }

        //Code that gets different types of averages using the close price
        public static List<DateTimeValue> getMovingAverages(List<DateTimeValue> closingData, int days)
        {
            List<DateTimeValue> movingAverages = new List<DateTimeValue>();
            for (int i = 0; i <= closingData.Count - days; i++)
            {
                double avg = calculateAvg(closingData.Skip(i).Take(days).ToList());
                DateTimeValue avgModel = new DateTimeValue();
                avgModel.DateTime = closingData[i].DateTime;
                avgModel.ClosingValue = avg;
                movingAverages.Add(avgModel);
            }
            return movingAverages;
        }       
        public static List<DateTimeValue> getExponentialAverages(List<DateTimeValue> closingData, int days)
        {
            List<DateTimeValue> movingAverages = new List<DateTimeValue>();
            for (int i = 0; i <= closingData.Count - days; i++)
            {
                double avg = calculateExponentialAvg(closingData.Skip(i).Take(days).ToList());
                DateTimeValue avgModel = new DateTimeValue();
                avgModel.DateTime = closingData[i].DateTime;
                avgModel.ClosingValue = avg;
                movingAverages.Add(avgModel);
            }
            return movingAverages;
        }
        public static List<DateTimeValue> getCenterAverages(List<DateTimeValue> closingData, int days)
        {
            List<DateTimeValue> centerAverages = new List<DateTimeValue>();
            for (int i = days; i < closingData.Count - days; i++)
            {
                double avg = calculateAvg(closingData.GetRange(i - days, 2 * days + 1).ToList());
                DateTimeValue avgModel = new DateTimeValue();
                avgModel.DateTime = closingData[i].DateTime;
                avgModel.ClosingValue = avg;
                centerAverages.Add(avgModel);
            }
            return centerAverages;
        }

        //Math related functions
        public static double calculateAvg(List<DateTimeValue> data)
        {
            double total = 0;
            foreach (DateTimeValue val in data)
            {
                total += val.ClosingValue;
            }
            return Math.Round(total / data.Count(), 2);
        }
        public static double calculateExponentialAvg(List<DateTimeValue> data)
        {
            double final = data.ElementAt(0).ClosingValue;
            var smoothingVar = smoothing / (1 + data.Count);
            for (int i = 1; i < data.Count; i++)
            {
                final = smoothingVar * data.ElementAt(i).ClosingValue + (1 - smoothingVar) * final;
            }
            return Math.Round(final, 2);
        }       
    }
}