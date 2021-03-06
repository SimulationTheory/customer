﻿using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PSE.Customer.V1.Repositories.DefinedTypes;

namespace PSE.Customer.V1.Models
{
    public class Phone
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [Required]
        public PhoneType Type { get; set; }

        [Required]
        public string Number { get; set; }

        public string Extension { get; set; }
    }
}
