using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace NetCore_PVGIS_v5_AzureFunctions {
    public class AzureFunctionsTools {
        #region Response Objects

        /// <summary>
        /// Return a custom response message.
        /// </summary>
        /// <param name="code">HTTP status to use.</param>
        /// <param name="payload">Object or message to return.</param>
        /// <returns>Formatted HttpResponseMessage.</returns>
        public static HttpResponseMessage ResponseObject(HttpStatusCode code, object payload) {
            if (payload is string) {
                payload = new {
                    message = payload.ToString()
                };
            }

            return new HttpResponseMessage(code) {
                Content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json")
            };
        }

        #endregion

        #region Shorthand Response Objects

        /// <summary>
        /// Shorthand function for 400 Bad Request.
        /// </summary>
        /// <param name="payload">Object or message to return.</param>
        /// <returns>Formatted HttpResponseMessage.</returns>
        public static HttpResponseMessage RoBadRequest(object payload) {
            return ResponseObject(HttpStatusCode.BadRequest, payload);
        }

        /// <summary>
        /// Shorthand function for 200 Ok.
        /// </summary>
        /// <param name="payload">Object or message to return.</param>
        /// <returns>Formatted HttpResponseMessage.</returns>
        public static HttpResponseMessage RoOk(object payload) {
            return ResponseObject(HttpStatusCode.OK, payload);
        }

        /// <summary>
        /// Shorthand function for 500 Internal Server Error.
        /// </summary>
        /// <param name="payload">Object or message to return.</param>
        /// <returns>Formatted HttpResponseMessage.</returns>
        public static HttpResponseMessage RoServerError(object payload) {
            return ResponseObject(HttpStatusCode.InternalServerError, payload);
        }

        #endregion

        #region Query-String Parse Functions

        /// <summary>
        /// Get and parse decimal value from query string.
        /// </summary>
        /// <param name="req">HttpRequest to get var from.</param>
        /// <param name="key">Key to look for.</param>
        /// <param name="defaultValue">Default value, if not present.</param>
        /// <param name="cultureInfo">Which culture to parse under.</param>
        /// <returns>Parsed decimal.</returns>
        public static decimal ParseQueryVarToDecimal(
            HttpRequest req,
            string key,
            decimal defaultValue,
            IFormatProvider cultureInfo) {

            if (req.Query.ContainsKey(key) &&
                decimal.TryParse(req.Query[key], NumberStyles.Any, cultureInfo, out var temp)) {

                return temp;
            }

            return defaultValue;
        }

        /// <summary>
        /// Get string value from query string.
        /// </summary>
        /// <param name="req">HttpRequest to get var from.</param>
        /// <param name="key">Key to look for.</param>
        /// <param name="defaultValue">Default value, if not present.</param>
        /// <returns>String value.</returns>
        public static string ParseQueryVarToString(
            HttpRequest req,
            string key,
            string defaultValue) {

            return req.Query.ContainsKey(key)
                ? req.Query[key].ToString()
                : defaultValue;
        }

        #endregion
    }
}