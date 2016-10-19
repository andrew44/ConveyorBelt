﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BeeHive;
using BeeHive.Configuration;
using Newtonsoft.Json.Serialization;

namespace ConveyorBelt.Tooling
{
    public class ElasticsearchClient : IElasticsearchClient
    {
        private IHttpClient _httpClient;

        private const string IndexFormat = "{0}/{1}";
        private const string IndexSearchFormat = "{0}/{1}/_search?size=0";
        private const string MappingFormat = "{0}/{1}/{2}/_mapping";
        private readonly ConcurrentDictionary<string, string> _existingIndices = new ConcurrentDictionary<string, string>();

        public ElasticsearchClient(IHttpClient httpClient)
        {
            _httpClient = httpClient;

        }

        public async Task<bool> CreateIndexIfNotExistsAsync(string baseUrl, string indexName, string jsonCommand = "")
        {
            if (_existingIndices.ContainsKey(indexName))
                return false;

            baseUrl = baseUrl.TrimEnd('/');
            string searchUrl = string.Format(IndexSearchFormat, baseUrl, indexName);
            TheTrace.TraceInformation("Just wanna check if this index exists: {0}. URL: {1}", indexName, searchUrl);
            var getResponse = await _httpClient.GetAsync(searchUrl);

            var getText = await getResponse.Content.ReadAsStringAsync();
            if (getResponse.IsSuccessStatusCode)
            {
                _existingIndices.TryAdd(indexName, null);
                return false;
            }

            if (getResponse.StatusCode == HttpStatusCode.NotFound)
            {
                TheTrace.TraceInformation("It sent Back this {0} and text => {1}", (int)getResponse.StatusCode, getText); 

                var url = string.Format(IndexFormat, baseUrl, indexName);
                var putResponse = await _httpClient.PutAsync(url, new StringContent(jsonCommand, Encoding.UTF8, "application/json"));
                var putText = "[NO CONTENT]";

                if (putResponse.Content != null)
                    putText = await putResponse.Content.ReadAsStringAsync();
    
                if (putResponse.IsSuccessStatusCode || (putResponse.StatusCode == HttpStatusCode.BadRequest && putText.Contains("already exists")))
                {
                    _existingIndices.TryAdd(indexName, null);
                    return true;
                }
                else
                {
                    throw new ApplicationException(string.Format("Error in PUT {0}: {1}",
                        putResponse.StatusCode,
                        putText));
                }
            }
            else
            {
                TheTrace.TraceInformation("It sent Back this {0}", (int) getResponse.StatusCode);
                throw new ApplicationException(string.Format("Error in GET: {0}: {1}",
                    getResponse.StatusCode,
                    getText));
            }
        }

        public async Task<bool> MappingExistsAsync(string baseUrl, string indexName, string typeName)
        {
            baseUrl = baseUrl.TrimEnd('/');
            var url = string.Format(MappingFormat, baseUrl, indexName, typeName);
            var response = await _httpClient.GetAsync(url);
            var text = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode && text != "{}")
            {
                return true;
            }
            if (response.StatusCode == HttpStatusCode.NotFound || text == "{}")
            {
                return false;
            }
            else
            {
                throw new ApplicationException(string.Format("Error {0}: {1}",
                    response.StatusCode,
                    text));
            }
        }

        public async Task<bool> UpdateMappingAsync(string baseUrl, string indexName, string typeName, string mapping)
        {
            baseUrl = baseUrl.TrimEnd('/');
            var url = string.Format(MappingFormat, baseUrl, indexName, typeName);
            var response = await _httpClient.PutAsync(url, 
                new StringContent(mapping, Encoding.UTF8, "application/json"));
            var text = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                throw new ApplicationException(string.Format("Error {0}: {1}",
                    response.StatusCode,
                    text));
            }
        }

    }
}
