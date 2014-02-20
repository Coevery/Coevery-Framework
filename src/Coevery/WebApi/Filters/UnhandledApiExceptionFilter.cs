using System.Web.Http.Filters;
using Coevery.Logging;

namespace Coevery.WebApi.Filters {
    public class UnhandledApiExceptionFilter : ExceptionFilterAttribute, IApiFilterProvider {
        public UnhandledApiExceptionFilter() {
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public override void OnException(HttpActionExecutedContext actionExecutedContext) {
            Logger.Error(actionExecutedContext.Exception, "Unexpected API exception");
        }
    }
}
