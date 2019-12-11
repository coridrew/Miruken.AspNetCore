namespace Miruken.AspNetCore.Tests
{
    using System;
    using System.Threading.Tasks;
    using Api;
    using Api.Route;
    using Callback;
    using Callback.Policy;
    using Functional;
    using Http;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Register;
    using Validate;
    using ServiceCollection = Microsoft.Extensions.DependencyInjection.ServiceCollection;

#if NETSTANDARD2_0
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Mvc;
#endif

#if NETSTANDARD2_1
    using Microsoft.Extensions.Hosting;
#endif

    [TestClass]
    public class HttpRouteControllerTests
    {
        private TestServer _server;
        protected IHandler _handler;

        [TestInitialize]
        public void TestInitialize()
        {
            _server = CreateTestServer();

            _handler = new ServiceCollection()
                .AddSingleton(_server)
                .AddSingleton<TestServerClientHandler>()
                .AddMiruken(configure => configure
                    .PublicSources(s => s.FromAssemblyOf<HttpRouteControllerTests>())
                    .WithHttp(http => http.AddHttpMessageHandler<TestServerClientHandler>())
                ).Build(HandlerDescriptorFactory.Current);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _server?.Dispose();
        }

        protected virtual TestServer CreateTestServer()
        {
#if NETSTANDARD2_0
            var builder = WebHost.CreateDefaultBuilder().UseStartup<Startup>();
            return new TestServer(builder);
#elif NETSTANDARD2_1
            var host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                    webBuilder.UseStartup<Startup>().UseTestServer())
                .UseServiceProviderFactory(new MirukenServiceProviderFactory())
                .Build();
            host.RunAsync();
            return host.GetTestServer();
#endif
        }

        private class Startup
        {
#if NETSTANDARD2_0
            public IServiceProvider ConfigureServices(IServiceCollection services)
            {
                services.AddMvc()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

                return services.AddMiruken(configure => configure
                    .Sources(sources => sources.FromAssemblyOf<Startup>())
                    .WithAspNet(options => options.AddControllers())
                    .WithValidation())
                    .Build();
            }
#elif NETSTANDARD2_1
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddMvcCore();

                services.AddMiruken(configure => configure
                    .Sources(sources => sources.FromAssemblyOf<Startup>())
                    .WithAspNet(options => options.AddControllers())
                    .WithValidation());
            }
#endif

            public void Configure(IApplicationBuilder app)
            {
#if NETSTANDARD2_0
                app.UseMvc();
#elif NETSTANDARD2_1
                app.UseRouting()
                   .UseEndpoints(endpoints => endpoints.MapControllers());
#endif
            }
        }
        
        [TestMethod]
        public async Task Should_Route_Requests()
        {
            var player = new Player
            {
                Name = "Philippe Coutinho"
            };
            var response = await _handler
                .Send(new CreatePlayer { Player = player }
                    .RouteTo(_server.BaseAddress.AbsoluteUri));
            Assert.AreEqual("Philippe Coutinho", response.Player.Name);
            Assert.IsTrue(response.Player.Id > 0);
        }

        [TestMethod,
         ExpectedException(typeof(NotFoundException))]
        public async Task Should_Fail_Unhandled_Requests()
        {
            var player = new Player
            {
                Id   = 1,
                Name = "Philippe Coutinho"
            };
            await _handler
                .Send(new RemovePlayer { PlayerId = player.Id }
                    .RouteTo(_server.BaseAddress.AbsoluteUri));
        }

        [TestMethod]
        public async Task Should_Fail_Validation_Rules()
        {
            try
            {
                await _handler.Send(new CreatePlayer()
                    .RouteTo(_server.BaseAddress.AbsoluteUri));
                Assert.Fail("Should have failed");
            }
            catch (ValidationException vex)
            {
                var outcome = vex.Outcome;
                Assert.IsNotNull(outcome);
                CollectionAssert.AreEqual(new[] { "Player" }, outcome.Culprits);
                Assert.AreEqual("'Player' must not be empty.", outcome["Player"]);
            }
        }

        [TestMethod,
         ExpectedException(typeof(NotSupportedException))]
        public async Task Should_Reject_Invalid_Route()
        {
            await _handler.Send(new CreatePlayer()
                .RouteTo("abc://localhost:9000"));
        }

        [TestMethod]
        public async Task Should_Batch_Single_Request()
        {
            var player = new Player
            {
                Name = "Paul Pogba"
            };
            var results = await _handler.Batch(batch =>
            {
                batch.Send(new CreatePlayer { Player = player }
                        .RouteTo(_server.BaseAddress.AbsoluteUri))
                    .Then((response, s) =>
                    {
                        Assert.AreEqual("Paul Pogba", response.Player.Name);
                        Assert.IsTrue(response.Player.Id > 0);
                    });
            });
            Assert.AreEqual(1, results.Length);
            var groups = (object[])results[0];
            Assert.AreEqual(1, groups.Length);
            var (url, group) = (Tuple<string, object[]>)groups[0];
            Assert.AreEqual(_server.BaseAddress.AbsoluteUri, url);
            Assert.AreEqual(1, group.Length);
        }

        [TestMethod]
        public async Task Should_Batch_Requests()
        {
            var player1 = new Player
            {
                Name = "Paul Pogba"
            };
            var player2 = new Player
            {
                Name = "Eden Hazard"
            };
            var results = await _handler.Batch(batch =>
            {
                batch.Send(new CreatePlayer { Player = player1 }
                        .RouteTo(_server.BaseAddress.AbsoluteUri))
                    .Then((response, s) =>
                    {
                        Assert.AreEqual("Paul Pogba", response.Player.Name);
                        Assert.IsTrue(response.Player.Id > 0);
                    });
                batch.Send(new CreatePlayer { Player = player2 }
                        .RouteTo(_server.BaseAddress.AbsoluteUri))
                    .Then((response, s) =>
                    {
                        Assert.AreEqual("Eden Hazard", response.Player.Name);
                        Assert.IsTrue(response.Player.Id > 0);
                    });
            });
            Assert.AreEqual(1, results.Length);
            var groups = (object[])results[0];
            Assert.AreEqual(1, groups.Length);
            var (url, group) = (Tuple<string, object[]>)groups[0];
            Assert.AreEqual(_server.BaseAddress.AbsoluteUri, url);
            Assert.AreEqual(2, group.Length);
        }

        [TestMethod]
        public async Task Should_Not_Batch_Awaited_Requests()
        {
            var player1 = new Player
            {
                Name = "Paul Pogba"
            };
            var player2 = new Player
            {
                Name = "Eden Hazard"
            };
            await _handler.Batch(async batch =>
            {
                var response = await batch.Send(new CreatePlayer { Player = player1 }
                    .RouteTo(_server.BaseAddress.AbsoluteUri));
                Assert.AreEqual("Paul Pogba", response.Player.Name);
                Assert.IsTrue(response.Player.Id > 0);
                await batch.Send(new CreatePlayer { Player = player2 }
                        .RouteTo(_server.BaseAddress.AbsoluteUri))
                    .Then((resp, s) =>
                    {
                        Assert.AreEqual("Eden Hazard", resp.Player.Name);
                        Assert.IsTrue(response.Player.Id > 0);
                    });
            });
        }

        [TestMethod]
        public async Task Should_Batch_Publications()
        {
            var player1 = new Player
            {
                Name = "Paul Pogba"
            };
            var player2 = new Player
            {
                Id   = 1,
                Name = "Eden Hazard"
            };
            var results = await _handler.Batch(batch =>
            {
                batch.Publish(new PlayerCreated { Player = player1 }
                    .RouteTo(_server.BaseAddress.AbsoluteUri));
                batch.Publish(new PlayerUpdated { Player = player2 }
                    .RouteTo(_server.BaseAddress.AbsoluteUri));
            });
            Assert.AreEqual(1, results.Length);
            var groups = (object[])results[0];
            Assert.AreEqual(1, groups.Length);
            var (url, group) = (Tuple<string, object[]>)groups[0];
            Assert.AreEqual(_server.BaseAddress.AbsoluteUri, url);
            Assert.AreEqual(2, group.Length);
        }

        [TestMethod]
        public async Task Should_Propagate_Failure()
        {
            var results = await _handler.Batch(batch =>
                batch.Send(new CreatePlayer { Player = new Player() }
                        .RouteTo(_server.BaseAddress.AbsoluteUri))
                    .Then((_, s) => Assert.Fail("Should have failed"))
                    .Catch((ValidationException vex, bool s) =>
                    {
                        var outcome = vex.Outcome;
                        Assert.IsNotNull(outcome);
                        CollectionAssert.AreEqual(new[] { "Player" }, outcome.Culprits);
                        Assert.AreEqual("'Player. Name' must not be empty.", outcome["Player.Name"]);
                    })
                    .Catch((ex, s) => Assert.Fail("Unexpected exception")));
            Assert.AreEqual(1, results.Length);
            var groups = (object[])results[0];
            Assert.AreEqual(1, groups.Length);
            var (url, group) = (Tuple<string, object[]>)groups[0];
            Assert.AreEqual(_server.BaseAddress.AbsoluteUri, url);
            Assert.AreEqual(1, group.Length);
        }

        [TestMethod]
        public async Task Should_Propagate_Multiple_Failures()
        {
            var results = await _handler.Batch(batch =>
            {
                batch.Send(new CreatePlayer { Player = new Player() }
                    .RouteTo(_server.BaseAddress.AbsoluteUri))
                    .Catch((ValidationException vex, bool s) =>
                    {
                        var outcome = vex.Outcome;
                        Assert.IsNotNull(outcome);
                        CollectionAssert.AreEqual(new[] { "Player" }, outcome.Culprits);
                        Assert.AreEqual("'Player. Name' must not be empty.", outcome["Player.Name"]);
                    })
                    .Catch((ex, s) => Assert.Fail("Unexpected exception"));
                batch.Send(new CreatePlayer
                    {
                        Player = new Player
                        {
                            Id   = 3,
                            Name = "Sergio Ramos"
                        }
                    }
                    .RouteTo(_server.BaseAddress.AbsoluteUri))
                    .Catch((ValidationException vex, bool s) =>
                    {
                        var outcome = vex.Outcome;
                        Assert.IsNotNull(outcome);
                        CollectionAssert.AreEqual(new[] { "Player" }, outcome.Culprits);
                        Assert.AreEqual("'Player. Id' should be equal to '0'.", outcome["Player.Id"]);
                    })
                    .Catch((ex, s) => Assert.Fail("Unexpected exception"));
            });
            Assert.AreEqual(1, results.Length);
            var groups = (object[])results[0];
            Assert.AreEqual(1, groups.Length);
            var (url, group) = (Tuple<string, object[]>)groups[0];
            Assert.AreEqual(_server.BaseAddress.AbsoluteUri, url);
            Assert.AreEqual(2, group.Length);
        }

        [TestMethod]
        public async Task Should_Propagate_Format_Errors()
        {
            var response = await _handler
                .Formatters(HttpFormatters.Route)
                .HttpPost<string, Try<Message, Message>>(
                    @"{
                       'payload': {
                           '$type': 'Miruken.AspNetCore.Tests.CreatePlayer, Miruken.AspNetCore.Tests',
                           'player': {
                              'id':   'ABC',
                              'name': 'Namee3c27ad6-b812-46c1-9b60-d99c9d3e7ba6',
                                  'person': {
                                  'dob': 'XYZ'
                              }
                           }
                        }
                    }", _server.BaseAddress.AbsoluteUri + "Process",
                    HttpFormatters.Route);
            response.Match(error =>
            {
                var errors = (ValidationErrors[])error.Payload;
                Assert.AreEqual(1, errors.Length);
            }, success => { Assert.Fail("Should have failed"); });
        }
    }
}

