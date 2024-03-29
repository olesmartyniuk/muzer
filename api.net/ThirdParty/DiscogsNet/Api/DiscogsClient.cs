﻿using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using DiscogsNet.Model;
using DiscogsNet.Model.Search;
using Newtonsoft.Json.Linq;

namespace DiscogsNet.Api
{
    public class DiscogsClient : IDiscogsApi
    {
        private string baseUrl;
        private WebClient webClient;
        private readonly string _useragent;        

        /// <summary>
        /// Gets or sets a value indicating whether the reader will throw an exception if an unknown property is encountered.
        /// </summary>
        public bool StrictReading { get; set; }

        /// <summary>
        /// This stores the X-RateLimit-Limit header returned by the last request.
        /// </summary>
        public int RateLimitLimit { get; private set; }

        /// <summary>
        /// This stores the X-RateLimit-Remaining header returned by the last request.
        /// </summary>
        public int RateLimitRemaining { get; private set; }

        /// <summary>
        /// This stores the X-RateLimit-Reset header returned by the last request.
        /// </summary>
        public int RateLimitReset { get; private set; }

        public DiscogsClient(string authToken, string useragent = "")
        {
            _useragent = useragent;

            this.baseUrl = "https://api.discogs.com/";

            this.webClient = new WebClient();
            this.webClient.Headers["Accept"] = "application/xml";
            this.webClient.Headers["Accept-Encoding"] = "gzip";
            this.webClient.Headers["Authorization"] = "Discogs token=" + authToken;
        } 

        private void UpdateRateLimit()
        {
            string limit = this.webClient.ResponseHeaders["X-RateLimit-Limit"];
            string remaining = this.webClient.ResponseHeaders["X-RateLimit-Remaining"];
            string reset = this.webClient.ResponseHeaders["X-RateLimit-Reset"];

            this.RateLimitLimit = limit != null ? int.Parse(limit) : -1;
            this.RateLimitRemaining = remaining != null ? int.Parse(remaining) : -1;
            this.RateLimitReset = reset != null ? int.Parse(reset) : -1;
        }

        public Release GetRelease(int id)
        {
            string response = this.DownloadString(this.baseUrl + "releases/" + id);
            this.UpdateRateLimit();
            return new DataReader(this.StrictReading).ReadRelease(JObject.Parse(response));
        }

        public Artist GetArtist(int id)
        {
            string response = this.DownloadString(this.baseUrl + "artists/" + id);
            this.UpdateRateLimit();
            return new DataReader(this.StrictReading).ReadArtist(JObject.Parse(response));
        }

        public Label GetLabel(int id)
        {
            string response = this.DownloadString(this.baseUrl + "labels/" + id);
            this.UpdateRateLimit();
            return new DataReader(this.StrictReading).ReadLabel(JObject.Parse(response));
        }

        public MasterRelease GetMasterRelease(int id)
        {
            string response = this.DownloadString(this.baseUrl + "masters/" + id);
            this.UpdateRateLimit();
            return new DataReader(this.StrictReading).ReadMasterRelease(JObject.Parse(response));
        }

        public ArtistReleases GetArtistReleases(int artistId, PaginationRequest paginationRequest = null)
        {
            if (paginationRequest == null)
            {
                paginationRequest = new PaginationRequest();
            }

            StringBuilder queryString = new StringBuilder();
            paginationRequest.AddQueryParams(queryString);

            string response = this.DownloadString(this.baseUrl + "artists/" + artistId + "/releases" + queryString);
            this.UpdateRateLimit();
            return new DataReader(this.StrictReading).ReadArtistReleases(JObject.Parse(response));
        }

        public byte[] GetImage(string url)
        {
            this.webClient.Headers["User-Agent"] = _useragent;
            byte[] data = this.webClient.DownloadData(url);
            this.UpdateRateLimit();
            return data;
        }

        public SearchResults Search(SearchQuery query, PaginationRequest paginationRequest = null)
        {
            if (paginationRequest == null)
            {
                paginationRequest = new PaginationRequest();
            }

            StringBuilder queryString = new StringBuilder();
            paginationRequest.AddQueryParams(queryString);
            query.AddQueryParams(queryString);

            string response = this.DownloadString(this.baseUrl + "database/search" + queryString);
            this.UpdateRateLimit();
            return new DataReader(this.StrictReading).ReadSearchResults(JObject.Parse(response));
        }

        private string DownloadString(string uri)
        {
            this.webClient.Headers.Add("User-Agent", _useragent); 
            byte[] data = this.webClient.DownloadData(uri);
            if (this.webClient.ResponseHeaders["Content-Encoding"] == "gzip")
            {
                using (MemoryStream inputStream = new MemoryStream(data))
                {
                    GZipStream gzip = new GZipStream(inputStream, CompressionMode.Decompress);
                    using (StreamReader reader = new StreamReader(gzip, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            return Encoding.UTF8.GetString(data);
        }
    }
}
