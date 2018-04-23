﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSE.Customer.Configuration;
using PSE.Customer.V1.Clients.Authentication.Interfaces;
using PSE.Customer.V1.Logic.Interfaces;
using PSE.Customer.V1.Models;
using PSE.Customer.V1.Repositories;
using PSE.Customer.V1.Repositories.Entities;
using PSE.Customer.V1.Repositories.DefinedTypes;
using PSE.Customer.V1.Repositories.Interfaces;
using PSE.WebAPI.Core.Configuration.Interfaces;
using RestSharp;
using Microsoft.AspNetCore.Mvc;
using PSE.Customer.V1.Clients.Mcf.Interfaces;
using System.Linq;
using PSE.Customer.Extensions;
using PSE.Customer.V1.Clients.Mcf.Enums;
using PSE.Customer.V1.Clients.Mcf.Request;
using PSE.Customer.V1.Clients.Mcf.Response;
using PSE.RestUtility.Core.Mcf;
using PSE.Customer.V1.Clients.Address.Interfaces;
using PSE.WebAPI.Core.Service.Enums;
using AutoMapper;
using PSE.Customer.V1.Clients.Address.Models.Request;

namespace PSE.Customer.V1.Logic
{
    /// <summary>
    /// Customer logic class
    /// </summary>
    /// <seealso cref="PSE.Customer.V1.Logic.Interfaces.ICustomerLogic" />
    public class CustomerLogic : ICustomerLogic
    {
        private readonly ILogger<CustomerLogic> _logger;
        private readonly IDistributedCache _redisCache;
        private readonly IMemoryCache _localCache;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ICoreOptions _options;
        private readonly IBPByContractAccountRepository _bpByContractAccountRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IAuthenticationApi _authenticationApi;
        private readonly IMcfClient _mcfClient;
        private readonly IAddressApi _addressApi;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomerLogic"/> class.
        /// </summary>
        /// <param name="redisCache">The redis cache.</param>
        /// <param name="localCache">The local cache.</param>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The options.</param>
        /// <param name="bpByContractAccountRepository">The bp by contract account repository.</param>
        /// <param name="customerRepository">The customer repository.</param>
        /// <param name="authenticationApi">The authentication API.</param>
        /// <param name="mcfClient"></param>
        /// <param name="addressApi"></param>
        public CustomerLogic(
            IDistributedCache redisCache,
            IMemoryCache localCache,
            IOptions<AppSettings> appSettings,
            ILogger<CustomerLogic> logger,
            ICoreOptions options,
            IBPByContractAccountRepository bpByContractAccountRepository,
            ICustomerRepository customerRepository,
            IAuthenticationApi authenticationApi,
            IMcfClient mcfClient,
            IAddressApi addressApi)
        {
            _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
            _localCache = localCache ?? throw new ArgumentNullException(nameof(localCache));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _bpByContractAccountRepository = bpByContractAccountRepository ?? throw new ArgumentNullException(nameof(bpByContractAccountRepository));
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository)); 
            _authenticationApi = authenticationApi ?? throw new ArgumentNullException(nameof(authenticationApi));
            _mcfClient = mcfClient ?? throw new ArgumentNullException(nameof(mcfClient));
            _addressApi = addressApi ?? throw new ArgumentNullException(nameof(addressApi));
        }

        /// <summary>
        /// Gets bp ID and acct status while validating acct ID and fullName.
        /// </summary>
        /// <param name="lookupCustomerRequest">The lookup customer request.</param>
        /// <returns></returns>
        public async Task<LookupCustomerModel> LookupCustomer(LookupCustomerRequest lookupCustomerRequest)
        {
            LookupCustomerModel lookupCustomerModel = new LookupCustomerModel();

            // Validate acct ID with bp ID lookup
            BPByContractAccountEntity bpByContractAccountEntity =
                await _bpByContractAccountRepository.GetBpByContractAccountId(lookupCustomerRequest.ContractAccountNumber);
            if (bpByContractAccountEntity != null)
            {
                // Update return value
                lookupCustomerModel.BPId = bpByContractAccountEntity.BusinessPartner_Id;

                // Get customer for this bp
                CustomerEntity customerEntity = await _customerRepository.GetCustomerByBusinessPartnerId(bpByContractAccountEntity.BusinessPartner_Id);
                if (customerEntity != null)
                {
                    try
                    {
                        // Validate name against Customer table- all uppercase
                        if (lookupCustomerRequest.NameOnBill.ToUpper() != customerEntity.FullName)
                        {
                            // Pass null model to indicate not found
                            lookupCustomerModel = null;
                        }
                        else
                        {
                            // Check for web account in auth
                            var accountExistsResponse = await _authenticationApi.GetAccountExists(customerEntity.BusinessPartnerId);
                            if (accountExistsResponse?.Data?.Exists != null)
                            {
                                // Update return value
                                lookupCustomerModel.HasWebAccount = accountExistsResponse.Data.Exists;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("GetAccountExists API call failed for " +
                                         $"{nameof(customerEntity.BusinessPartnerId)}: {customerEntity.BusinessPartnerId}\n" +
                                         $"{e.Message}");
                        throw;
                    }
                }
                else
                {
                    // Pass null model to indicate not found
                    lookupCustomerModel = null;
                }
            }
            else
            {
                // Pass null model to indicate not found
                lookupCustomerModel = null;
            }

            return lookupCustomerModel;
        }



        /// <summary>
        /// Returns CustomerProfileModel based customer and customer contact information retrieved from Cassandra
        /// </summary>
        /// <param name="bpId">Business partner ID</param>
        /// <returns>Awaitable CustomerProfileModel result</returns>
        public async Task<GetCustomerInteractionResponse> CreateCustomerInteractionRecord(CreateCustomerInteractionRequest createCustomerInteraction,string jwt)
        {
            GetCustomerInteractionResponse interactionResponse = _mcfClient.CreateCustomerInteractionRecord(createCustomerInteraction, jwt);
           

            return  interactionResponse;
        }
        /// <summary>
        /// Returns CustomerProfileModel based customer and customer contact information retrieved from Cassandra
        /// </summary>
        /// <param name="bpId">Business partner ID</param>
        /// <returns>Awaitable CustomerProfileModel result</returns>
        public async Task<CustomerProfileModel> GetCustomerProfileAsync(long bpId)
        {
            var getCustomer = _customerRepository.GetCustomerAsync(bpId);
            var getCustomerContact = _customerRepository.GetCustomerContactAsync(bpId);

            await Task.WhenAll(getCustomer, getCustomerContact);

            var customer = getCustomer.Result;
            var customerContact = getCustomerContact.Result;

            var model = customer.ToModel();

            customerContact.AddToModel(model);

            return model;
        }

        /// <summary>
        /// Saves the mailing address at the BP level
        /// </summary>
        /// <param name="address">Full mailing address</param>
        /// <param name="bpId">Business partner ID</param>
        /// <returns>Status code of async respository call</returns>
        public async Task PutMailingAddressAsync(AddressDefinedType address, long bpId)
        {
            _logger.LogInformation($"PutMailingAddressAsync({nameof(address)}: {address}," +
                                   $"{nameof(bpId)}: {bpId})");

            // This returns an empty set and the IsFullyFetched property is true.
            // There is apparently no way to determine if any rows were updated or not,
            // so unless an exception occurs, NoContent will always be returned.
            await _customerRepository.UpdateCustomerMailingAddress(address, bpId);
        }

        /// <summary>
        /// Saves the email address at the BP level
        /// </summary>
        /// <param name="jwt">Java web token for authentication</param>
        /// <param name="emailAddress">Customer email address</param>
        /// <param name="bpId">Business partner ID</param>
        /// <returns>Status code of async respository call</returns>
        public async Task PutEmailAddressAsync(string jwt, string emailAddress, long bpId)
        {
            _logger.LogInformation($"PutEmailAddressAsync(jwt, " +
                                   $"{nameof(emailAddress)}: {emailAddress}," +
                                   $"{nameof(bpId)}: {bpId})");

            // Call MCF to update SAP first.  If no exception is thrown, then update Cassandra.
            _mcfClient.CreateBusinessPartnerEmail(jwt, new CreateEmailRequest
            {
                AccountID = bpId.ToString(),
                Email = emailAddress,
                StandardFlag = true
            });

            // This returns an empty set and the IsFullyFetched property is true.
            // There is apparently no way to determine if any rows were updated or not,
            // so unless an exception occurs, NoContent will always be returned.
            await _customerRepository.UpdateCustomerEmailAddress(emailAddress, bpId);

            var success = await _authenticationApi.SyncUserEmail(jwt, RequestChannelEnum.Web);
        }

        /// <summary>
        /// Saves the cell phone number at the BP level
        /// </summary>
        /// <param name="jwt">Java web token for authentication</param>
        /// <param name="phone">Customer's cell phone</param>
        /// <param name="bpId">Business partner ID</param>
        public async Task PutPhoneNumberAsync(string jwt, Phone phone, long bpId)
        {
            _logger.LogInformation($"PutEmailAddressAsync({nameof(phone)}: {phone.ToJson()}," +
                                   $"{nameof(bpId)}: {bpId})");

            // Call MCF to update SAP first.  If no error, then update Cassandra.
            McfResponse<GetPhoneResponse> response = null;
            McfResponse<GetAccountAddressesResponse> addressResponse = null;
            if (phone.Type == PhoneType.Cell)
            {
                // Prepare MCF request
                var mcfRequest = new CreateAddressIndependantPhoneRequest
                {
                    BusinessPartnerId = bpId,
                    PhoneNumber = phone.Number,
                    Extension = phone.Extension ?? "",
                    IsHome = true,
                    IsStandard = true,
                    PhoneType = AddressIndependantContactInfoEnum.AccountAddressIndependentMobilePhones
                };

                // Save phone via MCF
                response = _mcfClient.CreateBusinessPartnerMobilePhone(jwt, mcfRequest);
            }
            else
            {
                addressResponse = _mcfClient.GetStandardMailingAddress(jwt, bpId);
                if (addressResponse.Error == null)
                {
                    // Prepare MCF request
                    var mcfDepRequest = new CreateAddressDependantPhoneRequest
                    {
                        BusinessPartnerId = bpId,
                        AddressId = addressResponse.Result.AddressID.ToString(),
                        PhoneNumber = phone.Number,
                        Extension = phone.Extension ?? "",
                        IsHome = true,
                        IsStandard = true,
                        PhoneType = "1"
                    };

                    // Save phone via MCF
                    response = _mcfClient.CreateAddressDependantPhone(jwt, mcfDepRequest);
                }
            }

            if (response?.Error != null)
            {
                _logger.LogError($"Failure saving phone number to SAP: {response.Error.ToJson()}");
            }
            else if (addressResponse?.Error != null)
            {
                _logger.LogError($"Failure getting standard mail address: {addressResponse.Error.ToJson()}");
            }
            else
            {
                _logger.LogInformation($"Success saving phone number to SAP: {response?.Result.ToJson()}");

                // This returns an empty set and the IsFullyFetched property is true.
                // There is apparently no way to determine if any rows were updated or not,
                // so unless an exception occurs, NoContent will always be returned.
                await _customerRepository.UpdateCustomerPhoneNumber(phone, bpId);
            }
        }

        /// <summary>
        /// Creates profile
        /// By signing up user in cognito and cassandra calling sign up API
        /// Save security questions by calling the security Questions API
        /// in eash step it will return approrate http status code
        /// </summary>
        /// <param name="webprofile"></param>
        /// <returns></returns>
        public async Task  CreateWebProfileAsync(WebProfile webprofile)
        {
            _logger.LogInformation($"CreateWebProfileAsync({nameof(webprofile)}: {webprofile}");

            //Signs up customer in cognito and cassandra
            var resp = await _authenticationApi.SignUpCustomer(webprofile);
            if (resp.StatusCode != HttpStatusCode.Created)
            {
                var message = resp.Content;
                _logger.LogError($"Unable to  sign up for user name {webprofile?.CustomerCredentials?.UserName} and Bp {webprofile?.BPId} with error message from Auth sign up service {message}");
                throw new Exception(message);
            }
            //save security questions
            await SaveSecurityQuestions(webprofile, resp);
        }

        /// <summary>
        /// Checks if the username exists by calling the Authentication service api
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public async Task<bool> UserNameExists(string userName)
        {
            bool usernameExists = true;
            // Check for web account in auth
            var userNameExistsResponse = await _authenticationApi.GetUserNameExists(userName);
            if (userNameExistsResponse?.Data?.Exists != null)
            {
                // Update return value
                usernameExists = userNameExistsResponse.Data.Exists;
            }
            return usernameExists;
        }

        /// <summary>
        ///  Gets the Mailing Addresses For A Given BP
        /// </summary>
        /// <param name="bpId"></param>
        /// <param name="isStandardOnly"></param>
        /// <param name="jwt"></param>
        /// <returns></returns>
        public async Task<IEnumerable<MailingAddressesModel>> GetMailingAddressesAsync(long bpId, bool isStandardOnly, string jwt)
        {
            var mailingAddresses = new List<MailingAddressesModel>();

            var key = $"selfservice:BusinessPartner:{bpId}:Address";

            var mcfResponse = _mcfClient.GetMailingAddresses(jwt, bpId);

            var results = (isStandardOnly ? mcfResponse?.Result?.Results?
                                    .Where(x => x.AddressInfo.StandardAddress)
                                    : mcfResponse?.Result?.Results)
                                    .ToList();

            results?.ForEach(x=> mailingAddresses.Add(new MailingAddressesModel
                                                    {
                                                        AddressID = x.AddressID.Value,
                                                        Address = x.AddressInfo.McfToCassandraModel()
                                                    }));

            await _redisCache.SetStringAsync(key, mailingAddresses.ToJson());


            return mailingAddresses;

        }

        /// <summary>
        /// Upserts the standard mailing address.
        /// </summary>
        /// <param name="bpId">The bp identifier.</param>
        /// <param name="address">The address.</param>
        /// <param name="jwt">The JWT.</param>
        /// <returns></returns>
        public async Task<long> UpsertStandardMailingAddressAsync(long bpId, UpdateMailingAddressModel address, string jwt)
        {
            _logger.LogInformation($"UpsertStandardMailingAddress({nameof(bpId)}: {bpId}," +
                                   $"{nameof(address)}: {address.ToJson()})");

            //Format To SaP Address From Cassandra
            var addressFormatRequest= Mapper.Map<AddressDefinedTypeRequest>(address);

            var addressResponse = await _addressApi.ToMcfMailingAddressAsync(addressFormatRequest);

            if (addressResponse != null)
            {
                var addressInfo = addressResponse.Data;

                var response = _mcfClient.GetStandardMailingAddress(jwt, bpId);

                var addressId = response.Result.AddressID;

                if (addressId != null)
                {
                    var request = new UpdateAddressRequest
                    {
                        AccountID = bpId,
                        AddressID = addressId.Value,
                        AddressInfo = addressInfo
                    };

                    UpdateStandardAddress(jwt, request);
                }
                else
                {
                    var request = new CreateAddressRequest
                    {
                        AccountID = bpId,
                        AddressInfo = addressInfo
                    };

                    addressId = CreateStandardAddress(jwt, request).Result.AddressID;
                }

                return addressId.Value; 
            }
            return default(long);
        }

        /// <summary>
        /// Get JWT Token
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<string> GetJWTTokenAsync(string userName, string password)
        {
            string token = null;
            try
            {
                //gets JwtToken
                var jwttoken = await _authenticationApi.GetJwtToken(userName, password);
                token = jwttoken?.Data?.JwtAccessToken;
                if (string.IsNullOrEmpty(token))
                {
                    var message = jwttoken?.Content;
                    _logger.LogError($"Unable to signin and get jwt token after sign up for user name {userName} with error message from Auth sign in service {message}");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError($"Getting JWTtoken from Signin for user name {userName}  Failed with error", ex);
            }
            
            return token;
        }

        #region private methods
        private async Task SaveSecurityQuestions(WebProfile webprofile, IRestResponse<OkResult> resp)
        {
            try
            {
                //gets JwtToken
                var jwttoken = await _authenticationApi.GetJwtToken(webprofile?.CustomerCredentials?.UserName, webprofile?.CustomerCredentials?.Password);

                //Save security questions one at at time    
                if (jwttoken.StatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(jwttoken?.Data?.JwtAccessToken))
                {
                    var savesecurityQuestions = await _authenticationApi.SaveSecurityQuestions(webprofile, jwttoken.Data.JwtAccessToken);

                    if (savesecurityQuestions.StatusCode != HttpStatusCode.Created)
                    {
                        //throw eception
                        var message = resp.Content;
                        _logger.LogError($"Security Questions can not be created for user name {webprofile?.CustomerCredentials?.UserName} and Bp {webprofile.BPId}");
                        //throw new Exception(message);
                    }
                }
                else
                {
                    var message = jwttoken.Content;
                    _logger.LogError($"Unable to signin and get jwt token after sign up for user name {webprofile?.CustomerCredentials?.UserName} and Bp {webprofile.BPId} with error message from Auth sign in service {message}");
                }
            }
            catch (Exception exp)
            {
                _logger.LogError("Saveing Security questions failed with unexpected error", exp);
            }
        }
        
        private void UpdateStandardAddress(string jwt, UpdateAddressRequest request)
        {
            request.AddressInfo.StandardAddress = true;

            _mcfClient.UpdateAddress(jwt, request);
        }

        private McfResponse<CreateAddressResponse> CreateStandardAddress(string jwt, CreateAddressRequest request)
        {
            request.AddressInfo.StandardAddress = true;

            return _mcfClient.CreateAddress(jwt, request);
        }
        #endregion
    }
}
