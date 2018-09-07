using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FakeHttpService
{
    public class ResponseBuilder
    {
        private readonly FakeHttpService _fakeHttpService;

        private readonly Expression<Func<HttpRequest, bool>> _requestValidatorExpression;

        private readonly Func<HttpRequest, bool> _requestValidatorFunc;

        internal ResponseBuilder(FakeHttpService fakeHttpService, Expression<Func<HttpRequest, bool>> requestValidator)
        {
            _requestValidatorExpression = requestValidator;

            _fakeHttpService = fakeHttpService;
        }

        internal ResponseBuilder(FakeHttpService fakeHttpService, Func<HttpRequest, bool> requestValidator)
        {
            _requestValidatorFunc = requestValidator;

            _fakeHttpService = fakeHttpService;
        }

        public FakeHttpService RespondWith(Func<HttpResponse, Task> responseConfiguration)
        {
            if (responseConfiguration == null)
                throw new ArgumentNullException(nameof(responseConfiguration));

            async Task ResponseFunction(HttpResponse c)
            {
                await responseConfiguration(c);
            }

            var handler = _requestValidatorFunc == null
                              ? new RequestHandler(_requestValidatorExpression, ResponseFunction)
                              : new RequestHandler(_requestValidatorFunc, ResponseFunction);

            _fakeHttpService.AddHandler(handler);

            return _fakeHttpService;
        }

        public FakeHttpService RespondWith(Func<HttpResponse, Uri, Task> responseConfiguration)
        {
            if (responseConfiguration == null)
                throw new ArgumentNullException(nameof(responseConfiguration));

            async Task ResponseFunction(HttpResponse c)
            {
                await responseConfiguration(c, _fakeHttpService.BaseAddress);
            }

            var handler = _requestValidatorFunc == null
                              ? new RequestHandler(_requestValidatorExpression, ResponseFunction)
                              : new RequestHandler(_requestValidatorFunc, ResponseFunction);

            _fakeHttpService.AddHandler(handler);

            return _fakeHttpService;
        }

        public FakeHttpService Succeed()
        {
            return RespondWith(async r =>
            {
                r.StatusCode = 200;
                await Task.Yield();
            });
        }

        public FakeHttpService Fail()
        {
            return RespondWith(async r =>
            {
                r.StatusCode = 500;
                await Task.Yield();
            });
        }
    }
}
