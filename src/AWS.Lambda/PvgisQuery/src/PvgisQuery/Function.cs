using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace PvgisQuery {
    public class Function {
        /// <summary>
        /// Query PVGIS.
        /// </summary>
        /// <param name="body">JSON input.</param>
        /// <param name="context">AWS Lambda context.</param>
        /// <returns>Values from PVGIS.</returns>
        public string FunctionHandler(RequestPostBody body, ILambdaContext context) {
            if (!body.lat.HasValue ||
                !body.lng.HasValue) {

                return JsonConvert.SerializeObject(
                    new {
                        reason = "client_error",
                        errors = new[] {
                            "Both 'lat' and 'lng' are required."
                        }
                    });
            }

            var culture = new CultureInfo("en-US");

            // Set default values.
            if (!body.peakpower.HasValue) {
                body.peakpower = 1;
            }

            if (!body.losses.HasValue) {
                body.losses = 14;
            }

            if (!body.slope.HasValue) {
                body.slope = 35;
            }

            if (!body.azimuth.HasValue) {
                body.azimuth = 0;
            }

            if (string.IsNullOrWhiteSpace(body.pvtech)) {
                body.pvtech = "crystSi";
            }

            if (string.IsNullOrWhiteSpace(body.mounting)) {
                body.mounting = "free";
            }

            // Query the actual Europa PVGIS server.
            var html = QueryPvgisEuropa(new Dictionary<string, string> {
                {"MAX_FILE_SIZE", "10000"},
                {"pv_database", "PVGIS-classic"},
                {"pvtechchoice", body.pvtech},
                {"peakpower", body.peakpower.Value.ToString(culture)},
                {"efficiency", body.losses.Value.ToString(culture)},
                {"mountingplace", body.mounting},
                {"angle", body.slope.Value.ToString(culture)},
                {"aspectangle", body.azimuth.Value.ToString(culture)},
                {"horizonfile", ""},
                {"outputchoicebuttons", "window"},
                {"sbutton", "Calculate"},
                {"outputformatchoice", "window"},
                {"optimalchoice", ""},
                {"latitude", body.lat.Value.ToString(culture)},
                {"longitude", body.lng.Value.ToString(culture)},
                {"regionname", "europe"},
                {"language", "en_en"}
            });

            if (string.IsNullOrWhiteSpace(html)) {
                return JsonConvert.SerializeObject(
                    new {
                        reason = "pvgis_error",
                        errors = new[] {
                            "No valid daily radiation data."
                        }
                    });
            }

            // Analyze the HTML from PVGIS and compile a response object.
            var values = ParsePvgisHtml(html);

            if (values == null) {
                return JsonConvert.SerializeObject(
                    new {
                        reason = "pvgis_error",
                        errors = new[] {
                            "Unable to parse data from PVGIS."
                        }
                    });
            }

            // All is ok, return the values.
            return JsonConvert.SerializeObject(values);
        }

        /// <summary>
        /// Query the actual Europa PVGIS server.
        /// </summary>
        /// <param name="dict">Parameters for call.</param>
        /// <returns>HTML from response.</returns>
        private string QueryPvgisEuropa(Dictionary<string, string> dict) {
            try {
                var req = WebRequest.Create("http://re.jrc.ec.europa.eu/pvgis/apps4/PVcalc.php") as HttpWebRequest;

                if (req == null) {
                    throw new Exception("Could not create HttpWebRequest.");
                }

                req.Method = "POST";
                req.UserAgent = "QueryPvgis";

                req.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                req.Headers.Add("Content-Encoding", "UTF8");

                var body = dict.Aggregate("", (c, p) => c + "&" + p.Key + "=" + WebUtility.UrlEncode(p.Value)).Substring(1);
                var buffer = Encoding.UTF8.GetBytes(body);
                var stream = req.GetRequestStream();

                stream.Write(buffer, 0, buffer.Length);
                stream.Close();

                var res = req.GetResponse() as HttpWebResponse;

                if (res == null) {
                    throw new Exception("Could not create HttpWebResponse.");
                }

                stream = res.GetResponseStream();

                if (stream == null) {
                    throw new Exception("Could not get ResponseStream from HttpWebResponse.");
                }

                return new StreamReader(stream).ReadToEnd();
            }
            catch (Exception _) {
                // We have no way of logging the error, so might as well, sadly, just supress it.
                return null;
            }
        }

        /// <summary>
        /// Analyze the HTML from PVGIS and compile a response object.
        /// </summary>
        /// <param name="html">HTML.</param>
        /// <returns>Parsed values.</returns>
        private ResponseBody ParsePvgisHtml(string html) {
            return new ResponseBody {
                monthlyAverage = new Dictionary<string, ResponseBody.Monthly> {
                    {"Jan", GetMonthlyRowFromHTML(ref html, "Jan")},
                    {"Feb", GetMonthlyRowFromHTML(ref html, "Feb")},
                    {"Mar", GetMonthlyRowFromHTML(ref html, "Mar")},
                    {"Apr", GetMonthlyRowFromHTML(ref html, "Apr")},
                    {"May", GetMonthlyRowFromHTML(ref html, "May")},
                    {"Jun", GetMonthlyRowFromHTML(ref html, "Jun")},
                    {"Jul", GetMonthlyRowFromHTML(ref html, "Jul")},
                    {"Aug", GetMonthlyRowFromHTML(ref html, "Aug")},
                    {"Sep", GetMonthlyRowFromHTML(ref html, "Sep")},
                    {"Oct", GetMonthlyRowFromHTML(ref html, "Oct")},
                    {"Nov", GetMonthlyRowFromHTML(ref html, "Nov")},
                    {"Dec", GetMonthlyRowFromHTML(ref html, "Dec")},
                },
                yearlyAverage = GetMonthlyRowFromHTML(ref html, "Yearly average"),
                yearlyTotal = GetYearlyRowFromHTML(ref html, "Total for year")
            };
        }

        /// <summary>
        /// Get values from a monthly row.
        /// </summary>
        /// <param name="html">The source HTML to parse.</param>
        /// <param name="searchKey">Key to search for.</param>
        /// <returns>Parsed values.</returns>
        private static ResponseBody.Monthly GetMonthlyRowFromHTML(ref string html, string searchKey) {
            var key = "<td> " + searchKey + " </td>";
            var sp = html.IndexOf(key, StringComparison.CurrentCultureIgnoreCase);

            if (sp == -1) {
                key = "<td><b> " + searchKey + " </b></td>";
                sp = html.IndexOf(key, StringComparison.CurrentCultureIgnoreCase);
            }

            if (sp == -1) {
                return null;
            }

            var temp = html.Substring(sp + key.Length);

            sp = temp.IndexOf("</td></tr>", StringComparison.CurrentCultureIgnoreCase);

            if (sp == -1) {
                return null;
            }

            temp = temp.Substring(0, sp)
                .Replace("</td>", "")
                .Replace("<b>", "")
                .Replace("</b>", "")
                .Replace("<td align=\"right\">", ",");

            var values = temp.Split(',');

            if (values.Length != 5) {
                return null;
            }

            return new ResponseBody.Monthly {
                ed = ParseDecimal(values[1]),
                em = ParseDecimal(values[2]),
                hd = ParseDecimal(values[3]),
                hm = ParseDecimal(values[4])
            };
        }

        /// <summary>
        /// Get values from a yearly row.
        /// </summary>
        /// <param name="html">The source HTML to parse.</param>
        /// <param name="searchKey">Key to search for.</param>
        /// <returns>Parsed values.</returns>
        private static ResponseBody.Yearly GetYearlyRowFromHTML(ref string html, string searchKey) {
            var key = "<td><b>" + searchKey + "</b></td>";
            var sp = html.IndexOf(key, StringComparison.CurrentCultureIgnoreCase);

            if (sp == -1) {
                return null;
            }

            var temp = html.Substring(sp + key.Length);

            sp = temp.IndexOf("</td> </tr>", StringComparison.CurrentCultureIgnoreCase);

            if (sp == -1) {
                return null;
            }

            temp = temp.Substring(0, sp)
                .Replace("<td align=\"right\" colspan=2 >", ",")
                .Replace("<b>", "")
                .Replace("</b>", "")
                .Replace("</td>", "");

            var values = temp.Split(',');

            if (values.Length != 3) {
                return null;
            }

            return new ResponseBody.Yearly {
                e = ParseDecimal(values[1]),
                h = ParseDecimal(values[2])
            };
        }

        /// <summary>
        /// Parse valid decimal from given string.
        /// </summary>
        /// <param name="value">String to parse.</param>
        /// <returns>Decimal</returns>
        public static decimal ParseDecimal(string value) {
            if (value == null) {
                return 0;
            }

            return decimal.TryParse(
                value.Trim(),
                NumberStyles.Any,
                new CultureInfo("en-US"),
                out var d)
                ? d
                : 0;
        }

        #region Helper classes

        public class RequestPostBody {
            public double? lat { get; set; }
            public double? lng { get; set; }
            public double? peakpower { get; set; }
            public double? losses { get; set; }
            public double? slope { get; set; }
            public double? azimuth { get; set; }
            public string mounting { get; set; }
            public string pvtech { get; set; }
        }

        public class ResponseBody {
            public Dictionary<string, Monthly> monthlyAverage { get; set; }
            public Monthly yearlyAverage { get; set; }
            public Yearly yearlyTotal { get; set; }

            public class Monthly {
                public decimal ed { get; set; }
                public decimal em { get; set; }
                public decimal hd { get; set; }
                public decimal hm { get; set; }
            }

            public class Yearly {
                public decimal e { get; set; }
                public decimal h { get; set; }
            }
        }

        #endregion
    }
}