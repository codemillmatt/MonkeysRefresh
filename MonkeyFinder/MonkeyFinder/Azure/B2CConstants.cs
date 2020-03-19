using System;
namespace MonkeyFinder
{
    public static class B2CConstants
    {
        // Azure AD B2C Coordinates
        public static string Tenant = "foxamarin.onmicrosoft.com";
        public static string AzureADB2CHostname = "foxamarin.b2clogin.com";
        public static string ClientID = "87fb2adf-7358-4509-9636-1e5d19015c6e";
        public static string PolicySignUpSignIn = "B2C_1_SignUpSignIn";
        
        public static string[] Scopes = { "" };

        public static string AuthorityBase = $"https://{AzureADB2CHostname}/tfp/{Tenant}/";
        public static string AuthoritySignInSignUp = $"{AuthorityBase}{PolicySignUpSignIn}";        
        public static string IOSKeyChainGroup = "com.microsoft.codemillmatt";
    }
}
