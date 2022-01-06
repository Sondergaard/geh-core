﻿// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Energinet.DataHub.Core.Logging.RequestResponseMiddleware
{
    public class RequestResponseLoggingMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly IRequestResponseLogging _requestResponseLogging;

        public RequestResponseLoggingMiddleware(IRequestResponseLogging requestResponseLogging)
        {
            _requestResponseLogging = requestResponseLogging;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var requestLogInformation = await BuildRequestLogInformationAsync(context);
            await LogRequestAsync(requestLogInformation).ConfigureAwait(false);

            await next(context).ConfigureAwait(false);

            var responseLogInformation = await BuildResponseLogInformationAsync(context);
            await LogResponseAsync(responseLogInformation, requestLogInformation.MetaData).ConfigureAwait(false);
        }

        private Task LogRequestAsync(LogInformation requestLogInformation)
        {
            var requestLogName = LogDataBuilder.BuildLogName(requestLogInformation.MetaData) + " request";
            return _requestResponseLogging.LogRequestAsync(requestLogInformation.LogStream, requestLogInformation.MetaData, requestLogInformation.IndexTags, requestLogName);
        }

        private Task LogResponseAsync(LogInformation responseLogInformation, Dictionary<string, string> requestMetaData)
        {
            var responseLogName = LogDataBuilder.BuildLogName(requestMetaData) + " response";
            return _requestResponseLogging.LogResponseAsync(responseLogInformation.LogStream, responseLogInformation.MetaData, responseLogInformation.IndexTags, responseLogName);
        }

        private static async Task<LogInformation> BuildRequestLogInformationAsync(FunctionContext context)
        {
            var (metaData, indexTags) = GetMetaDataAndIndexTagsDictionaries(context, true);

            if (context.GetHttpRequestData() is { } requestData)
            {
                foreach (var (key, value) in LogDataBuilder.ReadHeaderDataFromCollection(requestData.Headers))
                {
                    metaData.TryAdd(LogDataBuilder.MetaNameFormatter(key), value);
                }

                var streamToLog = new MemoryStream();
                await requestData.Body.CopyToAsync(streamToLog);
                requestData.Body.Position = 0;
                streamToLog.Position = 0;

                return new LogInformation(streamToLog, metaData, indexTags);
            }

            return new LogInformation(Stream.Null, metaData, indexTags);
        }

        private static async Task<LogInformation> BuildResponseLogInformationAsync(FunctionContext context)
        {
            var (metaData, indexTags) = GetMetaDataAndIndexTagsDictionaries(context, false);

            if (context.GetHttpResponseData() is { } responseData)
            {
                foreach (var (key, value) in LogDataBuilder.ReadHeaderDataFromCollection(responseData.Headers))
                {
                    metaData.TryAdd(LogDataBuilder.MetaNameFormatter(key), value);
                }

                metaData.TryAdd(LogDataBuilder.MetaNameFormatter("StatusCode"), responseData.StatusCode.ToString());
                indexTags.TryAdd(LogDataBuilder.MetaNameFormatter("StatusCode"), responseData.StatusCode.ToString());

                var streamToLog = new MemoryStream();
                await responseData.Body.CopyToAsync(streamToLog);
                responseData.Body.Position = 0;
                streamToLog.Position = 0;

                return new LogInformation(streamToLog, metaData, indexTags);
            }

            return new LogInformation(Stream.Null, metaData, indexTags);
        }

        private static (Dictionary<string, string> MetaData, Dictionary<string, string> IndexTags) GetMetaDataAndIndexTagsDictionaries(FunctionContext context, bool isRequest)
        {
            var metaData = context.BindingContext.BindingData
                .ToDictionary(e => LogDataBuilder.MetaNameFormatter(e.Key), pair => pair.Value as string ?? string.Empty);

            var indexTags =
                new Dictionary<string, string>(metaData.Where(e => e.Key != "headers" && e.Key != "query").Take(5));

            metaData.TryAdd(LogDataBuilder.MetaNameFormatter("FunctionId"), context.FunctionId);
            metaData.TryAdd(LogDataBuilder.MetaNameFormatter("FunctionName"), context.FunctionDefinition.Name);
            metaData.TryAdd(LogDataBuilder.MetaNameFormatter("InvocationId"), context.InvocationId);
            metaData.TryAdd(LogDataBuilder.MetaNameFormatter("TraceContext"), context.TraceContext?.TraceParent ?? string.Empty);
            metaData.TryAdd(LogDataBuilder.MetaNameFormatter("HttpDataType"), isRequest ? "request" : "response");

            indexTags.TryAdd(LogDataBuilder.MetaNameFormatter("FunctionName"), context.FunctionDefinition.Name);
            indexTags.TryAdd(LogDataBuilder.MetaNameFormatter("InvocationId"), context.InvocationId);
            indexTags.TryAdd(LogDataBuilder.MetaNameFormatter("TraceContext"), context.TraceContext?.TraceParent ?? string.Empty);
            indexTags.TryAdd(LogDataBuilder.MetaNameFormatter("HttpDataType"), isRequest ? "request" : "response");

            return (metaData, indexTags);
        }
    }
}