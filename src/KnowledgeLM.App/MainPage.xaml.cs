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
            if (webResourceRequestedEventArgs.Uri.AbsolutePath.StartsWith("/LearnLoomResource"))
            {
                var query = System.Web.HttpUtility.ParseQueryString(webResourceRequestedEventArgs.Uri.Query);
                var fullPath = query["fullpath"];

                if (string.IsNullOrEmpty(fullPath))
                {
                    webResourceRequestedEventArgs.SetResponse(400, "Bad Request");
                    return;
                }

                ServeLocalFile(webResourceRequestedEventArgs, fullPath);
                return;
            }

            if (!webResourceRequestedEventArgs.Uri.AbsolutePath.StartsWith("/Notes"))
                return;

            var assetsFilePath = Path.Combine("Q:\\Dev",webResourceRequestedEventArgs.Uri.LocalPath.TrimStart('/'));
            ServeLocalFile(webResourceRequestedEventArgs, assetsFilePath);
        }

        private static void ServeLocalFile(WebViewWebResourceRequestedEventArgs eventArgs, string filePath)
        {
            try
            {
                FileStream fs = File.OpenRead(filePath);
                ManagedStream ms = new ManagedStream(fs);
                string headers = GetContentTypeHeader(filePath);
                eventArgs.SetResponse(200, "OK", headers, ms);
            }
            catch (Exception)
            {
                eventArgs.SetResponse(404, "Not found");
            }
        }

        private static string GetContentTypeHeader(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".html" => "Content-Type: text/html",
                ".jpg" or ".jpeg" => "Content-Type: image/jpeg",
                ".png" => "Content-Type: image/png",
                ".gif" => "Content-Type: image/gif",
                ".svg" => "Content-Type: image/svg+xml",
                ".webp" => "Content-Type: image/webp",
                ".bmp" => "Content-Type: image/bmp",
                ".ico" => "Content-Type: image/x-icon",
                ".css" => "Content-Type: text/css",
                ".js" => "Content-Type: application/javascript",
                ".pdf" => "Content-Type: application/pdf",
                _ => ""
            };
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
