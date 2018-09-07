using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FakeHttpService
{
    public static class FakeHttpServiceExtensions
    {
        public static FakeHttpService WithContentAt(this FakeHttpService subject, string relativeUri, string content)
        {
            subject
                .OnRequest(r => r.GetUri().ToString().EndsWith(relativeUri))
                .RespondWith(async r =>
                {
                    await r.Body.WriteTextAsUtf8BytesAsync(content);
                });

            return subject;
        }

        public static FakeHttpService Responds(
            this FakeHttpService fakeHttpService,
            Func<HttpRequest, bool> when,
            Func<HttpResponse, Task> respondWith)
        {
            fakeHttpService.AddHandler(new RequestHandler(when, respondWith));
            return fakeHttpService;
        }
    }
}
