using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gremco.DocumentsCompressor.DocuWare.Models; 

public class OrganizationResponse
{
	public IEnumerable<Organization> Organization { get; set; }
}