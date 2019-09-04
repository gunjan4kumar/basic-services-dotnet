using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using Flurl.Http.Testing;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using JohnsonControls.Metasys.BasicServices;

namespace Tests
{
    public class TraditionalClientTests
    {
        Guid mockid;
        Guid mockid2;
        string mockAttributeName;
        string mockAttributeName2;
        string mockAttributeName3;
        string mockAttributeName4;
        string mockAttributeName5;
        TraditionalClient traditionalClient;

        [SetUp]
        public void Init()
        {
            traditionalClient = new TraditionalClient();
            mockAttributeName = "property";
            mockAttributeName2 = "property2";
            mockAttributeName3 = "property3";
            mockAttributeName4 = "property4";
            mockAttributeName5 = "property5";
            mockid = new Guid("11111111-2222-3333-4444-555555555555");
            mockid2 = new Guid("11111111-2222-3333-4444-555555555556");
        }

        #region Login Tests

        [Test]
        public void TestLogin()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");
                httpTest.ShouldHaveCalled($"https://hostname/api/V2/login")
                    .WithVerb(HttpMethod.Post)
                    .WithContentType("application/json")
                    .WithRequestBody("{\"username\":\"username\",\"password\":\"password\"")
                    .Times(1);
            }
        }

        [Test]
        public void TestUnauthorizedLoginHandlesException()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWith("unauthorized", 401);

                try
                {
                    traditionalClient.TryLogin("username", "badpassword", "hostname");

                    httpTest.ShouldHaveCalled($"https://hostname/api/V2/login")
                        .WithVerb(HttpMethod.Post)
                        .WithContentType("application/json")
                        .WithRequestBody("{\"username\":\"username\",\"password\":\"badpassword\"")
                        .Times(1);
                }
                catch
                {
                    Assert.Fail();
                }

            }
        }

        [Test]
        public void TestBadHostLoginHandlesException()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWith("Call failed. No such host is known POST https://badhost/api/V2/login", 404);

                try
                {
                    traditionalClient.TryLogin("username", "password", "badhost");

                    httpTest.ShouldHaveCalled($"https://badhost/api/V2/login")
                        .WithVerb(HttpMethod.Post)
                        .WithContentType("application/json")
                        .WithRequestBody("{\"username\":\"username\",\"password\":\"password\"")
                        .Times(1);
                }
                catch
                {
                    Assert.Fail();
                }
            }
        }

        // [Test]
        // public void TestBadVersionLogin()
        // {
        //     using (var httpTest = new HttpTest())
        //     {
        //         httpTest.RespondWithJson(new {accessToken = "faketoken", expires = "2030-01-01T00:00:00Z"});
        //         traditionalClient.TryLogin("username", "password", "hostname");
        //         httpTest.ShouldHaveCalled($"https://hostname/api/V2/login")
        //             .WithVerb(HttpMethod.Post)
        //             .WithContentType("application/json")
        //             .WithRequestBody("{\"username\":\"username\",\"password\":\"password\"")
        //             .Times(1);
        //     }
        // }

        #endregion

        #region Refresh Tests

        [Test]
        public void TestRefresh()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.Refresh();
                httpTest.ShouldHaveCalled($"https://hostname/api/V2/refreshToken")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
            }
        }

        [Test]
        public void TestRefreshNullClient()
        {
            using (var httpTest = new HttpTest())
            {
                try
                {
                    traditionalClient.Refresh();
                    httpTest.ShouldNotHaveCalled($"https://hostname/api/V2/refreshToken");
                }
                catch
                {
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void TestRefreshHandlesException()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("unauthorized", 401);

                try
                {
                    traditionalClient.Refresh();
                    httpTest.ShouldHaveCalled($"https://hostname/api/V2/refreshToken")
                        .WithVerb(HttpMethod.Get)
                        .Times(1);
                }
                catch
                {
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void TestRefreshTimer()
        {
            using (var httpTest = new HttpTest())
            {
                DateTime future = DateTime.Now;
                future.AddSeconds(5);
                string time = future.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'");

                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = time });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                System.Threading.Thread.Sleep(7000);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/refreshToken")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
            }
        }

        #endregion

        #region GetObjectIdentifier Tests

        [Test]
        public void TestGetObjectIdentifier()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith($"\"{mockid.ToString()}\"");
                var id = traditionalClient.GetObjectIdentifier("fully:qualified/reference");
                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objectIdentifiers")
                .WithVerb(HttpMethod.Get)
                .Times(1);
                Assert.AreEqual(typeof(Guid), id.GetType());
            }
        }

        [Test]
        public void TestGetObjectIdentifierBadRequestReturnsEmpty()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("Bad Request", 400);

                var id = traditionalClient.GetObjectIdentifier("fully:qualified/reference");
                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objectIdentifiers")
                .WithVerb(HttpMethod.Get)
                .Times(1);
                Assert.AreEqual(Guid.Empty, id);
            }
        }

        [Test]
        public void TestGetBadObjectIdentifierReturnsEmpty()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("Bad Request", 400);

                var id = traditionalClient.GetObjectIdentifier("fully:qualified/reference");
                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objectIdentifiers")
                .WithVerb(HttpMethod.Get)
                .Times(1);
                Assert.AreEqual(Guid.Empty, id);
            }
        }

        [Test]
        public void TestGetObjectIdentifierNullClient()
        {
            using (var httpTest = new HttpTest())
            {
                try
                {
                    var id = traditionalClient.GetObjectIdentifier("fully:qualified/reference");
                    httpTest.ShouldNotHaveCalled($"https://hostname/api/V2/objectIdentifiers");
                    Assert.AreEqual(Guid.Empty, id);
                }
                catch
                {
                    Assert.Fail();
                }

            }
        }

        #endregion

        #region ReadProperty Tests

        [Test]
        public void TestReadPropertyInteger()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{\"item\": { \"" + mockAttributeName + "\": 1 }}");
                ReadPropertyResult result = traditionalClient.ReadProperty(mockid, mockAttributeName);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}/attributes/{mockAttributeName}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(1, result.NumericValue);
                Assert.AreEqual("1", result.StringValue);
                Assert.AreEqual(true, result.BooleanValue);
                Assert.AreEqual(null, result.ArrayValue);
                Assert.AreEqual(null, result.Priority);
                Assert.AreEqual("reliabilityEnumSet.reliable", result.Reliability);
                Assert.AreEqual(true, result.IsReliable);
            }
        }

        [Test]
        public void TestReadPropertyFloat()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{\"item\": { \"" + mockAttributeName + "\": 1.1 }}");
                ReadPropertyResult result = traditionalClient.ReadProperty(mockid, mockAttributeName);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}/attributes/{mockAttributeName}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(1.1, result.NumericValue);
                Assert.AreEqual("1.1", result.StringValue);
                Assert.AreEqual(true, result.BooleanValue);
                Assert.AreEqual(null, result.ArrayValue);
                Assert.AreEqual(null, result.Priority);
                Assert.AreEqual("reliabilityEnumSet.reliable", result.Reliability);
                Assert.AreEqual(true, result.IsReliable);
            }
        }

        [Test]
        public void TestReadPropertyString()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{\"item\": { \"" + mockAttributeName + "\": \"stringvalue\" }}");
                ReadPropertyResult result = traditionalClient.ReadProperty(mockid, mockAttributeName);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}/attributes/{mockAttributeName}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(0, result.NumericValue);
                Assert.AreEqual("stringvalue", result.StringValue);
                Assert.AreEqual(false, result.BooleanValue);
                Assert.AreEqual(null, result.ArrayValue);
                Assert.AreEqual(null, result.Priority);
                Assert.AreEqual("reliabilityEnumSet.reliable", result.Reliability);
                Assert.AreEqual(true, result.IsReliable);
            }
        }

        [Test]
        public void TestReadPropertyBooleanTrue()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{\"item\": { \"" + mockAttributeName + "\": true }}");
                ReadPropertyResult result = traditionalClient.ReadProperty(mockid, mockAttributeName);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}/attributes/{mockAttributeName}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(1, result.NumericValue);
                Assert.AreEqual("True", result.StringValue);
                Assert.AreEqual(true, result.BooleanValue);
                Assert.AreEqual(null, result.ArrayValue);
                Assert.AreEqual(null, result.Priority);
                Assert.AreEqual("reliabilityEnumSet.reliable", result.Reliability);
                Assert.AreEqual(true, result.IsReliable);
            }
        }

        [Test]
        public void TestReadPropertyBooleanFalse()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{\"item\": { \"" + mockAttributeName + "\": false }}");
                ReadPropertyResult result = traditionalClient.ReadProperty(mockid, mockAttributeName);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}/attributes/{mockAttributeName}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(0, result.NumericValue);
                Assert.AreEqual("False", result.StringValue);
                Assert.AreEqual(false, result.BooleanValue);
                Assert.AreEqual(null, result.ArrayValue);
                Assert.AreEqual(null, result.Priority);
                Assert.AreEqual("reliabilityEnumSet.reliable", result.Reliability);
                Assert.AreEqual(true, result.IsReliable);
            }
        }

        [Test]
        public void TestReadPropertyObjectWithReliabilityPriorityInteger()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{ \"item\": { \"" + mockAttributeName + "\": {" +
                "\"value\": 60, \"reliability\": \"reliabilityEnumSet.reliable\", \"priority\": \"writePriorityEnumSet.priorityNone\"} } }");
                ReadPropertyResult result = traditionalClient.ReadProperty(mockid, mockAttributeName);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}/attributes/{mockAttributeName}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(60, result.NumericValue);
                Assert.AreEqual("60", result.StringValue);
                Assert.AreEqual(true, result.BooleanValue);
                Assert.AreEqual(null, result.ArrayValue);
                Assert.AreEqual("writePriorityEnumSet.priorityNone", result.Priority);
                Assert.AreEqual("reliabilityEnumSet.reliable", result.Reliability);
                Assert.AreEqual(true, result.IsReliable);
            }
        }

        [Test]
        public void TestReadPropertyObjectWithReliabilityPriorityString()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{ \"item\": { \"" + mockAttributeName + "\": {" +
                "\"value\": \"stringvalue\", \"reliability\": \"reliabilityEnumSet.reliable\", \"priority\": \"writePriorityEnumSet.priorityNone\"} } }");
                ReadPropertyResult result = traditionalClient.ReadProperty(mockid, mockAttributeName);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}/attributes/{mockAttributeName}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(0, result.NumericValue);
                Assert.AreEqual("stringvalue", result.StringValue);
                Assert.AreEqual(false, result.BooleanValue);
                Assert.AreEqual(null, result.ArrayValue);
                Assert.AreEqual("writePriorityEnumSet.priorityNone", result.Priority);
                Assert.AreEqual("reliabilityEnumSet.reliable", result.Reliability);
                Assert.AreEqual(true, result.IsReliable);
            }
        }

        [Test]
        public void TestReadPropertyObjectWithReliabilityPriorityStringNoValueField()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{ \"item\": { \"" + mockAttributeName + "\": { " +
                "\"property\": \"stringvalue\", \"property2\": \"stringvalue2\", " +
                "\"reliability\": \"reliabilityEnumSet.noInput\", \"priority\": \"writePriorityEnumSet.priorityDefault\"} } }");
                ReadPropertyResult result = traditionalClient.ReadProperty(mockid, mockAttributeName);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}/attributes/{mockAttributeName}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(1, result.NumericValue);
                Assert.AreEqual("Unsupported Data Type", result.StringValue);
                Assert.AreEqual(false, result.BooleanValue);
                Assert.AreEqual(null, result.ArrayValue);
                Assert.AreEqual("writePriorityEnumSet.priorityDefault", result.Priority);
                Assert.AreEqual("reliabilityEnumSet.noInput", result.Reliability);
                Assert.AreEqual(false, result.IsReliable);
            }
        }

        [Test]
        public void TestReadPropertyArrayIntegers()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{ \"item\": { \"" + mockAttributeName + "\": [ " +
                "0, 1 ] } }");
                ReadPropertyResult result = traditionalClient.ReadProperty(mockid, mockAttributeName);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}/attributes/{mockAttributeName}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(0, result.NumericValue);
                Assert.AreEqual("Array", result.StringValue);
                Assert.AreEqual(false, result.BooleanValue);
                Assert.AreEqual("0", result.ArrayValue[0].StringValue);
                Assert.AreEqual(0, result.ArrayValue[0].NumericValue);
                Assert.AreEqual(false, result.ArrayValue[0].BooleanValue);
                Assert.AreEqual("1", result.ArrayValue[1].StringValue);
                Assert.AreEqual(1, result.ArrayValue[1].NumericValue);
                Assert.AreEqual(true, result.ArrayValue[1].BooleanValue);
                Assert.AreEqual(null, result.Priority);
                Assert.AreEqual("reliabilityEnumSet.reliable", result.Reliability);
                Assert.AreEqual(true, result.IsReliable);
            }
        }

        [Test]
        public void TestReadPropertyArrayStrings()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{ \"item\": { \"" + mockAttributeName + "\": [ " +
                "\"stringvalue1\", \"stringvalue2\" ] } }");
                ReadPropertyResult result = traditionalClient.ReadProperty(mockid, mockAttributeName);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}/attributes/{mockAttributeName}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(0, result.NumericValue);
                Assert.AreEqual("Array", result.StringValue);
                Assert.AreEqual(false, result.BooleanValue);
                Assert.AreEqual("stringvalue1", result.ArrayValue[0].StringValue);
                Assert.AreEqual(0, result.ArrayValue[0].NumericValue);
                Assert.AreEqual(false, result.ArrayValue[0].BooleanValue);
                Assert.AreEqual("stringvalue2", result.ArrayValue[1].StringValue);
                Assert.AreEqual(0, result.ArrayValue[1].NumericValue);
                Assert.AreEqual(false, result.ArrayValue[1].BooleanValue);
                Assert.AreEqual(null, result.Priority);
                Assert.AreEqual("reliabilityEnumSet.reliable", result.Reliability);
                Assert.AreEqual(true, result.IsReliable);
            }
        }

        [Test]
        public void TestReadPropertyArrayObjectUnsupported()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{ \"item\": { \"" + mockAttributeName + "\": [ " +
                "{ \"item1\": \"stringvalue1\", \"item2\": \"stringvalue2\" }," +
                "{ \"item1\": \"stringvalue3\", \"item2\": \"stringvalue4\" } ] } }");
                ReadPropertyResult result = traditionalClient.ReadProperty(mockid, mockAttributeName);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}/attributes/{mockAttributeName}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(0, result.NumericValue);
                Assert.AreEqual("Array", result.StringValue);
                Assert.AreEqual(false, result.BooleanValue);
                Assert.AreEqual("Unsupported Data Type", result.ArrayValue[0].StringValue);
                Assert.AreEqual(1, result.ArrayValue[0].NumericValue);
                Assert.AreEqual(false, result.ArrayValue[0].BooleanValue);
                Assert.AreEqual("Unsupported Data Type", result.ArrayValue[1].StringValue);
                Assert.AreEqual(1, result.ArrayValue[1].NumericValue);
                Assert.AreEqual(false, result.ArrayValue[1].BooleanValue);
                Assert.AreEqual(null, result.Priority);
                Assert.AreEqual("reliabilityEnumSet.reliable", result.Reliability);
                Assert.AreEqual(true, result.IsReliable);
            }
        }

        [Test]
        public void TestReadPropertyDoesNotExist()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("Not Found", 404);
                try
                {
                    ReadPropertyResult result = traditionalClient.ReadProperty(mockid, mockAttributeName);
                    httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}/attributes/{mockAttributeName}")
                        .WithVerb(HttpMethod.Get)
                        .Times(1);
                }
                catch
                {
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void TestReadPropertyUnsupportedEmptyObject()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{ \"item\": { \"" + mockAttributeName + "\": {}");
                ReadPropertyResult result = traditionalClient.ReadProperty(mockid, mockAttributeName);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}/attributes/{mockAttributeName}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(1, result.NumericValue);
                Assert.AreEqual("Unsupported Data Type", result.StringValue);
                Assert.AreEqual(false, result.BooleanValue);
                Assert.AreEqual(null, result.ArrayValue);
                Assert.AreEqual(null, result.Priority);
                Assert.AreEqual("reliabilityEnumSet.reliable", result.Reliability);
                Assert.AreEqual(true, result.IsReliable);
            }
        }

        [Test]
        public void TestReadPropertyNullClient()
        {
            using (var httpTest = new HttpTest())
            {
                try
                {
                    ReadPropertyResult result = traditionalClient.ReadProperty(mockid, mockAttributeName);
                    httpTest.ShouldNotHaveCalled($"https://hostname/api/V2/objects/{mockid}/attributes/{mockAttributeName}");
                    Assert.AreEqual(1, result.NumericValue);
                    Assert.AreEqual("Unsupported Data Type", result.StringValue);
                    Assert.AreEqual(false, result.BooleanValue);
                    Assert.AreEqual(null, result.ArrayValue);
                    Assert.AreEqual(null, result.Priority);
                    Assert.AreEqual("reliabilityEnumSet.reliable", result.Reliability);
                    Assert.AreEqual(true, result.IsReliable);
                }
                catch
                {
                    Assert.Fail();
                }
            }
        }

        #endregion

        #region ReadPropertyMultiple Tests

        [Test]
        public void TestReadPropertyMultipleEmptyIds()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{ \"item\": { \"" + mockAttributeName + "\": \"stringvalue\" } }");
                List<Guid> ids = new List<Guid>() { };
                List<string> attributes = new List<string>() { mockAttributeName };
                IEnumerable<ReadPropertyResult> results = traditionalClient.ReadPropertyMultiple(ids, attributes);

                Assert.AreEqual(0, results.Count());
            }
        }

        [Test]
        public void TestReadPropertyMultipleEmptyAttribute()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{ \"item\": { \"" + mockAttributeName + "\": \"stringvalue\" } }");
                List<Guid> ids = new List<Guid>() { mockid };
                List<string> attributes = new List<string>() { };
                IEnumerable<ReadPropertyResult> results = traditionalClient.ReadPropertyMultiple(ids, attributes);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(0, results.Count());
            }
        }

        [Test]
        public void TestReadPropertyMultipleOneIdOneAttribute()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{ \"item\": { \"" + mockAttributeName + "\": \"stringvalue\" } }");
                List<Guid> ids = new List<Guid>() { mockid };
                List<string> attributes = new List<string>() { mockAttributeName };
                IEnumerable<ReadPropertyResult> results = traditionalClient.ReadPropertyMultiple(ids, attributes);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(1, results.Count());
            }
        }

        [Test]
        public void TestReadPropertyMultipleTwoIdFiveAttribute()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest
                    .RespondWith("{ \"item\": { \"" + mockAttributeName + "\": \"stringvalue\", " +
                        "\"" + mockAttributeName2 + "\": 23, " +
                        "\"" + mockAttributeName3 + "\": 23.5, " +
                        "\"" + mockAttributeName4 + "\": true, " +
                        "\"" + mockAttributeName5 + "\": [ 1, 2, 3] } }")
                    .RespondWith("{ \"item\": { \"" + mockAttributeName + "\": \"stringvalue\", " +
                        "\"" + mockAttributeName2 + "\": 23, " +
                        "\"" + mockAttributeName3 + "\": 23.5, " +
                        "\"" + mockAttributeName4 + "\": true, " +
                        "\"" + mockAttributeName5 + "\": [ 1, 2, 3] } }");

                List<Guid> ids = new List<Guid>() { mockid, mockid2 };
                List<string> attributes = new List<string>() { mockAttributeName, mockAttributeName2, mockAttributeName3, mockAttributeName4, mockAttributeName5 };
                IEnumerable<ReadPropertyResult> results = traditionalClient.ReadPropertyMultiple(ids, attributes);

                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid2}")
                    .WithVerb(HttpMethod.Get)
                    .Times(1);
                Assert.AreEqual(results.Count(), 10);
                foreach (var result in results)
                {
                    Assert.AreNotEqual("Unsupported Data Type", result.StringValue);
                }
            }
        }

        [Test]
        public void TestReadPropertyMultipleOneIdOneAttributeDNE()
        {
            using (var httpTest = new HttpTest())
            {
                httpTest.RespondWithJson(new { accessToken = "faketoken", expires = "2030-01-01T00:00:00Z" });
                traditionalClient.TryLogin("username", "password", "hostname");

                httpTest.RespondWith("{ \"item\": { \"attributeNoMatch\": \"stringvalue\" } }");
                List<Guid> ids = new List<Guid>() { mockid };
                List<string> attributes = new List<string>() { mockAttributeName };

                try
                {
                    IEnumerable<ReadPropertyResult> results = traditionalClient.ReadPropertyMultiple(ids, attributes);

                    httpTest.ShouldHaveCalled($"https://hostname/api/V2/objects/{mockid}")
                        .WithVerb(HttpMethod.Get)
                        .Times(1);
                    Assert.AreEqual(1, results.Count());
                }
                catch
                {
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void TestReadPropertyMultipleNullClient()
        {
            using (var httpTest = new HttpTest())
            {
                try
                {
                    List<Guid> ids = new List<Guid>() { mockid };
                    List<string> attributes = new List<string>() { mockAttributeName };
                    IEnumerable<ReadPropertyResult> results = traditionalClient.ReadPropertyMultiple(ids, attributes);

                    httpTest.ShouldNotHaveCalled($"https://hostname/api/V2/objects/{mockid}");
                    Assert.AreEqual(null, results);
                }
                catch
                {
                    Assert.Fail();
                }
            }
        }

        #endregion
    }
}
