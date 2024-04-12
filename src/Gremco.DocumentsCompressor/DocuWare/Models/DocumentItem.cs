using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gremco.DocumentsCompressor.DocuWare.Models;

public class DocumentItem
{
	public long Id { get; set; }

	public string ContentType { get; set; }

	public int TotalPages { get; set; }

	public long FileSize { get; set; }

	public string Title { get; set; }

	public string CreatedAt { get; set; }

	public string LastModified { get; set; }

	public string FileCabinetId { get; set; }

	public IEnumerable<Link> Links { get; set; }

	public IEnumerable<Section> Sections { get; set; }
}