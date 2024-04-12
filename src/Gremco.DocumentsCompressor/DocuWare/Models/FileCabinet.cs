using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gremco.DocumentsCompressor.DocuWare.Models; 

public class FileCabinet
{
	public string Id { get; set; }

	public string Name { get; set; }

	public bool IsBasket { get; set; }

	public bool Usable { get; set; }

	public bool Default { get; set; }

	public IEnumerable<Link> Links { get; set; }
}