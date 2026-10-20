using UnityEngine;
using Vuplex.WebView;
using UnityEngine.XR;

class OculusWebViewDemo2 : MonoBehaviour {
    private enum Tasks
    {
        Scroll,
        Docking,
        Google,
        Sketching,
        NationalParkOverview,
        ListOfPark,
        NationalParkService
    }

    WebViewPrefab _webViewPrefab2;
    Keyboard _keyboard2;

    [SerializeField]
    private Tasks task2;

    async void Start() {

        // Optional desktop User-Agent override (disabled below).
        // https://developer.vuplex.com/webview/Web#SetUserAgent
        Web.SetUserAgent(false);

        // Create a 0.6 x 0.4 instance of the prefab.
        // https://developer.vuplex.com/webview/WebViewPrefab#Instantiate
        _webViewPrefab2 = WebViewPrefab.Instantiate(0.6f, 0.4f);
        _webViewPrefab2.transform.SetParent(transform, false);
        _webViewPrefab2.transform.localPosition = new Vector3(0, 0.2f, 0.6f);
        _webViewPrefab2.transform.localEulerAngles = new Vector3(0, 180, 0);

        // Optional on-screen keyboard example (disabled below).
        // https://developer.vuplex.com/webview/Keyboard
        _keyboard2 = Keyboard.Instantiate();
        _keyboard2.transform.SetParent(_webViewPrefab2.transform, false);
        _keyboard2.transform.localPosition = new Vector3(0, -0.41f, 0);
        _keyboard2.transform.localEulerAngles = Vector3.zero;

        // Wait for the prefab to initialize because its WebView property is null until then.
        // https://developer.vuplex.com/webview/WebViewPrefab#WaitUntilInitialized
        await _webViewPrefab2.WaitUntilInitialized();

        // After the prefab has initialized, you can use the IWebView APIs via its WebView property.
        // https://developer.vuplex.com/webview/IWebView
        
        

        if (task2 == Tasks.Scroll)
            _webViewPrefab2.WebView.LoadUrl("http://localhost:5000/scrollingtask?participant_id=P01&target_position=-0.5&threshold=0.8&target_thickness=100&scroll_limit=5000&success_time_threshold=500&drag_enabled=true&scroll_enabled=true&inverted_scroll=false");
        else if (task2 == Tasks.Docking)
            _webViewPrefab2.WebView.LoadUrl("http://127.0.0.1:5001/dockingtask?participant_id=P01&source_position_x=0.2&source_position_y=0.2&source_size=100&source_orientation=0&target_position_x=0.8&target_position_y=0.7&target_size=250&target_orientation=90&distance_threshold=20&rotation_threshold=10&scale_threshold=10&dwell_time_threshold=500");
        else if (task2 == Tasks.Google)
        {
            _webViewPrefab2.WebView.LoadUrl("http://google.com/");
        }
        else if (task2 == Tasks.Sketching)
        {
            _webViewPrefab2.WebView.LoadUrl("http://localhost:5002/");
        }
        else if (task2 == Tasks.NationalParkOverview)
        {
            _webViewPrefab2.WebView.LoadUrl("https://en.wikipedia.org/wiki/National_park");
        }
        else if (task2 == Tasks.ListOfPark)
        {
            _webViewPrefab2.WebView.LoadUrl("https://en.wikipedia.org/wiki/List_of_national_parks");
        }
        else if (task2 == Tasks.NationalParkService)
        {
            _webViewPrefab2.WebView.LoadUrl("https://en.wikipedia.org/wiki/National_Park_Service");
        }
    }

    



}
