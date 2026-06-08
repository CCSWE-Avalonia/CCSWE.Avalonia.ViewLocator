namespace ViewLocatorSample;

public sealed class MainViewModel : ViewModelBase
{
    public string Greeting => "This view was located by the source-generated ViewLocator and built from DI.";
}
