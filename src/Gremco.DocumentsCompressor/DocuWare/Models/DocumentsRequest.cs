using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gremco.DocumentsCompressor.DocuWare.Models
{
    public class DocumentsRequest
    {
        public List<DocumentsCondition> Condition { get; set; }
        public string Operation { get; set; }
        public int Start { get; set; }
        public int Count { get; set; }
    }
}
