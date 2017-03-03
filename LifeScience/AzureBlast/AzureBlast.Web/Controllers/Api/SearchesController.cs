// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.Azure.Batch.Blast.Searches;
using Microsoft.Azure.Batch.Blast.Storage.Entities;

namespace Microsoft.Azure.Blast.Web.Controllers.Api
{
    [RoutePrefix("api")]
    public class SearchesController : BaseApiController
    {
        private readonly ISearchProvider _searchProvider;

        public SearchesController(ISearchProvider searchProvider)
        {
            _searchProvider = searchProvider;
        }

        [Route("searches"), HttpGet]
        public IEnumerable<SearchEntity> GetAll()
        {
            return _searchProvider.ListSearches().OrderByDescending(s => s.StartTime);
        }

        [Route("searches/{searchId}/queries/{queryId}/outputs/{filename}"), HttpGet]
        public HttpResponseMessage GetSearchQueries(Guid searchId, string queryId, string filename)
        {
            var response = _searchProvider.GetSearchQueryOutput(searchId, queryId, filename);
            return Request.CreateResponse(HttpStatusCode.OK, response);
        }

        [Route("searches/{searchId}/queries"), HttpGet]
        public IEnumerable<SearchQuery> GetSearchQueries(Guid searchId)
        {
            return _searchProvider.ListSearchQueries(searchId).OrderBy(q => q.InputFilename);
        }

        [Route("searches/{searchId}"), HttpGet]
        public HttpResponseMessage Get(Guid searchId)
        {
            var search = _searchProvider.GetSearch(searchId);
            if (search == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format("searchId: {0} was not found", searchId));
            }

            return Request.CreateResponse(HttpStatusCode.OK, search);
        }

        [Route("searches/{searchId}"), HttpDelete]
        public HttpResponseMessage Delete(Guid searchId)
        {
            var search = _searchProvider.GetSearch(searchId);
            if (search == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format("searchId: {0} was not found", searchId));
            }

            _searchProvider.DeleteSearch(searchId);

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [Route("searches/{searchId}/actions/cancel"), HttpPost]
        public HttpResponseMessage Cancel(Guid searchId)
        {
            var search = _searchProvider.GetSearch(searchId);

            if (search == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format("searchId: {0} was not found", searchId));
            }

            _searchProvider.CancelSearch(searchId);

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [Route("searches"), HttpPost]
        public async Task<HttpResponseMessage> SubmitSearch()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var root = HttpContext.Current.Server.MapPath("~/App_Data");
            var provider = new MultipartFormDataStreamProvider(root);

            try
            {
                // Read the async multipart form data.
                await Request.Content.ReadAsMultipartAsync(provider);

                // read the form data into a search model
                var searchModel = CreateSearchModel(provider.FormData);

                var searchInputFiles = new List<SearchInputFile>(searchModel.SearchInputFiles);

                // get the contents of the files from the request
                foreach (MultipartFileData file in provider.FileData)
                {
                    var fileInfo = new FileInfo(file.LocalFileName);

                    using (var reader = fileInfo.OpenText())
                    {
                        var sequenceText = reader.ReadToEnd();
                        searchInputFiles.Add(new SearchInputFile
                        {
                            Filename = file.Headers.ContentDisposition.FileName.Replace("\"", ""),
                            Length = fileInfo.Length,
                            Content = new MemoryStream(Encoding.UTF8.GetBytes(sequenceText)),
                        });
                    }

                    try
                    {
                        fileInfo.Delete();
                    }
                    catch
                    {
                        // don't really care too much if this fails
                        Trace.WriteLine(string.Format("failed to delete temp file: {0}", file.LocalFileName));
                    }
                }

                searchModel.SearchInputFiles = searchInputFiles;

                // do some basic server side validation ...
                if (String.IsNullOrEmpty(searchModel.Name))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        "The name must be provided with the search");
                }

                if (String.IsNullOrEmpty(searchModel.DatabaseName))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        "The database name must be provided with the search");
                }

                if (String.IsNullOrEmpty(searchModel.Executable))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        "The program name must be provided with the search");
                }

                if (!searchModel.SearchInputFiles.Any())
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        "You must enter either a plain text sequence, or upload file/files contining the sequence(s) to search");
                }

                var id = _searchProvider.SubmitSearch(searchModel);

                return Request.CreateResponse(HttpStatusCode.OK, id);
            }
            catch (Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e.ToString());
            }
        }

        private SearchSpecification CreateSearchModel(NameValueCollection formData)
        {
            // note: only to see what is in here, can be removed
            foreach (var key in formData.AllKeys)
            {
                foreach (var val in formData.GetValues(key))
                {
                    Trace.WriteLine(string.Format("{0}: {1}", key, val));
                }
            }

            var spec = new SearchSpecification
            {
                Name = formData["searchName"],
                DatabaseName = formData["databaseName"],
                Executable = formData["executable"],
                ExecutableArgs = formData["executableArgs"],
                SearchInputFiles = new List<SearchInputFile>(),
            };

            AddPoolSpecToSearch(formData, spec);

            if (formData["searchSequenceText"] != null && !string.IsNullOrEmpty(formData["searchSequenceText"].Trim()))
            {
                var bytes = Encoding.UTF8.GetBytes(formData["searchSequenceText"]);

                spec.SearchInputFiles = new List<SearchInputFile>()
                {
                    new SearchInputFile
                    {
                        Filename = "searchsequence.txt",
                        Content = new MemoryStream(bytes),
                        Length = bytes.Length,
                    }
                };
            }

            return spec;
        }

        private void AddPoolSpecToSearch(NameValueCollection formData, SearchSpecification spec)
        {
            if (string.IsNullOrEmpty(formData["poolId"]) && (string.IsNullOrEmpty(formData["targetDedicated"]) || string.IsNullOrEmpty(formData["virtualMachineSize"])))
            {
                throw new Exception("An existing PoolId or targetDedicated and virtualMachineSize must be specified");
            }

            if (!string.IsNullOrEmpty(formData["poolId"]))
            {
                spec.PoolId = formData["poolId"];
            }
            else
            {
                spec.TargetDedicated = int.Parse(formData["targetDedicated"]);
                spec.VirtualMachineSize = formData["virtualMachineSize"];
                spec.PoolDisplayName = formData["poolName"];
            }
        }
    }
}
