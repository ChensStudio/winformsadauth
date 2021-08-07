using Microsoft.Identity.Client;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            // User ID is optional for .NET Core and .NET Standard.
            string ConnectionString = @"Server=tcp:mf-west-us-svr1.database.windows.net,1433;Initial Catalog=model-force-dev;Persist Security Info=False;MultipleActiveResultSets=False;
                                                Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Integrated;User Id=ychen@chenyangyinpengmsn.onmicrosoft.com;";

            //string ConnectionString = @"Server = mf-west-us-svr1.database.windows.net,1433; Authentication = Active Directory Integrated; Database = model-force-dev;";  

            // Provide the query string with a parameter placeholder.
            string queryString = "select Name, ListPrice, Weight "
                            + "from [SalesLT].[Product] "
                            + "where ProductID = 680";

            // Create and open the connection in a using block. This
            // ensures that all resources will be closed and disposed
            // when the code exits.
            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            {
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
}
