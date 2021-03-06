using System.Collections.Generic;
using PSE.Customer.V1.Models;
using PSE.Customer.V1.Repositories.DefinedTypes;

namespace PSE.Customer.Tests.Unit.TestObjects
{
    public static class TestHelper
    {
        /// <summary>
        /// This account is set up to use MCF and has a mailing address and phone number
        /// </summary>
        /// <remarks>
        /// The JwtToken is a access token in a valid format but has expired
        /// </remarks>
        public static readonly TestUser PaDev1 = new TestUser
        {
            BpNumber = 1002647070,
            Username = "testuserpadev1",
            JwtToken = "eyJraWQiOiJSRGlmUFwvbEQ0Q0VVNXJadEJQeWdFSDI5S1haNThoOUtwXC9hRWxzK2t3bUE9IiwiYWxnIjoiUlMyNTYifQ.eyJzdWIiOiIxNzI1Mzc4NS1jN2NkLTRmZTItOTEwOC05Y2NiNDJhZjA0NmIiLCJhdWQiOiI2dGpzc2ZyaGh0cGw1dnRsYWN1bTdjdHVjbyIsImVtYWlsX3ZlcmlmaWVkIjpmYWxzZSwidG9rZW5fdXNlIjoiaWQiLCJhdXRoX3RpbWUiOjE1MjE0OTgxNTgsImlzcyI6Imh0dHBzOlwvXC9jb2duaXRvLWlkcC51cy13ZXN0LTIuYW1hem9uYXdzLmNvbVwvdXMtd2VzdC0yX1pmZzBmVVNwUyIsImN1c3RvbTpicCI6IjEwMDI2NDcwNzAiLCJjb2duaXRvOnVzZXJuYW1lIjoidGVzdHVzZXJwYWRldjEiLCJleHAiOjE1MjE1MDE3NTgsImlhdCI6MTUyMTQ5ODE1OCwiZW1haWwiOiJ0ZXN0dXNlcnBhRGV2MUB0ZXN0LmNvbSJ9.v7drhl7NffykIfWh5ZkqEKuyQbh9G9EhBk167vAshEIpw2BhHSfzU9kwa7wDWR0OF7PXS-V_Wncv5zcvwN4CIgNTvIKITGPcELvQthF2ch2D4eWC5HCnfYNYsMwut7ZpyurYuePM4j_JCBONnuqPlVUQbblWNqUzIZiIokvD0kkXR0xFdcqA6WfWmWHDCnycIsiRJohJlS4uPEpSJQVjsmY-r-XmGVJtlUamSs16WUCSS_uIRjAI19y1-fUw-tz5GaB57KW_GbQfd_nyB2sUyJhdrbUzcgh-jUIOjRDdw-JqEq7IFaE6Dd8yL-f8yDpr8C8DkcA9LdSb7phPOKqhkw",
            Email = "test7@test.com",
            Address = new AddressDefinedType
            {
                AddressLine1 = "1717 S 18TH ST",
                City = "MOUNT VERNON",
                State = "WA",
                PostalCode = "98274-6031"
            },
            Phones = new List<Phone>
            {
                new Phone
                {
                    Type = PhoneType.Cell,
                    Number = "4254573067"
                },
                new Phone
                {
                    Type = PhoneType.Work,
                    Number = "4258824091"
                }
            }
        };
    }
}
