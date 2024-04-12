using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gremco.DocumentsCompressor.DocuWare.Models; 

public class GetFileCabinetsResponse
{
	public IEnumerable<FileCabinet> FileCabinet { get; set; }
}