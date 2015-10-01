﻿using AonWeb.FluentHttp.Client;
using AonWeb.FluentHttp.HAL.Representations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AonWeb.FluentHttp.HAL
{
    public class HalBuilder : IAdvancedHalBuilder
    {
        private readonly IChildTypedBuilder _innerBuilder;

        public HalBuilder(IChildTypedBuilder innerBuilder)
        {
            _innerBuilder = innerBuilder;
        }

        public IAdvancedHalBuilder Advanced => this;

        

        

        

        //public IAdvancedHalBuilder WithDependentResources(params IHalResource[] resources)
        //{
        //    if (resources == null) 
        //        return this;

        //    var uris = resources.Select(r => r.Links.Self());

        //    _innerBuilder.WithDependentUris(uris);

        //    return this;
        //}

        //public IAdvancedHalBuilder WithDependentLink(Uri link)
        //{
        //    _innerBuilder.WithDependentUri(link);

        //    return this;
        //}

        //public IAdvancedHalBuilder WithDependentLink(Func<Uri> linkFactory)
        //{
        //    if (linkFactory == null)
        //        throw new ArgumentNullException(nameof(linkFactory));

        //    _innerBuilder.WithDependentUri(linkFactory());

        //    return this;
        //}


        public IAdvancedHalBuilder WithClientConfiguration(Action<IHttpClientBuilder> configuration)
        {
            _innerBuilder.WithClientConfiguration(configuration);

            return this;
        }

        public void CancelRequest()
        {
            _innerBuilder.CancelRequest();
        }

        public async Task<TResult> ResultAsync<TResult>()
            where TResult : IHalResource
        {
            return await _innerBuilder.ResultAsync<TResult>().ConfigureAwait(false);
        }

        public async Task<TResult> ResultAsync<TResult>(CancellationToken token)
            where TResult : IHalResource
        {
            return await _innerBuilder.ResultAsync<TResult>(token).ConfigureAwait(false);
        }

        public async Task SendAsync()
        {
            await _innerBuilder.SendAsync().ConfigureAwait(false);
        }

        public async Task SendAsync(CancellationToken token)
        {
            await _innerBuilder.SendAsync(token).ConfigureAwait(false);
        }

        public IHalBuilder WithConfiguration(Action<ITypedBuilderSettings> configuration)
        {
            _innerBuilder.WithConfiguration(configuration);

            return this;
        }

        public IAdvancedHalBuilder WithConfiguration(Action<IAdvancedTypedBuilder> configuration)
        {
            configuration?.Invoke(_innerBuilder);

            return this;
        }

        void IConfigurable<ITypedBuilderSettings>.WithConfiguration(Action<ITypedBuilderSettings> configuration)
        {
            WithConfiguration(configuration);
        }

        void IConfigurable<IAdvancedTypedBuilder>.WithConfiguration(Action<IAdvancedTypedBuilder> configuration)
        {
            WithConfiguration(configuration);
        }

        void IConfigurable<IHttpBuilderSettings>.WithConfiguration(Action<IHttpBuilderSettings> configuration)
        {
            _innerBuilder.WithConfiguration(configuration);
        }
    }
}
