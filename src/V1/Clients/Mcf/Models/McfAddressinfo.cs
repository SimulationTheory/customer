﻿using Newtonsoft.Json;

namespace PSE.Customer.V1.Clients.Mcf.Models
{
    /// <summary>
    /// Object Model For Mailing Addresses From Mcf Get Call
    /// </summary>
    public class McfAddressinfo : McfBaseAddressinfo
    {
        /// <summary>
        /// Gets or sets the valid from date.
        /// </summary>
        /// <value>
        /// The valid from date.
        /// </value>
        [JsonProperty("Validfromdate")]
        public string ValidFromDate { get; set; }

        /// <summary>
        /// Gets or sets the valid to date.
        /// </summary>
        /// <value>
        /// The valid to date.
        /// </value>
        [JsonProperty("Validtodate")]
        public string ValidToDate { get; set; }

        /// <summary>
        /// Gets or sets the type of the address.
        /// </summary>
        /// <value>
        /// The type of the address.
        /// </value>
        [JsonIgnore]
        public McfAddressType AddressType { get; set; }
    }
}
