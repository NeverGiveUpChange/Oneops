using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OneOpsClient.Api
{
    public class HttpWebClient
    {
        readonly IHttpClientFactory httpClientFactory;

        public HttpWebClient(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<string> GetAsync(string url, string clientName = "OneopsClient", Dictionary<string, string> dicHeaders = null)
        {
            var httpClient = httpClientFactory.CreateClient(clientName);
    
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (dicHeaders != null)
                {

                    foreach (var item in dicHeaders)
                    {
                        if (request.Headers.Contains(item.Key))
                        {
                            request.Headers.Remove(item.Key);
                        }
                        request.Headers.Add(item.Key, item.Value);
                    }
                }
                var response = await httpClient.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
        }
        public async Task<string> PostAsync(string requestBody, string url = "", string clientName = "OneopsClient", Dictionary<string, string> dicHeaders = null, string contentType = "application/json")
        {
            var httpClient = httpClientFactory.CreateClient(clientName);
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                if (dicHeaders != null)
                {

                    foreach (var item in dicHeaders)
                    {
                        if (request.Headers.Contains(item.Key))
                        {
                            request.Headers.Remove(item.Key);

                        }
                        request.Headers.Add(item.Key, item.Value);
                    }
                }
                using (var httpContent = new StringContent(requestBody, Encoding.UTF8, contentType))
                {
                    request.Content = httpContent;

                    var response = await httpClient.SendAsync(request);
                    return await response.Content.ReadAsStringAsync();
                }
            }

        }


        public async Task<string> PostAsync(MultipartFormDataContent requestBody, string url = "", string clientName = "OneopsClient", Dictionary<string, string> dicHeaders = null)
        {
            var httpClient = httpClientFactory.CreateClient(clientName);
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                if (dicHeaders != null)
                {

                    foreach (var item in dicHeaders)
                    {
                        if (request.Headers.Contains(item.Key))
                        {
                            request.Headers.Remove(item.Key);

                        }
                        request.Headers.Add(item.Key, item.Value);
                    }
                }
                using (requestBody)
                {
                    request.Content = requestBody;

                    var response = await httpClient.SendAsync(request);
                    return await response.Content.ReadAsStringAsync();
                }
            }

        }
    }
}
