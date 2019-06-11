using System.Collections.Generic;
using Xunit;
using Amazon.Lambda.TestUtilities;
using Newtonsoft.Json;

namespace PvgisQuery.Tests {
    public class FunctionTest {
        /// <summary>
        /// Test if we get a null response from function.
        /// </summary>
        [Fact]
        public void ReturnNotNull() {
            var json = new Function().FunctionHandler(
                new Function.RequestPostBody {
                    lat = 63.3568997750934,
                    lng = 10.3955364185906
                },
                new TestLambdaContext());

            Assert.NotNull(json);
        }

        /// <summary>
        /// Test if we get the correct JSON from function.
        /// </summary>
        [Fact]
        public void ResponseIsOfCorrectType() {
            var json = new Function().FunctionHandler(
                new Function.RequestPostBody {
                    lat = 63.3568997750934,
                    lng = 10.3955364185906
                },
                new TestLambdaContext());

            var obj = JsonConvert.DeserializeObject<ResponseBody>(json);

            Assert.IsType<ResponseBody>(obj);
        }

        /// <summary>
        /// Test if we get a correct 400 if lat/lng is missing.
        /// </summary>
        [Fact]
        public void GetErrorMissingLatLng() {
            var json = new Function().FunctionHandler(
                new Function.RequestPostBody(),
                new TestLambdaContext());

            var error = JsonConvert.DeserializeObject<ClientError>(json);

            Assert.IsType<ClientError>(error);
        }

        #region Helper classes

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

        public class ClientError {
            public string reason { get; set; }
            public string[] errors { get; set; }
        }

        #endregion
    }
}