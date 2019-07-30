using System.Globalization;
using System.IO;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NetCore_PVGIS_v5_AzureFunctions {
    public static class PvgisQuery {
        [FunctionName("PvgisQuery")]
        public static HttpResponseMessage Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            ILogger log) {

            var ci = new CultureInfo("en-US");

            decimal lat;
            decimal lng;
            decimal angle;
            decimal aspect;
            decimal peakpower;
            decimal loss;
            string pvtech;
            string mounting;

            log.LogInformation("PVGIS HTTP Trigger Function");
            log.LogInformation("HTTP Method: " + req.Method);

            switch (req.Method.ToUpper()) {
                case "GET":
                    lat = AzureFunctionsTools.ParseQueryVarToDecimal(req, "lat", -9999, ci);
                    lng = AzureFunctionsTools.ParseQueryVarToDecimal(req, "lng", -9999, ci);
                    angle = AzureFunctionsTools.ParseQueryVarToDecimal(req, "angle", 35, ci);
                    aspect = AzureFunctionsTools.ParseQueryVarToDecimal(req, "aspect", 0, ci);
                    peakpower = AzureFunctionsTools.ParseQueryVarToDecimal(req, "peakpower", 1, ci);
                    loss = AzureFunctionsTools.ParseQueryVarToDecimal(req, "loss", 14, ci);
                    pvtech = AzureFunctionsTools.ParseQueryVarToString(req, "pvtech", "crystSi");
                    mounting = AzureFunctionsTools.ParseQueryVarToString(req, "mounting", "free");

                    break;

                case "POST":
                    var json = new StreamReader(req.Body).ReadToEnd();

                    if (string.IsNullOrWhiteSpace(json)) {
                        return AzureFunctionsTools.RoBadRequest("Payload is required.");
                    }

                    var data = JsonConvert.DeserializeObject<PvgisPostBody>(json);

                    if (data == null) {
                        return AzureFunctionsTools.RoBadRequest("Unable to process payload. Both 'lat' and 'lng' are required.");
                    }

                    lat = data.lat;
                    lng = data.lng;
                    angle = data.angle ?? 35;
                    aspect = data.aspect ?? 0;
                    peakpower = data.peakpower ?? 1;
                    loss = data.loss ?? 14;
                    pvtech = data.pvtech ?? "crystSi";
                    mounting = data.mounting ?? "free";

                    break;

                default:
                    return AzureFunctionsTools.RoBadRequest("Only GET and POST are allowed.");
            }

            log.LogInformation("lat: " + lat.ToString(ci));
            log.LogInformation("lng: " + lng.ToString(ci));
            log.LogInformation("angle: " + angle.ToString(ci));
            log.LogInformation("aspect: " + aspect.ToString(ci));
            log.LogInformation("peakpower: " + peakpower.ToString(ci));
            log.LogInformation("loss: " + loss.ToString(ci));
            log.LogInformation("pvtech: " + pvtech);
            log.LogInformation("mounting: " + mounting);

            if (lat == -9999 || lng == -9999) {
                return AzureFunctionsTools.RoBadRequest("Both 'lat' and 'lng' are required.");
            }

            if (loss < 0 || loss > 100) {
                return AzureFunctionsTools.RoBadRequest("'loss' must be between (and including) 0 and 100.");
            }

            if (angle < 0 || angle > 90) {
                return AzureFunctionsTools.RoBadRequest("'angle' must be between (and including) 0 and 90.");
            }

            if (aspect < -180 || aspect > 180) {
                return AzureFunctionsTools.RoBadRequest("'aspect' must be between (and including) -180 and 180.");
            }

            if (peakpower < 0) {
                return AzureFunctionsTools.RoBadRequest("'peakpower' must be 0 or above.");
            }

            if (mounting != "free" && mounting != "building") {
                return AzureFunctionsTools.RoBadRequest("'mounting' must be either 'free' or 'building'.");
            }

            if (pvtech != "crystSi" && pvtech != "CIS" && pvtech != "CdTe") {
                return AzureFunctionsTools.RoBadRequest("'pvtech' must be either 'crystSi', 'CIS' or 'CdTe'. Case sensitive.");
            }

            var pvqv = PVGISv5.Query(
                lat,
                lng,
                angle,
                aspect,
                peakpower,
                loss,
                pvtech,
                mounting);

            return pvqv != null
                ? AzureFunctionsTools.RoOk(pvqv)
                : AzureFunctionsTools.RoServerError("Unable to get proper response from PVGIS service.");
        }

        #region Helper classes

        public class PvgisPostBody {
            public decimal lat { get; set; }
            public decimal lng { get; set; }
            public decimal? angle { get; set; }
            public decimal? aspect { get; set; }
            public decimal? peakpower { get; set; }
            public decimal? loss { get; set; }
            public string pvtech { get; set; }
            public string mounting { get; set; }
        }

        #endregion
    }
}