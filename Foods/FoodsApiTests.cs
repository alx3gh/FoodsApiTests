using NUnit.Framework;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using Foods.Models;

namespace Foods
{
    [TestFixture]
    public class FoodsApiTests
    {
        private RestClient _client;
        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";

        private static string createdFoodId;

        [OneTimeSetUp]
        public void Setup()
        {
            string token =  GetJwtToken("userbg1", "user01");

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            _client = new RestClient(options);
        }
        private string GetJwtToken(string username, string password)
        {
            var client = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });

            var response = client.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString();
        }

        [Test, Order(1)]

        public void CreateFood_ShouldReturnCreated()
        {
            var food = new FoodDTO
            {
                Name = "Test Food",
                Description = "Test Description",
                Url = ""
            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);

            var response = _client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createdFoodId = json.GetProperty("foodId").GetString() ?? string.Empty;

            Assert.That(createdFoodId, Is.Not.Null.And.Not.Empty);
        }

        [Test, Order(2)]

        public void EditFoodTitle_ShouldReturnOk()
        {
            var changes = new[]
            {
                new {path = "/name", op = "replace", value = "Updated Food Name"},
            };

            var request = new RestRequest($"/api/Food/Edit/{createdFoodId}", Method.Patch);

            request.AddJsonBody(changes);
            var response = _client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Successfully edited"));
        }

        [Test, Order(3)]

        public void GetAllFoods_ShouldReturnList()
        {
            var request = new RestRequest($"/api/Food/All", Method.Get);
            
            var response = _client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(json, Is.Not.Null.And.Not.Empty);
        }

        [Test, Order(4)]

        public void DeleteFood_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Food/Delete/{createdFoodId}", Method.Delete);
            var response = _client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));

        }

        [Test, Order(5)]

        public void CreateFoodWithInvalidData_ShouldReturnBadRequest()
        {
            var food = new FoodDTO
            {
                Name = "",
                Description = "",
                Url = ""
            };
            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);
            var response = _client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        }

        [Test, Order(6)]

        public void EditNonExistingFood_ShouldReturnNotFound()
        {
            string nonExistingFoodId = "123";
            var changes = new[]
            {
                new {path = "/name", op = "replace", value = "new title"},
            };
            var request = new RestRequest($"/api/Food/Edit/{nonExistingFoodId}", Method.Patch);
            request.AddJsonBody(changes);
            var response = _client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No food revues..."));
        }

        [Test, Order(7)]

        public void DeleteNonExistingFood_ShouldReturnBadRequest()
        {
            string nonExistingFoodId = "123";

            var request = new RestRequest($"/api/Food/Delete/{nonExistingFoodId}", Method.Delete);
            var response = _client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this food revue!"));
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _client?.Dispose();
        }
    }
}