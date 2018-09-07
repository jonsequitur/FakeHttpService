using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Pocket;
using static Pocket.Logger<FakeHttpService.FakeHttpService>;

namespace FakeHttpService
{
    public class FakeHttpService : IDisposable
    {
        private readonly bool _serviceIdIsUserSpecified;
        private readonly IWebHost _host;

        private readonly ConcurrentBag<RequestHandler> _handlers = new ConcurrentBag<RequestHandler>();

        private readonly bool _throwOnUnusedHandlers;

        public FakeHttpService(
            string serviceId = null,
            bool throwOnUnusedHandlers = false)
        {
            _throwOnUnusedHandlers = throwOnUnusedHandlers;
            ServiceId = serviceId ?? Guid.NewGuid().ToString();

            _serviceIdIsUserSpecified = serviceId != null;

            FakeHttpServiceRepository.Register(this);

            var config = new ConfigurationBuilder().Build();

            var builder = new WebHostBuilder()
                .UseConfiguration(config)
                 .UseKestrel()
                 .UseStartup<Startup>()
                 .UseSetting("applicationName", ServiceId)
                 .UseUrls("http://127.0.0.1:0");

            _host = builder.Build();

            _host.Start();

            BaseAddress = new Uri(_host
                                      .ServerFeatures.Get<IServerAddressesFeature>()
                                      .Addresses.First());
        }

        internal FakeHttpService AddHandler(RequestHandler requestHandler)
        {
            if (requestHandler == null)
            {
                throw new ArgumentNullException(nameof(requestHandler));
            }

            _handlers.Add(requestHandler);

            Log.Info("Setting up condition {condition}",
                     requestHandler.Description);

            return this;
        }

        public ResponseBuilder OnRequest(Expression<Func<HttpRequest, bool>> condition)
        {
            return new ResponseBuilder(this, condition);
        }

        public FakeHttpService FailOnUnexpectedRequest()
        {
            var rb = new ResponseBuilder(this, new Func<HttpRequest, bool>(_ => true));
            return rb.Fail();
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                foreach (var handler in _handlers)
                {
                    if (!handler.Matches(context.Request))
                    {
                        continue;
                    }
                    
                    await handler.Respond(context.Response);

                    return;
                }

                context.Response.StatusCode = 404;

                Log.Warning($"No handler for request: {context.Request.Method} {context.Request.Path}");
            }
            catch (Exception e)
            {
                context.Response.StatusCode = 500;

                await context.Response.WriteAsync(e.ToString());
            }
        }

        public Uri BaseAddress { get; }

        public string ServiceId { get; }

        public void Dispose()
        {
            Task.Run(() => _host.Dispose()).Wait();

            FakeHttpServiceRepository.Unregister(this);

            if (_throwOnUnusedHandlers)
            {
                var requestHandlers = _handlers
                    .Where(h => !h.HasBeenMatched)
                    .ToArray();

                if (!requestHandlers.Any())
                {
                    return;
                }

                var unusedHandlerSummary = requestHandlers
                    .Select(h => h.Description)
                    .Aggregate((c, n) => $"{c}{Environment.NewLine}{n}");

                throw new InvalidOperationException(
                    $@"{GetType().Name} {ToString()} expected requests
{unusedHandlerSummary}
but they were not made.");
            }
        }

        public override string ToString() =>
            _serviceIdIsUserSpecified
                ? $"\"{ServiceId}\" @ {BaseAddress}"
                : $"@ {BaseAddress}";
    }
}