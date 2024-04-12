using Gremco.DocumentsCompressor.DocuWare;
using Gremco.DocumentsCompressor.DocuWare.Models;
using Gremco.DocumentsCompressor.Pdf;
using Microsoft.Extensions.Logging;
using System;

namespace Gremco.DocumentsCompressor;

public class Startup
{
    private readonly IDocuWareService docuWareService;
    private readonly ILogger logger;
    private readonly IPdfManipulation pdfManipulation;

    public Startup(ILogger<Startup> logger, IDocuWareService docuWareService, IPdfManipulation pdfManipulation)
    {
        this.logger = logger;
        this.docuWareService = docuWareService;
        this.pdfManipulation = pdfManipulation;
    }

    public async Task ExecuteAsync()
    {

        logger.LogInformation("Get Identity Service");
        var identityServer = await docuWareService.GetIdentityServiceUrl();

        logger.LogInformation("Get Service Configuration");
        if (identityServer != null)
        {
            var serviceConfiguration = await docuWareService.GetServiceConfiguration(identityServer.IdentityServiceUrl);


            logger.LogInformation("Get Request Token");
            if (serviceConfiguration != null)
            {
                var requestToken = await docuWareService.RequestToken(serviceConfiguration.token_endpoint);
                if (requestToken != null)
                {
                    var accessToken = requestToken.access_token;

                    var lastTimestamp = GetLastTimestamp();

                    logger.LogInformation("Get Large Documents Items list");
                    var largeDocs = await GetLargeDocumentItems(accessToken, 1000000, lastTimestamp);

                    logger.LogInformation("Save last timestamp");
                    SaveTimestamp();

                    logger.LogInformation($"Complete Count of PDF Documents: {largeDocs.Count()}");

                    await ProcessPdf(largeDocs, accessToken);
                }

            }

        }
    }

    private void SaveTimestamp()
    {
        var timestamp = DateTime.Now;
        var timestampString = timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        var filePath = "timestamp.txt";
        var projectDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName;
        if (projectDirectory != null)
        {
            filePath = Path.Combine(projectDirectory, "timestamp.txt");
        }
        File.AppendAllText(filePath, timestampString + Environment.NewLine);
    }

    private string GetLastTimestamp()
    {
        var filePath = "timestamp.txt";
        var projectDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName;
        if (projectDirectory != null)
        {
            filePath = Path.Combine(projectDirectory, "timestamp.txt");
        }

        var lines = new string[1];
        var lastTimestampString = "";

        if (File.Exists(filePath))
        {
            lines = File.ReadAllLines(filePath);
        }

        if (lines.Length > 0)
        {
            lastTimestampString = lines[lines.Length - 1];
        }

        return lastTimestampString;
    }

    private async Task ProcessPdf(IEnumerable<DocumentItem> pdfsToCompress, string accessToken)
    {
        float saveFileSizesOrginal = 0;
        var saveSizeToOrginal = new List<float>();
        DateTime currentDate = DateTime.Today;
        string formattedDate = currentDate.ToString("dd.MM.yyyy");
        var projectDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName;
        if (projectDirectory != null)
        {
            var pdfFolder = Path.Combine(projectDirectory, formattedDate);
            var exportedFilesPath = Path.Combine(pdfFolder, "exportedFiles.txt");
            Directory.CreateDirectory(pdfFolder);
            foreach (var pdfToCompress in pdfsToCompress)
            {
                try
                {
                    logger.LogInformation($"Processing Document: {pdfToCompress.Title}");

                    var sections =
                        await docuWareService.GetSectionsOfADocument(accessToken, pdfToCompress.FileCabinetId, pdfToCompress.Id);

                    var downloadedDocument =
                        await docuWareService.DownloadDocument(accessToken, pdfToCompress.FileCabinetId, pdfToCompress.Id);


                    var compressedDocumentGs =
                        await pdfManipulation.CompressDocumentByGhostScript(downloadedDocument, pdfFolder, pdfToCompress.Id);


                    if (compressedDocumentGs == null || compressedDocumentGs.Length == 0)
                    {
                        logger.LogWarning("File byte array is null, continue");
                        continue;
                    }

                    var smallerOrginal = (float)(100 - compressedDocumentGs.Length * 100 / downloadedDocument.Length);

                    logger.LogInformation(
                        $"downloadedDocument Length:   {(float)(downloadedDocument.Length / 1024 / 1024)} MB - {downloadedDocument.Length} Byte");

                    logger.LogInformation(
                        $"compressedDocumentGs Length:   {(float)(compressedDocumentGs.Length / 1024 / 1024)} MB - {compressedDocumentGs.Length} Byte");

                    logger.LogInformation(
                        $"Smaller vs Orginal: {smallerOrginal}%");

                    if (sections != null && sections.Section.Count > 1)
                    {
                        var splittedDocument = await pdfManipulation.SplitDocumentInSections(sections, compressedDocumentGs);
                        foreach (var section in splittedDocument)
                        {
                            await docuWareService.UploadCompressedSection(accessToken, pdfToCompress.FileCabinetId, pdfToCompress.Id,
                                section);
                        }
                    }
                    else
                    {
                        await docuWareService.UploadCompressedSection(accessToken, pdfToCompress.FileCabinetId, pdfToCompress.Id,
                            compressedDocumentGs);
                    }
                    if (sections != null)
                    {
                        foreach (var section in sections.Section)
                        {
                            await docuWareService.RemoveOldSectionsOfADocument(accessToken, pdfToCompress.FileCabinetId, section.Id);
                        }
                    }

                    logger.LogInformation("[42x900004]: Document with Id " + pdfToCompress.Id + " in fileCabinet: " +
                                      pdfToCompress.FileCabinetId + " was compressed successfully");

                File.AppendAllText(exportedFilesPath,
                    $"File ID: '{pdfToCompress.Id}', Title: '{pdfToCompress.Title}', Cabinet ID: {pdfToCompress.FileCabinetId}, Smaller as Orginal: {smallerOrginal}%, Filesize Orginal: {(float)(downloadedDocument.Length / 1024 / 1024)} MB, Filesize Ghostscript: {(float)(compressedDocumentGs.Length / 1024 / 1024)} MB\n");

                    saveFileSizesOrginal += downloadedDocument.Length - compressedDocumentGs.Length;

                    saveSizeToOrginal.Add(smallerOrginal);

                }
                catch (Exception exception)
                {
                    logger.LogError(exception, $"Error at Processing Document '{pdfToCompress.Title}'");
                File.AppendAllText(exportedFilesPath,
                        $"File ID: '{pdfToCompress.Id}', Error: {exception}\n");
                }
            }

        File.AppendAllText(exportedFilesPath,
                        $"Save Filespace (median in %) opposite Orginal:   {saveSizeToOrginal.Sum(x => x) / saveSizeToOrginal.Count}");

            logger.LogInformation(
                $"Save Filespace (median in %) opposite Orginal:   {saveSizeToOrginal.Sum(x => x) / saveSizeToOrginal.Count}");
        }
    }

    private async Task<List<DocumentItem>> GetLargeDocumentItems(string accessToken, int maxSize, string lastTimeStamp)
    {
        var resultItems = new List<DocumentItem>();

        var organizationResponse = await docuWareService.GetOrganzations(accessToken);
        if (organizationResponse != null)
        {
            foreach (var organization in organizationResponse.Organization)
            {
                var fileCabinetResponse = await docuWareService.GetFileCabinets(accessToken, organization.Id);
                if (fileCabinetResponse != null)
                {
                    foreach (var fileCabinet in fileCabinetResponse.FileCabinet)
                    {
                        var documentsResponse = await docuWareService.GetDocuments(accessToken, fileCabinet.Id, maxSize, lastTimeStamp);
                        if (documentsResponse != null)
                        {
                            foreach (var document in documentsResponse)
                            {
                                resultItems.Add(document);
                            }
                        }

                    }
                }

            }
        }

        return resultItems;
    }
}