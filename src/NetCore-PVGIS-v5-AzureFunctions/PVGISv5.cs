using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;

namespace NetCore_PVGIS_v5_AzureFunctions {
    /// <summary>
    /// Tool for querying the PVGIS v5 database.
    /// </summary>
    public class PVGISv5 {
        /// <summary>
        /// Query the PVGIS service for monthly and yearly means.
        /// </summary>
        /// <param name="lat">Latitude.</param>
        /// <param name="lng">Longitude.</param>
        /// <param name="angle">Fixed slope of modules.</param>
        /// <param name="aspect">Orientation (azimuth) of modules.</param>
        /// <param name="peakpower">Nominal power of the PV system.</param>
        /// <param name="loss">System losses (%).</param>
        /// <param name="pvtech">PV technology.</param>
        /// <param name="mounting">Mounting position.</param>
        /// <returns>Parsed values.</returns>
        public static PVGISv5Response Query(
            decimal lat,
            decimal lng,
            decimal angle,
            decimal aspect,
            decimal peakpower,
            decimal loss,
            string pvtech,
            string mounting) {

            // Query for which PVGIS databases to use.
            var databases = QueryForPvgisDatabases(lat, lng);

            if (databases == null) {
                return null;
            }

            // Cycle databases and look for satisfactory responses.
            foreach (var database in databases) {
                // Query PVGIS itself for values.
                var csv = QueryPvgisService(
                    database,
                    lat,
                    lng,
                    angle,
                    aspect,
                    peakpower,
                    loss,
                    pvtech,
                    mounting);

                if (csv == null) {
                    continue;
                }

                // Parse plain text/CSV.
                var res = ParsePvgisCsv(
                    csv);

                // Are we satisfied?
                if (res != null) {
                    return res;
                }
            }

            // We got not satisfactory response. Throw in the towel.
            return null;
        }

        /// <summary>
        /// Query the PVGIS system to figure out which database to use.
        /// </summary>
        /// <param name="lat">Latitude.</param>
        /// <param name="lng">Longitude.</param>
        /// <returns>Array of databases.</returns>
        private static IEnumerable<string> QueryForPvgisDatabases(
            decimal lat,
            decimal lng) {

            var databases = new[] {
                "PVGIS-CMSAF",
                "PVGIS-SARAH",
                "PVGIS-NSRDB",
                "PVGIS-ERA5",
                "PVGIS-COSMO"
            };

            var ci = new CultureInfo("en-US");

            var url = string.Format(
                "https://re.jrc.ec.europa.eu/pvgis5/extent.php?lat={0}&lon={1}&database={2}",
                lat.ToString(ci),
                lng.ToString(ci),
                string.Join(",", databases));

            var content = Request(url);

            if (content == null) {
                return null;
            }

            if (content.StartsWith("\n")) {
                content = content.Substring(1);
            }

            if (content.EndsWith("\n")) {
                content = content.Substring(0, content.Length - 1);
            }

            var indexes = content.Split('\n');

            if (indexes.Length != databases.Length) {
                return null;
            }

            var list = new List<string>();

            for (var i = 0; i < indexes.Length; i++) {
                if (indexes[i] == "0") {
                    continue;
                }

                list.Add(databases[i]);
            }

            return list;
        }

        /// <summary>
        /// Query the PVGIS service for monthly and yearly means.
        /// </summary>
        /// <param name="database">PVGIS database.</param>
        /// <param name="lat">Latitude.</param>
        /// <param name="lng">Longitude.</param>
        /// <param name="angle">Fixed slope of modules.</param>
        /// <param name="aspect">Orientation (azimuth) of modules.</param>
        /// <param name="peakpower">Nominal power of the PV system.</param>
        /// <param name="loss">System losses (%).</param>
        /// <param name="pvtech">PV technology.</param>
        /// <param name="mounting">Mounting position.</param>
        /// <returns>Plain text/CSV document.</returns>
        private static string QueryPvgisService(
            string database,
            decimal lat,
            decimal lng,
            decimal angle,
            decimal aspect,
            decimal peakpower,
            decimal loss,
            string pvtech,
            string mounting) {

            var ci = new CultureInfo("en-US");

            var url = string.Format(
                "https://re.jrc.ec.europa.eu/pvgis5/PVcalc.php?" +
                "lat={0}&" +
                "lon={1}&" +
                "raddatabase={2}&" +
                "browser=1&" +
                "userhorizon=&" +
                "usehorizon=1&" +
                "select_database_grid={2}&" +
                "pvtechchoice={7}&" +
                "peakpower={3}&" +
                "loss={6}&" +
                "mountingplace={8}&" +
                "angle={4}&" +
                "aspect={5}",
                lat.ToString(ci),
                lng.ToString(ci),
                database,
                peakpower.ToString(ci),
                angle.ToString(ci),
                aspect.ToString(ci),
                loss.ToString(ci),
                pvtech,
                mounting);

            return Request(url);
        }

        /// <summary>
        /// Remove superflaus bits and parse the remaining 'CSV' parts.
        /// </summary>
        /// <param name="csv">Plain text/CSV document.</param>
        /// <returns>Formatted response wrapper.</returns>
        private static PVGISv5Response ParsePvgisCsv(string csv) {
            // Get a lined array of the content.
            var lines = csv
                .Replace("\n", "")
                .Replace("\t\t", "\t")
                .Split('\r');

            var res = new PVGISv5Response {
                Monthly = new PVGISv5Response.PVGISv5ResponseMonthly {
                    Jan = GetRowValues(lines, "1"),
                    Feb = GetRowValues(lines, "2"),
                    Mar = GetRowValues(lines, "3"),
                    Apr = GetRowValues(lines, "4"),
                    May = GetRowValues(lines, "5"),
                    Jun = GetRowValues(lines, "6"),
                    Jul = GetRowValues(lines, "7"),
                    Aug = GetRowValues(lines, "8"),
                    Sep = GetRowValues(lines, "9"),
                    Oct = GetRowValues(lines, "10"),
                    Nov = GetRowValues(lines, "11"),
                    Dec = GetRowValues(lines, "12")
                },
                Yearly = new PVGISv5Response.PVGISv5ResponseYearly {
                    Average = GetRowValues(lines, "Year"),
                    Total = new PVGISv5Response.PVGISv5ResponseValues()
                }
            };

            res.Yearly.Total.Ed =
                res.Monthly.Jan.Ed +
                res.Monthly.Feb.Ed +
                res.Monthly.Mar.Ed +
                res.Monthly.Apr.Ed +
                res.Monthly.May.Ed +
                res.Monthly.Jun.Ed +
                res.Monthly.Jul.Ed +
                res.Monthly.Aug.Ed +
                res.Monthly.Sep.Ed +
                res.Monthly.Oct.Ed +
                res.Monthly.Nov.Ed +
                res.Monthly.Dec.Ed;

            res.Yearly.Total.Em =
                res.Monthly.Jan.Em +
                res.Monthly.Feb.Em +
                res.Monthly.Mar.Em +
                res.Monthly.Apr.Em +
                res.Monthly.May.Em +
                res.Monthly.Jun.Em +
                res.Monthly.Jul.Em +
                res.Monthly.Aug.Em +
                res.Monthly.Sep.Em +
                res.Monthly.Oct.Em +
                res.Monthly.Nov.Em +
                res.Monthly.Dec.Em;

            res.Yearly.Total.Hd =
                res.Monthly.Jan.Hd +
                res.Monthly.Feb.Hd +
                res.Monthly.Mar.Hd +
                res.Monthly.Apr.Hd +
                res.Monthly.May.Hd +
                res.Monthly.Jun.Hd +
                res.Monthly.Jul.Hd +
                res.Monthly.Aug.Hd +
                res.Monthly.Sep.Hd +
                res.Monthly.Oct.Hd +
                res.Monthly.Nov.Hd +
                res.Monthly.Dec.Hd;

            res.Yearly.Total.Hm =
                res.Monthly.Jan.Hm +
                res.Monthly.Feb.Hm +
                res.Monthly.Mar.Hm +
                res.Monthly.Apr.Hm +
                res.Monthly.May.Hm +
                res.Monthly.Jun.Hm +
                res.Monthly.Jul.Hm +
                res.Monthly.Aug.Hm +
                res.Monthly.Sep.Hm +
                res.Monthly.Oct.Hm +
                res.Monthly.Nov.Hm +
                res.Monthly.Dec.Hm;

            res.Yearly.Total.SDm =
                res.Monthly.Jan.SDm +
                res.Monthly.Feb.SDm +
                res.Monthly.Mar.SDm +
                res.Monthly.Apr.SDm +
                res.Monthly.May.SDm +
                res.Monthly.Jun.SDm +
                res.Monthly.Jul.SDm +
                res.Monthly.Aug.SDm +
                res.Monthly.Sep.SDm +
                res.Monthly.Oct.SDm +
                res.Monthly.Nov.SDm +
                res.Monthly.Dec.SDm;

            return res;
        }

        /// <summary>
        /// Get the values from an indexed row.
        /// </summary>
        /// <param name="lines">All lines.</param>
        /// <param name="index">Index to use.</param>
        /// <returns>Values from indexed row.</returns>
        private static PVGISv5Response.PVGISv5ResponseValues GetRowValues(
            IEnumerable<string> lines,
            string index) {

            foreach (var line in lines) {
                var columns = line.Split('\t');

                if (columns.Length != 6 ||
                    columns[0] != index) {

                    continue;
                }

                return new PVGISv5Response.PVGISv5ResponseValues {
                    Ed = GetParsedDecimal(columns[1]),
                    Em = GetParsedDecimal(columns[2]),
                    Hd = GetParsedDecimal(columns[3]),
                    Hm = GetParsedDecimal(columns[4]),
                    SDm = GetParsedDecimal(columns[5])
                };
            }

            return null;
        }

        /// <summary>
        /// Parse a string to decimal.
        /// </summary>
        /// <param name="input">String to parse.</param>
        /// <returns>Parsed decimal.</returns>
        private static decimal GetParsedDecimal(string input) {
            return decimal.TryParse(input, NumberStyles.Any, new CultureInfo("en-US"), out var temp)
                ? temp
                : 0;
        }

        /// <summary>
        /// Perform a HttpWebRequest and return string content.
        /// </summary>
        /// <param name="url">URL to query.</param>
        /// <returns>String content.</returns>
        private static string Request(string url) {
            try {
                var req = WebRequest.Create(url) as HttpWebRequest;

                if (req == null) {
                    throw new Exception(
                        "Unable to create HttpWebRequest for URL: " + url);
                }

                req.Method = "GET";
                req.UserAgent = "IncreoPvgisV5/1.0.0";

                using (var stream = req.GetResponse().GetResponseStream()) {
                    if (stream == null) {
                        throw new Exception(
                            "Unable to get ResponseStream from HttpWebResponse for URL: " + url);
                    }

                    return
                        new StreamReader(stream)
                            .ReadToEnd();
                }
            }
            catch {
                //
            }

            return null;
        }

        #region Response Classes

        public class PVGISv5Response {
            public PVGISv5ResponseMonthly Monthly { get; set; }
            public PVGISv5ResponseYearly Yearly { get; set; }

            public class PVGISv5ResponseMonthly {
                public PVGISv5ResponseValues Jan { get; set; }
                public PVGISv5ResponseValues Feb { get; set; }
                public PVGISv5ResponseValues Mar { get; set; }
                public PVGISv5ResponseValues Apr { get; set; }
                public PVGISv5ResponseValues May { get; set; }
                public PVGISv5ResponseValues Jun { get; set; }
                public PVGISv5ResponseValues Jul { get; set; }
                public PVGISv5ResponseValues Aug { get; set; }
                public PVGISv5ResponseValues Sep { get; set; }
                public PVGISv5ResponseValues Oct { get; set; }
                public PVGISv5ResponseValues Nov { get; set; }
                public PVGISv5ResponseValues Dec { get; set; }
            }

            public class PVGISv5ResponseYearly {
                public PVGISv5ResponseValues Average { get; set; }
                public PVGISv5ResponseValues Total { get; set; }
            }

            public class PVGISv5ResponseValues {
                public decimal Ed { get; set; }
                public decimal Em { get; set; }
                public decimal Hd { get; set; }
                public decimal Hm { get; set; }
                public decimal SDm { get; set; }
            }
        }

        #endregion
    }
}