using Gremco.DocumentsCompressor.DocuWare.Models;

namespace Gremco.DocumentsCompressor.Pdf;

public interface IPdfManipulation {
	public Task<List<byte[]>> SplitDocumentInSections(Sections sections, byte[] downloadedDocument);

	public Task<byte[]?> CompressDocumentByGhostScript(byte[] downloadedDocument, string workingDir, long docId);
}