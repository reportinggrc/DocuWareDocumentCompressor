using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gremco.DocumentsCompressor.DocuWare.Models; 

public class DocumentCount
{
	public bool HasMore { get; set; }

	public bool ExceedLimit { get; set; }

	public int Value { get; set; }
}