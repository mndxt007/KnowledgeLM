namespace KnowledgeLM.App
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
            //https://github.com/dotnet/maui/pull/23019
            Window.TitleBar = new TitleBar()
            {
                Title = "KnowledgeLM",
                //Icon = "appicon.png",
                BackgroundColor = Colors.Transparent,
            };
        }
    }
}
