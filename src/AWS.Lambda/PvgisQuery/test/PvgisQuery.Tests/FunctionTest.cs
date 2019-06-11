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

            var obj = JsonConvert.DeserializeObject<Function.ResponseBody>(json);

            Assert.IsType<Function.ResponseBody>(obj);
        }

        /// <summary>
        /// Test if we get a correct 400 if lat/lng is missing.
        /// </summary>
        [Fact]
        public void GetErrorMissingLatLng() {
            var json = new Function().FunctionHandler(
                new Function.RequestPostBody(),
                new TestLambdaContext());

            var error = JsonConvert.DeserializeObject<Function.ClientError>(json);

            Assert.IsType<Function.ClientError>(error);
        }
    }
}