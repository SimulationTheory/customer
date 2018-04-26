﻿using System;
using System.ComponentModel.DataAnnotations;

namespace PSE.Customer.V1.Request
{
    public class CreateBpRelationshipRequest
    {
        public string FirstAccountBpId { get; set; }
        public string SecondAccountBpId { get; set; }
        public string Relationshipcategory { get; set; }
    }
}