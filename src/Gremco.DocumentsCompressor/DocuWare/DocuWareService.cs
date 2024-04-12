using System.Net.Http.Headers;
using Gremco.DocumentsCompressor.DocuWare.Models;
using RestSharp;

namespace Gremco.DocumentsCompressor.DocuWare;

public class DocuWareService : IDocuWareService
{
    private readonly DocuWareConfiguration docuWareConfiguration;

    private const string GetIdentityServiceUrl_Endpoint = "/DocuWare/Platform/Home/IdentityServiceInfo";
    private const string GetServiceConfiguration_Endpoint = "/.well-known/openid-configuration";
    private const string RequestToken_Endpoint = "{0}";
    private const string GetOrganzations_Endpoint = "/{0}/Organizations";
    private const string GetFileCabinets_Endpoint = "/{0}/FileCabinets?orgid={1}";
    private const string GetDocuments_Endpoint = "/DocuWare/Platform/FileCabinets/{0}/Query/DialogExpression";
    private const string DownloadDocument_Endpoint = "/DocuWare/Platform/FileCabinets/{0}/Documents/{1}/FileDownload?targetFileType=Pdf&keepAnnotations=true";
    private const string Sections_Endpoint = "/DocuWare/Platform/FileCabinets/{0}/Sections?DocId={1}";
    private const string DeleteSection_Endpoint = "/DocuWare/Platform/FileCabinets/{0}/Sections/{1}";
    private const string DownloadSection_Endpoint = "/DocuwWare/Platform/FileCabinets/{0}/Sections/{1}/Data";


    private const string grant_type = "password";
    private const string scope = "docuware.platform";
    private const string client_id = "docuware.platform.net.client";

    private readonly HttpClient httpClient;
    public DocuWareService(DocuWareConfiguration docuWareConfiguration, HttpClient httpClient)
    {
        this.docuWareConfiguration = docuWareConfiguration;
        this.httpClient = httpClient;
    }

    public async Task<IdentityServiceInformation?> GetIdentityServiceUrl()
    {
        var request = new RestRequest(GetIdentityServiceUrl_Endpoint);

        using var restClient = new RestClient(docuWareConfiguration.ServerUrl);
        var response = await restClient.GetAsync<IdentityServiceInformation>(request);

        return response;
    }

    public async Task<ServiceConfiguration?> GetServiceConfiguration(string identityServiceUrl)
    {
        var request = new RestRequest(GetServiceConfiguration_Endpoint);

        using var restClient = new RestClient(identityServiceUrl);

        var response = await restClient.GetAsync<ServiceConfiguration>(request);

        return response;
    }

    public async Task<TokenResponse?> RequestToken(string authorizationEndpoint)
    {
        var request = new RestRequest(string.Format(RequestToken_Endpoint, authorizationEndpoint));
        request.AddHeader("Accept", "application/json");
        request.AddParameter("client_id", client_id);
        request.AddParameter("scope", scope);
        request.AddParameter("grant_type", grant_type);
        request.AddParameter("username", docuWareConfiguration.Username);
        request.AddParameter("password", docuWareConfiguration.Password);

        using var restClient = new RestClient(docuWareConfiguration.ServerUrl);
        var response = await restClient.PostAsync<TokenResponse>(request);

        return response;
    }

    public async Task<OrganizationResponse?> GetOrganzations(string accessToken)
    {
        var request = new RestRequest(string.Format(GetOrganzations_Endpoint, docuWareConfiguration.Platform));
        request.AddHeader("Accept", "application/json");
        request.AddHeader("Authorization", "Bearer " + accessToken);

        using var restClient = new RestClient(docuWareConfiguration.ServerUrl);
        var response = await restClient.GetAsync<OrganizationResponse>(request);

        return response;
    }

    public async Task<GetFileCabinetsResponse?> GetFileCabinets(string accessToken, string orgId)
    {
        var url = string.Format(GetFileCabinets_Endpoint, docuWareConfiguration.Platform, orgId);
        var request = new RestRequest(url);
        request.AddHeader("Accept", "application/json");
        request.AddHeader("Authorization", "Bearer " + accessToken);

        using var restClient = new RestClient(docuWareConfiguration.ServerUrl);
        var response = await restClient.GetAsync<GetFileCabinetsResponse>(request);

        return response;
    }

    public async Task<List<DocumentItem>?> GetDocuments(string accessToken, string fileCabinetId, int maxSize, string lastTimeStamp)
    {
        var resultItems = new List<DocumentItem>();
        bool hasMoreDocuments = true;
        int start = 0;
        var timestamp = string.IsNullOrEmpty(lastTimeStamp) ? "1900-01-01 12:52:47" : lastTimeStamp;

        while (hasMoreDocuments)
        {
            var url = string.Format(GetDocuments_Endpoint, fileCabinetId);


            var request = new RestRequest(url);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", "Bearer " + accessToken);
            var requestData = new DocumentsRequest
            {
                Condition = new List<DocumentsCondition>
        {
            new DocumentsCondition
            {
                DBName = "DWDOCSIZE",
                Value = new List<string> { maxSize.ToString(), "" }
            },
            new DocumentsCondition
            {
                DBName = "DWEXTENSION",
                Value = new List<string> { ".pdf" }
            },
            new DocumentsCondition
            {
                DBName = "DWSTOREDATETIME",
                Value = new List<string> { timestamp, "" }
            }
        },
                Operation = "And",
                Start = start,
                Count = 1000
            };

            request.AddJsonBody(requestData);
            using var restClient = new RestClient(docuWareConfiguration.ServerUrl);
            var response = await restClient.PostAsync<GetDocumentsResponse>(request);
            if (response != null)
            {
                if (response.Count.HasMore)
                {
                    start += 1000;
                }
                else
                {
                    hasMoreDocuments = false;
                }

                foreach (var document in response.Items)
                {
                    resultItems.Add(document);
                }
            }

        }
        return resultItems;
    }

    public async Task<byte[]> DownloadDocument(string accessToken, string fileCabinetId, long documentId)
    {

        var url = string.Format(DownloadDocument_Endpoint, fileCabinetId, documentId);

        var httpRequest = new HttpRequestMessage(HttpMethod.Get, docuWareConfiguration.ServerUrl + url);
        httpRequest.Headers.Add("Authorization", "Bearer " + accessToken);
        var httpResult = await httpClient.SendAsync(httpRequest);
        var result = await httpResult.Content.ReadAsByteArrayAsync();

        return result;
    }

    public async Task<Sections?> GetSectionsOfADocument(string accessToken, string fileCabinetId, long documentId)
    {
        var url = string.Format(Sections_Endpoint, fileCabinetId, documentId);
        var request = new RestRequest(url);
        request.AddHeader("Accept", "application/json");
        request.AddHeader("Authorization", "Bearer " + accessToken);

        using var restClient = new RestClient(docuWareConfiguration.ServerUrl);

        var httpResult = await restClient.GetAsync<Sections>(request);
        return httpResult;
    }

    public async Task RemoveOldSectionsOfADocument(string accessToken, string fileCabinetId, string sectionsId)
    {
        var url = string.Format(DeleteSection_Endpoint, fileCabinetId, sectionsId);
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, docuWareConfiguration.ServerUrl + url);
        httpRequest.Headers.Add("Authorization", "Bearer " + accessToken);
        await httpClient.SendAsync(httpRequest);
    }

    public async Task<byte[]> DownloadSection(string accessToken, string fileCabinetId, string sectionId)
    {
        var url = string.Format(DownloadSection_Endpoint, fileCabinetId, sectionId);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, docuWareConfiguration.ServerUrl + url);
        httpRequest.Headers.Add("Authorization", "Bearer " + accessToken);
        var httpResult = await httpClient.SendAsync(httpRequest);

        var result = await httpResult.Content.ReadAsByteArrayAsync();

        return result;

    }

    public async Task UploadCompressedSection(string accessToken, string fileCabinetId, long documentId, byte[] pdfCompressed)
    {
        var url = string.Format(Sections_Endpoint, fileCabinetId, documentId);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, docuWareConfiguration.ServerUrl + url);
        httpRequest.Headers.Add("Authorization", "Bearer " + accessToken);

        var content = new MultipartFormDataContent();
        var binaryFileContent = new ByteArrayContent(pdfCompressed);
        binaryFileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(binaryFileContent, "file", "filename.pdf");
        httpRequest.Content = content;

        await httpClient.SendAsync(httpRequest);

    }
}