﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PSE.Customer.V1.Models;
using PSE.Customer.V1.Repositories.DefinedTypes;
using PSE.WebAPI.Core.Interfaces;
using System.Collections.Generic;

namespace PSE.Customer.V1.Response
{
    public class GetCustomerProfileResponse : IAPIResponse
    {
        public string CustomerName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public bool FraudCheck { get; set; }
        public string OrganizationName { get; set; }
        public bool IsPva { get; set; }
        public string EmailAddress { get; set; }
        public AddressDefinedType MailingAddress { get; set; }
        public IEnumerable<Phone> Phones { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public PhoneType? PrimaryPhone { get; set; }
    }
}
