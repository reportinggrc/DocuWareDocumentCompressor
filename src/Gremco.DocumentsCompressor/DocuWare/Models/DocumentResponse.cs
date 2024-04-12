using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gremco.DocumentsCompressor.DocuWare.Models; 

public class GetDocumentsResponse
{
	public DocumentCount Count { get; set; } = null!;

	public IEnumerable<DocumentItem> Items { get; set; } = null!;

    public IEnumerable<Link>? Links { get; set; }
}