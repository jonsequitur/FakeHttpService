using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FakeHttpService
{
    internal class RequestHandler
    {
        private readonly Func<HttpResponse, Task> _response;

        private readonly Func<HttpRequest, bool> _matches;

        public RequestHandler(Expression<Func<HttpRequest, bool>> expression, Func<HttpResponse, Task> response)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            _response = response ?? throw new ArgumentNullException(nameof(response));
            Description = expression.ToString();
            _matches = expression.Compile();
        }

        public RequestHandler(Func<HttpRequest, bool> match, Func<HttpResponse, Task> response)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            _response = response ?? throw new ArgumentNullException(nameof(response));
            Description = match.ToString();
            _matches = match;
        }

        public string Description { get; }

        public bool Matches(HttpRequest contextRequest)
        {
            var matches = _matches(contextRequest);

            if (matches)
            {
                HasBeenMatched = true;
            }

            return matches;
        }

        public bool HasBeenMatched { get; private set; }

        public Task Respond(HttpResponse context) =>
            _response(context);
    }
}
