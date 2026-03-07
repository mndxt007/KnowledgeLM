using KnowledgeLM.App.Helpers;

namespace KnowledgeLM.App
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            this.blazorWebView.WebResourceRequested += BlazorWebView_WebResourceRequested;
        }

        private void BlazorWebView_WebResourceRequested(object? sender, WebViewWebResourceRequestedEventArgs webResourceRequestedEventArgs)
        {
            //Check URI is a local file
            if (!webResourceRequestedEventArgs.Uri.AbsolutePath.StartsWith("/Notes"))
                return;

            var assetsFilePath = Path.Combine("Q:\\Dev",webResourceRequestedEventArgs.Uri.LocalPath.TrimStart('/'));


            try
            {
                FileStream fs = File.OpenRead(assetsFilePath);
                ManagedStream ms = new ManagedStream(fs);
                string headers = "";
                if (assetsFilePath.EndsWith(".html"))
                {
                    headers = "Content-Type: text/html";
                }
                else if (assetsFilePath.EndsWith(".jpg"))
                {
                    headers = "Content-Type: image/jpeg";
                }
                else if (assetsFilePath.EndsWith(".png"))
                {
                    headers = "Content-Type: image/png";
                }
                else if (assetsFilePath.EndsWith(".css"))
                {
                    headers = "Content-Type: text/css";
                }
                else if (assetsFilePath.EndsWith(".js"))
                {
                    headers = "Content-Type: application/javascript";
                }
                webResourceRequestedEventArgs.SetResponse(200, "OK", headers, ms);
            }
            catch (Exception)
            {
                webResourceRequestedEventArgs.SetResponse(404, "Not found");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            //https://github.com/dotnet/maui/pull/23019
            Window.TitleBar = new TitleBar()
            {
                Title = "KnowledgeLM",
                //Icon = "appicon.png",
                BackgroundColor = Color.FromArgb("#6264A7"),
                ForegroundColor = Colors.White,
            };
        }
    }
}
