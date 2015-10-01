﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AonWeb.FluentHttp.Exceptions;
using AonWeb.FluentHttp.Mocks;
using AonWeb.FluentHttp.Mocks.WebServer;
using AonWeb.FluentHttp.Tests.Helpers;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AonWeb.FluentHttp.Tests
{
    [Collection("LocalWebServer Tests")]
    public class AdvancedTypedBuilderExtensionsTests
    {
        private static readonly Uri MockUri = new Uri("http://testsite.com");
        private readonly ITestOutputHelper _logger;

        public AdvancedTypedBuilderExtensionsTests(ITestOutputHelper logger)
        {
            _logger = logger;
            Defaults.Caching.Enabled = false;
            Cache.Clear();
        }
        #region WithMethod

        [Fact]
        public async Task WithMethod_WhenValidString_ExpectResultUsesMethod()
        {
            using (var server = LocalWebServer.ListenInBackground(new XUnitMockLogger(_logger)))
            {
                //arrange
                var method = HttpMethod.Get;
                var builder = new TypedBuilderFactory().Create().WithUri(server.ListeningUri).Advanced.WithMethod(method);

                HttpMethod actual = null;
                server.WithRequestInspector(r => actual = r.Method);

                //act
                await builder.SendAsync();

                method.ShouldBe(actual);
            }
        }

        [Fact]
        // await Should.ThrowAsync<TypeMismatchException>(async () => );
        public void WithMethod_WhenNullString_ExpectException()
        {
            //arrange
            string method = null;
            var builder = new TypedBuilderFactory().Create();

            //act
            Should.Throw<ArgumentException>(() => builder.Advanced.WithMethod(method));
        }

        [Fact]
        // await Should.ThrowAsync<TypeMismatchException>(async () => );
        public void WithMethod_WhenEmptyString_ExpectException()
        {
            //arrange
            var method = string.Empty;
            var builder = new TypedBuilderFactory().Create();

            //act
            Should.Throw<ArgumentException>(() => builder.Advanced.WithMethod(method));
        }

        [Fact]
        public async Task WithMethod_WhenValidMethod_ExpectResultUsesMethod()
        {
            using (var server = LocalWebServer.ListenInBackground(new XUnitMockLogger(_logger)))
            {
                //arrange
                var method = HttpMethod.Get;
                var builder = new TypedBuilderFactory().Create().WithUri(server.ListeningUri).Advanced.WithMethod(method);

                HttpMethod actual = null;
                server.WithRequestInspector(r => actual = r.Method);

                //act
                await builder.SendAsync();

                method.ShouldBe(actual);
            }
        }

        [Fact]
        // await Should.ThrowAsync<TypeMismatchException>(async () => );
        public void WithMethod_WhenNullMethod_ExpectException()
        {
            //arrange
            HttpMethod method = null;

            //act
            Should.Throw<ArgumentException>(() => new TypedBuilderFactory().Create().Advanced.WithMethod(method));
        }

        [Fact]
        public async Task WithMethod_WhenCalledMultipleTimes_ExpectLastWins()
        {
            using (var server = LocalWebServer.ListenInBackground(new XUnitMockLogger(_logger)))
            {
                //arrange
                var method1 = HttpMethod.Post;
                var method2 = HttpMethod.Get;
                var builder = new TypedBuilderFactory().Create()
                    .WithUri(server.ListeningUri)
                    .Advanced
                        .WithMethod(method1)
                        .WithMethod(method2);

                HttpMethod actual = null;
                server.WithRequestInspector(r => actual = r.Method);

                //act
                await builder.SendAsync();

                method2.ShouldBe(actual);
            }
        }

        #endregion

        #region Client Configuration

        [Fact]
        public async Task WithClientConfiguration_WhenAction_ExpectConfigurationApplied()
        {
            using (var server = LocalWebServer.ListenInBackground(new XUnitMockLogger(_logger)))
            {
                var expected = "GoogleBot/1.0";
                string actual = null;
                server.WithRequestInspector(r => actual = r.Headers.UserAgent.First().ToString());

                //act
                await new TypedBuilderFactory().Create().WithUri(server.ListeningUri)
                    .Advanced
                    .WithClientConfiguration(b =>
                        b.WithHeadersConfiguration(h =>
                            h.UserAgent.Add(new ProductInfoHeaderValue("GoogleBot", "1.0"))))
                        .SendAsync();

                actual.ShouldBe(expected);

            }
        }

        #endregion

        #region Timeout & Cancellation

        [Fact]
        // await Should.ThrowAsync<TypeMismatchException>(async () => );
        public async Task CancelRequest_WhenSuppressCancelOff_ExpectException()
        {
            //arrange

            using (var server = LocalWebServer.ListenInBackground(new XUnitMockLogger(_logger)))
            {
                var uri = server.ListeningUri;
                var delay = 500;
                var builder = new TypedBuilderFactory().Create().WithUri(uri);
                server.WithRequestInspector(r => Task.Delay(delay));
                Exception exception = null;

                // act
                try
                {
                    var task = builder.Advanced.WithSuppressCancellationExceptions(false).SendAsync();

                    builder.CancelRequest();

                    await task;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    exception.ShouldNotBeNull();
                    exception.ShouldBeOfType<TaskCanceledException>();
                }
            }
        }

        [Fact]
        public async Task WithTimeout_WithLongCall_ExpectTimeoutBeforeCompletionWithNoException()
        {
            //arrange

            using (var server = LocalWebServer.ListenInBackground(new XUnitMockLogger(_logger)))
            {
                var uri = server.ListeningUri;
                var delay = 1000;
                var builder = new TypedBuilderFactory().Create().WithUri(uri);
                server.WithRequestInspector(r => Task.Delay(delay));

                // act
                var watch = new Stopwatch();
                watch.Start();

                await builder.Advanced
                    .WithSuppressCancellationExceptions(true)
                    .WithTimeout(TimeSpan.FromMilliseconds(100))
                    .SendAsync();

                // assert
                watch.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(100);
                watch.ElapsedMilliseconds.ShouldBeLessThan(delay);
            }
        }

        [Fact]
        // await Should.ThrowAsync<TypeMismatchException>(async () => );
        public async Task WithTimeout_WithLongCallAndSuppressCancelFalse_ExpectException()
        {
            //arrange
            using (var server = LocalWebServer.ListenInBackground(new XUnitMockLogger(_logger)))
            {
                var uri = server.ListeningUri;
                var delay = 10000;
                var builder = new TypedBuilderFactory().Create().WithUri(uri);
                server.WithRequestInspector(r => Task.Delay(delay));
                Exception exception = null;

                // act
                try
                {
                    await builder.Advanced.WithTimeout(TimeSpan.FromMilliseconds(100)).WithSuppressCancellationExceptions(false).SendAsync();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    exception.ShouldNotBeNull();
                    exception.ShouldBeOfType<TaskCanceledException>();
                }
            }
        }

        [Fact]
        public async Task WithTimeout_WithLongCallAndExceptionHandler_ExpectExceptionHandlerCalled()
        {
            //arrange

            using (var server = LocalWebServer.ListenInBackground(new XUnitMockLogger(_logger)))
            {
                var uri = server.ListeningUri;
                var delay = 1000;
                var builder = new TypedBuilderFactory().Create().WithUri(uri);
                server.WithRequestInspector(r => Task.Delay(delay));
                var callbackCalled = false;
                // act
                await builder.Advanced.WithTimeout(TimeSpan.FromMilliseconds(100)).OnException(ctx =>
                {
                    ctx.ExceptionHandled = true;
                    callbackCalled = true;
                }).SendAsync();

                callbackCalled.ShouldBeTrue();
            }
        }

        #endregion

        #region DependentUri

        [Fact]
        public async Task WhenDependentUriIsNull_ExpectNoException()
        {
            using (var server = LocalWebServer.ListenInBackground(new XUnitMockLogger(_logger)))
            {
                var builder =
                    new MockTypedBuilderFactory().Create()
                        .WithAllResponsesOk()
                        .WithUri(server.ListeningUri)
                        .Advanced;

                //act
                await builder
                    .WithDependentUri(null)
                    .SendAsync();
            }
        }

        [Fact]
        public async Task WhenDependentUrisIsNull_ExpectNoException()
        {

            var builder =
                new MockTypedBuilderFactory().Create()
                    .WithAllResponsesOk()
                    .WithUri(MockUri)
                    .Advanced;

            //act
            await builder
                .WithDependentUris(null)
                .SendAsync();
        }

        #endregion

        #region Handlers

        [Fact]
        // await Should.ThrowAsync<TypeMismatchException>(async () => );
        public async Task WhenResultAndSendingHandlerTypesMismatch_ExpectException()
        {

            var builder = new MockTypedBuilderFactory().Create().WithUri(MockUri);

            //act
            await builder.Advanced.OnSendingWithResult<Uri>(ctx => { }).ResultAsync<TestResult>();
        }

        [Fact]
        public async Task WhenResultAndSendingTypesMismatchAndSuppressTypeException_ExpectResult()
        {

            var builder = new MockTypedBuilderFactory().Create().WithResult(TestResult.Default1()).WithUri(MockUri);

            //act
            var actual = await builder.Advanced.WithSuppressTypeMismatchExceptions().OnSendingWithResult<Uri>(ctx => { }).ResultAsync<TestResult>();

            actual.ShouldNotBeNull();
        }

        [Fact]
        // await Should.ThrowAsync<TypeMismatchException>(async () => );
        public async Task WhenContentAndSendingHandlerTypesMismatch_ExpectException()
        {

            var builder = new MockTypedBuilderFactory().Create().WithUri(MockUri);

            //act
            await builder.WithContent(TestResult.Default1()).AsPost().Advanced.OnSendingWithContent<Uri>(ctx => { }).ResultAsync<TestResult>();
        }

        [Fact]
        public async Task WhenContentAndSendingHandlerTypesMismatchAndSuppressTypeException_ExpectResult()
        {

            var actual = await new MockTypedBuilderFactory().Create()
                .WithResult(TestResult.Default1())
                .WithUri(MockUri)
                .WithContent(TestResult.Default1())
                .AsPost()
                .Advanced
                .WithSuppressTypeMismatchExceptions()
                .OnSendingWithContent<Uri>(ctx =>
                {
                    var content = ctx.Content;
                })
                .ResultAsync<TestResult>();

            actual.ShouldNotBeNull();
        }

        [Fact]
        public async Task OnSending_WhenResultSet_ExpectIsAvailableAndReturned()
        {

            var builder = new MockTypedBuilderFactory().Create().WithUri(MockUri);

            string actual1 = null;
            string actual2 = null;

            //act
            var actualFinal = await builder
                .Advanced
                .OnSending<string, object>(ctx => ctx.Result = "one")
                .OnSending<string, object>(
                    ctx =>
                    {
                        actual1 = ctx.Result;
                        ctx.Result = "two";
                    })
                .OnSending<string, object>(
                    ctx =>
                    {
                        actual2 = ctx.Result;
                        ctx.Result = "buckle my shoe";
                    })
                .ResultAsync<string>();

            actual1.ShouldBe("one");
            actual2.ShouldBe("two");
            actualFinal.ShouldBe("buckle my shoe");
        }

        [Fact]
        public async Task OnSendingWithContent_WhenResultSet_ExpectIsAvailableAndReturned()
        {

            var builder = new MockTypedBuilderFactory().Create().WithUri(MockUri);

            string actual1 = null;
            string actual2 = null;

            //act
            var actualFinal = await builder
                .Advanced
                .OnSendingWithResult<string>(ctx => ctx.Result = "one")
                .OnSendingWithResult<string>(
                    ctx =>
                    {
                        actual1 = ctx.Result;
                        ctx.Result = "two";
                    })
                .OnSendingWithResult<string>(
                    ctx =>
                    {
                        actual2 = ctx.Result;
                        ctx.Result = "buckle my shoe";
                    })
                .ResultAsync<string>();

            actual1.ShouldBe("one");
            actual2.ShouldBe("two");
            actualFinal.ShouldBe("buckle my shoe");
        }

        [Fact]
        public async Task OnSent_WhenResultSet_ExpectIsAvailableAndReturned()
        {

            var builder = new MockTypedBuilderFactory().Create()
                .WithNextResponseOk("zero")
                .WithUri(MockUri);

            string actual1 = null;
            string actual2 = null;

            //act
            var actualFinal = await builder
                .Advanced
                .OnSent<string>(ctx => ctx.Result = "one")
                .OnSent<string>(
                    ctx =>
                    {
                        actual1 = ctx.Result;
                        ctx.Result = "two";
                    })
                .OnSent<string>(
                    ctx =>
                    {
                        actual2 = ctx.Result;
                        ctx.Result = "buckle my shoe";
                    })
                .ResultAsync<string>();

            actual1.ShouldBe("one");
            actual2.ShouldBe("two");
            actualFinal.ShouldBe("buckle my shoe");
        }

        [Fact]
        public async Task OnResult_WhenResultSet_ExpectIsAvailableAndReturned()
        {

            var builder = new MockTypedBuilderFactory().Create()
                .WithNextResponseOk("\"zero\"")
                .WithUri(MockUri);

            string actual1 = null;
            string actual2 = null;

            //act
            var actualFinal = await builder
                .Advanced
                .OnResult<string>(ctx => ctx.Result = "one")
                .OnResult<string>(
                    ctx =>
                    {
                        actual1 = ctx.Result;
                        ctx.Result = "two";
                    })
                .OnResult<string>(
                    ctx =>
                    {
                        actual2 = ctx.Result;
                        ctx.Result = "buckle my shoe";
                    })
                .ResultAsync<string>();

            actual1.ShouldBe("one");
            actual2.ShouldBe("two");
            actualFinal.ShouldBe("buckle my shoe");
        }

        [Fact]
        public async Task OnError_WhenHandledSet_ExpectIsAvailableAndReturned()
        {

            var builder = new MockTypedBuilderFactory().Create()
                .WithNextResponse(HttpStatusCode.InternalServerError, "\"error\"")
                .WithUri(MockUri);

            bool? actual1 = null;
            bool? actual2 = null;

            //act
            var actualFinal = await builder
                .Advanced
                .OnError<string>(
                    ctx =>
                    {
                        actual1 = ctx.ErrorHandled;
                        ctx.ErrorHandled = true;
                    })
                .OnError<string>(
                    ctx =>
                    {
                        actual2 = ctx.ErrorHandled;
                    })
                .ResultAsync<string>();

            actual1.ShouldSatisfyAllConditions(
                () => actual1.ShouldNotBeNull(),
                () => actual1.Value.ShouldBeFalse());

            actual2.ShouldSatisfyAllConditions(
                () => actual2.ShouldNotBeNull(),
                () => actual2.Value.ShouldBeTrue());

            actualFinal.ShouldBeNull();
        }

        [Fact]
        public async Task OnException_WhenHandledSet_ExpectIsAvailableAndReturned()
        {

            var builder = new MockTypedBuilderFactory().Create()
                .WithNextResponse(HttpStatusCode.InternalServerError, "\"error\"")
                .WithUri(MockUri);

            bool? actual1 = null;
            bool? actual2 = null;

            //act
            var actualFinal = await builder
                .Advanced
                .OnException(
                    ctx =>
                    {
                        actual1 = ctx.ExceptionHandled;
                        ctx.ExceptionHandled = true;
                    })
                .OnException(
                    ctx =>
                    {
                        actual2 = ctx.ExceptionHandled;
                    })
                .ResultAsync<string>();

            actual1.ShouldNotBeNull();
            actual1.Value.ShouldBeFalse();
            actual2.ShouldNotBeNull();
            actual2.Value.ShouldBeTrue();
            actualFinal.ShouldBeNull();
        }

        [Fact]
        // await Should.ThrowAsync<TypeMismatchException>(async () => );
        public async Task WhenErrorAndErrorHandlerTypesMismatch_ExpectException()
        {

            var builder = new MockTypedBuilderFactory().Create().WithError(TestResult.Default1()).WithUri(MockUri);

            //act
            await Should.ThrowAsync<TypeMismatchException>(
                builder.WithErrorType<TestResult>().Advanced.OnError<Uri>(ctx => { var error = ctx.Error; }).ResultAsync<TestResult>());

        }

        [Fact]
        public async Task WhenErrorAndErrorHandlerTypesMismatchAndSuppressTypeException_ExpectResult()
        {

            var builder = new MockTypedBuilderFactory().Create().WithError(TestResult.Default1()).WithUri(MockUri);

            //act
            var actual = await builder
                .WithErrorType<TestResult>()
                .Advanced
                .WithExceptionFactory(context => null)
                .WithSuppressTypeMismatchExceptions()
                .OnError<Uri>(ctx => { })
                .ResultAsync<TestResult>();

            actual.ShouldBeNull();
        }

        [Fact]
        // await Should.ThrowAsync<TypeMismatchException>(async () => );
        public async Task WhenDefaultResultAndResultTypesMismatch_ExpectException()
        {

            var builder = new MockTypedBuilderFactory().Create().WithError(TestResult.Default1()).WithUri(MockUri);

            //act
            await Should.ThrowAsync<TypeMismatchException>(
                builder.WithDefaultResult(TestResult.Default1())
                    .Advanced.WithExceptionFactory(context => null)
                    .ResultAsync<Uri>()
                );
        }

        [Fact]
        public async Task WhenDefaultResultAndResultTypesMismatchAndSuppressTypeException_ExpectNullResult()
        {

            var builder = new MockTypedBuilderFactory().Create().WithError(TestResult.Default1()).WithUri(MockUri);

            //act
            var actual = await builder.WithDefaultResult(TestResult.Default1())
                 .Advanced.WithSuppressTypeMismatchExceptions()
                 .WithExceptionFactory(context => null)
                 .ResultAsync<Uri>();

            actual.ShouldBeNull();
        }

        [Fact]
        public async Task WhenHandlerIsSubTypeOfResult_ExpectSuccess()
        {

            var builder = new MockTypedBuilderFactory().Create().WithAllResponses(new MockHttpResponseMessage().WithContent(TestResult.SerializedDefault1)).WithUri(MockUri);

            //act
            var called = false;
            var result = await builder
                .Advanced
                .OnResult<TestResult>(ctx => { called = true; })
                .ResultAsync<SubTestResult>();

            result.ShouldNotBeNull();
            called.ShouldBeTrue();
        }

        [Fact]
        // await Should.ThrowAsync<TypeMismatchException>(async () => );
        public async Task WhenHandlerIsSuperTypeOfResult_ExpectException()
        {

            var builder = new MockTypedBuilderFactory().Create().WithResult(TestResult.Default1()).WithUri(MockUri);

            await Should.ThrowAsync<TypeMismatchException>(async () =>
            {

                var actual = await builder
                 .Advanced
                 .OnResult<SubTestResult>(ctx =>
                 {
                     var result = ctx.Result;
                 })
                 .ResultAsync<TestResult>();

                actual.ShouldBeNull();
            });
        }

        [Fact]
        public async Task WhenSendingContentHandlerIsObjectType_ExpectSuccess()
        {

            var builder = new MockTypedBuilderFactory().Create().WithResult(TestResult.Default1()).WithUri(MockUri);

            //act
            var called = false;
            var result = await builder.WithContent(TestResult.Default1())
                .AsPost()
                .Advanced
                .OnSendingWithContent<object>(ctx => { called = true; })
                .ResultAsync<TestResult>();

            result.ShouldNotBeNull();
            called.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenSendingResultHandlerIsObjectType_ExpectSuccess()
        {

            var builder = new MockTypedBuilderFactory().Create().WithResult(TestResult.Default1()).WithUri(MockUri);

            //act
            var called = false;
            var result = await builder.WithContent(TestResult.Default1())
                .AsPost()
                .Advanced
                .OnSendingWithResult<object>(ctx => { called = true; })
                .ResultAsync<TestResult>();

            result.ShouldNotBeNull();
            called.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenSentHandlerIsObjectType_ExpectSuccess()
        {

            var builder = new MockTypedBuilderFactory().Create().WithResult(TestResult.Default1()).WithUri(MockUri);

            //act
            var called = false;
            var result = await builder.WithContent(TestResult.Default1())
                .AsPost()
                .Advanced
                .OnSent<object>(ctx => { called = true; })
                .ResultAsync<TestResult>();

            result.ShouldNotBeNull();
            called.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenResultHandlerIsObjectType_ExpectSuccess()
        {

            var builder = new MockTypedBuilderFactory().Create().WithResult(TestResult.Default1()).WithUri(MockUri);

            //act
            var called = false;
            var result = await builder
                .Advanced
                .OnResult<object>(ctx => { called = true; })
                .ResultAsync<TestResult>();

            result.ShouldNotBeNull();
            called.ShouldBeTrue();
        }

        [Fact]
        public async Task WhenErrorHandlerIsObjectType_ExpectSuccess()
        {

            var builder = new MockTypedBuilderFactory().Create().WithError(TestResult.Default1()).WithUri(MockUri);

            //act
            var called = false;
            var result = await builder
                .WithErrorType<TestResult>()
                .Advanced
                .OnError<object>(ctx => { called = true; })
                .WithExceptionFactory(context => null)
                .ResultAsync<SubTestResult>();

            result.ShouldBeNull();
            called.ShouldBeTrue();
        }


        [Fact]
        public async Task WhenErrorTypeSetMultipleTimes_ExpectLastWins()
        {

            var builder = new MockTypedBuilderFactory().Create().WithError(TestResult.Default1()).WithUri(MockUri);

            //act
            Type type = null;
            TestResult error = null;
            var result = await builder
                .WithErrorType<Uri>()
                .WithErrorType<string>()
                .WithErrorType<TestResult>()
                .Advanced
                .OnError<object>(ctx =>
                {
                    type = ctx.ErrorType;
                    error = ctx.Error as TestResult;
                })
                .WithExceptionFactory(context => null)
                .ResultAsync<TestResult>();

            error.ShouldNotBeNull();
            result.ShouldBeNull();
            typeof(TestResult).ShouldBe(type);
        }
        #endregion
    }
}