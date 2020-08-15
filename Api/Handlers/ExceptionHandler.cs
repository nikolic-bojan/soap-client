using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Handlers
{
    public class ExceptionHandler
    {
        private const string ApplicationProblemJson = "application/problem+json";

        public static RequestDelegate CreateExceptionHandler(IWebHostEnvironment env)
        {
            return async context =>
            {
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                string title = exceptionHandlerFeature.Error.Message;
                string type = exceptionHandlerFeature.Error.GetType().FullName ?? string.Empty;
                int statusCode = 500;
                string? details = env.IsDevelopment() ? exceptionHandlerFeature.Error.ToString() : null;

                var state = new Dictionary<string, object>();
                var logger = context.RequestServices.GetRequiredService<ILogger<ExceptionHandler>>();

                var problem = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = details,
                    Type = type
                };

                switch (exceptionHandlerFeature.Error)
                {
                    case ValidationException ve:
                        statusCode = 400;
                        var validationProblemDetails = new ValidationProblemDetails();
                        validationProblemDetails.Type = ve.GetType().Name; // or just use ValidationException constant

                        if (ve.ValidationResult is ValidationResult failure)
                        {
                            validationProblemDetails.Errors.Add(failure.MemberNames.FirstOrDefault() ?? "_", new[] { failure.ErrorMessage });

                            state.Add("ValidationErrors", JsonSerializer.Serialize(validationProblemDetails.Errors));
                        }

                        using (logger.BeginScope(state))
                        {
                            logger.LogInformation("Validation problem with status {response_status}", 400);
                        }

                        problem = validationProblemDetails;
                        break;

                    case ServiceException se:
                        state.Add("ServiceException", se.InnerException?.GetType().FullName ?? string.Empty);
                        state.Add("ServiceMessage", se.InnerException?.Message ?? string.Empty);

                        using (logger.BeginScope(state))
                        {
                            logger.LogError(se, "External Service returned error");
                        }
                        break;

                    default:
                        logger.LogError(exceptionHandlerFeature.Error, "Unexpected Exception");
                        break;
                }

                var traceId = Activity.Current?.TraceId.ToString() ?? context?.TraceIdentifier;
                if (traceId != null)
                {
                    problem.Extensions["traceId"] = traceId;
                }

                context!.Response.StatusCode = statusCode;
                context.Response.ContentType = ApplicationProblemJson;
                var stream = context.Response.Body;
                await JsonSerializer.SerializeAsync(stream, problem, problem.GetType()).ConfigureAwait(false);
            };
        }
    }
}
