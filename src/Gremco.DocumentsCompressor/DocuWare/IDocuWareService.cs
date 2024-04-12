using Gremco.DocumentsCompressor.DocuWare.Models;

namespace Gremco.DocumentsCompressor.DocuWare;

public interface IDocuWareService
{
	public Task<IdentityServiceInformation?> GetIdentityServiceUrl();

	public Task<ServiceConfiguration?> GetServiceConfiguration(string identityServerUrl);

	public Task<TokenResponse?> RequestToken(string authorizationEndpoint);

	public Task<OrganizationResponse?> GetOrganzations(string accessToken);

	public Task<GetFileCabinetsResponse?> GetFileCabinets(string accessToken, string orgId);

	public Task<List<DocumentItem>?> GetDocuments(string accessToken, string fileCabinetId, int maxSize, string lastTimeStamp);

	public Task<byte[]> DownloadDocument(string accessToken, string fileCabinetId, long documentId);

	public Task<Sections?> GetSectionsOfADocument(string accessToken, string fileCabinetId, long documentId);

	public Task RemoveOldSectionsOfADocument(string accessToken, string fileCabinetId, string sectionsId);

	public Task UploadCompressedSection(string accessToken, string fileCabinetId, long documentId, byte[] pdfCompressed);
	public Task<byte[]> DownloadSection(string accessToken, string fileCabinetId, string sectionId);
}