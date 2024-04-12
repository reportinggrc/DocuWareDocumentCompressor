using Ghostscript.NET;
using Ghostscript.NET.Processor;
using Gremco.DocumentsCompressor.DocuWare.Models;
using Microsoft.Extensions.Logging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Gremco.DocumentsCompressor.Pdf;

public class PdfManipulation : IPdfManipulation
{
    private readonly ILogger _logger;

    public PdfManipulation(ILogger<PdfManipulation> logger)
    {
        _logger = logger;
    }

    public async Task<List<byte[]>> SplitDocumentInSections(Sections sections, byte[] downloadedDocument)
    {
        var result = new List<byte[]>();
        var ms = new MemoryStream(downloadedDocument);
        PdfDocument pdfDocument = PdfReader.Open(new MemoryStream(downloadedDocument, 0, downloadedDocument.Length), PdfDocumentOpenMode.Import);
        var startPage = 0;

        foreach (var section in sections.Section)
        {
            var memoryStream = new MemoryStream();
            var subPdfDocument = new PdfDocument();
            for (var i = 0; i < section.PageCount; i++)
            {

                var page = pdfDocument.Pages[startPage + i];
                subPdfDocument.Pages.Add(page);
            }

            subPdfDocument.Save(memoryStream);
            result.Add(memoryStream.ToArray());
            startPage += section.PageCount;
        }

        return await Task.FromResult(result);
    }

    public async Task<byte[]?> CompressDocumentByGhostScript(byte[] downloadedDocument, string workingDir, long docId)
    {
        _logger.LogInformation("CompressDocumentByGhostScript start");
        byte[]? pdfCompressed = null;
        var inputFile = Path.Combine(workingDir, $"{docId}.pdf");
        var outputFile = Path.Combine(workingDir, $"output_{docId}.pdf");

        try
        {
            List<GhostscriptVersionInfo> gsVersions = GhostscriptVersionInfo.GetInstalledVersions();
            if (gsVersions.Count == 0)
            {
                _logger.LogError("No Ghostscript Installation was found");
            }

            foreach (var gsv in gsVersions)
            {
                _logger.LogInformation("Installed " + gsv.LicenseType + " Ghostscript " + gsv.Version);
            }

            File.WriteAllBytes(inputFile, downloadedDocument);

            using (var ghostscript = new GhostscriptProcessor())
            {
                ghostscript.Processing += processor_Processing;

                var switches = new List<string>();
                switches.Add(@"-q");
                switches.Add(@"-dNOPAUSE");
                switches.Add(@"-dBATCH");
                switches.Add(@"-dSAFER");
                switches.Add(@"-dOverPrint=/simulate");
                switches.Add(@"-sDEVICE=pdfwrite");
                switches.Add(@"-dPDFSETTINGS=/ebook");
                switches.Add(@"-dEmbedAllFonts=true");
                switches.Add(@"-dSubsetFonts=true");
                switches.Add(@"-dAutoRotatePages=/None");
                switches.Add(@"-dColorImageDownsampleType=/Bicubic");
                switches.Add(@"-dColorImageResolution=130");
                switches.Add(@"-dGrayImageDownsampleType=/Bicubic");
                switches.Add(@"-dGrayImageResolution=180");
                switches.Add(@"-dMonoImageDownsampleType=/Subsample");
                switches.Add(@"-dMonoImageResolution=180");

                switches.Add(@"-dDownsampleColorImages=true");
                switches.Add(@"-dDownsampleGrayImages=true");
                switches.Add(@"-dDownsampleMonoImages=true");

                switches.Add(@"-sOutputFile=" + outputFile);
                switches.Add(@"-f");
                switches.Add(inputFile);

                var stdio = new LogStdio();
                ghostscript.StartProcessing(switches.ToArray(), stdio);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error at compress PDF. Working Dir '{workingDir}'");
            File.Delete(outputFile);
            File.Delete(inputFile);
            return await Task.FromResult(pdfCompressed);
        }
        finally
        {
            if (Path.Exists(outputFile))
            {
                pdfCompressed = File.ReadAllBytes(outputFile);
                if (downloadedDocument.Length <= pdfCompressed.Length)
                {
                    _logger.LogInformation("The Compressfile is not smaller then Downloadedfile");
                    pdfCompressed = null;
                    File.Delete(inputFile);
                }
            }
        }
        File.Delete(outputFile);
        return await Task.FromResult(pdfCompressed);
    }

    private void processor_Processing(object sender, GhostscriptProcessorProcessingEventArgs e)
    {
        _logger.LogInformation($"Processing Site {e.CurrentPage} of {e.TotalPages}");
    }
}

public class LogStdio : GhostscriptStdIO
{
    public LogStdio() : base(true, true, true) { }

    public override void StdIn(out string input, int count)
    {
        input = new string('\n', count);
    }

    public override void StdOut(string output)
    {
    }

    public override void StdError(string error)
    {
        throw new Exception(error);
    }
}