using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Gremco.DocumentsCompressor.DocuWare.Models;

public class ServiceConfiguration
{
    public string issuer { get; set; }

    public string jwks_uri { get; set; }

    public string authorization_endpoint { get; set; }

    public string token_endpoint { get; set; }
}