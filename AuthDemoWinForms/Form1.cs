using Microsoft.Identity.Client;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

using SC = System.Data.SqlClient;
using TT = System.Threading.Tasks;
using AD = Microsoft.IdentityModel.Clients.ActiveDirectory;


namespace AuthDemoWinForms
{
    public partial class Form1 : Form
    {
        private static bool loggeIn = false;
        private static readonly string[] scopes = { "user.read" };

        public Form1()
        {
            InitializeComponent();
        }

        private string GetCurrentPrincipal()
        {
            var currentUser = WindowsIdentity.GetCurrent();
            return currentUser.Name;

        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            if (!loggeIn)
            {
                //Windows Auth
                WindowsIdentity current = WindowsIdentity.GetCurrent();
                WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
                label1.Text = windowsPrincipal.Identity.Name;

                var authResult = await Login();
                label4.Text = authResult.Account.Username;

                button1.Text = "Log Out";
                loggeIn = true;
            }
            else
            {
                await Logout();
                button1.Text = "Log In";
                loggeIn = false;
            }

        }

        private async Task<AuthenticationResult> Login()
        {
            AuthenticationResult authResult = null;
            var accounts = await Program.PublicClientApp.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();

            try
            {
                authResult = await Program.PublicClientApp.AcquireTokenSilent(scopes, firstAccount)
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilent.
                // This indicates you need to call AcquireTokenInteractive to acquire a token
                System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

                try
                {
                    authResult = await Program.PublicClientApp.AcquireTokenInteractive(scopes)
                        .WithAccount(accounts.FirstOrDefault())
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync();
                }
                catch (MsalException msalex)
                {
                    label1.Text = $"Error Acquiring Token:{System.Environment.NewLine}{msalex}";
                }
            }
            catch (Exception ex)
            {
                label1.Text = $"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}";
            }
            return authResult;
        }

        private async Task Logout()
        {

            var accounts = await Program.PublicClientApp.GetAccountsAsync();
            if (accounts.Any())
            {
                try
                {
                    await Program.PublicClientApp.RemoveAsync(accounts.FirstOrDefault());
                    this.label1.Text = "User has signed-out";
                    this.label4.Text = "User has signed-out";
                }
                catch (MsalException ex)
                {
                    throw new Exception($"Error signing-out user: {ex.Message}");
                }
            }
        }

        private void btnGetData_Click(object sender, EventArgs e)
        {
            if (label4.Text == "Anonymous")
            {
                MessageBox.Show("Please login first");
                return;
            }

            int productID;

            try
            {
                productID = Int32.Parse(txtProductID.Text);
            }
            catch (FormatException ex)
            {
                throw ex;
            }

            // User ID is optional for .NET Core and .NET Standard.
            //string ConnectionString = @"Server=tcp:mf-west-us-svr1.database.windows.net,1433;Initial Catalog=model-force-dev;Persist Security Info=False;MultipleActiveResultSets=False;
            //                                    Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";


            // Provide the query string with a parameter placeholder.
            string queryString = String.Format("select Name, ListPrice, Weight "
                            + "from [SalesLT].[Product] "
                            + "where ProductID = {0}", productID.ToString());


            SC.SqlConnectionStringBuilder builder = new SC.SqlConnectionStringBuilder();

            // Program._  static values that you set earlier.
            builder["Data Source"] = "tcp:mf-west-us-svr1.database.windows.net,1433";
            builder.UserID = label4.Text;
            builder["Initial Catalog"] = "model-force-dev";

            // This "Password" is not used with .ActiveDirectoryInteractive.
            //builder["Password"] = "<YOUR PASSWORD HERE>";

            builder["Connect Timeout"] = 15;
            builder["TrustServerCertificate"] = true;
            builder.Pooling = false;

            // Assigned enum value must match the enum given to .SetProvider().
            builder.Authentication = SC.SqlAuthenticationMethod.ActiveDirectoryInteractive;

            // Create and open the connection in a using block. This
            // ensures that all resources will be closed and disposed
            // when the code exits.
            using (SqlConnection connection =
                new SqlConnection(builder.ConnectionString))
            {
                var provider = new ActiveDirectoryAuthProvider();

                SC.SqlAuthenticationProvider.SetProvider(
                    SC.SqlAuthenticationMethod.ActiveDirectoryInteractive
                    ,provider);

                // Create the Command and Parameter objects.
                SqlCommand command = new SqlCommand(queryString, connection);

                // Open the connection in a try/catch block.
                // Create and execute the DataReader, writing the result
                // set to the console window.
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        textBox1.Text = string.Format("\t{0}\t{1}\t{2}",
                            reader[0], reader[1], reader[2]);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                Console.ReadLine();
            }
        }
    }

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
