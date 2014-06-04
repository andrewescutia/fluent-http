﻿using System;
using System.Net;
using System.Net.Http;

using AonWeb.FluentHttp.HAL;
using AonWeb.FluentHttp.HAL.Representations;
using AonWeb.FluentHttp.Handlers;

namespace AonWeb.FluentHttp.Mocks
{
    public class MockHalCallBuilder<TResult, TContent, TError> : 
        HalCallBuilder<TResult, TContent, TError>,
        IMockBuilder<TResult, TContent, TError>
        where TResult : IHalResource
        where TContent : IHalRequest
    {
        private readonly MockHttpCallBuilder<TResult, TContent, TError> _innerBuilder;

        protected MockHalCallBuilder()
            : this(MockHttpCallBuilder<TResult, TContent, TError>.CreateMock())
        { }

        private MockHalCallBuilder(MockHttpCallBuilder<TResult, TContent, TError> builder)
            : base(builder)
        {
            _innerBuilder = builder;
        }

        public static MockHalCallBuilder<TResult, TContent, TError> CreateMock()
        {
            return new MockHalCallBuilder<TResult, TContent, TError>();
        }

        public IMockBuilder<TResult, TContent, TError> WithResult(Func<HttpResponseMessage, HttpCallContext<TResult, TContent, TError>, TResult> resultFactory)
        {
            _innerBuilder.WithResult(resultFactory);

            return this;
        }

        public IMockBuilder<TResult, TContent, TError> WithResult(TResult result)
        {
            return WithResult(result, HttpStatusCode.OK);
        }

        public IMockBuilder<TResult, TContent, TError> WithResult(TResult result, HttpStatusCode statusCode)
        {
            _innerBuilder.WithResult((r, c) => result);

            return WithResponse(new ResponseInfo(statusCode));
        }

        public IMockBuilder<TResult, TContent, TError> WithError(Func<HttpResponseMessage, HttpCallContext<TResult, TContent, TError>, TError> errorFactory)
        {
            _innerBuilder.WithError(errorFactory);

            return this;
        }

        public IMockBuilder<TResult, TContent, TError> WithError(TError error)
        {
            return WithError(error, HttpStatusCode.InternalServerError);
        }

        public IMockBuilder<TResult, TContent, TError> WithError(TError error, HttpStatusCode statusCode)
        {
            _innerBuilder.WithError((r, c) => error);

            return WithResponse(new ResponseInfo(statusCode));
        }

        public IMockBuilder<TResult, TContent, TError> WithResponse(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _innerBuilder.WithResponse(responseFactory);

            return this;
        }

        public IMockBuilder<TResult, TContent, TError> WithResponse(HttpResponseMessage response)
        {
            return WithResponse(r => response);
        }

        public IMockBuilder<TResult, TContent, TError> WithResponse(ResponseInfo response)
        {
            return WithResponse(r => response.ToHttpResponseMessage());
        }
    }
}