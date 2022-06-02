using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SC = System.Data.SqlClient;
using TT = System.Threading.Tasks;
using AD = Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AuthDemoWinForms
{
    public class ActiveDirectoryAuthProvider : SC.SqlAuthenticationProvider
    {
        static public readonly string ClientApplicationID = "acbdaf98-6949-45dc-bfc4-4de781e1317f";
        static public readonly Uri RedirectUri = new Uri("https://login.microsoftonline.com/common/oauth2/nativeclient");

        // Program._ more static values that you set!
        private readonly string _clientId = ClientApplicationID;
        private readonly Uri _redirectUri = RedirectUri;

        public override async TT.Task<SC.SqlAuthenticationToken>
            AcquireTokenAsync(SC.SqlAuthenticationParameters parameters)
        {
            AD.AuthenticationContext authContext =
                new AD.AuthenticationContext(parameters.Authority);
            authContext.CorrelationId = parameters.ConnectionId;
            AD.AuthenticationResult result;

            switch (parameters.AuthenticationMethod)
            {
                case SC.SqlAuthenticationMethod.ActiveDirectoryInteractive:
                    Console.WriteLine("In method 'AcquireTokenAsync', case_0 == '.ActiveDirectoryInteractive'.");

                    result = await authContext.AcquireTokenAsync(
                        parameters.Resource,  // "https://database.windows.net/"
                        _clientId,
                        _redirectUri,
                        new AD.PlatformParameters(AD.PromptBehavior.Auto),
                        new AD.UserIdentifier(
                            parameters.UserId,
                            AD.UserIdentifierType.RequiredDisplayableId));
                    break;

                //case SC.SqlAuthenticationMethod.ActiveDirectoryIntegrated:
                //    Console.WriteLine("In method 'AcquireTokenAsync', case_1 == '.ActiveDirectoryIntegrated'.");

                //    result = await authContext.AcquireTokenAsync(
                //        parameters.Resource,
                //        _clientId,
                //        new AD.UserCredential());
                //    break;

                //case SC.SqlAuthenticationMethod.ActiveDirectoryPassword:
                //    Console.WriteLine("In method 'AcquireTokenAsync', case_2 == '.ActiveDirectoryPassword'.");

                //    result = await authContext.AcquireTokenAsync(
                //        parameters.Resource,
                //        _clientId,
                //        new AD.UserPasswordCredential(
                //            parameters.UserId,
                //            parameters.Password));
                //    break;

                default: throw new InvalidOperationException();
            }
            return new SC.SqlAuthenticationToken(result.AccessToken, result.ExpiresOn);
        }

        public override bool IsSupported(SC.SqlAuthenticationMethod authenticationMethod)
        {
            return authenticationMethod == SC.SqlAuthenticationMethod.ActiveDirectoryIntegrated
                || authenticationMethod == SC.SqlAuthenticationMethod.ActiveDirectoryInteractive
                || authenticationMethod == SC.SqlAuthenticationMethod.ActiveDirectoryPassword;
        }
    } // EOClass ActiveDirectoryAuthProvider.
}
