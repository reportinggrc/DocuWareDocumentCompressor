using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gremco.DocumentsCompressor.DocuWare.Models;

public class IdentityServiceInformation
{
    public string IdentityServiceUrl { get; set; }

    public bool RefreshTokenSupported { get; set; }
}