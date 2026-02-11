using ButlerSDK.ApiKeyMgr.Contract;
using Azure.Maps.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ButlerSDK.ApiKeyMgr;
using SecureStringHelper;

namespace ButlerSDK.Tools.Internal
{
    /// <summary>
    /// Internal tool meant to initialize enough of Azure to request a country code and promptly be disposed
    /// </summary>
    internal sealed class AzureCountryCodeHelper : IDisposable
    {
        public enum AzureCountryCodeCall_Result
        {
            /// <summary>
            ///  call hasn't happened yet
            /// </summary>
            Unknown,
            /// <summary>
            /// The call to <see cref="AzureGPSHelper.ResolveAddressToGPS(string, bool, out AzureCountryCodeCall_Result, SearchAddressOptions?, CancellationToken)"/> OK
            /// </summary>
            Success,
            /// <summary>
            /// couldn't fetch the key from the vault
            /// </summary>
            NoApiKeyFound,
            /// <summary>
            /// key was fetched OK BUT we couldn't create <see cref="Azure.AzureKeyCredential"/> object
            /// </summary>
            ApiKeyInitFailure,
            /// <summary>
            /// json trouble
            /// </summary>
            JsonSerialzeProblem

        }
        bool Disposed = false;
        Azure.AzureKeyCredential? Cred = null;

        string TargetVar;
        IButlerVaultKeyCollection? KeyReader;

        public AzureCountryCodeHelper(IButlerVaultKeyCollection Key, string TargetVar = "AZUREMAPS")
        {
            ArgumentNullException.ThrowIfNull(Key);
            ArgumentNullException.ThrowIfNull(TargetVar);
            this.KeyReader = Key;
            this.TargetVar = TargetVar;
        }


        /// <summary>
        /// Asks Azure for the country code this IP belongs too
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public string? ResolveIPToCountryCode(IPAddress ip, bool ForceJson, out AzureCountryCodeCall_Result Result)
        {
            try
            {
                // A word. KeyReader shouldn't be null - constructor punts back if it's null on assignment.
                // Additionally, We're counting  on this thing returning null on resolve key if it can't find the right key plus
                // assuming AzureKeyCredential will go wait - i can't do that with null, and throw argument null - which we catch
                Cred ??= new Azure.AzureKeyCredential(
                    (
                    (KeyReader!.ResolveKey(TargetVar) ?? throw new ArgumentException(TargetVar))
                    ).
                    DecryptString());
            }
            catch (ArgumentException)
            {
                Result = AzureCountryCodeCall_Result.NoApiKeyFound;
                return null;
            }
            if (Cred is null)
            {
                Result = AzureCountryCodeCall_Result.ApiKeyInitFailure;
                return null;
            }
            Azure.Maps.Geolocation.MapsGeolocationClient GeoLocation = new Azure.Maps.Geolocation.MapsGeolocationClient(Cred);

            if (GeoLocation is not null)
            {
                var DataRes = GeoLocation.GetCountryCode(ip).Value.IsoCode;
                if (ForceJson)
                {
                    Dictionary<string, string> Json = new Dictionary<string, string>();
                    Json["CountryCode"] = DataRes;
                    Json["IP"] = ip.ToString();
                    Result = AzureCountryCodeCall_Result.Success;
                    return JsonSerializer.Serialize(Json);
                }
                else
                {
                    Result = AzureCountryCodeCall_Result.Success;
                    return DataRes;
                }
            }
            else
            {
                Result = AzureCountryCodeCall_Result.Unknown;
                return null;
            }

        }

        public void Dispose()
        {
            if (!Disposed)
            {
                this.KeyReader = null;
                this.Cred = null;
                this.TargetVar = null!;
                Disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~AzureCountryCodeHelper()
        {
            Dispose();
        }
    }


    internal sealed class AzureGPSHelper : IDisposable
    {
        public enum AzureGPSLookUp_Results
        {
            /// <summary>
            ///  call hasn't happened yet
            /// </summary>
            Unknown,
            /// <summary>
            /// The call to <see cref="AzureGPSHelper.ResolveAddressToGPS(string, bool, out AzureGPSLookUp_Results, SearchAddressOptions?, CancellationToken)"/> OK
            /// </summary>
            Success,
            /// <summary>
            /// couldn't fetch the key from the vault
            /// </summary>
            NoApiKeyFound,
            /// <summary>
            /// key was fetched OK BUT we couldn't create <see cref="Azure.AzureKeyCredential"/> object
            /// </summary>
            ApiKeyInitFailure,
            /// <summary>
            /// The json in ForceJson mode of <see cref="AzureGPSHelper.ResolveAddressToGPS(string, bool, out AzureGPSLookUp_Results, SearchAddressOptions?, CancellationToken)"/> was not able to be converted OK to string
            /// </summary>
            JsonSerialzeProblem

        }
        bool Disposed = false;
        Azure.AzureKeyCredential? Cred = null;

        string TargetVar;
        IButlerVaultKeyCollection? KeyReader;
        public AzureGPSHelper(IButlerVaultKeyCollection Key, string TargetVar = "AZUREMAPS")
        {
            ArgumentNullException.ThrowIfNull(Key);
            ArgumentNullException.ThrowIfNull(TargetVar);
            this.KeyReader = Key;
            this.TargetVar = TargetVar;
        }

        public string? ResolveAddressToGPS(string address, bool ForceJson, out AzureGPSLookUp_Results Results, SearchAddressOptions? Opts = null, CancellationToken cancellation = default)
        {


            Dictionary<string, object> json;
            StringBuilder sb = new();

            try
            {
                Cred ??= new Azure.AzureKeyCredential(
                    (
                    (KeyReader!.ResolveKey(TargetVar) ?? throw new ArgumentException(TargetVar))
                    ).
                    DecryptString());
            }
            catch (ArgumentException)
            {
                Results = AzureGPSLookUp_Results.NoApiKeyFound;
                return null;
            }
            if (Cred is null)
            {
                Results = AzureGPSLookUp_Results.ApiKeyInitFailure;
                return null;
            }
            MapsSearchClient Maps = new(Cred);

            var rets = Maps.SearchAddress(WebUtility.UrlEncode(address), Opts, cancellation);

            if (!ForceJson)
            {
                if (rets.Value.Results.Count > 0)
                {

                    for (int i = 0; i < rets.Value.Results.Count; i++)
                    {
                        sb.AppendLine($"result {i + 1} : ({rets.Value.Results[i].Position.Latitude}, {rets.Value.Results[i].Position.Longitude}) ");
                    }
                }
                else
                {
                    sb.AppendLine($"0 GPS coordinates found for {address}. Was it correct?");
                }

                Results = AzureGPSLookUp_Results.Success;
                return sb.ToString();
            }
            else
            {
                json = new()
                {
                    ["resultnumber"] = rets.Value.Results.Count.ToString(),
                    ["address"] = address,
                    ["results"] = new List<string>()
                };
                for (int i = 0; i < rets.Value.Results.Count; i++)
                {
                    ((List<string>)json["results"]).Add($"{rets.Value.Results[i].Position.Latitude}, {rets.Value.Results[i].Position.Longitude}");
                }
                string? ret;
                try
                {
                    ret = JsonSerializer.Serialize(json);
                }
                catch (NotSupportedException)
                {
                    Results = AzureGPSLookUp_Results.JsonSerialzeProblem;
                    return null;
                }
                Results = AzureGPSLookUp_Results.Success;
                return ret;

            }


        }
        public void Dispose()
        {
            if (!Disposed)
            {
                this.KeyReader?.Dispose();
                this.KeyReader = null;
                this.Cred = null;
                this.TargetVar = null!;
            }
            Disposed = true;
            GC.SuppressFinalize(this);
        }

        ~AzureGPSHelper() { Dispose(); }


    }
}
