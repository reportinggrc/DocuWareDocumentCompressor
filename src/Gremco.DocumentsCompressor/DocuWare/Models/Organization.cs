using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gremco.DocumentsCompressor.DocuWare.Models;

public class Organization
{
	public string Id { get; set; }

	public string Name { get; set; }

	public string Guid { get; set; }

	public IEnumerable<Link> Links { get; set; }
}